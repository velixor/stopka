using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
        [SerializeField] private TowerWaveController towerWave;
        private Material blockBaseMaterial;
        private ScoreManager scoreManager;
        private Block currentBlock;
        private GameState state;
        private readonly List<Block> placedBlocks = new List<Block>();

        // The foundation block (no slicing on first placement)
        private GameObject foundationBlock;

        private void Start()
        {
            Application.targetFrameRate = 60;
            blockBaseMaterial = Resources.Load<Material>("BlockBase");
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
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
                return true;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
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
            foundationBlock.transform.position = Vector3.zero;
            foundationBlock.transform.localScale = new Vector3(
                config.startBlockSize.x, config.blockHeight, config.startBlockSize.y);

            var renderer = foundationBlock.GetComponent<Renderer>();
            SetBlockMaterial(renderer, colorManager.GetColorForLayer(0));

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

            // Track the final size/position for tower state (may differ if recovery animates)
            Vector2 finalSize;
            Vector3 finalPosition;

            if (result.IsPerfect)
            {
                scoreManager.AddPlacement(isPerfect: true);

                // Compute snap position (align to previous block center on slide axis)
                Vector3 snapPos = currentBlock.transform.position;
                if (axis == SlideAxis.X)
                    snapPos.x = result.NewCenter;
                else
                    snapPos.z = result.NewCenter;

                // Start with current size (perfect = no cut)
                Vector2 targetSize = currentBlock.Size;
                Vector3 targetPos = snapPos;

                // Combo recovery: after streak threshold, random axis, clamped to foundation bounds
                if (scoreManager.ShouldRecover(config.comboRecoveryThreshold))
                {
                    bool recoverX = Random.value < 0.5f;

                    if (recoverX)
                    {
                        targetSize.x = ScoreManager.CalculateRecoveredSize(
                            currentBlock.Size.x, config.startBlockSize.x,
                            scoreManager.ComboCount, config.comboRecoveryThreshold,
                            config.comboRecoveryRate, config.recoveryRandomMin, config.recoveryRandomMax);
                    }
                    else
                    {
                        targetSize.y = ScoreManager.CalculateRecoveredSize(
                            currentBlock.Size.y, config.startBlockSize.y,
                            scoreManager.ComboCount, config.comboRecoveryThreshold,
                            config.comboRecoveryRate, config.recoveryRandomMin, config.recoveryRandomMax);
                    }

                    targetPos = ClampToFoundation(snapPos, targetSize);
                }

                // Single animation: snap to alignment + optional recovery
                currentBlock.AnimateSize(targetSize, targetPos);
                finalSize = targetSize;
                finalPosition = targetPos;
                if (gameUI != null)
                    gameUI.ShowCombo(scoreManager.ComboCount);
                if (audioManager != null) audioManager.PlayPlace(scoreManager.ComboCount);
                if (cameraShake != null) cameraShake.Shake(0.05f + scoreManager.ComboCount * 0.02f);

                // Pulse glow on the placed block
                var pulse = currentBlock.gameObject.AddComponent<BlockPulse>();
                pulse.Pulse(colorManager.GetColorForLayer(spawner.CurrentLayer - 1), config.pulseIntensity, config.pulseDuration);

                // Tower glow wave on 3+ combo
                if (scoreManager.ComboCount >= config.comboRecoveryThreshold && towerWave != null)
                    towerWave.TriggerWave(placedBlocks, foundationBlock, colorManager, spawner.CurrentLayer);
            }
            else
            {
                // Slice the block
                currentBlock.ApplySlice(result.NewCenter, result.NewSize);
                SpawnCutoffPiece(currentBlock, result);
                scoreManager.AddPlacement(isPerfect: false);
                finalSize = currentBlock.Size;
                finalPosition = currentBlock.transform.position;
                if (audioManager != null) audioManager.PlaySlice();
            }

            // Update tower state with final size/position (includes recovery target)
            tower.PlaceBlock(finalPosition, finalSize);
            cameraController.SetTargetHeight(currentBlock.transform.position.y);

            // Update UI
            if (gameUI != null)
                gameUI.UpdateScore(scoreManager);

            // Track placed block and spawn next
            placedBlocks.Add(currentBlock);
            currentBlock.gameObject.AddComponent<BoxCollider>();
            SpawnNextBlock();
        }

        private void SpawnNextBlock()
        {
            Vector2 size = tower.TopSize;
            currentBlock = spawner.SpawnBlock(tower.TopPosition, size);

            // Apply color and disable shadows
            int layer = spawner.CurrentLayer;
            var renderer = currentBlock.GetComponent<Renderer>();
            SetBlockMaterial(renderer, colorManager.GetColorForLayer(layer));
        }

        private void SpawnCutoffPiece(Block block, SliceResult result)
        {
            GameObject cutoff = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cutoff.name = "Cutoff";

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
            SetBlockMaterial(cutoffRenderer, blockRenderer.material.GetColor("_BaseColor"));

            // Slide outward direction
            Vector3 slideDir = block.Axis == SlideAxis.X
                ? new Vector3(Mathf.Sign(result.CutCenter - result.NewCenter), 0f, 0f)
                : new Vector3(0f, 0f, Mathf.Sign(result.CutCenter - result.NewCenter));

            // Use Rigidbody with frozen rotation for natural-looking fall
            var rb = cutoff.AddComponent<Rigidbody>();
            rb.freezeRotation = true;
            rb.linearVelocity = slideDir * 2f;

            // Only destroy when it falls off-screen, not on a timer
            cutoff.AddComponent<DestroyWhenFallen>();
        }

        /// <summary>Clamp block position so it stays within foundation bounds after recovery.</summary>
        private Vector3 ClampToFoundation(Vector3 pos, Vector2 blockSize)
        {
            float foundHalfX = config.startBlockSize.x / 2f;
            float foundHalfZ = config.startBlockSize.y / 2f;
            float blockHalfX = blockSize.x / 2f;
            float blockHalfZ = blockSize.y / 2f;

            // Shift center so block doesn't exceed foundation edges
            pos.x = Mathf.Clamp(pos.x, -foundHalfX + blockHalfX, foundHalfX - blockHalfX);
            pos.z = Mathf.Clamp(pos.z, -foundHalfZ + blockHalfZ, foundHalfZ - blockHalfZ);

            return pos;
        }

        private void SetBlockMaterial(Renderer renderer, Color color)
        {
            var mat = new Material(blockBaseMaterial);
            mat.SetColor("_BaseColor", color);
            renderer.material = mat;
            // Lit shader for face shading, but no shadows between blocks
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
            go.AddComponent<DestroyWhenFallen>();
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
            cameraController.ShowFullTower(tower.TopPosition.y);
            SetState(GameState.GameOver);
        }

        private void RestartGame()
        {
            cameraController.StopPullback();

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

            // Destroy any remaining cutoff/fallen pieces
            foreach (var piece in FindObjectsByType<DestroyWhenFallen>(FindObjectsSortMode.None))
                Destroy(piece.gameObject);

            if (foundationBlock != null)
                Destroy(foundationBlock);

            StartGame();
        }
    }
}
