# Gameplay Polish Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix 5 gameplay issues: combo recovery logic, cutoff collision, block height, game-over camera, and color contrast.

**Architecture:** All changes are modifications to existing files. Combo recovery gets a streak threshold and single-axis logic. Placed blocks get BoxColliders so cutoff pieces interact properly. Game-over state gets a camera pullback before allowing restart. Color hue step is increased.

**Tech Stack:** Unity 6, C#, URP.

---

## File Structure

```
Modified files:
  Assets/Scripts/GameConfig.cs          — add comboRecoveryThreshold, increase blockHeight
  Assets/Scripts/ScoreManager.cs        — add ShouldRecover() method with threshold check
  Assets/Scripts/GameManager.cs         — single-axis recovery, animate size, add colliders, game-over camera
  Assets/Scripts/Block.cs               — add AnimateSize() coroutine, add collider after placement
  Assets/Scripts/CutoffPiece.cs         — increase outward speed
  Assets/Scripts/CameraController.cs    — add PullBackToShowTower() for game-over view
  Assets/Scripts/BlockColorManager.cs   — increase hueStep default
  Assets/Tests/EditMode/ScoreManagerTests.cs — update tests for new threshold logic
```

---

## Chunk 1: Combo Recovery, Block Height & Color

### Task 1: Update GameConfig defaults

**Files:**
- Modify: `Assets/Scripts/GameConfig.cs`

- [ ] **Step 1: Add combo threshold and update blockHeight**

```csharp
// In GameConfig.cs, update the Combo and Block sections:

[Header("Combo")]
public float comboRecoveryRate = 0.05f;
[Tooltip("Number of consecutive perfects before recovery kicks in")]
public int comboRecoveryThreshold = 3;

[Header("Block")]
public float blockHeight = 0.5f;  // was 0.2f
public Vector2 startBlockSize = new Vector2(3f, 3f);
```

- [ ] **Step 2: Update GameConfig asset in Unity**

Use Unity MCP to update the GameConfig asset at `Assets/Resources/GameConfig.asset`:
- Set `blockHeight` to `0.5`
- Set `comboRecoveryThreshold` to `3`

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/GameConfig.cs
git commit -m "feat: add combo recovery threshold, increase block height"
```

---

### Task 2: Update ScoreManager with threshold logic (TDD)

**Files:**
- Modify: `Assets/Scripts/ScoreManager.cs`
- Modify: `Assets/Tests/EditMode/ScoreManagerTests.cs`

- [ ] **Step 1: Update tests for threshold-based recovery**

Add new tests and update existing ones in `ScoreManagerTests.cs`:

```csharp
[Test]
public void ShouldRecover_FalseBeforeThreshold()
{
    score.AddPlacement(isPerfect: true); // combo = 1
    score.AddPlacement(isPerfect: true); // combo = 2
    Assert.IsFalse(score.ShouldRecover(threshold: 3));
}

[Test]
public void ShouldRecover_TrueAtThreshold()
{
    score.AddPlacement(isPerfect: true); // combo = 1
    score.AddPlacement(isPerfect: true); // combo = 2
    score.AddPlacement(isPerfect: true); // combo = 3
    Assert.IsTrue(score.ShouldRecover(threshold: 3));
}

[Test]
public void ShouldRecover_TrueAboveThreshold()
{
    for (int i = 0; i < 5; i++)
        score.AddPlacement(isPerfect: true);
    Assert.IsTrue(score.ShouldRecover(threshold: 3));
}

// Update ComboRecovery_WidensBlock test to use excess combo count
[Test]
public void ComboRecovery_UsesExcessOverThreshold()
{
    float currentSize = 2f;
    float maxSize = 3f;
    float recoveryRate = 0.05f;

    // combo=5, threshold=3 → excess=2, recovery = 2 * 0.05 = 0.10
    float recovered = ScoreManager.CalculateRecoveredSize(
        currentSize, maxSize, comboCount: 5, threshold: 3, recoveryRate);

    Assert.AreEqual(2.10f, recovered, 0.001f);
}

[Test]
public void ComboRecovery_ZeroWhenBelowThreshold()
{
    float currentSize = 2f;
    float maxSize = 3f;
    float recoveryRate = 0.05f;

    // combo=2, threshold=3 → excess=0, no recovery
    float recovered = ScoreManager.CalculateRecoveredSize(
        currentSize, maxSize, comboCount: 2, threshold: 3, recoveryRate);

    Assert.AreEqual(2f, recovered, 0.001f);
}
```

- [ ] **Step 2: Run tests to verify they fail**

- [ ] **Step 3: Update ScoreManager implementation**

```csharp
public bool ShouldRecover(int threshold)
{
    return ComboCount >= threshold;
}

public static float CalculateRecoveredSize(
    float currentSize, float maxSize, int comboCount, int threshold, float recoveryRate)
{
    int excess = Mathf.Max(0, comboCount - threshold);
    if (excess == 0) return currentSize;
    return Mathf.Min(currentSize + excess * recoveryRate, maxSize);
}
```

Also keep the old 2-param overload working for backward compat or remove it and update all callers. Since we control all callers, update the signature directly.

- [ ] **Step 4: Update existing tests that use old CalculateRecoveredSize signature**

The old `ComboRecovery_WidensBlock` and `ComboRecovery_CapsAtMaxSize` tests need to pass threshold parameter. Update them:

```csharp
[Test]
public void ComboRecovery_WidensBlock()
{
    float currentSize = 2f;
    float maxSize = 3f;
    float recoveryRate = 0.05f;

    // combo=5, threshold=2 → excess=3, recovery = 3 * 0.05 = 0.15
    float recovered = ScoreManager.CalculateRecoveredSize(
        currentSize, maxSize, comboCount: 5, threshold: 2, recoveryRate);

    Assert.AreEqual(2.15f, recovered, 0.001f);
}

[Test]
public void ComboRecovery_CapsAtMaxSize()
{
    float currentSize = 2.95f;
    float maxSize = 3f;
    float recoveryRate = 0.05f;

    // combo=7, threshold=2 → excess=5
    float recovered = ScoreManager.CalculateRecoveredSize(
        currentSize, maxSize, comboCount: 7, threshold: 2, recoveryRate);

    Assert.AreEqual(3f, recovered, 0.001f);
}
```

- [ ] **Step 5: Run tests to verify they pass**

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/ScoreManager.cs Assets/Tests/EditMode/ScoreManagerTests.cs
git commit -m "feat: combo recovery requires streak threshold before activating"
```

---

### Task 3: Single-axis recovery with animation in GameManager

**Files:**
- Modify: `Assets/Scripts/GameManager.cs`
- Modify: `Assets/Scripts/Block.cs`

- [ ] **Step 1: Add AnimateSize coroutine to Block.cs**

Add to `Block.cs`:

```csharp
using System.Collections;

// Add method:
public void AnimateSize(Vector2 targetSize, float duration = 0.15f)
{
    StartCoroutine(AnimateSizeCoroutine(targetSize, duration));
}

private IEnumerator AnimateSizeCoroutine(Vector2 targetSize, float duration)
{
    Vector2 startSize = Size;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        // Ease out
        t = 1f - (1f - t) * (1f - t);

        Vector2 current = Vector2.Lerp(startSize, targetSize, t);
        Size = current;
        transform.localScale = new Vector3(current.x, transform.localScale.y, current.y);
        yield return null;
    }

    Size = targetSize;
    transform.localScale = new Vector3(targetSize.x, transform.localScale.y, targetSize.y);
}
```

- [ ] **Step 2: Update combo recovery in GameManager.PlaceBlock()**

Replace the current combo recovery block (lines 140-148) with:

```csharp
if (result.IsPerfect)
{
    // Snap to alignment
    currentBlock.ApplySlice(result.NewCenter, currSize);
    scoreManager.AddPlacement(isPerfect: true);

    // Combo recovery: only after streak threshold, only on slide axis
    if (scoreManager.ShouldRecover(config.comboRecoveryThreshold))
    {
        Vector2 recoveredSize = currentBlock.Size;
        if (axis == SlideAxis.X)
        {
            recoveredSize.x = ScoreManager.CalculateRecoveredSize(
                currentBlock.Size.x, config.startBlockSize.x,
                scoreManager.ComboCount, config.comboRecoveryThreshold,
                config.comboRecoveryRate);
        }
        else
        {
            recoveredSize.y = ScoreManager.CalculateRecoveredSize(
                currentBlock.Size.y, config.startBlockSize.y,
                scoreManager.ComboCount, config.comboRecoveryThreshold,
                config.comboRecoveryRate);
        }
        currentBlock.AnimateSize(recoveredSize);
    }

    if (gameUI != null)
        gameUI.ShowCombo(scoreManager.ComboCount);
    if (audioManager != null) audioManager.PlayPlace(scoreManager.ComboCount);
    if (cameraShake != null) cameraShake.Shake(0.05f + scoreManager.ComboCount * 0.02f);
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/GameManager.cs Assets/Scripts/Block.cs
git commit -m "feat: combo recovery only after streak, single-axis, with animation"
```

---

### Task 4: Increase color contrast

**Files:**
- Modify: `Assets/Scripts/BlockColorManager.cs`

- [ ] **Step 1: Increase hueStep for more contrast**

Change the default `hueStep` from `0.03f` to `0.08f`:

```csharp
[SerializeField] private float hueStep = 0.08f;
```

Also increase saturation slightly for more vivid colors:

```csharp
[SerializeField] private float saturation = 0.7f;
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/BlockColorManager.cs
git commit -m "polish: increase color contrast between layers"
```

---

## Chunk 2: Cutoff Collision, Game-Over Camera

### Task 5: Add colliders to placed blocks, fix cutoff speed

**Files:**
- Modify: `Assets/Scripts/GameManager.cs`
- Modify: `Assets/Scripts/CutoffPiece.cs`

- [ ] **Step 1: Add BoxCollider to blocks after placement**

In `GameManager.PlaceBlock()`, after `placedBlocks.Add(currentBlock)` (line 172), add:

```csharp
// Add collider to placed block so cutoff pieces bounce off
var col = currentBlock.gameObject.AddComponent<BoxCollider>();
```

Also add collider to the foundation in `CreateFoundation()`, after setting the scale:

```csharp
foundationBlock.AddComponent<BoxCollider>();
```

Note: we still `Destroy(go.GetComponent<Collider>())` in `BlockSpawner.SpawnBlock` — that's fine, the collider is added AFTER placement, so it doesn't interfere with the sliding block.

- [ ] **Step 2: Give cutoff pieces Rigidbody for proper collision**

Replace the kinematic CutoffPiece approach with a Rigidbody-based one that has constrained rotation. Update `SpawnCutoffPiece` in `GameManager.cs`:

```csharp
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
    rb.linearVelocity = slideDir * 2f; // push outward

    Destroy(cutoff, 3f);
}
```

- [ ] **Step 3: Simplify CutoffPiece.cs — it's no longer needed**

Since cutoff pieces now use Rigidbody physics with frozen rotation, `CutoffPiece.cs` is no longer attached. We can either delete it or keep it for reference. Delete it to keep the project clean:

```bash
rm Assets/Scripts/CutoffPiece.cs Assets/Scripts/CutoffPiece.cs.meta
```

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/GameManager.cs
git rm Assets/Scripts/CutoffPiece.cs
git commit -m "fix: add colliders to placed blocks, use Rigidbody for cutoff pieces"
```

---

### Task 6: Game-over camera pullback

**Files:**
- Modify: `Assets/Scripts/CameraController.cs`
- Modify: `Assets/Scripts/GameManager.cs`

- [ ] **Step 1: Add ShowFullTower method to CameraController**

```csharp
// Add to CameraController.cs:

[SerializeField] private float pullbackSmoothTime = 1f;

private float pullbackTargetY;
private bool isPullingBack;

public void ShowFullTower(float towerHeight)
{
    // Target the midpoint of the tower, pull camera back to see it all
    pullbackTargetY = towerHeight / 2f;
    isPullingBack = true;
}

public void StopPullback()
{
    isPullingBack = false;
}
```

Update `LateUpdate` to handle pullback mode:

```csharp
private void LateUpdate()
{
    float target = isPullingBack ? pullbackTargetY : targetY;
    currentY = Mathf.SmoothDamp(currentY, target, ref velocityY,
        isPullingBack ? pullbackSmoothTime : smoothTime);

    float extraDistance = isPullingBack ? targetY * 0.3f : 0f;
    Vector3 camOffset = offset + new Vector3(extraDistance, extraDistance, extraDistance);

    transform.position = new Vector3(camOffset.x, currentY + camOffset.y, camOffset.z);
    transform.LookAt(new Vector3(0f, currentY, 0f));

    if (cam != null)
    {
        float hue = (baseHue + targetY * colorShiftSpeed) % 1f;
        cam.backgroundColor = Color.HSVToRGB(hue, 0.3f, 0.15f);
    }
}
```

- [ ] **Step 2: Update ResetToOrigin to clear pullback state**

```csharp
public void ResetToOrigin()
{
    targetY = 0f;
    currentY = 0f;
    velocityY = 0f;
    isPullingBack = false;
    transform.position = offset;
    transform.LookAt(Vector3.zero);
}
```

- [ ] **Step 3: Wire game-over camera in GameManager**

In `GameOver()`, after setting state, trigger camera pullback:

```csharp
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

    // Pull camera back to show the full tower
    cameraController.ShowFullTower(tower.TopPosition.y);

    SetState(GameState.GameOver);
}
```

In `RestartGame()`, stop the pullback:

```csharp
private void RestartGame()
{
    cameraController.StopPullback();
    // ... rest of existing cleanup
```

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/CameraController.cs Assets/Scripts/GameManager.cs
git commit -m "feat: camera pulls back to show full tower on game over"
```
