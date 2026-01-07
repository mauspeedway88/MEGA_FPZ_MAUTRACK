# Single Player Mode Setup - 03_Shooter Scene

## Overview

The 03_Shooter scene can now be played as a single player in the Unity Editor for testing purposes. This allows you to test gameplay, weapons, and mechanics without needing a second player.

## How to Enable Single Player Mode

### Method 1: Using UIGameMenu (Recommended)

1. **Open the 03_Shooter scene** in Unity Editor
2. **Find the UIGameMenu component** in the scene hierarchy
3. **Enable "Force Single Player"** checkbox in the Inspector
4. **Press Play** in the Editor
5. **Click "Start Game"** button in the UI
   - The game will start in single player mode
   - Player will be active immediately (no waiting room)

### Method 2: Using FusionBootstrap (Alternative)

If your scene has a `FusionBootstrap` component:

1. **Press Play** in the Editor
2. **In the Fusion Debug GUI** (appears in-game), click **"Start Single Player"** button
   - This will start the game in single player mode immediately

## What Happens in Single Player Mode

- ✅ Player spawns immediately (no waiting for second player)
- ✅ Player is active and can move/shoot immediately
- ✅ Waiting room UI is hidden
- ✅ All game mechanics work (weapons, chickens, etc.)
- ✅ Battle timer and game systems function normally
- ⚠️ No multiplayer features (no second player, no network sync with others)

## Important Notes

### Game Mode Detection

The `GameManager` automatically detects single player mode by checking:
```csharp
bool isSinglePlayer = Runner.GameMode == GameMode.Single;
```

When in single player mode:
- Player activates immediately (no waiting)
- Room/waiting UI is hidden
- Game starts right away

### Testing Scenarios

Single player mode is perfect for testing:
- ✅ Weapon systems and variations
- ✅ Player movement and controls
- ✅ Enemy AI (chickens)
- ✅ Game mechanics and balance
- ✅ UI elements
- ✅ Visual effects and animations

### Limitations

- ❌ Cannot test multiplayer synchronization
- ❌ Cannot test player vs player combat
- ❌ Network-specific features may behave differently

## Troubleshooting

### Player Doesn't Spawn

1. **Check Game Mode**: Ensure `ForceSinglePlayer` is enabled in UIGameMenu
2. **Check Console**: Look for `[Shooter GameManager]` debug messages
3. **Verify Runner**: Make sure NetworkRunner is starting correctly

### Waiting Room Still Shows

- This should not happen in single player mode
- Check that `Runner.GameMode == GameMode.Single`
- Verify the UIGameMenu's `ForceSinglePlayer` is enabled

### Game Doesn't Start

1. **Check NetworkRunner**: Ensure RunnerPrefab is assigned
2. **Check Scene**: Make sure 03_Shooter scene is in Build Settings
3. **Check Console**: Look for error messages

## Code Changes Made

The `GameManager.cs` was updated to:
1. Detect single player mode via `Runner.GameMode == GameMode.Single`
2. Activate player immediately in single player mode
3. Skip waiting room logic for single player
4. Skip multiplayer activation logic in `FixedUpdateNetwork`

## Quick Start Checklist

- [ ] Open 03_Shooter scene
- [ ] Find UIGameMenu in hierarchy
- [ ] Enable "Force Single Player" checkbox
- [ ] Press Play
- [ ] Click "Start Game" button
- [ ] Player should spawn and be playable immediately

Enjoy testing! 🎮

