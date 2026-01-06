# 2-Player Battle Lobby Setup Guide

This guide explains how to set up both lobby modes for your multiplayer battle system.

---

## MODE 1: PUBLIC RANDOM MATCHMAKING

### Overview
1. **Player 1** presses Play → enters lobby, sees "SEARCHING FOR ANOTHER PLAYER..."
2. **Player 1** waits for another player
3. **Player 2** presses Play → joins the same lobby
4. Both players see each other connected, lobby locks (no more players can join)
5. **START BATTLE** button appears for both players
6. When **BOTH** players press START BATTLE → combat scene loads

## Scripts

### BattleLobbyController.cs (RECOMMENDED - All-in-One Solution)
The main script that handles everything:
- Network connection via Photon Fusion
- Matchmaking into 2-player sessions
- UI state management
- Ready state synchronization
- Combat scene loading

### Alternative Scripts (for custom implementations):
- `Lobby.cs` - Connection management only
- `LobbyManager.cs` - Networked state management
- `LobbyUI.cs` - UI controller

## Quick Setup

### Option 1: Using BattleLobbyController (Recommended)

1. **Create a new scene** or modify `00_MainMenu`

2. **Create UI Canvas** with these elements:
   - Main Menu Panel
     - Play Button
     - Nickname Input Field
     - Quit Button
   - Searching Panel
     - Searching Text ("SEARCHING FOR ANOTHER PLAYER...")
     - Cancel Button
   - Lobby Panel
     - Player 1 Slot (Image + Name Text + Status Text)
     - Player 2 Slot (Image + Name Text + Status Text)
     - Start Battle Button
     - Leave Lobby Button
     - Status Text
   - Transition Panel
     - "BATTLE STARTING!" Text

3. **Add BattleLobbyController** to an empty GameObject

4. **Create a NetworkObject prefab** with `BattleLobbyController`:
   - Create empty GameObject
   - Add `NetworkObject` component
   - Add `BattleLobbyController` component
   - Save as prefab

5. **Configure BattleLobbyController**:
   - Assign Runner Prefab (use existing `Runner.prefab` from Common folder)
   - Assign all UI references
   - Set Combat Scene Index (default: 1 for ThirdPersonCharacter)

6. **Build Settings**: Ensure scenes are in correct order:
   - 0: MainMenu/Lobby Scene
   - 1: ThirdPersonCharacter (Combat Scene)

## UI Structure Example

```
Canvas
├── MainMenuPanel
│   ├── TitleText ("BATTLE ARENA")
│   ├── NicknameInput
│   ├── PlayButton ("PLAY")
│   └── QuitButton ("QUIT")
│
├── SearchingPanel (initially hidden)
│   ├── SearchingText ("SEARCHING FOR ANOTHER PLAYER...")
│   ├── Player1Slot
│   │   ├── Background (Image)
│   │   ├── NameText ("You")
│   │   └── StatusText ("Connected")
│   ├── Player2Slot
│   │   ├── Background (Image - gray/empty)
│   │   ├── NameText ("Waiting...")
│   │   └── StatusText ("")
│   └── CancelButton ("CANCEL")
│
├── LobbyPanel (initially hidden)
│   ├── TitleText ("BATTLE LOBBY")
│   ├── Player1Slot
│   │   ├── Background (Image - green when ready)
│   │   ├── NameText
│   │   └── StatusText ("READY!" or "Connected")
│   ├── VSText ("VS")
│   ├── Player2Slot
│   │   ├── Background (Image - green when ready)
│   │   ├── NameText
│   │   └── StatusText ("READY!" or "Connected")
│   ├── StartBattleButton ("START BATTLE")
│   ├── LobbyStatusText ("Both players must press START BATTLE")
│   └── LeaveLobbyButton ("LEAVE LOBBY")
│
└── TransitionPanel (initially hidden)
    └── TransitionText ("BATTLE STARTING!")
```

## Color Scheme

- **Connected (not ready)**: Green `(0.2, 0.7, 0.2)`
- **Ready**: Bright Green `(0.1, 0.9, 0.1)`
- **Waiting/Pending**: Orange `(0.7, 0.5, 0.1)`
- **Empty Slot**: Gray `(0.3, 0.3, 0.3)`

## Network Flow

1. Player presses Play → `ConnectToLobby()` called
2. Creates NetworkRunner with Shared mode
3. Matchmaking finds or creates a 2-player session
4. When 2nd player joins:
   - `OnPlayerJoined` callback fires
   - UI switches from Searching to Lobby panel
   - Lobby is "locked" (max 2 players already set)
5. Each player can toggle ready state via `RPC_SetPlayerReady`
6. When both ready, master client calls `RPC_StartBattle`
7. All clients load combat scene via `Runner.LoadScene`

## Testing

### Local Testing (Same Machine)
1. Build the game
2. Run one instance from Unity Editor
3. Run one instance from the build
4. Both should connect and see each other

### Multi-Device Testing
1. Build for target platforms
2. Ensure all devices have network access
3. Photon cloud handles matchmaking automatically

## Troubleshooting

**Players not finding each other:**
- Check Photon App ID in PhotonAppSettings
- Ensure same session properties ("GameMode": "BattleLobby2v2")
- Check network connectivity

**Ready states not syncing:**
- Ensure BattleLobbyController has NetworkObject component
- Check RPC method has correct attributes

**Scene not loading:**
- Verify combat scene is in Build Settings at index 1
- Check `combatSceneIndex` is set correctly

---

## MODE 2: PRIVATE MATCH (PASSWORD LOBBY)

### Overview

Private Match allows two players to fight 1vs1 using a shared password.

### Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    PRIVATE MATCH                             │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  PLAYER 1 selects PRIVATE MATCH                              │
│       ↓                                                      │
│  Enters password (e.g., "1234")                              │
│       ↓                                                      │
│  Enters private lobby tied to that password                  │
│  Shows: "WAITING FOR OPPONENT..."                            │
│  Displays: "Room Code: 1234"                                 │
│       ↓                                                      │
│  PLAYER 2 selects PRIVATE MATCH                              │
│       ↓                                                      │
│  Enters SAME password ("1234")                               │
│       ↓                                                      │
│  Joins the same private lobby                                │
│       ↓                                                      │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  PRIVATE LOBBY LOCKED                               │    │
│  │  Room Code: 1234                                    │    │
│  │                                                      │    │
│  │  [PLAYER 1 (You)]    VS    [OPPONENT]               │    │
│  │   Connected                 Connected                │    │
│  │                                                      │    │
│  │           [ START BATTLE ]                          │    │
│  │                                                      │    │
│  │  "Both players must press START BATTLE"             │    │
│  └─────────────────────────────────────────────────────┘    │
│       ↓                                                      │
│  BOTH press START BATTLE → Combat arena loads                │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Script: PrivateMatchController.cs

Handles the complete private match flow:
- Password entry and validation
- Private session creation (password = session name)
- Waiting for opponent with room code display
- Ready state synchronization
- Battle start when both players ready

### UI Structure for Private Match

```
Canvas
├── MainMenuPanel
│   ├── TitleText ("BATTLE ARENA")
│   ├── NicknameInput
│   ├── PrivateMatchButton ("PRIVATE MATCH")
│   ├── PublicMatchButton ("FIND MATCH")
│   └── QuitButton ("QUIT")
│
├── PasswordPanel (initially hidden)
│   ├── InstructionText ("Enter password to create/join private lobby...")
│   ├── PasswordInput (TMP_InputField)
│   ├── JoinLobbyButton ("JOIN LOBBY")
│   ├── BackButton ("BACK")
│   └── ErrorText (hidden, shows validation errors)
│
├── WaitingPanel (initially hidden)
│   ├── WaitingText ("WAITING FOR OPPONENT...")
│   ├── RoomCodeText ("Room Code: XXXX")
│   └── CancelButton ("CANCEL")
│
├── LobbyPanel (initially hidden)
│   ├── TitleText ("PRIVATE BATTLE LOBBY")
│   ├── RoomCodeText ("Private Room: XXXX")
│   ├── Player1Slot
│   │   ├── Background
│   │   ├── NameText
│   │   └── StatusText
│   ├── VSText ("VS")
│   ├── Player2Slot
│   │   ├── Background
│   │   ├── NameText
│   │   └── StatusText
│   ├── StartBattleButton ("START BATTLE")
│   ├── StatusText
│   └── LeaveLobbyButton ("LEAVE LOBBY")
│
└── TransitionPanel (initially hidden)
    └── TransitionText ("BATTLE STARTING!")
```

### How Password Matching Works

1. Password is used as part of the Photon session name:
   - Session name = `"PRIVATE_" + password.ToUpper()`
   - Example: Password "1234" → Session "PRIVATE_1234"

2. When Player 1 enters "1234":
   - Creates or joins session "PRIVATE_1234"
   - If session doesn't exist, creates it and waits

3. When Player 2 enters "1234":
   - Joins existing session "PRIVATE_1234"
   - Both players now in same lobby

4. Different passwords = different sessions (players never meet)

### Password Validation

- Minimum: 2 characters
- Maximum: 20 characters
- Case-insensitive (converted to uppercase)
- Cannot be empty

### Setup Steps

1. Add `PrivateMatchController` to a GameObject with `NetworkObject`
2. Assign Runner Prefab from `Assets/Common/Runner.prefab`
3. Create and assign all UI panels/elements
4. Set Combat Scene Index (default: 1)

---

## COMBINED SETUP (Both Modes)

For a game with both Public and Private match options:

```
MainMenuPanel
├── FIND MATCH button    → BattleLobbyController (public random)
├── PRIVATE MATCH button → PrivateMatchController (password lobby)
└── QUIT button
```

You can either:
1. Use two separate controllers on the same GameObject
2. Create a unified controller that combines both flows
3. Use different scenes for each mode

---

## INTEGRATION WITH EXISTING GAMEPLAY

### Files Modified for Integration

1. **MainMenuController.cs** (NEW - Unified lobby controller)
   - Combines Public and Private match modes
   - Passes NetworkRunner to combat scene via static reference
   - Sets `ComingFromLobby = true` when transitioning

2. **GameManager.cs** (UPDATED)
   - Detects if coming from lobby system
   - Spawns players using existing runner
   - Supports optional spawn points for 2-player battles

3. **UIGameMenu.cs** (UPDATED)
   - Detects lobby integration
   - Hides connection UI when coming from lobby
   - Only shows disconnect/back options during battle

### How Integration Works

```
┌─────────────────────────────────────────────────────────────┐
│                    LOBBY → COMBAT FLOW                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  MainMenuController (Scene 0: MainMenu)                      │
│       │                                                      │
│       ├─ Creates NetworkRunner                               │
│       ├─ Both players connect & ready up                     │
│       ├─ Sets: MainMenuController.ActiveRunner = runner      │
│       ├─ Sets: MainMenuController.ComingFromLobby = true     │
│       ├─ DontDestroyOnLoad(runner)                          │
│       └─ Calls: runner.LoadScene(combatSceneIndex)          │
│                                                              │
│       ↓                                                      │
│                                                              │
│  GameManager (Scene 1: ThirdPersonCharacter)                 │
│       │                                                      │
│       ├─ Checks: MainMenuController.ComingFromLobby          │
│       ├─ Uses existing runner (already connected)            │
│       ├─ Spawns player prefab for local player              │
│       └─ Clears: ComingFromLobby = false                    │
│                                                              │
│  UIGameMenu (Scene 1: ThirdPersonCharacter)                  │
│       │                                                      │
│       ├─ Detects: ComingFromLobby was true                   │
│       ├─ Uses: MainMenuController.ActiveRunner               │
│       ├─ Hides connection UI (already connected)             │
│       └─ Shows only: Disconnect, Back to Menu                │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Setup Instructions

#### Step 1: Set Up Main Menu Scene (Scene 0)

1. Remove or disable the old `UIMainMenu` component
2. Create a new GameObject called "MainMenuController"
3. Add `MainMenuController` component
4. Assign Runner Prefab from `Assets/Common/Runner.prefab`
5. Create UI panels and assign references (see UI structure above)
6. Set `Combat Scene Index = 1`

#### Step 2: Combat Scene Already Works (Scene 1)

The existing `GameManager` and `UIGameMenu` have been updated to:
- Detect when coming from lobby
- Use the existing NetworkRunner
- Skip connection UI (players already connected)

#### Step 3: Build Settings

Ensure scenes are in correct order:
```
0: Assets/00_MainMenu/00_MainMenu.unity
1: Assets/01_ThirdPersonCharacter/01_ThirdPersonCharacter.unity
```

### Optional: Add Spawn Points for 1v1 Battles

In the combat scene, you can set specific spawn locations:

1. Create empty GameObjects as spawn points
2. Position them on opposite sides of the arena
3. Assign to `GameManager.SpawnPoints[]` array
4. Player 1 spawns at SpawnPoints[0], Player 2 at SpawnPoints[1]

```csharp
// GameManager now supports:
public Transform[] SpawnPoints; // Assign 2 transforms for 1v1
```

### Legacy Flow Still Works

If players access the combat scene directly (not through lobby):
- `UIGameMenu` shows full connection UI
- Players can enter room name and connect as before
- `GameManager` spawns players normally

This maintains backwards compatibility with the existing flow.

