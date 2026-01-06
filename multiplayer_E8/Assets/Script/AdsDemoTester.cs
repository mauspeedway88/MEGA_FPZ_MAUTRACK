using UnityEngine;
using Starter.PlayFabIntegration;
using System;

/// <summary>
/// Demo script to test all ad reward scenarios
/// Add this to a GameObject in your scene and call the functions from UI buttons or console
/// </summary>
public class AdsDemoTester : MonoBehaviour
{
  [Header("Ad Button References")]
  [SerializeField] private RewardedAdsButton coinsAdButton;
  [SerializeField] private RewardedAdsButton elixirAdButton;
  [SerializeField] private RewardedAdsButton weapon1AdButton;
  [SerializeField] private RewardedAdsButton weapon2AdButton;

  private PlayFabGameIntegration integration;

  void Start()
  {
    integration = PlayFabGameIntegration.Instance;
    
    if (integration == null)
    {
      Debug.LogWarning("AdsDemoTester: PlayFabGameIntegration not found. Some features may not work.");
    }
  }

  #region Demo Functions

  /// <summary>
  /// Demo: Watch ad for 13 coins
  /// </summary>
  [ContextMenu("Demo: Watch Ad for Coins")]
  public void DemoCoinsAd()
  {
    Debug.Log("=== DEMO: Coins Ad ===");
    
    if (coinsAdButton == null)
    {
      Debug.LogError("Coins Ad Button not assigned!");
      return;
    }

    // Set reward type to coins
    coinsAdButton.SetRewardType(RewardType.Coins);
    
    // Show the ad
    if (coinsAdButton.IsAdReady())
    {
      Debug.Log("Showing coins ad...");
      coinsAdButton.ShowAd();
    }
    else
    {
      Debug.LogWarning("Coins ad not ready. Loading...");
      coinsAdButton.LoadAd();
      // For demo purposes, grant reward directly if in testing mode
      if (coinsAdButton.IsPlatformSupported() == false || !coinsAdButton.IsAdReady())
      {
        Debug.Log("Ad not ready - granting coins directly for demo");
        GrantCoinsDirectly();
      }
    }
  }

  /// <summary>
  /// Demo: Watch ad for First Special Weapon (direct unlock)
  /// </summary>
  [ContextMenu("Demo: Watch Ad for Weapon 1")]
  public void DemoWeapon1Ad()
  {
    Debug.Log("=== DEMO: First Special Weapon Ad ===");
    
    if (weapon1AdButton == null)
    {
      Debug.LogError("Weapon 1 Ad Button not assigned!");
      return;
    }

    // Set reward type to weapon 1
    weapon1AdButton.SetRewardType(RewardType.Weapon1);
    
    // Show the ad
    if (weapon1AdButton.IsAdReady())
    {
      Debug.Log("Showing weapon 1 ad...");
      weapon1AdButton.ShowAd();
    }
    else
    {
      Debug.LogWarning("Weapon 1 ad not ready. Granting directly for demo...");
      GrantWeapon1Directly();
    }
  }

  /// <summary>
  /// Demo: Watch ad for Second Special Weapon (direct unlock)
  /// </summary>
  [ContextMenu("Demo: Watch Ad for Weapon 2")]
  public void DemoWeapon2Ad()
  {
    Debug.Log("=== DEMO: Second Special Weapon Ad ===");
    
    if (weapon2AdButton == null)
    {
      Debug.LogError("Weapon 2 Ad Button not assigned!");
      return;
    }

    // Set reward type to weapon 2
    weapon2AdButton.SetRewardType(RewardType.Weapon2);
    
    // Show the ad
    if (weapon2AdButton.IsAdReady())
    {
      Debug.Log("Showing weapon 2 ad...");
      weapon2AdButton.ShowAd();
    }
    else
    {
      Debug.LogWarning("Weapon 2 ad not ready. Granting directly for demo...");
      GrantWeapon2Directly();
    }
  }

  /// <summary>
  /// Demo: Watch 3 ads to fill elixir bottle, then exchange for Third Special Weapon
  /// Each ad fills 33.33% (1/3) of the bottle
  /// </summary>
  [ContextMenu("Demo: Watch Ad for Elixir (1/3)")]
  public void DemoElixirAd()
  {
    Debug.Log("=== DEMO: Elixir Ad (Fill 1/3) ===");
    
    if (elixirAdButton == null)
    {
      Debug.LogError("Elixir Ad Button not assigned!");
      return;
    }

    // Set reward type to elixir
    elixirAdButton.SetRewardType(RewardType.Elixir);
    
    // Show the ad
    if (elixirAdButton.IsAdReady())
    {
      Debug.Log("Showing elixir ad...");
      elixirAdButton.ShowAd();
      
      // Listen for reward granted to check if bottle is full
      elixirAdButton.OnRewardGranted += OnElixirAdCompleted;
    }
    else
    {
      Debug.LogWarning("Elixir ad not ready. Granting elixir directly for demo...");
      GrantElixirDirectly();
    }
  }

  /// <summary>
  /// Demo: Exchange full elixir bottle for Third Special Weapon
  /// </summary>
  [ContextMenu("Demo: Exchange Elixir for Weapon 3")]
  public void DemoExchangeElixirForWeapon3()
  {
    Debug.Log("=== DEMO: Exchange Elixir Bottle for Third Special Weapon ===");
    
    if (integration == null)
    {
      Debug.LogError("PlayFabGameIntegration not available!");
      return;
    }

    float currentFill = integration.GetElixirFill();
    int fillPercent = integration.GetElixirFillPercent();
    
    Debug.Log($"Current elixir fill: {fillPercent}%");

    if (integration.IsElixirBottleFull())
    {
      bool success = integration.ExchangeElixirBottleForWeapon();
      if (success)
      {
        Debug.Log("✅ SUCCESS: Third Special Weapon unlocked!");
        Debug.Log($"Weapon 3 Unlocked: {integration.IsSpecialWeaponUnlocked()}");
      }
      else
      {
        Debug.LogError("Failed to exchange elixir bottle for weapon!");
      }
    }
    else
    {
      int adsNeeded = integration.GetAdsNeededToFillBottle();
      Debug.LogWarning($"Elixir bottle is not full! Current: {fillPercent}%");
      Debug.LogWarning($"Watch {adsNeeded} more ad(s) to fill the bottle.");
    }
  }

  #endregion

  #region Direct Grant Functions (for testing without ads)

  /// <summary>
  /// Grant coins directly (for testing)
  /// </summary>
  private void GrantCoinsDirectly()
  {
    if (integration != null)
    {
      integration.AddCoins(13);
      Debug.Log($"✅ Granted 13 coins directly. Total coins: {integration.GetCoins()}");
    }
    else
    {
      int currentCoins = PlayerPrefs.GetInt("Coins", 0);
      PlayerPrefs.SetInt("Coins", currentCoins + 13);
      PlayerPrefs.Save();
      Debug.Log($"✅ Granted 13 coins directly. Total coins: {currentCoins + 13}");
    }
  }

  /// <summary>
  /// Grant first special weapon directly (for testing)
  /// </summary>
  private void GrantWeapon1Directly()
  {
    if (integration != null)
    {
      integration.UnlockSpecialWeapon1();
      Debug.Log("✅ First Special Weapon unlocked!");
      Debug.Log($"Weapon 1 Status: {integration.IsSpecialWeapon1Unlocked()}");
    }
    else
    {
      PlayerPrefs.SetInt("SpecialWeapon1Unlocked", 1);
      PlayerPrefs.Save();
      Debug.Log("✅ First Special Weapon unlocked directly!");
      Debug.Log($"Weapon 1 Status: {PlayerPrefs.GetInt("SpecialWeapon1Unlocked", 0) == 1}");
    }
  }

  /// <summary>
  /// Grant second special weapon directly (for testing)
  /// </summary>
  private void GrantWeapon2Directly()
  {
    if (integration != null)
    {
      integration.UnlockSpecialWeapon2();
      Debug.Log("✅ Second Special Weapon unlocked!");
      Debug.Log($"Weapon 2 Status: {integration.IsSpecialWeapon2Unlocked()}");
    }
    else
    {
      PlayerPrefs.SetInt("SpecialWeapon2Unlocked", 1);
      PlayerPrefs.Save();
      Debug.Log("✅ Second Special Weapon unlocked directly!");
      Debug.Log($"Weapon 2 Status: {PlayerPrefs.GetInt("SpecialWeapon2Unlocked", 0) == 1}");
    }
  }

  /// <summary>
  /// Grant elixir fill directly (1/3 = 33.33% per ad, 3 ads = 100%)
  /// </summary>
  private void GrantElixirDirectly()
  {
    if (integration != null)
    {
      integration.AddElixirFill(0.3333f); // 1/3 per ad
      float fill = integration.GetElixirFill();
      int percent = integration.GetElixirFillPercent();
      int adsWatched = integration.GetAdsWatchedForElixir();
      
      Debug.Log($"✅ Granted 33.33% elixir fill. Current: {percent}% ({adsWatched}/3 ads)");
      
      if (integration.IsElixirBottleFull())
      {
        Debug.Log("🎉 Elixir bottle is FULL! You can now exchange it for the Third Special Weapon!");
      }
    }
    else
    {
      float currentFill = PlayerPrefs.GetFloat("ElixirFill", 0f);
      float newFill = Mathf.Min(currentFill + 0.3333f, 1f);
      PlayerPrefs.SetFloat("ElixirFill", newFill);
      PlayerPrefs.Save();
      
      int percent = Mathf.RoundToInt(newFill * 100);
      int adsWatched = Mathf.FloorToInt(newFill / 0.3333f);
      
      Debug.Log($"✅ Granted 33.33% elixir fill. Current: {percent}% ({adsWatched}/3 ads)");
      
      if (newFill >= 1f)
      {
        Debug.Log("🎉 Elixir bottle is FULL! You can now exchange it for the Third Special Weapon!");
      }
    }
  }

  #endregion

  #region Event Handlers

  private void OnElixirAdCompleted()
  {
    if (elixirAdButton != null)
    {
      elixirAdButton.OnRewardGranted -= OnElixirAdCompleted;
    }
    
    if (integration != null)
    {
      float fill = integration.GetElixirFill();
      int percent = integration.GetElixirFillPercent();
      int adsWatched = integration.GetAdsWatchedForElixir();
      
      Debug.Log($"Elixir fill updated: {percent}% ({adsWatched}/3 ads)");
      
      if (integration.IsElixirBottleFull())
      {
        Debug.Log("🎉 Elixir bottle is FULL! Call DemoExchangeElixirForWeapon3() to unlock Weapon 3!");
      }
      else
      {
        int adsNeeded = integration.GetAdsNeededToFillBottle();
        Debug.Log($"Watch {adsNeeded} more ad(s) to fill the bottle completely.");
      }
    }
  }

  #endregion

  #region Status Check Functions

  /// <summary>
  /// Display current status of all rewards and weapons
  /// </summary>
  [ContextMenu("Demo: Check Status")]
  public void CheckStatus()
  {
    Debug.Log("=== CURRENT STATUS ===");
    
    // Coins
    if (integration != null)
    {
      Debug.Log($"Coins: {integration.GetCoins()}");
    }
    else
    {
      Debug.Log($"Coins: {PlayerPrefs.GetInt("Coins", 0)}");
    }
    
    // Elixir
    if (integration != null)
    {
      float fill = integration.GetElixirFill();
      int percent = integration.GetElixirFillPercent();
      int adsWatched = integration.GetAdsWatchedForElixir();
      bool isFull = integration.IsElixirBottleFull();
      
      Debug.Log($"Elixir Fill: {percent}% ({adsWatched}/3 ads) - {(isFull ? "FULL" : "Not Full")}");
    }
    else
    {
      float fill = PlayerPrefs.GetFloat("ElixirFill", 0f);
      int percent = Mathf.RoundToInt(fill * 100);
      int adsWatched = Mathf.FloorToInt(fill / 0.3333f);
      bool isFull = fill >= 1f;
      
      Debug.Log($"Elixir Fill: {percent}% ({adsWatched}/3 ads) - {(isFull ? "FULL" : "Not Full")}");
    }
    
    // Weapons
    bool weapon1 = integration != null ? integration.IsSpecialWeapon1Unlocked() : 
                   PlayerPrefs.GetInt("SpecialWeapon1Unlocked", 0) == 1;
    bool weapon2 = integration != null ? integration.IsSpecialWeapon2Unlocked() : 
                   PlayerPrefs.GetInt("SpecialWeapon2Unlocked", 0) == 1;
    bool weapon3 = integration != null ? integration.IsSpecialWeaponUnlocked() : 
                   PlayerPrefs.GetInt("SpecialWeaponUnlocked", 0) == 1;
    
    Debug.Log($"Weapon 1 (First Special): {(weapon1 ? "UNLOCKED ✅" : "Locked ❌")}");
    Debug.Log($"Weapon 2 (Second Special): {(weapon2 ? "UNLOCKED ✅" : "Locked ❌")}");
    Debug.Log($"Weapon 3 (Third Special): {(weapon3 ? "UNLOCKED ✅" : "Locked ❌")}");
    
    Debug.Log("====================");
  }

  /// <summary>
  /// Reset all demo data (for testing)
  /// </summary>
  [ContextMenu("Demo: Reset All Data")]
  public void ResetAllData()
  {
    Debug.Log("=== RESETTING ALL DATA ===");
    
    // Reset coins
    PlayerPrefs.SetInt("Coins", 0);
    if (integration != null)
    {
      integration.AddCoins(-integration.GetCoins()); // Reset to 0
    }
    
    // Reset elixir
    PlayerPrefs.SetFloat("ElixirFill", 0f);
    if (integration != null)
    {
      // Reset elixir by setting it to 0
      float currentFill = integration.GetElixirFill();
      if (currentFill > 0)
      {
        // We need to manually reset since there's no direct set method
        PlayerPrefs.SetFloat("ElixirFill", 0f);
        PlayerPrefs.Save();
      }
    }
    
    // Reset weapons
    PlayerPrefs.SetInt("SpecialWeapon1Unlocked", 0);
    PlayerPrefs.SetInt("SpecialWeapon2Unlocked", 0);
    PlayerPrefs.SetInt("SpecialWeaponUnlocked", 0);
    PlayerPrefs.Save();
    
    Debug.Log("✅ All data reset!");
    CheckStatus();
  }

  #endregion

  #region UI Button Functions (Assign these to Unity UI Buttons)

  /// <summary>
  /// Button function: Watch ad for coins
  /// Assign this to a UI Button's onClick event
  /// </summary>
  public void OnCoinsAdButtonClicked()
  {
    DemoCoinsAd();
  }

  /// <summary>
  /// Button function: Watch ad for First Special Weapon
  /// Assign this to a UI Button's onClick event
  /// </summary>
  public void OnWeapon1AdButtonClicked()
  {
    DemoWeapon1Ad();
  }

  /// <summary>
  /// Button function: Watch ad for Second Special Weapon
  /// Assign this to a UI Button's onClick event
  /// </summary>
  public void OnWeapon2AdButtonClicked()
  {
    DemoWeapon2Ad();
  }

  /// <summary>
  /// Button function: Watch ad for Elixir (fills 1/3 of bottle)
  /// Assign this to a UI Button's onClick event
  /// </summary>
  public void OnElixirAdButtonClicked()
  {
    DemoElixirAd();
  }

  /// <summary>
  /// Button function: Exchange full elixir bottle for Third Special Weapon
  /// Assign this to a UI Button's onClick event
  /// </summary>
  public void OnExchangeElixirButtonClicked()
  {
    DemoExchangeElixirForWeapon3();
  }

  /// <summary>
  /// Button function: Check current status
  /// Assign this to a UI Button's onClick event
  /// </summary>
  public void OnCheckStatusButtonClicked()
  {
    CheckStatus();
  }

  /// <summary>
  /// Button function: Reset all data
  /// Assign this to a UI Button's onClick event
  /// </summary>
  public void OnResetDataButtonClicked()
  {
    ResetAllData();
  }

  #endregion
}
