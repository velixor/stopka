# Stopka Core Game Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the complete Stopka block-stacking game — from first block slide to game-over screen — as a playable iOS game in Unity.

**Architecture:** Pure game logic (slice math, scoring, combo) lives in plain C# classes for easy unit testing. MonoBehaviours are thin wrappers for Unity lifecycle (Block, BlockSpawner, CameraController). GameManager orchestrates state transitions (Start → Playing → GameOver). All tuning parameters are centralized in a GameConfig ScriptableObject.

**Tech Stack:** Unity 6 (6000.3.10f1), C#, Universal Render Pipeline, Unity Test Framework (NUnit), iOS target.

**Spec:** See [docs/gameplay.md](../../gameplay.md) for full gameplay design.

---

## File Structure

```
Assets/
  Scripts/
    Stopka.asmdef                 — Main assembly definition
    GameConfig.cs                 — ScriptableObject: all tuning parameters
    GameManager.cs                — Game state machine (Start/Playing/GameOver), orchestrator
    Block.cs                      — MonoBehaviour: sliding movement, stop on tap
    BlockSpawner.cs               — Spawns blocks, alternates axis, manages speed ramp
    BlockSlicer.cs                — Pure static class: overlap/slice math
    SliceResult.cs                — Data struct for slice computation output
    TowerManager.cs               — Tracks tower height, top block bounds
    CameraController.cs           — Follows tower upward with damping
    ScoreManager.cs               — Plain C# class: score, combo, high score
    GameUI.cs                     — Manages Start/Playing/GameOver UI panels
    BlockColorManager.cs          — Assigns colors per layer (hue rotation)
    AudioManager.cs               — Handles all game audio (place, slice, combo, music)
    CameraShake.cs                — Screen shake effect for combo feedback
  Tests/
    EditMode/
      Stopka.Tests.EditMode.asmdef — Test assembly definition
      BlockSlicerTests.cs          — Unit tests for slice math
      ScoreManagerTests.cs         — Unit tests for score/combo logic
  Scenes/
    GameScene.unity                — Main game scene
  Resources/
    GameConfig.asset               — Default configuration asset
```

---

## Chunk 1: Foundation — Config, Slice Math & Score Logic

### Task 1: Assembly Definitions

**Files:**
- Create: `Assets/Scripts/Stopka.asmdef`
- Create: `Assets/Tests/EditMode/Stopka.Tests.EditMode.asmdef`

- [ ] **Step 1: Create the main scripts assembly definition**

Create `Assets/Scripts/Stopka.asmdef`:
```json
{
    "name": "Stopka",
    "rootNamespace": "Stopka",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Create the edit-mode test assembly definition**

Create `Assets/Tests/EditMode/Stopka.Tests.EditMode.asmdef`:
```json
{
    "name": "Stopka.Tests.EditMode",
    "rootNamespace": "Stopka.Tests",
    "references": [
        "Stopka"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Stopka.asmdef Assets/Tests/EditMode/Stopka.Tests.EditMode.asmdef
git commit -m "chore: add assembly definitions for game scripts and tests"
```

---

### Task 2: GameConfig ScriptableObject

**Files:**
- Create: `Assets/Scripts/GameConfig.cs`

- [ ] **Step 1: Create GameConfig**

```csharp
using UnityEngine;

namespace Stopka
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Stopka/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Speed")]
        public float startSpeed = 3f;
        public float speedIncrement = 0.05f;
        public float maxSpeed = 8f;

        [Header("Placement")]
        public float perfectTolerance = 0.1f;

        [Header("Combo")]
        public float comboRecoveryRate = 0.05f;

        [Header("Block")]
        public float blockHeight = 0.2f;
        public Vector2 startBlockSize = new Vector2(3f, 3f);

        [Header("Spawning")]
        [Tooltip("How far off-screen the block starts sliding from")]
        public float slideRange = 5f;

        public float GetSpeedForLayer(int layer)
        {
            return Mathf.Min(startSpeed + speedIncrement * layer, maxSpeed);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/GameConfig.cs
git commit -m "feat: add GameConfig ScriptableObject with tuning parameters"
```

---

### Task 3: SliceResult Data Struct

**Files:**
- Create: `Assets/Scripts/SliceResult.cs`

- [ ] **Step 1: Create SliceResult**

```csharp
namespace Stopka
{
    public struct SliceResult
    {
        /// <summary>True if the blocks overlap at all.</summary>
        public bool HasOverlap;

        /// <summary>True if placement is within perfect tolerance.</summary>
        public bool IsPerfect;

        // --- Remaining block (the overlap region) ---
        /// <summary>Center position of the remaining block along the slide axis.</summary>
        public float NewCenter;
        /// <summary>Size of the remaining block along the slide axis.</summary>
        public float NewSize;

        // --- Cutoff piece (the overhang that falls away) ---
        /// <summary>Center position of the cut piece along the slide axis.</summary>
        public float CutCenter;
        /// <summary>Size of the cut piece along the slide axis.</summary>
        public float CutSize;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/SliceResult.cs
git commit -m "feat: add SliceResult data struct"
```

---

### Task 4: BlockSlicer — Slice Math (TDD)

**Files:**
- Create: `Assets/Scripts/BlockSlicer.cs`
- Create: `Assets/Tests/EditMode/BlockSlicerTests.cs`

- [ ] **Step 1: Write failing tests for BlockSlicer**

Create `Assets/Tests/EditMode/BlockSlicerTests.cs`:
```csharp
using NUnit.Framework;
using Stopka;

namespace Stopka.Tests
{
    public class BlockSlicerTests
    {
        private const float Tolerance = 0.1f;

        [Test]
        public void PerfectPlacement_ReturnsIsPerfect()
        {
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 3f,
                currCenter: 0.05f, currSize: 3f,
                perfectTolerance: Tolerance);

            Assert.IsTrue(result.HasOverlap);
            Assert.IsTrue(result.IsPerfect);
            Assert.AreEqual(0f, result.NewCenter, 0.001f, "Should snap to prev center");
            Assert.AreEqual(3f, result.NewSize, 0.001f, "Should keep current size");
        }

        [Test]
        public void ExactAlignment_IsPerfect()
        {
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 3f,
                currCenter: 0f, currSize: 3f,
                perfectTolerance: Tolerance);

            Assert.IsTrue(result.IsPerfect);
        }

        [Test]
        public void OverhangPositiveSide_SlicesCorrectly()
        {
            // prev: [-1.5, 1.5], curr: [-0.5, 2.5] → overlap [-0.5, 1.5] size=2, cut [1.5, 2.5] size=1
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 3f,
                currCenter: 1f, currSize: 3f,
                perfectTolerance: Tolerance);

            Assert.IsTrue(result.HasOverlap);
            Assert.IsFalse(result.IsPerfect);
            Assert.AreEqual(0.5f, result.NewCenter, 0.001f);
            Assert.AreEqual(2f, result.NewSize, 0.001f);
            Assert.AreEqual(2f, result.CutCenter, 0.001f);
            Assert.AreEqual(1f, result.CutSize, 0.001f);
        }

        [Test]
        public void OverhangNegativeSide_SlicesCorrectly()
        {
            // prev: [-1.5, 1.5], curr: [-2.5, 0.5] → overlap [-1.5, 0.5] size=2, cut [-2.5, -1.5] size=1
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 3f,
                currCenter: -1f, currSize: 3f,
                perfectTolerance: Tolerance);

            Assert.IsTrue(result.HasOverlap);
            Assert.IsFalse(result.IsPerfect);
            Assert.AreEqual(-0.5f, result.NewCenter, 0.001f);
            Assert.AreEqual(2f, result.NewSize, 0.001f);
            Assert.AreEqual(-2f, result.CutCenter, 0.001f);
            Assert.AreEqual(1f, result.CutSize, 0.001f);
        }

        [Test]
        public void NoOverlap_ReturnsNoOverlap()
        {
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 2f,
                currCenter: 5f, currSize: 2f,
                perfectTolerance: Tolerance);

            Assert.IsFalse(result.HasOverlap);
        }

        [Test]
        public void BarelyOverlapping_NotPerfect()
        {
            // prev: [-1, 1], curr: [0.5, 2.5] → overlap [0.5, 1.0] size=0.5
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 2f,
                currCenter: 1.5f, currSize: 2f,
                perfectTolerance: Tolerance);

            Assert.IsTrue(result.HasOverlap);
            Assert.IsFalse(result.IsPerfect);
            Assert.AreEqual(0.75f, result.NewCenter, 0.001f);
            Assert.AreEqual(0.5f, result.NewSize, 0.001f);
        }

        [Test]
        public void SmallerCurrentBlock_OverhangSliced()
        {
            // prev: [-1.5, 1.5], curr: [0, 2] (size 2, offset 1) → overlap [0, 1.5] size=1.5, cut [1.5, 2] size=0.5
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 3f,
                currCenter: 1f, currSize: 2f,
                perfectTolerance: Tolerance);

            Assert.IsTrue(result.HasOverlap);
            Assert.IsFalse(result.IsPerfect);
            Assert.AreEqual(0.75f, result.NewCenter, 0.001f);
            Assert.AreEqual(1.5f, result.NewSize, 0.001f);
            Assert.AreEqual(1.75f, result.CutCenter, 0.001f);
            Assert.AreEqual(0.5f, result.CutSize, 0.001f);
        }

        [Test]
        public void AtExactTolerance_IsPerfect()
        {
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 3f,
                currCenter: Tolerance, currSize: 3f,
                perfectTolerance: Tolerance);

            Assert.IsTrue(result.IsPerfect);
        }

        [Test]
        public void JustBeyondTolerance_IsNotPerfect()
        {
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 3f,
                currCenter: Tolerance + 0.01f, currSize: 3f,
                perfectTolerance: Tolerance);

            Assert.IsFalse(result.IsPerfect);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run Unity tests in edit mode. Expected: all tests fail with `BlockSlicer` not found.

- [ ] **Step 3: Implement BlockSlicer**

Create `Assets/Scripts/BlockSlicer.cs`:
```csharp
using UnityEngine;

namespace Stopka
{
    public static class BlockSlicer
    {
        public static SliceResult Slice(
            float prevCenter, float prevSize,
            float currCenter, float currSize,
            float perfectTolerance)
        {
            float prevMin = prevCenter - prevSize / 2f;
            float prevMax = prevCenter + prevSize / 2f;
            float currMin = currCenter - currSize / 2f;
            float currMax = currCenter + currSize / 2f;

            float overlapMin = Mathf.Max(prevMin, currMin);
            float overlapMax = Mathf.Min(prevMax, currMax);
            float overlapSize = overlapMax - overlapMin;

            if (overlapSize <= 0f)
            {
                return new SliceResult { HasOverlap = false };
            }

            float offset = Mathf.Abs(currCenter - prevCenter);
            if (offset <= perfectTolerance)
            {
                return new SliceResult
                {
                    HasOverlap = true,
                    IsPerfect = true,
                    NewCenter = prevCenter,
                    NewSize = currSize
                };
            }

            float cutMin, cutMax;
            if (currCenter > prevCenter)
            {
                cutMin = overlapMax;
                cutMax = currMax;
            }
            else
            {
                cutMin = currMin;
                cutMax = overlapMin;
            }

            return new SliceResult
            {
                HasOverlap = true,
                IsPerfect = false,
                NewCenter = (overlapMin + overlapMax) / 2f,
                NewSize = overlapSize,
                CutCenter = (cutMin + cutMax) / 2f,
                CutSize = cutMax - cutMin
            };
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

All BlockSlicerTests should pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/BlockSlicer.cs Assets/Scripts/SliceResult.cs Assets/Tests/EditMode/BlockSlicerTests.cs
git commit -m "feat: add BlockSlicer with overlap/slice math and unit tests"
```

---

### Task 5: ScoreManager (TDD)

**Files:**
- Create: `Assets/Scripts/ScoreManager.cs`
- Create: `Assets/Tests/EditMode/ScoreManagerTests.cs`

- [ ] **Step 1: Write failing tests for ScoreManager**

Create `Assets/Tests/EditMode/ScoreManagerTests.cs`:
```csharp
using NUnit.Framework;
using Stopka;

namespace Stopka.Tests
{
    public class ScoreManagerTests
    {
        private ScoreManager score;

        [SetUp]
        public void SetUp()
        {
            score = new ScoreManager();
        }

        [Test]
        public void InitialState_ZeroScoreAndCombo()
        {
            Assert.AreEqual(0, score.Score);
            Assert.AreEqual(0, score.ComboCount);
        }

        [Test]
        public void NormalPlacement_ScoreIncrements()
        {
            score.AddPlacement(isPerfect: false);
            Assert.AreEqual(1, score.Score);
        }

        [Test]
        public void NormalPlacement_ComboResets()
        {
            score.AddPlacement(isPerfect: true);
            score.AddPlacement(isPerfect: false);
            Assert.AreEqual(0, score.ComboCount);
        }

        [Test]
        public void PerfectPlacement_ComboBuildUp()
        {
            score.AddPlacement(isPerfect: true);
            Assert.AreEqual(1, score.ComboCount);

            score.AddPlacement(isPerfect: true);
            Assert.AreEqual(2, score.ComboCount);

            score.AddPlacement(isPerfect: true);
            Assert.AreEqual(3, score.ComboCount);
        }

        [Test]
        public void PerfectPlacement_BonusPoints()
        {
            // First perfect: 1 (base) + 1 (combo bonus) = 2
            score.AddPlacement(isPerfect: true);
            Assert.AreEqual(2, score.Score);

            // Second perfect: 2 + 1 (base) + 2 (combo bonus) = 5
            score.AddPlacement(isPerfect: true);
            Assert.AreEqual(5, score.Score);
        }

        [Test]
        public void Reset_ClearsScoreAndCombo()
        {
            score.AddPlacement(isPerfect: true);
            score.AddPlacement(isPerfect: true);
            score.Reset();
            Assert.AreEqual(0, score.Score);
            Assert.AreEqual(0, score.ComboCount);
        }

        [Test]
        public void HighScore_UpdatesWhenBeaten()
        {
            score.HighScore = 5;
            score.AddPlacement(isPerfect: false); // score = 1
            Assert.IsFalse(score.TryUpdateHighScore());

            // Get score above 5
            for (int i = 0; i < 5; i++)
                score.AddPlacement(isPerfect: false); // score = 6
            Assert.IsTrue(score.TryUpdateHighScore());
            Assert.AreEqual(6, score.HighScore);
        }

        [Test]
        public void ComboRecovery_WidensBlock()
        {
            float currentSize = 2f;
            float maxSize = 3f;
            float recoveryRate = 0.05f;

            // After combo 3, recovery = 3 * 0.05 = 0.15
            float recovered = ScoreManager.CalculateRecoveredSize(
                currentSize, maxSize, comboCount: 3, recoveryRate);

            Assert.AreEqual(2.15f, recovered, 0.001f);
        }

        [Test]
        public void ComboRecovery_CapsAtMaxSize()
        {
            float currentSize = 2.95f;
            float maxSize = 3f;
            float recoveryRate = 0.05f;

            float recovered = ScoreManager.CalculateRecoveredSize(
                currentSize, maxSize, comboCount: 5, recoveryRate);

            Assert.AreEqual(3f, recovered, 0.001f);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Expected: all tests fail with `ScoreManager` not found.

- [ ] **Step 3: Implement ScoreManager**

Create `Assets/Scripts/ScoreManager.cs`:
```csharp
using UnityEngine;

namespace Stopka
{
    public class ScoreManager
    {
        public int Score { get; private set; }
        public int ComboCount { get; private set; }
        public int HighScore { get; set; }

        public void AddPlacement(bool isPerfect)
        {
            Score++;
            if (isPerfect)
            {
                ComboCount++;
                Score += ComboCount;
            }
            else
            {
                ComboCount = 0;
            }
        }

        public bool TryUpdateHighScore()
        {
            if (Score > HighScore)
            {
                HighScore = Score;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            Score = 0;
            ComboCount = 0;
        }

        public static float CalculateRecoveredSize(
            float currentSize, float maxSize, int comboCount, float recoveryRate)
        {
            return Mathf.Min(currentSize + comboCount * recoveryRate, maxSize);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

All ScoreManagerTests should pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/ScoreManager.cs Assets/Tests/EditMode/ScoreManagerTests.cs
git commit -m "feat: add ScoreManager with score, combo, high score logic and tests"
```

---

## Chunk 2: Block Movement & Spawning

### Task 6: Block MonoBehaviour

**Files:**
- Create: `Assets/Scripts/Block.cs`

The Block component handles sliding movement along one axis. It uses an accumulated timer with `Mathf.PingPong` for smooth back-and-forth motion.

- [ ] **Step 1: Create Block.cs**

```csharp
using UnityEngine;

namespace Stopka
{
    public enum SlideAxis { X, Z }

    public class Block : MonoBehaviour
    {
        private SlideAxis axis;
        private float speed;
        private float slideRange;
        private float basePosition;
        private float timeAccumulator;
        private bool isMoving;

        /// <summary>Current size of this block (X and Z dimensions).</summary>
        public Vector2 Size { get; private set; }

        /// <summary>The axis this block slides along.</summary>
        public SlideAxis Axis => axis;

        public void Initialize(SlideAxis axis, float speed, float slideRange, Vector2 size, float height)
        {
            this.axis = axis;
            this.speed = speed;
            this.slideRange = slideRange;
            this.isMoving = true;
            this.timeAccumulator = 0f;
            Size = size;

            // Set scale from size
            transform.localScale = new Vector3(size.x, height, size.y);

            // Base position is the center of the block below (Y is set by spawner)
            basePosition = axis == SlideAxis.X ? transform.position.x : transform.position.z;
        }

        /// <summary>Set the base position the block oscillates around.</summary>
        public void SetBasePosition(float pos)
        {
            basePosition = pos;
        }

        private void Update()
        {
            if (!isMoving) return;

            timeAccumulator += Time.deltaTime;
            float offset = Mathf.PingPong(timeAccumulator * speed, slideRange * 2f) - slideRange;

            Vector3 pos = transform.position;
            if (axis == SlideAxis.X)
                pos.x = basePosition + offset;
            else
                pos.z = basePosition + offset;

            transform.position = pos;
        }

        /// <summary>Stop sliding. Returns the current position along the slide axis.</summary>
        public float Stop()
        {
            isMoving = false;
            return axis == SlideAxis.X ? transform.position.x : transform.position.z;
        }

        /// <summary>Resize this block after slicing (updates transform scale and position).</summary>
        public void ApplySlice(float newCenter, float newSize)
        {
            Size = axis == SlideAxis.X
                ? new Vector2(newSize, Size.y)
                : new Vector2(Size.x, newSize);

            Vector3 pos = transform.position;
            if (axis == SlideAxis.X)
                pos.x = newCenter;
            else
                pos.z = newCenter;

            transform.position = pos;
            transform.localScale = new Vector3(Size.x, transform.localScale.y, Size.y);
        }

        /// <summary>Update size for combo recovery.</summary>
        public void SetSize(Vector2 newSize)
        {
            Size = newSize;
            transform.localScale = new Vector3(newSize.x, transform.localScale.y, newSize.y);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Block.cs
git commit -m "feat: add Block MonoBehaviour with sliding movement"
```

---

### Task 7: BlockSpawner

**Files:**
- Create: `Assets/Scripts/BlockSpawner.cs`

BlockSpawner creates new block GameObjects each layer. It alternates the slide axis (X → Z → X → ...), sets the speed based on the current layer, and positions the new block at the correct height.

- [ ] **Step 1: Create BlockSpawner.cs**

```csharp
using UnityEngine;

namespace Stopka
{
    public class BlockSpawner : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Material blockMaterial;

        private int currentLayer;

        public Block SpawnBlock(Vector3 basePosition, Vector2 currentSize)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Block_{currentLayer}";

            // Remove default collider — we'll manage physics manually for cutoff pieces
            Destroy(go.GetComponent<Collider>());

            Block block = go.AddComponent<Block>();

            SlideAxis axis = currentLayer % 2 == 0 ? SlideAxis.X : SlideAxis.Z;
            float speed = config.GetSpeedForLayer(currentLayer);
            float height = config.blockHeight;

            // Position: centered on basePosition, at correct height
            float y = basePosition.y + height;
            Vector3 spawnPos = new Vector3(basePosition.x, y, basePosition.z);
            go.transform.position = spawnPos;

            block.Initialize(axis, speed, config.slideRange, currentSize, height);

            // The block oscillates around the base position
            float center = axis == SlideAxis.X ? basePosition.x : basePosition.z;
            block.SetBasePosition(center);

            if (blockMaterial != null)
                go.GetComponent<Renderer>().material = blockMaterial;

            currentLayer++;
            return block;
        }

        public int CurrentLayer => currentLayer;

        public void ResetLayer()
        {
            currentLayer = 0;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/BlockSpawner.cs
git commit -m "feat: add BlockSpawner for creating and initializing blocks"
```

---

### Task 8: TowerManager

**Files:**
- Create: `Assets/Scripts/TowerManager.cs`

TowerManager tracks the top of the tower — the last placed block's position and size. It provides the reference for slicing the next block and determines where to spawn the next one.

- [ ] **Step 1: Create TowerManager.cs**

```csharp
using UnityEngine;

namespace Stopka
{
    public class TowerManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        /// <summary>Position of the top of the tower (center of the top block).</summary>
        public Vector3 TopPosition { get; private set; }

        /// <summary>Current size of the top block (X, Z).</summary>
        public Vector2 TopSize { get; private set; }

        public void Initialize()
        {
            TopPosition = Vector3.zero;
            TopSize = config.startBlockSize;
        }

        /// <summary>Update after a block is placed.</summary>
        public void PlaceBlock(Vector3 position, Vector2 size)
        {
            TopPosition = position;
            TopSize = size;
        }

        /// <summary>Get the center of the top block along a given axis.</summary>
        public float GetTopCenter(SlideAxis axis)
        {
            return axis == SlideAxis.X ? TopPosition.x : TopPosition.z;
        }

        /// <summary>Get the size of the top block along a given axis.</summary>
        public float GetTopSize(SlideAxis axis)
        {
            return axis == SlideAxis.X ? TopSize.x : TopSize.y;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/TowerManager.cs
git commit -m "feat: add TowerManager for tracking tower state"
```

---

## Chunk 3: Camera, Color & Game Manager

### Task 9: CameraController

**Files:**
- Create: `Assets/Scripts/CameraController.cs`

Perspective camera at ~45 deg azimuth, ~30 deg elevation. Follows the tower upward with smooth damping.

- [ ] **Step 1: Create CameraController.cs**

```csharp
using UnityEngine;

namespace Stopka
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float followSpeed = 3f;
        [SerializeField] private Vector3 offset = new Vector3(5f, 5f, 5f);

        private float targetY;

        private void Start()
        {
            targetY = 0f;
            transform.position = offset;
            transform.LookAt(Vector3.zero);
        }

        public void SetTargetHeight(float height)
        {
            targetY = height;
        }

        private void LateUpdate()
        {
            Vector3 target = new Vector3(offset.x, targetY + offset.y, offset.z);
            transform.position = Vector3.Lerp(transform.position, target, followSpeed * Time.deltaTime);
            transform.LookAt(new Vector3(0f, targetY, 0f));
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/CameraController.cs
git commit -m "feat: add CameraController with smooth height follow"
```

---

### Task 10: BlockColorManager

**Files:**
- Create: `Assets/Scripts/BlockColorManager.cs`

Assigns colors to blocks using hue rotation. Each layer shifts the hue slightly for a smooth gradient effect.

- [ ] **Step 1: Create BlockColorManager.cs**

```csharp
using UnityEngine;

namespace Stopka
{
    public class BlockColorManager : MonoBehaviour
    {
        [SerializeField] private float hueStep = 0.03f;
        [SerializeField] private float saturation = 0.6f;
        [SerializeField] private float value = 0.9f;

        private float currentHue;

        public void Initialize()
        {
            currentHue = Random.Range(0f, 1f);
        }

        public Color GetColorForLayer(int layer)
        {
            float hue = (currentHue + hueStep * layer) % 1f;
            return Color.HSVToRGB(hue, saturation, value);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/BlockColorManager.cs
git commit -m "feat: add BlockColorManager with hue rotation"
```

---

### Task 11: GameManager — Full Game Loop

**Files:**
- Create: `Assets/Scripts/GameManager.cs`

GameManager is the orchestrator. It manages the game state machine (Start → Playing → GameOver), handles tap input, triggers block placement, slicing, scoring, camera updates, and game-over logic.

- [ ] **Step 1: Create GameManager.cs**

```csharp
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
        private readonly List<GameObject> cutoffPieces = new List<GameObject>();

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

            // Create foundation block (static, sits at origin)
            CreateFoundation();

            // Spawn first moving block
            SpawnNextBlock();
            SetState(GameState.Playing);
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
            renderer.material.color = colorManager.GetColorForLayer(0);

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
            }
            else
            {
                // Slice the block
                currentBlock.ApplySlice(result.NewCenter, result.NewSize);
                SpawnCutoffPiece(currentBlock, result);
                scoreManager.AddPlacement(isPerfect: false);
            }

            // Update tower state
            tower.PlaceBlock(currentBlock.transform.position, currentBlock.Size);
            cameraController.SetTargetHeight(currentBlock.transform.position.y);

            // Update UI
            if (gameUI != null)
                gameUI.UpdateScore(scoreManager);

            // Spawn next
            SpawnNextBlock();
        }

        private void SpawnNextBlock()
        {
            Vector2 size = tower.TopSize;
            currentBlock = spawner.SpawnBlock(tower.TopPosition, size);

            // Apply color
            int layer = spawner.CurrentLayer;
            var renderer = currentBlock.GetComponent<Renderer>();
            renderer.material.color = colorManager.GetColorForLayer(layer);
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
            cutoffRenderer.material.color = blockRenderer.material.color;

            AddRigidbodyAndFall(cutoff);
            cutoffPieces.Add(cutoff);
            Destroy(cutoff, 3f); // Clean up after falling
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
            SetState(GameState.GameOver);
        }

        private void RestartGame()
        {
            // Destroy all blocks
            foreach (var block in FindObjectsByType<Block>(FindObjectsSortMode.None))
                Destroy(block.gameObject);

            // Destroy tracked cutoff pieces
            foreach (var cutoff in cutoffPieces)
            {
                if (cutoff != null)
                    Destroy(cutoff);
            }
            cutoffPieces.Clear();

            if (foundationBlock != null)
                Destroy(foundationBlock);

            StartGame();
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/GameManager.cs
git commit -m "feat: add GameManager with full game loop orchestration"
```

---

## Chunk 4: UI & Scene Setup

### Task 12: GameUI

**Files:**
- Create: `Assets/Scripts/GameUI.cs`

Manages three UI panels: Start screen ("Tap to Start" + high score), Playing HUD (score + combo), and Game Over screen (final score, high score, "Tap to Restart").

- [ ] **Step 1: Create GameUI.cs**

```csharp
using UnityEngine;
using TMPro;

namespace Stopka
{
    public class GameUI : MonoBehaviour
    {
        [Header("Start Screen")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private TextMeshProUGUI highScoreStartText;

        [Header("Playing HUD")]
        [SerializeField] private GameObject playingPanel;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI comboText;

        [Header("Game Over Screen")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI highScoreEndText;
        [SerializeField] private GameObject newHighScoreBadge;

        public void SetState(GameState state, ScoreManager score)
        {
            startPanel.SetActive(state == GameState.Start);
            playingPanel.SetActive(state == GameState.Playing);
            gameOverPanel.SetActive(state == GameState.GameOver);

            switch (state)
            {
                case GameState.Start:
                    highScoreStartText.text = $"Best: {score.HighScore}";
                    break;
                case GameState.Playing:
                    UpdateScore(score);
                    break;
                case GameState.GameOver:
                    finalScoreText.text = $"{score.Score}";
                    highScoreEndText.text = $"Best: {score.HighScore}";
                    newHighScoreBadge.SetActive(score.Score >= score.HighScore && score.Score > 0);
                    break;
            }
        }

        public void UpdateScore(ScoreManager score)
        {
            scoreText.text = $"{score.Score}";
            if (score.ComboCount > 1)
            {
                comboText.gameObject.SetActive(true);
                comboText.text = $"PERFECT x{score.ComboCount}";
            }
            else if (score.ComboCount == 1)
            {
                comboText.gameObject.SetActive(true);
                comboText.text = "PERFECT!";
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/GameUI.cs
git commit -m "feat: add GameUI with start, playing, and game-over panels"
```

---

### Task 13: Scene Setup

**Files:**
- Modify: `Assets/Scenes/GameScene.unity` (or rename SampleScene)

This task is done in the Unity Editor. Set up the game scene with all required objects.

- [ ] **Step 1: Create the GameConfig asset**

In Unity Editor: Right-click in Project window → Create → Stopka → Game Config. Save as `Assets/Resources/GameConfig.asset`. Set all defaults per gameplay.md.

- [ ] **Step 2: Set up the scene hierarchy**

Create the following GameObjects in the scene:

```
GameScene
├── GameManager          (GameManager, BlockSpawner, TowerManager, BlockColorManager, AudioManager)
├── Main Camera          (Camera, CameraController)
│   └── ShakeTarget      (CameraShake — shake offset applied here)
│                         Position: (5, 5, 5), Rotation: looking at origin
├── Directional Light    (default light)
├── Canvas               (Canvas, CanvasScaler — Scale With Screen Size, 1080x1920)
│   ├── StartPanel       (Panel)
│   │   ├── TitleText    (Text: "STOPKA", centered, large font)
│   │   ├── TapText      (Text: "Tap to Start", centered)
│   │   └── HighScoreText(Text: "Best: 0")
│   ├── PlayingPanel     (Panel, transparent background)
│   │   ├── ScoreText    (Text: "0", top center, large font)
│   │   └── ComboText    (Text: "PERFECT!", center, initially inactive)
│   └── GameOverPanel    (Panel)
│       ├── GameOverText (Text: "GAME OVER", centered)
│       ├── FinalScore   (Text: "0", large)
│       ├── HighScoreText(Text: "Best: 0")
│       ├── NewBadge     (Text: "NEW!", initially inactive)
│       └── RestartText  (Text: "Tap to Restart")
└── EventSystem          (for UI input)
```

- [ ] **Step 3: Wire up references**

On the GameManager GameObject:
- Assign `GameConfig` asset to all components that need it (GameManager, BlockSpawner, TowerManager)
- Assign CameraController reference
- Assign BlockColorManager reference
- Assign GameUI reference

On GameUI:
- Assign all panel and text references from the Canvas hierarchy

- [ ] **Step 4: Configure Camera**

On Main Camera:
- CameraController component with `offset = (5, 5, 5)` and `followSpeed = 3`
- Ensure perspective projection
- Set Clear Flags to "Solid Color"
- Set Background Color to `#1a1a2e` (dark navy). The CameraController's `colorShiftSpeed` field (added in Task 16) will dynamically shift this hue as the tower grows, creating a smooth evolving background.

- [ ] **Step 5: Test the full game loop**

Play in Editor:
1. Start screen shows → tap → first block spawns sliding on X axis
2. Tap to place → slice happens correctly → next block spawns on Z axis
3. Perfect placement → "PERFECT!" shows, combo increments
4. Miss → block falls, game over screen shows
5. Tap → game restarts

- [ ] **Step 6: Commit**

```bash
git add Assets/Scenes/ Assets/Resources/ Assets/Scripts/
git commit -m "feat: set up game scene with all components wired"
```

---

## Chunk 5: Polish & Final Integration

### Task 14: Falling Cutoff Visual Polish

**Files:**
- Modify: `Assets/Scripts/GameManager.cs`

- [ ] **Step 1: Add slight random torque to cutoff pieces for visual interest**

In `SpawnCutoffPiece`, after adding Rigidbody:
```csharp
rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/GameManager.cs
git commit -m "polish: add random torque to falling cutoff pieces"
```

---

### Task 15: Combo Visual Feedback

**Files:**
- Modify: `Assets/Scripts/GameUI.cs`

- [ ] **Step 1: Add combo text fade-out animation**

Add a coroutine to GameUI that shows the combo text, scales it up briefly, then fades it out:

```csharp
using System.Collections;

// Add to GameUI class:
private Coroutine comboFadeCoroutine;

public void ShowCombo(int comboCount)
{
    if (comboFadeCoroutine != null)
        StopCoroutine(comboFadeCoroutine);

    comboText.gameObject.SetActive(true);
    comboText.text = comboCount > 1 ? $"PERFECT x{comboCount}" : "PERFECT!";
    comboFadeCoroutine = StartCoroutine(FadeComboText());
}

private IEnumerator FadeComboText()
{
    Color color = comboText.color;
    color.a = 1f;
    comboText.color = color;

    yield return new WaitForSeconds(0.5f);

    float fadeDuration = 0.5f;
    float elapsed = 0f;
    while (elapsed < fadeDuration)
    {
        elapsed += Time.deltaTime;
        color.a = 1f - elapsed / fadeDuration;
        comboText.color = color;
        yield return null;
    }

    comboText.gameObject.SetActive(false);
}
```

- [ ] **Step 2: Call ShowCombo from GameManager**

In `GameManager.PlaceBlock()`, after a perfect placement, call:
```csharp
gameUI.ShowCombo(scoreManager.ComboCount);
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/GameUI.cs Assets/Scripts/GameManager.cs
git commit -m "polish: add combo text fade animation"
```

---

### Task 16: CameraShake — Screen Shake for Combos

**Files:**
- Create: `Assets/Scripts/CameraShake.cs`

A simple screen shake effect triggered on perfect placements. Intensity scales with combo count.

- [ ] **Step 1: Create CameraShake.cs**

```csharp
using System.Collections;
using UnityEngine;

namespace Stopka
{
    public class CameraShake : MonoBehaviour
    {
        public void Shake(float intensity = 0.1f, float duration = 0.15f)
        {
            StopAllCoroutines();
            StartCoroutine(ShakeCoroutine(intensity, duration));
        }

        private IEnumerator ShakeCoroutine(float intensity, float duration)
        {
            Vector3 originalLocalPos = transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float strength = intensity * (1f - elapsed / duration);
                transform.localPosition = originalLocalPos + Random.insideUnitSphere * strength;
                yield return null;
            }

            transform.localPosition = originalLocalPos;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/CameraShake.cs
git commit -m "feat: add CameraShake for combo screen shake"
```

---

### Task 17: AudioManager

**Files:**
- Create: `Assets/Scripts/AudioManager.cs`

Handles all game audio: placement sound (with pitch shift based on combo), slice sound, and combo escalation cue. Uses `AudioSource` components.

- [ ] **Step 1: Create AudioManager.cs**

```csharp
using UnityEngine;

namespace Stopka
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Clips")]
        [SerializeField] private AudioClip placeSound;
        [SerializeField] private AudioClip sliceSound;
        [SerializeField] private AudioClip perfectSound;
        [SerializeField] private AudioClip gameOverSound;
        [SerializeField] private AudioClip backgroundMusic;

        [Header("Settings")]
        [SerializeField] private float basePitch = 1f;
        [SerializeField] private float pitchIncreasePerCombo = 0.05f;
        [SerializeField] private float maxPitch = 1.5f;
        [SerializeField] private float musicVolume = 0.3f;

        private AudioSource sfxSource;
        private AudioSource musicSource;

        private void Awake()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
        }

        public void StartMusic()
        {
            if (backgroundMusic != null && !musicSource.isPlaying)
            {
                musicSource.clip = backgroundMusic;
                musicSource.Play();
            }
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }

        public void PlayPlace(int comboCount)
        {
            if (comboCount > 0 && perfectSound != null)
            {
                sfxSource.pitch = Mathf.Min(basePitch + comboCount * pitchIncreasePerCombo, maxPitch);
                sfxSource.PlayOneShot(perfectSound);
            }
            else if (placeSound != null)
            {
                sfxSource.pitch = basePitch;
                sfxSource.PlayOneShot(placeSound);
            }
        }

        public void PlaySlice()
        {
            if (sliceSound != null)
            {
                sfxSource.pitch = basePitch;
                sfxSource.PlayOneShot(sliceSound);
            }
        }

        public void PlayGameOver()
        {
            if (gameOverSound != null)
            {
                sfxSource.pitch = basePitch;
                sfxSource.PlayOneShot(gameOverSound);
            }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/AudioManager.cs
git commit -m "feat: add AudioManager for game sound effects"
```

---

### Task 18: Wire Audio & Screen Shake into GameManager

**Files:**
- Modify: `Assets/Scripts/GameManager.cs`

- [ ] **Step 1: Add audio and shake calls to PlaceBlock**

In `GameManager.PlaceBlock()`, add audio and shake calls at the appropriate points:

```csharp
// In StartGame(), after SetState(GameState.Playing):
if (audioManager != null) audioManager.StartMusic();

// After perfect placement (inside the if (result.IsPerfect) block):
if (audioManager != null) audioManager.PlayPlace(scoreManager.ComboCount);
if (cameraShake != null) cameraShake.Shake(0.05f + scoreManager.ComboCount * 0.02f);

// After slice (inside the else block):
if (audioManager != null) audioManager.PlaySlice();

// In GameOver():
if (audioManager != null) audioManager.PlayGameOver();
```

- [ ] **Step 2: Add AudioManager and CameraShake to scene**

In the Unity Editor:
- Add `AudioManager` component to the GameManager GameObject
- Add `CameraShake` component to the Main Camera (as a child empty object, so shake doesn't affect the follow position)
- Wire the references in GameManager's inspector
- Audio clips can be added later — the AudioManager gracefully handles null clips

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/GameManager.cs
git commit -m "feat: wire audio and screen shake into game loop"
```

---

### Task 19: Background Color Shift

**Files:**
- Modify: `Assets/Scripts/CameraController.cs`

- [ ] **Step 1: Add background color shift based on tower height**

```csharp
// Add fields:
[SerializeField] private Camera cam;
[SerializeField] private float colorShiftSpeed = 0.01f;
private float baseHue;

// In Start():
baseHue = Random.Range(0f, 1f);
cam = GetComponent<Camera>();

// In LateUpdate(), after position update:
float hue = (baseHue + targetY * colorShiftSpeed) % 1f;
cam.backgroundColor = Color.HSVToRGB(hue, 0.3f, 0.15f);
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/CameraController.cs
git commit -m "polish: shift background color as tower grows"
```

---

### Task 20: iOS Build Configuration

**Files:**
- Modify: Unity Project Settings (via Editor)

- [ ] **Step 1: Configure iOS build settings**

In Unity Editor → Edit → Project Settings → Player → iOS tab:
- Set Company Name and Product Name
- Set Bundle Identifier (e.g., `com.yourname.stopka`)
- Set target minimum iOS version (e.g., 15.0)
- Set orientation to Portrait
- Set default screen orientation to Portrait

- [ ] **Step 2: Test build**

Build to iOS via File → Build Settings → iOS → Build. Verify the Xcode project generates without errors.

- [ ] **Step 3: Commit**

```bash
git add ProjectSettings/
git commit -m "chore: configure iOS build settings"
```

---

### Task 21: Final Play Test & Tuning

- [ ] **Step 1: Play through 20+ layers and verify:**

- Blocks alternate X/Z correctly
- Speed increases feel gradual and fair
- Perfect tolerance feels right (not too easy/hard)
- Combo recovery is noticeable but not overpowered
- Camera follows smoothly
- Screen shake triggers on perfect placements, intensifies with combo
- Colors shift pleasantly
- Background color evolves as tower grows
- Audio: placement sound plays, pitch rises with combo, slice sound on cut, game-over sound
- Game over triggers correctly on miss
- Restart works cleanly (no leftover objects)
- Score and combo display correctly
- High score persists between sessions

- [ ] **Step 2: Adjust tuning parameters in GameConfig if needed**

Refer to the defaults in [gameplay.md](../../gameplay.md) and adjust based on feel.

- [ ] **Step 3: Final commit**

```bash
git add -A
git commit -m "chore: final tuning adjustments"
```
