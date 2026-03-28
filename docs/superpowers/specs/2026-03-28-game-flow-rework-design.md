# Game Flow Rework Design Spec

## Goal

Change the game flow so that:
1. The foundation block is visible behind the Start screen
2. Game Over leads back to Start screen (not directly to gameplay)
3. A transition animation plays between Game Over and Start: old tower exits, new block drops in

## New Flow

```
Start (block visible) → Playing → GameOver → (tap) → Transition → Start (block visible) → ...
```

## GameState

Add `Transition` to the enum:

```csharp
public enum GameState { Start, Playing, GameOver, Transition }
```

During `Transition`, all tap input is blocked.

## Start Screen

- Foundation block is created and visible at game launch, before the Start panel appears
- Camera is in its initial position
- Start panel (frosted glass) overlays the scene — block visible through the semi-transparent panel
- Tapping starts the game as before (spawns first moving block on top of existing foundation)

## Game Over Screen

- "TAP TO RESTART" text changed to "TAP TO CONTINUE"
- Tap behavior: first tap skips score count-up (existing behavior), second tap triggers transition. If count-up already finished, first tap triggers transition.

## Transition State

Sequence triggered when player taps on Game Over screen:

1. **Game Over panel fades out** (~0.3s)
2. **Old tower exits** — configurable via `GameConfig.towerExitMode` enum:
   - **Sink**: All blocks (placed + foundation) are parented to a temporary container. Container lerps downward by 30 units below current position (~1.5s, ease-in). Destroy container + children on completion.
   - **Collapse**: Remove all BoxColliders first, then add Rigidbody to each block with random small forces. Add `DestroyWhenFallen` for gravity cleanup. As a safety net, also `Destroy(block, 3f)` with a timed fallback to prevent orphaned blocks.
3. **Camera smooth reset**: `CameraController.SmoothResetToOrigin(float duration)` — new method. Lerps `currentY → 0`, `cam.orthographicSize → baseOrthoSize`, sets `isPullingBack = false`, `targetY = 0`. Duration ~1s, runs in parallel with tower exit. Works from any camera state (mid-pullback or fully pulled back).
4. **Cleanup**: After tower exit completes, destroy any remaining block refs, call `placedBlocks.Clear()`, set `foundationBlock = null`, `currentBlock = null`.
5. **Tower state re-init**: Call `tower.Initialize()` and `spawner.ResetLayer()` to reset tower tracking.
6. **New foundation drops in**: Create foundation block at Y = 15 (above screen), lerp to Y = 0 over ~0.8s using `AnimationCurve.EaseInOut` with slight overshoot. Call `tower.PlaceBlock()` with foundation position/size. Apply color from `colorManager.GetColorForLayer(0)`.
7. **Start panel fades in** (~0.3s). WorldScoreDisplay stays hidden.
8. **State → Start**: Player sees Start screen with new foundation block behind it.

Total transition duration: ~2-3s depending on exit mode.

## Architecture Changes

### `GameManager.cs`
- `Start()`: create foundation block before setting state to `Start` (block visible behind Start panel)
- `RestartGame()` → renamed/reworked: sets state to `Transition`, starts `TransitionCoroutine()`
- New `TransitionCoroutine()`: orchestrates steps 1-8 above as a coroutine
- `StartGame()` no longer creates foundation (it already exists from transition or initial setup). Still calls `tower.Initialize()` is NOT needed here since transition already did it. Just does: `spawner.ResetLayer()`, `scoreManager.Reset()`, spawn first moving block, set state Playing.
- `HasTapInput()`: block input during `Transition` state
- `CreateFoundation()` stays as-is but is also called from transition coroutine

### `CameraController.cs`
- New `SmoothResetToOrigin(float duration)` coroutine: lerps currentY, cam.orthographicSize back to initial values over duration. Sets isPullingBack = false. Can be started from any state.

### `GameConfig.cs`
- Add enum: `public enum TowerExitMode { Sink, Collapse }`
- Add field: `public TowerExitMode towerExitMode = TowerExitMode.Sink;`

### `GameUI.cs`
- `SetState(Transition)`: fade out game over panel, hide others
- WorldScoreDisplay hidden during transition

### `SceneSetup.cs`
- Change "TAP TO RESTART" → "TAP TO CONTINUE" in the hardcoded text

### Audio
- No new audio during transition for now. Music stops at GameOver (existing behavior), restarts when player taps "TAP TO START" on the new Start screen (existing `StartGame()` calls `audioManager.StartMusic()`).

### Color Manager
- `colorManager.Initialize()` is called once in `Start()`. Not reset between games — colors continue evolving across sessions. This is intentional (each new game starts with a fresh color based on cumulative layer count).

## Verification

1. Launch game — foundation block visible behind Start panel
2. Play and lose — Game Over shows "TAP TO CONTINUE"
3. Tap once — count-up skips to final score. Tap again — transition starts.
4. Tower exits (test both Sink and Collapse via GameConfig toggle in inspector)
5. Camera smoothly resets to initial position and ortho size
6. New foundation drops in from above with slight bounce
7. Start panel appears with block behind it
8. Tap to start — game plays normally on top of existing foundation
9. Repeat cycle 3+ times — no object leaks, no errors in console
