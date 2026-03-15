# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Stopka** is a 3D block-stacking mobile game for iOS, built with Unity (C#). Inspired by "Stack" — the player taps to place sliding blocks on top of each other, trying to align them perfectly. Misaligned parts get cut off, making the tower progressively narrower.

## Tech Stack

- **Engine:** Unity (C#)
- **Target platform:** iOS
- **Rendering:** 3D (perspective camera, simple geometry)

## Build & Run

```bash
# Open the project in Unity
open -a Unity stopka

# Build iOS from command line (requires Unity CLI and Xcode)
/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath . \
  -executeMethod BuildScript.BuildIOS -quit

# Run tests
/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath . \
  -runTests -testResults results.xml

# Run a specific test category
/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath . \
  -runTests -testFilter <TestClassName.MethodName>
```

## Architecture

See [docs/gameplay.md](docs/gameplay.md) for full gameplay design.

### Core Loop

1. A block slides back and forth along one axis (alternating X/Z each layer)
2. Player taps to drop it
3. Overhanging part is sliced off and falls away
4. A new block spawns one layer up, sliding on the perpendicular axis
5. If the remaining area is zero → game over

### Key Systems (to be implemented)

- **BlockSpawner** — spawns new blocks, alternates slide axis, controls speed ramp
- **BlockSlicer** — computes overlap, slices the block, spawns the falling cutoff piece
- **TowerManager** — tracks tower height, moves camera up, handles game-over state
- **ScoreManager** — tracks score, combo (perfect placements), persists high score
- **GameUI** — start screen, score display, game-over screen with restart
