using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Stopka
{
    public enum GameState { Start, Playing, GameOver, Transition }

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
        [SerializeField] private SkyboxController skyboxController;
        private Material blockBaseMaterial;
        private ScoreManager scoreManager;
        private Block currentBlock;
        private GameState state;
        private readonly List<Block> placedBlocks = new List<Block>();

        // The foundation block (no slicing on first placement)
        private GameObject foundationBlock;
        private bool hasTriggeredNewRecord;

        private void Start()
        {
            Application.targetFrameRate = 60;
            blockBaseMaterial = Resources.Load<Material>("BlockBase");
            scoreManager = new ScoreManager();
            scoreManager.HighScore = PlayerPrefs.GetInt("HighScore", 0);
            colorManager.Initialize();

            // Create foundation block visible behind Start panel
            tower.Initialize();
            CreateFoundation();

            if (skyboxController != null)
                skyboxController.UpdateFromBlockColor(colorManager.CurrentBlockColor);
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
            // Block input during splash screen
            if (gameUI != null && gameUI.IsSplashActive)
                return false;

            // Block input when settings overlay is open
            if (gameUI != null && gameUI.IsSettingsOpen)
                return false;

            // Block input during score count-up (first tap skips, second restarts)
            if (gameUI != null && gameUI.IsCountingUp)
            {
                bool tapped = RawTapInput();
                if (tapped) gameUI.SkipCountUp();
                return false;
            }

            // Ignore taps on UI elements (gear icon, etc.)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;

            return RawTapInput();
        }

        private bool RawTapInput()
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
            // Foundation already exists from initial Start() or TransitionCoroutine
            spawner.ResetLayer();
            scoreManager.Reset();
            hasTriggeredNewRecord = false;

            // Spawn first moving block on top of existing foundation
            SpawnNextBlock();
            if (gameUI != null)
                gameUI.SetScoreTargetHeight(config.blockHeight);
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
            if (skyboxController != null)
                skyboxController.UpdateFromBlockColor(colorManager.CurrentBlockColor);

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
            {
                gameUI.UpdateScore(scoreManager);
                gameUI.SetScoreTargetHeight(currentBlock.transform.position.y);
            }

            // New record detection moved to GameOver screen only

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
            if (skyboxController != null)
                skyboxController.UpdateFromBlockColor(colorManager.CurrentBlockColor);
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
            int previousHighScore = scoreManager.HighScore;
            bool isNewHighScore = scoreManager.TryUpdateHighScore();
            if (isNewHighScore)
            {
                PlayerPrefs.SetInt("HighScore", scoreManager.HighScore);
                PlayerPrefs.Save();
            }
            if (audioManager != null) audioManager.PlayGameOver();
            if (gameUI != null) gameUI.SetNewHighScore(isNewHighScore, previousHighScore);
            cameraController.ShowFullTower(tower.TopPosition.y);
            SetState(GameState.GameOver);
        }

        private void RestartGame()
        {
            SetState(GameState.Transition);
            StartCoroutine(TransitionCoroutine());
        }

        private IEnumerator TransitionCoroutine()
        {
            // 1. Game Over panel fades out (handled by SetState Transition)
            yield return new WaitForSeconds(0.3f);

            // 2. Camera smooth reset (runs in parallel with tower exit)
            cameraController.SmoothResetToOrigin(1.5f);

            // 3. Exit old tower
            if (config.towerExitMode == TowerExitMode.Sink)
            {
                yield return SinkTower();
            }
            else
            {
                CollapseTower();
                yield return new WaitForSeconds(2f);
            }

            // 4. Cleanup remaining objects
            CleanupAllBlocks();

            // 5. Re-init tower state
            tower.Initialize();
            spawner.ResetLayer();

            // 6. Drop new foundation from above
            yield return DropNewFoundation();

            // 7. Show Start panel
            SetState(GameState.Start);
        }

        private IEnumerator SinkTower()
        {
            var container = new GameObject("SinkContainer");

            // Parent all blocks to container
            if (foundationBlock != null)
                foundationBlock.transform.SetParent(container.transform);
            foreach (var block in placedBlocks)
            {
                if (block != null)
                    block.transform.SetParent(container.transform);
            }
            if (currentBlock != null)
                currentBlock.transform.SetParent(container.transform);

            // Lerp container down
            float startY = container.transform.position.y;
            float targetY = startY - 30f;
            float duration = 1.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t * t; // ease-in
                container.transform.position = new Vector3(0, Mathf.Lerp(startY, targetY, t), 0);
                yield return null;
            }

            Destroy(container);
        }

        private void CollapseTower()
        {
            var allBlocks = new List<GameObject>();
            if (foundationBlock != null) allBlocks.Add(foundationBlock);
            foreach (var block in placedBlocks)
            {
                if (block != null) allBlocks.Add(block.gameObject);
            }
            if (currentBlock != null) allBlocks.Add(currentBlock.gameObject);

            foreach (var go in allBlocks)
            {
                // Remove colliders to prevent block-on-block jitter
                foreach (var col in go.GetComponents<Collider>())
                    Destroy(col);

                var rb = go.AddComponent<Rigidbody>();
                rb.useGravity = true;
                // Random small force for scatter
                rb.AddForce(new Vector3(
                    Random.Range(-2f, 2f), Random.Range(1f, 3f), Random.Range(-2f, 2f)),
                    ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 3f, ForceMode.Impulse);

                go.AddComponent<DestroyWhenFallen>();
                Destroy(go, 3f); // safety net
            }
        }

        private void CleanupAllBlocks()
        {
            // Destroy any remaining blocks
            foreach (var block in placedBlocks)
            {
                if (block != null)
                    Destroy(block.gameObject);
            }
            placedBlocks.Clear();

            if (currentBlock != null)
            {
                Destroy(currentBlock.gameObject);
                currentBlock = null;
            }

            if (foundationBlock != null)
            {
                Destroy(foundationBlock);
                foundationBlock = null;
            }

            // Destroy any remaining cutoff/fallen pieces
            foreach (var piece in FindObjectsByType<DestroyWhenFallen>(FindObjectsSortMode.None))
                Destroy(piece.gameObject);
        }

        private IEnumerator DropNewFoundation()
        {
            CreateFoundation();

            // Start above screen, lerp down to origin
            float startY = 15f;
            float targetY = 0f;
            float duration = 0.8f;
            float elapsed = 0f;

            foundationBlock.transform.position = new Vector3(0, startY, 0);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease out with slight overshoot
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                if (t > 0.7f)
                {
                    // Small bounce at the end
                    float bounceT = (t - 0.7f) / 0.3f;
                    float bounce = Mathf.Sin(bounceT * Mathf.PI) * 0.15f;
                    eased += bounce * (1f - bounceT);
                }
                float y = Mathf.Lerp(startY, targetY, eased);
                foundationBlock.transform.position = new Vector3(0, y, 0);
                yield return null;
            }

            foundationBlock.transform.position = Vector3.zero;
        }
    }
}
