# PlayFab Integration Setup Guide

This guide explains how to set up and configure PlayFab for your MultiplayerFusion game.

## Prerequisites

1. **PlayFab Account**: Create a free account at [PlayFab.com](https://playfab.com)
2. **PlayFab SDK**: Install the PlayFab Unity SDK

## Step 1: Install PlayFab SDK

### Option A: Unity Package Manager (Recommended)
1. Open Unity
2. Go to **Window > Package Manager**
3. Click **+** button > **Add package from git URL**
4. Enter: `https://github.com/PlayFab/UnitySDK.git?path=/Source/PlayFabSDK`
5. Click **Add**

### Option B: Asset Store
1. Go to Unity Asset Store
2. Search for "PlayFab SDK"
3. Download and import the package

### Option C: Manual Download
1. Download from [PlayFab Unity SDK Releases](https://github.com/PlayFab/UnitySDK/releases)
2. Import the `.unitypackage` file

## Step 2: Get Your PlayFab Title ID

1. Log into [PlayFab Game Manager](https://developer.playfab.com)
2. Create a new title or select existing one
3. Go to **Settings > API Features**
4. Copy your **Title ID** (looks like: "XXXXX")

## Step 3: Configure Unity Project

### Method 1: Using PlayFabSetup Component
1. Open your **00_MainMenu** scene
2. Create empty GameObject named "PlayFabSetup"
3. Add the `PlayFabSetup` component
4. Enter your **Title ID** in the inspector

### Method 2: Edit PlayFabManager.cs
1. Open `Assets/Script/PlayFab/PlayFabManager.cs`
2. Find line: `[SerializeField] private string titleId = "YOUR_TITLE_ID";`
3. Replace `YOUR_TITLE_ID` with your actual Title ID

## Step 4: Configure PlayFab Dashboard

### Enable Email/Password Authentication
1. In PlayFab Game Manager, go to **Settings > Authentication**
2. Enable **Email and Password** authentication

### Create Statistics for Leaderboard
1. Go to **Leaderboards > Statistics**
2. Click **New Statistic**
3. Create statistic named: `Rankings`
   - Aggregation: Max (or Sum depending on your needs)
   - Reset frequency: Never (or your preference)

### Configure Player Data Permissions
1. Go to **Settings > API Features**
2. Ensure **Allow client to post player statistics** is enabled
3. Ensure **Client access to player profile** is enabled

## Step 5: Set Up UI in Unity

### Main Menu Scene (00_MainMenu)

1. **Add PlayFabAuthUI Component**:
   - Create a Canvas or use existing
   - Add `PlayFabAuthUI` component
   - Connect UI elements (see UI Setup below)

2. **Connect to MainMenuController**:
   - Reference `PlayFabAuthUI` in the MainMenuController
   - Connect profile, leaderboard buttons

### Required UI Elements for Authentication

```
AuthPanel (Panel)
├── LoginPanel
│   ├── EmailInput (TMP_InputField)
│   ├── PasswordInput (TMP_InputField)
│   ├── LoginButton (Button)
│   ├── GuestLoginButton (Button)
│   ├── GoToRegisterButton (Button)
│   └── LoginErrorText (TextMeshProUGUI)
├── RegisterPanel
│   ├── EmailInput (TMP_InputField)
│   ├── PasswordInput (TMP_InputField)
│   ├── ConfirmPasswordInput (TMP_InputField)
│   ├── DisplayNameInput (TMP_InputField)
│   ├── ReferralCodeInput (TMP_InputField)
│   ├── RegisterButton (Button)
│   ├── GoToLoginButton (Button)
│   └── RegisterErrorText (TextMeshProUGUI)
├── LoadingPanel
│   └── LoadingText (TextMeshProUGUI)
└── ProfilePanel
    ├── NameText (TextMeshProUGUI)
    ├── EmailText (TextMeshProUGUI)
    ├── RankingText (TextMeshProUGUI)
    ├── CoinsText (TextMeshProUGUI)
    ├── ReferralCodeText (TextMeshProUGUI)
    ├── WinRateText (TextMeshProUGUI)
    ├── CopyReferralButton (Button)
    ├── LogoutButton (Button)
    └── CloseProfileButton (Button)
```

### Leaderboard UI

```
LeaderboardPanel (Panel)
├── TitleText (TextMeshProUGUI)
├── EntriesContainer (VerticalLayoutGroup)
├── EntryPrefab (see below)
├── RefreshButton (Button)
├── CloseButton (Button)
├── StatusText (TextMeshProUGUI)
├── LoadingIndicator (GameObject)
└── PlayerRankPanel
    ├── PlayerRankText (TextMeshProUGUI)
    └── PlayerScoreText (TextMeshProUGUI)
```

### Leaderboard Entry Prefab
```
LeaderboardEntry (Prefab)
├── Background (Image)
├── RankText (TextMeshProUGUI)
├── NameText (TextMeshProUGUI)
└── ScoreText (TextMeshProUGUI)
```

## Step 6: Test the Integration

1. Enter Play Mode
2. You should see the login panel
3. Try:
   - Guest login
   - Register with email
   - Login with email
4. Check PlayFab Dashboard to see registered players

## Data Synced with PlayFab

| Field | Description | Storage Key |
|-------|-------------|-------------|
| Email | Player's email address | `Email` |
| PlayerName | Display name | `PlayerName` |
| ReferralCode | Unique referral code | `ReferralCode` |
| Ranking | Player's ranking score | `Ranking` (Statistic) |
| Coins | In-game currency | `Coins` |
| TotalGamesPlayed | Total matches played | `TotalGamesPlayed` |
| TotalWins | Total wins | `TotalWins` |

## API Usage

### From Any Script

```csharp
using Starter.PlayFabIntegration;

// Check login status
if (PlayFabManager.Instance.IsLoggedIn)
{
    // Access player data
    var data = PlayFabManager.Instance.CurrentPlayerData;
    Debug.Log($"Player: {data.PlayerName}, Coins: {data.Coins}");
}

// Add coins
PlayFabGameIntegration.Instance.AddCoins(100);

// Report game result
PlayFabGameIntegration.Instance.ReportGameResult(won: true, chickenKills: 5);

// Get leaderboard
PlayFabManager.Instance.GetLeaderboard("Rankings", 100, entries => {
    foreach (var entry in entries)
    {
        Debug.Log($"#{entry.Position}: {entry.DisplayName} - {entry.Score}");
    }
});
```

## Troubleshooting

### "PlayFab SDK not found" Error
- Make sure you've installed the PlayFab SDK (Step 1)
- Check if `using PlayFab;` and `using PlayFab.ClientModels;` compile without errors

### "Title ID not set" Warning
- Configure your Title ID in PlayFabSetup or PlayFabManager

### Login Fails with "Invalid Title ID"
- Verify your Title ID in PlayFab Dashboard
- Make sure there are no extra spaces

### Statistics/Leaderboard Not Updating
- Check that the statistic exists in PlayFab Dashboard
- Ensure "Allow client to post player statistics" is enabled

### Player Data Not Saving
- Check the PlayFab Dashboard > Players section
- Verify the player's data in Player Data tab

## Support

- [PlayFab Documentation](https://docs.microsoft.com/en-us/gaming/playfab/)
- [PlayFab Forums](https://community.playfab.com/)
- [Unity SDK GitHub](https://github.com/PlayFab/UnitySDK)

