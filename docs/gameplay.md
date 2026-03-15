# Stopka — Gameplay Design

## Concept

A single-tap 3D stacking game. Blocks slide across the screen; the player taps to place each one. Precision is rewarded — misaligned portions are cut off, making the tower narrower. The game ends when the block is trimmed to nothing.

## Core Mechanics

### Block Movement
- Each block slides back and forth along a single axis at constant speed.
- The slide axis alternates every layer: X → Z → X → Z → ...
- Speed increases gradually as the tower grows (configurable ramp curve).

### Placement & Slicing
- On tap, the block stops and is compared to the block below.
- **Perfect placement:** if the offset is within a small tolerance, the block snaps to exact alignment. A "perfect" visual/audio cue plays and the combo counter increments.
- **Partial overlap:** the overhanging part is sliced off. The cut piece becomes a separate physics object that falls away. The remaining piece stays as part of the tower.
- **No overlap (miss):** the entire block falls. Game over.

### Combo System
- Consecutive perfect placements build a combo.
- Each combo level slightly widens the block back toward its original size (recovery mechanic), up to the original block width.
- Combo resets on any non-perfect placement.
- Visual feedback intensifies with combo level (color pulse, particle burst, screen shake).

### Scoring
- +1 point per successful placement.
- Bonus points for perfect placements (e.g., +1 per combo level).
- High score persisted locally (PlayerPrefs or file).

## Camera

- Perspective camera looking at the tower from a fixed angle (roughly 45° azimuth, ~30° elevation).
- Camera follows the tower upward smoothly (lerp/damping) as blocks are stacked.
- Subtle zoom-out as the tower gets very tall (optional).

## Visual Style

- Clean, minimal aesthetic — solid-color blocks with soft shadows.
- Block color shifts gradually per layer (hue rotation or curated palette).
- Background is a smooth gradient that also shifts with tower height.
- No textures on blocks — flat/unlit or simple lit shader.

## Audio

- Tap/place sound with pitch variation based on combo.
- Satisfying slice sound when a piece is cut.
- Ambient background music (calm, loopable).
- Escalating audio cue as combo grows.

## UI Flow

```
[Start Screen]
    Title + "Tap to Start"
    High score display
        |
        v
[Gameplay]
    Score counter (top)
    Combo indicator (center, fades)
        |
        v
[Game Over]
    Final score
    High score (with "NEW" badge if beaten)
    "Tap to Restart"
```

## Game Over

- Triggered when a placed block has zero overlap with the block below.
- The missed block falls away with physics.
- Brief pause → game-over overlay slides in.
- Tap anywhere to restart (tower resets, score resets, speed resets).

## Tuning Parameters

| Parameter | Description | Suggested Default |
|---|---|---|
| `startSpeed` | Initial slide speed (units/sec) | 3.0 |
| `speedIncrement` | Speed increase per layer | 0.05 |
| `maxSpeed` | Speed cap | 8.0 |
| `perfectTolerance` | Max offset to count as perfect (units) | 0.1 |
| `comboRecoveryRate` | Width added back per combo level (units) | 0.05 |
| `blockHeight` | Height of each block | 0.2 |
| `startBlockSize` | Initial block dimensions (X, Z) | (3.0, 3.0) |
