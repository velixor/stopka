# UI Rework Design Spec

## Goal

Full rework of the game UI: replace current basic panels with a polished, minimalist frosted glass design. Add settings screen, improve score counter, add new high score celebration effects.

## Visual Style

- **Frosted glass panels**: Semi-transparent dark background `rgba(0,0,0,0.55)` + 24px rounded corners + 1px `rgba(255,255,255,0.1)` border. No blur shader (keeping it simple for mobile). The dark tint ensures text readability on any block/skybox color combination.
- **Font**: Thin sans-serif — Inter Light, imported as TMP font asset at `Assets/Fonts/Inter-Light SDF.asset`. Atlas: 512x512, sampling 42pt, ASCII + Cyrillic character set. All weight variation comes from the font file itself (Light = thin look). Character spacing set per-element via TMP `characterSpacing` property. Uppercase throughout.
- **Colors**: White text at varying opacities (0.7 primary, 0.5 secondary, 0.4 tertiary). Gold (#FFD700) for record effects
- **Transitions**: Fade in/out (alpha 0→1 + scale 0.95→1.0), duration ~0.3s, eased

## Screens

### 1. Start Screen

Frosted glass panel, centered vertically.

- **Title**: "STOPKA" — large (~72pt), weight 200, letter-spacing 8-10px
- **Tap prompt**: "TAP TO START" — small (~36pt), opacity 0.7, letter-spacing 3-4px, gentle pulse animation (opacity 0.5↔0.9, 2s period, ease-in-out)
- **Divider**: Thin horizontal line (40px wide, opacity 0.3)
- **High score**: "BEST: N" — small (~28pt), opacity 0.5
- **Settings gear**: Top-right corner, frosted circle (36px), gear icon at opacity 0.6

Panel appears with fade + slight scale-up (0.95→1.0).

### 2. Playing HUD

No panel — bare text over the 3D scene.

- **Score**: Large number (~64pt), centered horizontally, near top of screen (respecting safe area). Drop shadow via TMP Underlay settings (offset Y: 1, dilate: 0.3, softness: 0.5) for readability
- **Score animation**: Scale-bounce on increment (brief scale to 1.1, ease back to 1.0)
- **Settings gear**: Top-right, very subtle (opacity 0.35), same frosted circle

Playing HUD fades in when Start panel fades out.

### 3. Game Over Screen

Frosted glass panel, centered vertically. Same style as Start.

- **Header**: "GAME OVER" — small (~18pt), weight 300, opacity 0.5, letter-spacing 6px
- **Score**: Large number (~72pt), weight 200, white
- **Divider**: Same thin line as Start
- **High score**: "BEST: N" — opacity 0.45, letter-spacing 3px
- **Restart prompt**: "TAP TO RESTART" — opacity 0.55, letter-spacing 4px

Panel fades in 0.5s after camera pullback begins (delayed coroutine).

### 4. Game Over — New Record Variant

When the player beats their high score, the Game Over panel changes:

- **Panel border**: Shifts to gold tint — `rgba(255,215,0,0.2)`
- **Header**: "★ NEW RECORD ★" — gold color at opacity 0.7, letter-spacing 6px
- **Score**: Gold (#FFD700) with TMP Underlay glow (color gold, dilate 0.5, softness 1.0)
- **Previous score**: "PREVIOUS: N" replaces "BEST: N" — opacity 0.4
- **Score count-up**: Score animates from 0 to final value over ~1s

### 5. New Record Effect During Play

When the score exceeds the current high score mid-game:

- **Score color**: Transitions from white to gold (#FFD700)
- **Glow**: Pulsing gold via TMP Underlay (color gold, softness animated) with 1.5s period
- **Label**: "NEW BEST" text fades in below the score (small, gold, opacity 0.6, letter-spacing 4px)
- Trigger: once, on the placement that first exceeds HighScore. Detection: in `GameManager.PlaceBlock()`, after `scoreManager.AddPlacement()`, compare `scoreManager.Score > scoreManager.HighScore`. If true and not already triggered this game, call `gameUI.ShowNewRecordDuringPlay()`

### 6. Settings Panel

Frosted glass overlay panel, centered. Opens from gear icon tap.

- **Title**: "SETTINGS" — small, letter-spacing 5px, opacity 0.6
- **Sound toggle**: Label "SOUND" + iOS-style toggle (44x24px, frosted pill shape, white knob)
- **Vibration toggle**: Label "VIBRATION" + same toggle style
- **Divider**: Thin line
- **Close**: "TAP TO CLOSE" — small, opacity 0.4. Also closes on tap outside panel

Opens/closes with same fade + scale transition. While open, the sliding block continues moving but taps are blocked (no Time.timeScale change — keeps animations smooth). A boolean `isSettingsOpen` in GameUI gates `HasTapInput()` in GameManager.

Toggle on/off states: knob position (left=off, right=on) + track color change (off: `rgba(255,255,255,0.15)`, on: `rgba(255,255,255,0.3)` with white knob).

## Architecture Changes

### Files to Modify

- **`GameUI.cs`** — Major rework: add settings panel, fade transitions (CanvasGroup + coroutines), score animations, new record effects, toggle management. New public methods: `ShowNewRecordDuringPlay()`, `IsSettingsOpen` property. Remove existing `newHighScoreBadge` field (replaced by gold variant panel).
- **`SceneSetup.cs`** — Rebuild UI hierarchy: use dark semi-transparent panels instead of current `Color(0,0,0,0.5)`, add CanvasGroup to each panel, create Settings panel with toggles, load Inter Light font from `Assets/Fonts/Inter-Light SDF.asset`, update font/size/spacing. Add SafeArea wrapper RectTransform for top-area elements (score, gear icon).
- **`GameManager.cs`** — Add new record detection in `PlaceBlock()` (after `AddPlacement`, compare score vs HighScore, call `gameUI.ShowNewRecordDuringPlay()`). Guard `HasTapInput()` with `EventSystem.current.IsPointerOverGameObject()` to prevent gear-icon taps from also triggering block placement. Check `gameUI.IsSettingsOpen` to block game input.
- **`AudioManager.cs`** — Add `SetMuted(bool)` method that reads `SoundEnabled` PlayerPref and mutes/unmutes audio sources accordingly.

### New Components

- **`UIFader.cs`** — Reusable component for fade + scale transitions on CanvasGroup. Methods: `FadeIn(duration)`, `FadeOut(duration)`. Uses coroutines.
- **`ScoreBounce.cs`** — Animates score text scale on value change (punch scale effect)
- **`ScoreCountUp.cs`** — Animates score from 0 to target on Game Over screen

### Settings Persistence

- Use `PlayerPrefs` (consistent with existing HighScore storage):
  - `SoundEnabled` (int, 1/0, default 1)
  - `VibrationEnabled` (int, 1/0, default 1)
- Load on Start, save on toggle change

### Input Handling for Settings

- Gear icon is a UI Button (transparent background, gear icon child)
- `HasTapInput()` in GameManager checks `EventSystem.current.IsPointerOverGameObject()` — if pointer is over UI, tap is consumed by UI and does not place a block
- `GameUI.IsSettingsOpen` property checked in `HasTapInput()` to block all game input when settings overlay is visible
- Settings panel closes on: "TAP TO CLOSE" button, or tap on a dimmed fullscreen background behind the panel (a Button covering the whole canvas)
- **Score count-up tap**: If player taps during Game Over score count-up animation, it skips to the final score value (does not restart the game until count-up completes or is skipped)

### Safe Area

All top-area UI elements (score text, settings gear) must be placed inside a SafeArea RectTransform that adjusts to `Screen.safeArea` at runtime. This prevents overlap with iOS notch/Dynamic Island/home indicator. A small `SafeAreaFitter` component on this RectTransform reads `Screen.safeArea` in `Start()` and `OnRectTransformDimensionsChange()`.

### Font

- Download Inter Light (OFL license) and place the .ttf at `Assets/Fonts/Inter-Light.ttf`
- Generate TMP font asset via TMP Font Asset Creator: atlas 512x512, sampling 42pt, ASCII character set
- Save as `Assets/Fonts/Inter-Light SDF.asset`
- `SceneSetup.cs` loads this font asset and assigns it to all TMP components

## Verification

1. Run `SceneSetup` menu item to rebuild the scene
2. Play in editor — verify all 3 game states display correct panels with fade transitions
3. Tap settings gear — verify overlay opens, toggles work, game input is blocked
4. Beat high score — verify gold score effect during play and gold Game Over variant
5. Check on different aspect ratios (iPhone SE, iPhone 15 Pro Max) via Game view
6. Verify PlayerPrefs persistence for settings and high score
