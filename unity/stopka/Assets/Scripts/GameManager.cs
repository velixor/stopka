using System.Collections.Generic;
using UnityEngine;

namespace Stopka
{
    public enum GameState { Start, Playing, GameOver }

    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private BlockSpawner spawner;
        [SerializeField] private TowerManager tower;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private BlockColorManager colorManager;
        [SerializeField] private GameUI gameUI;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private CameraShake cameraShake;

        private ScoreManager scoreManager;
        private Block currentBlock;
        private GameState state;
        private readonly List<Block> placedBlocks = new List<Block>();

        // The foundation block (no slicing on first placement)
        private GameObject foundationBlock;

        private void Start()
        {
            scoreManager = new ScoreManager();
            scoreManager.HighScore = PlayerPrefs.GetInt("HighScore", 0);
            colorManager.Initialize();
            SetState(GameState.Start);
        }

        private void Update()
        {
            if (!HasTapInput()) return;

            switch (state)
            {
                case GameState.Start:
                    StartGame();
                    break;
                case GameState.Playing:
                    PlaceBlock();
                    break;
                case GameState.GameOver:
                    RestartGame();
                    break;
            }
        }

        private bool HasTapInput()
        {
            // Touch or mouse click
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                return true;
            if (Input.GetMouseButtonDown(0))
                return true;
            return false;
        }

        private void SetState(GameState newState)
        {
            state = newState;
            if (gameUI != null)
                gameUI.SetState(newState, scoreManager);
        }

        private void StartGame()
        {
            tower.Initialize();
            spawner.ResetLayer();
            scoreManager.Reset();
            cameraController.ResetToOrigin();

            // Create foundation block (static, sits at origin)
            CreateFoundation();

            // Spawn first moving block
            SpawnNextBlock();
            SetState(GameState.Playing);
            if (audioManager != null) audioManager.StartMusic();
        }

        private void CreateFoundation()
        {
            if (foundationBlock != null)
                Destroy(foundationBlock);

            foundationBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foundationBlock.name = "Foundation";
            Destroy(foundationBlock.GetComponent<Collider>());
            foundationBlock.transform.position = Vector3.zero;
            foundationBlock.transform.localScale = new Vector3(
                config.startBlockSize.x, config.blockHeight, config.startBlockSize.y);

            var renderer = foundationBlock.GetComponent<Renderer>();
            SetupBlockRenderer(renderer);
            SetMaterialColor(renderer, colorManager.GetColorForLayer(0));

            tower.PlaceBlock(Vector3.zero, config.startBlockSize);
        }

        private void PlaceBlock()
        {
            if (currentBlock == null) return;

            float stoppedPosition = currentBlock.Stop();
            SlideAxis axis = currentBlock.Axis;

            float prevCenter = tower.GetTopCenter(axis);
            float prevSize = tower.GetTopSize(axis);
            float currSize = axis == SlideAxis.X ? currentBlock.Size.x : currentBlock.Size.y;

            SliceResult result = BlockSlicer.Slice(
                prevCenter, prevSize,
                stoppedPosition, currSize,
                config.perfectTolerance);

            if (!result.HasOverlap)
            {
                // Miss — game over
                AddRigidbodyAndFall(currentBlock.gameObject);
                currentBlock = null;
                GameOver();
                return;
            }

            if (result.IsPerfect)
            {
                // Snap to alignment
                currentBlock.ApplySlice(result.NewCenter, currSize);
                scoreManager.AddPlacement(isPerfect: true);

                // Combo recovery: widen the block
                Vector2 recoveredSize = new Vector2(
                    ScoreManager.CalculateRecoveredSize(
                        currentBlock.Size.x, config.startBlockSize.x,
                        scoreManager.ComboCount, config.comboRecoveryRate),
                    ScoreManager.CalculateRecoveredSize(
                        currentBlock.Size.y, config.startBlockSize.y,
                        scoreManager.ComboCount, config.comboRecoveryRate));
                currentBlock.SetSize(recoveredSize);
                if (gameUI != null)
                    gameUI.ShowCombo(scoreManager.ComboCount);
                if (audioManager != null) audioManager.PlayPlace(scoreManager.ComboCount);
                if (cameraShake != null) cameraShake.Shake(0.05f + scoreManager.ComboCount * 0.02f);
            }
            else
            {
                // Slice the block
                currentBlock.ApplySlice(result.NewCenter, result.NewSize);
                SpawnCutoffPiece(currentBlock, result);
                scoreManager.AddPlacement(isPerfect: false);
                if (audioManager != null) audioManager.PlaySlice();
            }

            // Update tower state
            tower.PlaceBlock(currentBlock.transform.position, currentBlock.Size);
            cameraController.SetTargetHeight(currentBlock.transform.position.y);

            // Update UI
            if (gameUI != null)
                gameUI.UpdateScore(scoreManager);

            // Track placed block and spawn next
            placedBlocks.Add(currentBlock);
            SpawnNextBlock();
        }

        private void SpawnNextBlock()
        {
            Vector2 size = tower.TopSize;
            currentBlock = spawner.SpawnBlock(tower.TopPosition, size);

            // Apply color and disable shadows
            int layer = spawner.CurrentLayer;
            var renderer = currentBlock.GetComponent<Renderer>();
            SetupBlockRenderer(renderer);
            SetMaterialColor(renderer, colorManager.GetColorForLayer(layer));
        }

        private void SpawnCutoffPiece(Block block, SliceResult result)
        {
            GameObject cutoff = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cutoff.name = "Cutoff";
            Destroy(cutoff.GetComponent<Collider>());

            Vector3 pos = block.transform.position;
            Vector3 scale = block.transform.localScale;

            if (block.Axis == SlideAxis.X)
            {
                pos.x = result.CutCenter;
                scale.x = result.CutSize;
            }
            else
            {
                pos.z = result.CutCenter;
                scale.z = result.CutSize;
            }

            cutoff.transform.position = pos;
            cutoff.transform.localScale = scale;

            // Copy color from the block
            var blockRenderer = block.GetComponent<Renderer>();
            var cutoffRenderer = cutoff.GetComponent<Renderer>();
            SetupBlockRenderer(cutoffRenderer);
            SetMaterialColor(cutoffRenderer, blockRenderer.material.GetColor("_BaseColor"));

            // Slide outward in the overhang direction (no physics, no spinning)
            Vector3 slideDir = block.Axis == SlideAxis.X
                ? new Vector3(Mathf.Sign(result.CutCenter - result.NewCenter), 0f, 0f)
                : new Vector3(0f, 0f, Mathf.Sign(result.CutCenter - result.NewCenter));

            var piece = cutoff.AddComponent<CutoffPiece>();
            piece.Initialize(slideDir, 1.5f);
        }

        private static void SetMaterialColor(Renderer renderer, Color color)
        {
            renderer.material.SetColor("_BaseColor", color);
        }

        private static void SetupBlockRenderer(Renderer renderer)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void AddRigidbodyAndFall(GameObject go)
        {
            // Ensure it has a collider for physics (re-add if removed)
            if (go.GetComponent<Collider>() == null)
                go.AddComponent<BoxCollider>();

            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = true;
        }

        private void GameOver()
        {
            bool isNewHighScore = scoreManager.TryUpdateHighScore();
            if (isNewHighScore)
            {
                PlayerPrefs.SetInt("HighScore", scoreManager.HighScore);
                PlayerPrefs.Save();
            }
            if (audioManager != null) audioManager.PlayGameOver();
            if (gameUI != null) gameUI.SetNewHighScore(isNewHighScore);
            SetState(GameState.GameOver);
        }

        private void RestartGame()
        {
            // Destroy all placed blocks
            foreach (var block in placedBlocks)
            {
                if (block != null)
                    Destroy(block.gameObject);
            }
            placedBlocks.Clear();

            // Destroy current sliding block if still alive
            if (currentBlock != null)
                Destroy(currentBlock.gameObject);

            if (foundationBlock != null)
                Destroy(foundationBlock);

            StartGame();
        }
    }
}
