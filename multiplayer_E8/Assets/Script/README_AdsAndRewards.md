# Ads and Rewards System - Usage Guide

This guide explains how to use the Unity Ads rewarded video system with coins and elixir bottle rewards.

## Table of Contents
- [Overview](#overview)
- [Setup](#setup)
- [Coins Rewards](#coins-rewards)
- [Elixir Bottle System](#elixir-bottle-system)
- [Special Weapon Exchange](#special-weapon-exchange)
- [Code Examples](#code-examples)
- [Unity Inspector Configuration](#unity-inspector-configuration)
- [Events and Callbacks](#events-and-callbacks)

---

## Overview

The reward system supports two types of rewards:

1. **Coins**: Each ad watched grants **13 coins** immediately
2. **Elixir Bottle**: Each ad watched fills **20% (1/5)** of the bottle. After **5 ads**, the bottle is full and can be exchanged for a **special weapon** (only obtainable this way).

---

## Setup

### 1. Initialize Ads

Make sure you have an `AdsInitializer` component in your scene that initializes Unity Ads on startup.

```csharp
// AdsInitializer.cs handles this automatically in Awake()
// Just add the component to a GameObject in your scene
```

### 2. Configure Ad Unit IDs

In the Unity Inspector, set your Ad Unit IDs:
- **Android Ad Unit ID**: Your Android rewarded ad unit ID
- **iOS Ad Unit ID**: Your iOS rewarded ad unit ID

---

## Coins Rewards

### Unity Inspector Setup

1. Add `RewardedAdsButton` component to a GameObject (e.g., a button)
2. Set **Reward Type** to `Coins`
3. Set **Coin Reward Amount** to `13` (default)
4. Optionally assign a Button reference to enable/disable it based on ad availability

### Basic Usage

```csharp
using UnityEngine;
using UnityEngine.UI;

public class CoinsAdButton : MonoBehaviour
{
    [SerializeField] private RewardedAdsButton rewardedAdsButton;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Text coinsDisplayText;

    private PlayFabGameIntegration playFabIntegration;

    void Start()
    {
        playFabIntegration = PlayFabGameIntegration.Instance;
        
        // Update coins display
        UpdateCoinsDisplay();
        
        // Listen to reward events
        if (rewardedAdsButton != null)
        {
            rewardedAdsButton.OnRewardGranted += OnCoinsRewarded;
        }
        
        // Setup button click
        if (watchAdButton != null)
        {
            watchAdButton.onClick.AddListener(OnWatchAdClicked);
        }
    }

    void OnWatchAdClicked()
    {
        if (rewardedAdsButton != null && rewardedAdsButton.IsAdReady())
        {
            rewardedAdsButton.ShowAd();
        }
        else
        {
            Debug.LogWarning("Ad is not ready yet!");
        }
    }

    void OnCoinsRewarded()
    {
        Debug.Log("Coins rewarded! Updating display...");
        UpdateCoinsDisplay();
        
        // Show reward notification
        ShowRewardNotification("+13 Coins!");
    }

    void UpdateCoinsDisplay()
    {
        if (playFabIntegration != null)
        {
            int coins = playFabIntegration.GetCoins();
            if (coinsDisplayText != null)
            {
                coinsDisplayText.text = $"Coins: {coins}";
            }
        }
    }

    void ShowRewardNotification(string message)
    {
        // Implement your notification UI here
        Debug.Log(message);
    }
}
```

### Advanced: Listen to Coins Changed Event

```csharp
void Start()
{
    var integration = PlayFabGameIntegration.Instance;
    if (integration != null)
    {
        integration.OnCoinsChanged += OnCoinsChanged;
    }
}

void OnCoinsChanged(int newCoins)
{
    Debug.Log($"Coins updated: {newCoins}");
    // Update UI, play sound, etc.
}
```

---

## Elixir Bottle System

### How It Works

- Each rewarded ad fills **20% (1/5)** of the elixir bottle
- After **5 ads**, the bottle is **100% full**
- When full, player can **exchange** the bottle for a **special weapon**
- The special weapon can **only be obtained** by exchanging a full elixir bottle

### Unity Inspector Setup

1. Add `RewardedAdsButton` component to a GameObject
2. Set **Reward Type** to `Elixir`
3. Set **Elixir Fill Amount** to `0.2` (20% = 1/5, default)
4. Optionally assign a Button reference

### Basic Usage

```csharp
using UnityEngine;
using UnityEngine.UI;
using Starter.PlayFabIntegration;

public class ElixirAdButton : MonoBehaviour
{
    [SerializeField] private RewardedAdsButton rewardedAdsButton;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Image elixirFillBar; // UI Image with Image Type = Filled
    [SerializeField] private Text elixirProgressText;

    private PlayFabGameIntegration playFabIntegration;

    void Start()
    {
        playFabIntegration = PlayFabGameIntegration.Instance;
        
        // Update elixir display
        UpdateElixirDisplay();
        
        // Listen to reward events
        if (rewardedAdsButton != null)
        {
            rewardedAdsButton.OnRewardGranted += OnElixirRewarded;
        }
        
        // Setup button click
        if (watchAdButton != null)
        {
            watchAdButton.onClick.AddListener(OnWatchAdClicked);
        }
    }

    void Update()
    {
        // Update display every frame (or use events for better performance)
        UpdateElixirDisplay();
    }

    void OnWatchAdClicked()
    {
        if (rewardedAdsButton != null && rewardedAdsButton.IsAdReady())
        {
            rewardedAdsButton.ShowAd();
        }
    }

    void OnElixirRewarded()
    {
        Debug.Log("Elixir fill added! Updating display...");
        UpdateElixirDisplay();
        
        // Check if bottle is now full
        if (playFabIntegration != null && playFabIntegration.IsElixirBottleFull())
        {
            ShowBottleFullNotification();
        }
    }

    void UpdateElixirDisplay()
    {
        if (playFabIntegration == null) return;

        float fill = playFabIntegration.GetElixirFill();
        int percent = playFabIntegration.GetElixirFillPercent();
        int adsWatched = playFabIntegration.GetAdsWatchedForElixir();
        int adsNeeded = playFabIntegration.GetAdsNeededToFillBottle();

        // Update fill bar
        if (elixirFillBar != null)
        {
            elixirFillBar.fillAmount = fill;
        }

        // Update progress text
        if (elixirProgressText != null)
        {
            elixirProgressText.text = $"{percent}% ({adsWatched}/5 ads)";
        }
    }

    void ShowBottleFullNotification()
    {
        // Show notification that bottle is full and can be exchanged
        Debug.Log("Elixir bottle is FULL! You can exchange it for a special weapon!");
        // Implement your notification UI here
    }
}
```

---

## Special Weapon Exchange

### Exchange Full Bottle for Weapon

When the elixir bottle is full (100%), the player can exchange it for a special weapon. This weapon can **only be obtained** this way.

### Example: Exchange UI

```csharp
using UnityEngine;
using UnityEngine.UI;
using Starter.PlayFabIntegration;

public class ElixirExchangeUI : MonoBehaviour
{
    [SerializeField] private Button exchangeButton;
    [SerializeField] private GameObject exchangePanel;
    [SerializeField] private Text exchangeButtonText;

    private PlayFabGameIntegration playFabIntegration;

    void Start()
    {
        playFabIntegration = PlayFabGameIntegration.Instance;
        
        if (exchangeButton != null)
        {
            exchangeButton.onClick.AddListener(OnExchangeClicked);
        }

        // Listen to elixir changes
        if (playFabIntegration != null)
        {
            playFabIntegration.OnElixirFillChanged += OnElixirFillChanged;
            playFabIntegration.OnSpecialWeaponUnlocked += OnSpecialWeaponUnlocked;
        }

        UpdateExchangeButton();
    }

    void OnElixirFillChanged(float newFill)
    {
        UpdateExchangeButton();
    }

    void UpdateExchangeButton()
    {
        if (playFabIntegration == null) return;

        bool isFull = playFabIntegration.IsElixirBottleFull();
        bool alreadyUnlocked = playFabIntegration.IsSpecialWeaponUnlocked();

        if (exchangeButton != null)
        {
            exchangeButton.interactable = isFull && !alreadyUnlocked;
        }

        if (exchangeButtonText != null)
        {
            if (alreadyUnlocked)
            {
                exchangeButtonText.text = "Special Weapon Unlocked!";
            }
            else if (isFull)
            {
                exchangeButtonText.text = "Exchange for Special Weapon";
            }
            else
            {
                int adsNeeded = playFabIntegration.GetAdsNeededToFillBottle();
                exchangeButtonText.text = $"Watch {adsNeeded} more ad(s)";
            }
        }
    }

    void OnExchangeClicked()
    {
        if (playFabIntegration == null) return;

        if (!playFabIntegration.IsElixirBottleFull())
        {
            Debug.LogWarning("Bottle is not full yet!");
            return;
        }

        if (playFabIntegration.IsSpecialWeaponUnlocked())
        {
            Debug.LogWarning("Special weapon already unlocked!");
            return;
        }

        // Exchange bottle for weapon
        bool success = playFabIntegration.ExchangeElixirBottleForWeapon();
        
        if (success)
        {
            Debug.Log("Special weapon unlocked!");
            OnSpecialWeaponUnlocked();
            
            // Unlock weapon in your game system
            UnlockSpecialWeapon();
        }
        else
        {
            Debug.LogError("Failed to exchange elixir bottle!");
        }
    }

    void OnSpecialWeaponUnlocked()
    {
        Debug.Log("Special weapon unlocked event fired!");
        UpdateExchangeButton();
        
        // Show celebration UI, play sound, etc.
        ShowWeaponUnlockedCelebration();
    }

    void UnlockSpecialWeapon()
    {
        // Implement your weapon unlocking logic here
        // For example:
        // - Add weapon to player's inventory
        // - Unlock weapon in weapon selection menu
        // - Save weapon unlock status
        
        Debug.Log("Special weapon added to inventory!");
    }

    void ShowWeaponUnlockedCelebration()
    {
        // Show celebration UI, play sound, show weapon preview, etc.
        if (exchangePanel != null)
        {
            exchangePanel.SetActive(true);
        }
    }
}
```

---

## Code Examples

### Complete Example: Combined Coins and Elixir System

```csharp
using UnityEngine;
using UnityEngine.UI;
using Starter.PlayFabIntegration;

public class RewardsManager : MonoBehaviour
{
    [Header("Ad Buttons")]
    [SerializeField] private RewardedAdsButton coinsAdButton;
    [SerializeField] private RewardedAdsButton elixirAdButton;

    [Header("UI References")]
    [SerializeField] private Text coinsText;
    [SerializeField] private Image elixirFillBar;
    [SerializeField] private Text elixirProgressText;
    [SerializeField] private Button exchangeButton;

    private PlayFabGameIntegration integration;

    void Start()
    {
        integration = PlayFabGameIntegration.Instance;

        // Setup coins ad button
        if (coinsAdButton != null)
        {
            coinsAdButton.SetRewardType(RewardType.Coins);
            coinsAdButton.OnRewardGranted += OnCoinsRewarded;
        }

        // Setup elixir ad button
        if (elixirAdButton != null)
        {
            elixirAdButton.SetRewardType(RewardType.Elixir);
            elixirAdButton.OnRewardGranted += OnElixirRewarded;
        }

        // Setup exchange button
        if (exchangeButton != null)
        {
            exchangeButton.onClick.AddListener(OnExchangeClicked);
        }

        // Listen to integration events
        if (integration != null)
        {
            integration.OnCoinsChanged += UpdateCoinsDisplay;
            integration.OnElixirFillChanged += UpdateElixirDisplay;
            integration.OnSpecialWeaponUnlocked += OnWeaponUnlocked;
        }

        // Initial UI update
        UpdateCoinsDisplay(integration != null ? integration.GetCoins() : 0);
        UpdateElixirDisplay(integration != null ? integration.GetElixirFill() : 0f);
    }

    void OnCoinsRewarded()
    {
        Debug.Log("+13 Coins!");
        // Show notification, play sound, etc.
    }

    void OnElixirRewarded()
    {
        float fill = integration != null ? integration.GetElixirFill() : 0f;
        Debug.Log($"Elixir fill: {fill * 100}%");
        
        if (integration != null && integration.IsElixirBottleFull())
        {
            Debug.Log("Bottle is FULL! Ready to exchange!");
        }
    }

    void UpdateCoinsDisplay(int coins)
    {
        if (coinsText != null)
        {
            coinsText.text = $"Coins: {coins}";
        }
    }

    void UpdateElixirDisplay(float fill)
    {
        if (elixirFillBar != null)
        {
            elixirFillBar.fillAmount = fill;
        }

        if (elixirProgressText != null && integration != null)
        {
            int percent = integration.GetElixirFillPercent();
            int adsWatched = integration.GetAdsWatchedForElixir();
            elixirProgressText.text = $"{percent}% ({adsWatched}/5)";
        }

        // Update exchange button state
        if (exchangeButton != null && integration != null)
        {
            exchangeButton.interactable = integration.IsElixirBottleFull() && 
                                         !integration.IsSpecialWeaponUnlocked();
        }
    }

    void OnExchangeClicked()
    {
        if (integration == null) return;

        bool success = integration.ExchangeElixirBottleForWeapon();
        if (success)
        {
            Debug.Log("Special weapon unlocked!");
            // Handle weapon unlock in your game
        }
    }

    void OnWeaponUnlocked()
    {
        Debug.Log("Weapon unlocked event received!");
        // Show celebration, unlock weapon in game, etc.
    }
}
```

### Check Elixir Status

```csharp
void CheckElixirStatus()
{
    var integration = PlayFabGameIntegration.Instance;
    if (integration == null) return;

    float fill = integration.GetElixirFill(); // 0.0 to 1.0
    int percent = integration.GetElixirFillPercent(); // 0 to 100
    int adsWatched = integration.GetAdsWatchedForElixir(); // 0 to 5
    int adsNeeded = integration.GetAdsNeededToFillBottle(); // 0 to 5
    bool isFull = integration.IsElixirBottleFull(); // true when 100%
    bool weaponUnlocked = integration.IsSpecialWeaponUnlocked();

    Debug.Log($"Elixir Fill: {percent}%");
    Debug.Log($"Ads Watched: {adsWatched}/5");
    Debug.Log($"Ads Needed: {adsNeeded}");
    Debug.Log($"Bottle Full: {isFull}");
    Debug.Log($"Weapon Unlocked: {weaponUnlocked}");
}
```

---

## Unity Inspector Configuration

### RewardedAdsButton Component

| Property | Description | Default |
|----------|-------------|---------|
| **Android Ad Unit ID** | Your Android rewarded ad unit ID | "Rewarded_Android" |
| **iOS Ad Unit ID** | Your iOS rewarded ad unit ID | "Rewarded_iOS" |
| **Reward Type** | Type of reward: `Coins` or `Elixir` | Coins |
| **Coin Reward Amount** | Coins granted per ad | 13 |
| **Elixir Fill Amount** | Fill percentage per ad (0.0 to 1.0) | 0.2 (20%) |
| **Ad Button** | Optional Button reference for auto enable/disable | None |

### Setup Steps

1. **Create GameObject** for your ad button
2. **Add Component** → `RewardedAdsButton`
3. **Configure** reward type and amounts
4. **Assign Button** reference (optional, for auto state management)
5. **Set Ad Unit IDs** in Inspector

---

## Events and Callbacks

### RewardedAdsButton Events

```csharp
// Fired when reward is granted
rewardedAdsButton.OnRewardGranted += () => {
    Debug.Log("Reward granted!");
};

// Fired with reward details
rewardedAdsButton.OnRewardGrantedWithDetails += (rewardType, amount) => {
    Debug.Log($"Reward: {rewardType}, Amount: {amount}");
};
```

### PlayFabGameIntegration Events

```csharp
// Coins changed
integration.OnCoinsChanged += (newCoins) => {
    Debug.Log($"Coins: {newCoins}");
};

// Elixir fill changed
integration.OnElixirFillChanged += (newFill) => {
    Debug.Log($"Elixir fill: {newFill * 100}%");
};

// Special weapon unlocked
integration.OnSpecialWeaponUnlocked += () => {
    Debug.Log("Special weapon unlocked!");
};
```

---

## Best Practices

1. **Always check if ad is ready** before showing:
   ```csharp
   if (rewardedAdsButton.IsAdReady())
   {
       rewardedAdsButton.ShowAd();
   }
   ```

2. **Update UI using events** instead of polling:
   ```csharp
   integration.OnCoinsChanged += UpdateCoinsUI;
   integration.OnElixirFillChanged += UpdateElixirUI;
   ```

3. **Handle ad loading failures** gracefully:
   - The system automatically retries after 5 seconds
   - Disable buttons when ads aren't ready

4. **Show feedback** when rewards are granted:
   - Display notification ("+13 Coins!", "+20% Elixir!")
   - Play sound effects
   - Animate UI elements

5. **Prevent multiple exchanges**:
   - Check `IsSpecialWeaponUnlocked()` before allowing exchange
   - Disable exchange button after weapon is unlocked

---

## Troubleshooting

### Ad not loading?
- Check that `AdsInitializer` is in the scene and initialized
- Verify Ad Unit IDs are correct
- Check Unity Ads dashboard for ad unit status
- Enable test mode during development

### Rewards not granted?
- Ensure ad is fully watched (not skipped)
- Check that `OnUnityAdsShowComplete` is called with `COMPLETED` state
- Verify reward type is set correctly in Inspector

### Elixir not filling?
- Check that reward type is set to `Elixir`
- Verify `Elixir Fill Amount` is 0.2 (20%)
- Check PlayerPrefs for "ElixirFill" value

### Weapon not unlocking?
- Ensure bottle is 100% full (`IsElixirBottleFull()` returns true)
- Check that weapon isn't already unlocked
- Verify `ExchangeElixirBottleForWeapon()` returns true

---

## API Reference

### RewardedAdsButton

| Method | Description |
|--------|-------------|
| `LoadAd()` | Load an ad (called automatically on Start) |
| `ShowAd()` | Show the loaded ad |
| `IsAdReady()` | Check if ad is loaded and ready |
| `SetRewardType(RewardType)` | Set reward type programmatically |

### PlayFabGameIntegration

| Method | Description |
|--------|-------------|
| `GetCoins()` | Get current coins |
| `AddCoins(int)` | Add coins |
| `GetElixirFill()` | Get elixir fill (0.0 to 1.0) |
| `GetElixirFillPercent()` | Get elixir fill as percentage (0-100) |
| `AddElixirFill(float)` | Add elixir fill (0.0 to 1.0) |
| `IsElixirBottleFull()` | Check if bottle is 100% full |
| `GetAdsWatchedForElixir()` | Get number of ads watched (0-5) |
| `GetAdsNeededToFillBottle()` | Get remaining ads needed |
| `ExchangeElixirBottleForWeapon()` | Exchange full bottle for weapon |
| `IsSpecialWeaponUnlocked()` | Check if special weapon is unlocked |

---

## Support

For issues or questions:
- Check Unity Ads documentation
- Review PlayFab integration docs
- Check console logs for error messages

---

**Last Updated**: 2024
**Version**: 1.0
