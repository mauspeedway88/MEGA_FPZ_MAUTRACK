using UnityEngine;
using UnityEngine.UI;
using GoogleMobileAds.Api;
using Starter.Shooter;
using Starter.PlayFabIntegration;
using System;

public enum RewardType
{
  Coins,
  Elixir,
  Weapon1,  // First Special Weapon (direct unlock)
  Weapon2   // Second Special Weapon (direct unlock)
}

public class RewardedAdsButton : MonoBehaviour
{
  [Header("AdMob Configuration")]
  [SerializeField] string _androidAdUnitId = "ca-app-pub-6016513053121401/1716293301";
  [SerializeField] string _iOSAdUnitId = "ca-app-pub-6016513053121401/1716293301";
  string _adUnitId = null;

  [Header("Reward Configuration")]
  [SerializeField] RewardType rewardType = RewardType.Coins;
  [SerializeField] int coinRewardAmount = 100; // Coins per ad (Updated for Economy Balance)
  [SerializeField] float elixirFillAmount = 0.3333f; // Fill 1/3 (33.33%) of bottle per ad (3 ads to fill completely)

  [Header("References")]
  [SerializeField] Button adButton; // Optional: button to enable/disable based on ad availability

  // Events for UI updates
  public event Action OnRewardGranted;
  public event Action<RewardType, int> OnRewardGrantedWithDetails;

  private RewardedAd _rewardedAd;
  private bool _isAdLoaded = false;

  [Header("Unsupported Platform Settings")]
  [SerializeField] bool grantRewardsOnUnsupportedPlatforms = false; // For testing on Mac/Windows/WebGL

  private bool _isPlatformSupported = false;

  void Awake()
  {
    // AdMob only supports Android and iOS
    // Check platform support
    #if UNITY_IOS || UNITY_ANDROID
    _isPlatformSupported = true;
        // Get the Ad Unit ID for the current platform:
    #if UNITY_IOS
    _adUnitId = _iOSAdUnitId;
    #elif UNITY_ANDROID
        _adUnitId = _androidAdUnitId;
    #endif
    #elif UNITY_EDITOR || UNITY_STANDALONE || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
    // In editor AND STANDALONE (MAC/PC Build), enable support (Simulated)
    _isPlatformSupported = true;
    grantRewardsOnUnsupportedPlatforms = true; // Force this to true for Builds
    _adUnitId = _androidAdUnitId; // For testing
    #else
    // Unsupported platforms: WebGL, etc.
    _isPlatformSupported = false;
    _adUnitId = null;
    Debug.LogWarning($"RewardedAdsButton: AdMob is not supported on {Application.platform}. " +
                     $"Ads will be disabled. Set 'Grant Rewards On Unsupported Platforms' to true for testing.");
    #endif
  }

  void Start()
  {
        // [MODIFICATION] User requested to disable Weapon Ads
        if (rewardType == RewardType.Weapon1 || rewardType == RewardType.Weapon2)
        {
            if (adButton != null)
            {
                adButton.interactable = false;
                adButton.gameObject.SetActive(false); // Hide it completely
            }
            return;
        }

        // GUEST PROTECTION: Disable ads for guests
        var pm = Starter.PlayFabIntegration.PlayFabManager.Instance;
        // GUEST UPDATE: We now ALLOW guests to watch ads to get temporary coins for weapons
        // but we warn that these coins are not saved to the cloud.
        // var pm = Starter.PlayFabIntegration.PlayFabManager.Instance; // REMOVED DUPLICATE
        if (pm != null && pm.IsGuestSession)
        {
            Debug.Log("RewardedAdsButton: Guest Mode Active - Ads enabled for temporary session coins.");
        }

        if (adButton != null)
        {
            adButton.onClick.RemoveAllListeners();
            adButton.onClick.AddListener(ShowAd);
        }
        // Only load ads on supported platforms
        if (_isPlatformSupported)
    {
      LoadAd();
    }
    else if (grantRewardsOnUnsupportedPlatforms)
    {
            // For testing: simulate ad loaded on unsupported platforms
            _isAdLoaded = true;
      if (adButton != null)
      {
        adButton.interactable = true;
      }
      Debug.Log("RewardedAdsButton: Simulating ad ready on unsupported platform (testing mode)");
    }
    else
    {
      // Disable button on unsupported platforms
      if (adButton != null)
      {
        adButton.interactable = false;
      }
    }
  }

  /// <summary>
  /// Load a rewarded ad
  /// </summary>
  public void LoadAd()
  {
    // Don't load ads on unsupported platforms
    if (!_isPlatformSupported)
    {
      Debug.LogWarning("RewardedAdsButton: Cannot load ads on unsupported platform!");
      return;
    }

    if (string.IsNullOrEmpty(_adUnitId))
    {
      Debug.LogWarning("RewardedAdsButton: Ad Unit ID is not set!");
      return;
    }

    // Clean up the old ad before loading a new one
    if (_rewardedAd != null)
    {
      _rewardedAd.Destroy();
      _rewardedAd = null;
    }

    Debug.Log("Loading AdMob Rewarded Ad: " + _adUnitId);

    // Create our request used to load the ad
    var adRequest = new AdRequest();

    // Send the request to load the ad
    RewardedAd.Load(_adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
    {
      // If error is not null, the load request failed
      if (error != null || ad == null)
      {
        Debug.LogError($"RewardedAd failed to load an ad with error: {error?.GetMessage()}");
        _isAdLoaded = false;
        
        // Disable button if assigned
        if (adButton != null)
        {
          adButton.interactable = false;
        }
        
        // Retry loading after a delay
        Invoke(nameof(LoadAd), 5f);
        return;
      }

      Debug.Log("RewardedAd loaded with response: " + ad.GetResponseInfo());
      _rewardedAd = ad;
      _isAdLoaded = true;

      // Enable button if assigned
      if (adButton != null)
      {
        adButton.interactable = true;
      }

      // Register to ad events to extend functionality
      RegisterEventHandlers(ad);
    });
  }

  /// <summary>
  /// Show the rewarded ad
  /// </summary>
  public void ShowAd()
  {
    // Handle unsupported platforms
    if (!_isPlatformSupported)
    {
      if (grantRewardsOnUnsupportedPlatforms)
      {
        // For testing: grant reward directly without showing ad
        Debug.Log("RewardedAdsButton: Granting reward directly on unsupported platform (testing mode)");
        GrantReward();
        _isAdLoaded = false;
        // Simulate reload after a delay
        Invoke(nameof(SimulateAdReload), 1f);
        return;
      }
      else
      {
        Debug.LogWarning("RewardedAdsButton: Ads are not supported on this platform!");
        return;
      }
    }

    if (!_isAdLoaded || _rewardedAd == null)
    {
      Debug.LogWarning("RewardedAdsButton: Ad is not loaded yet. Loading now...");
      LoadAd();
      return;
    }

    if (_rewardedAd.CanShowAd())
    {
      Debug.Log("Showing AdMob Rewarded Ad: " + _adUnitId);
      _rewardedAd.Show((Reward reward) =>
      {
        Debug.Log($"RewardedAd granted reward: {reward.Type} - {reward.Amount}");
        GrantReward();
      });
    }
    else
    {
      Debug.LogWarning("RewardedAdsButton: Ad is not ready to show!");
      LoadAd();
    }
  }

  /// <summary>
  /// Register event handlers for the rewarded ad
  /// </summary>
  private void RegisterEventHandlers(RewardedAd ad)
  {
    // Raised when the ad is estimated to have earned money
    ad.OnAdPaid += (AdValue adValue) =>
    {
      Debug.Log($"RewardedAd paid {adValue.Value} {adValue.CurrencyCode}.");
    };

    // Raised when an impression is recorded for an ad
    ad.OnAdImpressionRecorded += () =>
    {
      Debug.Log("RewardedAd recorded an impression.");
    };

    // Raised when a click is recorded for an ad
    ad.OnAdClicked += () =>
    {
      Debug.Log("RewardedAd was clicked.");
    };

    // Raised when an ad opened full screen content
    ad.OnAdFullScreenContentOpened += () =>
    {
      Debug.Log("RewardedAd full screen content opened.");
      // Disable button while ad is showing
      if (adButton != null)
      {
        adButton.interactable = false;
      }
    };

    // Raised when the ad closed full screen content
    ad.OnAdFullScreenContentClosed += () =>
    {
      Debug.Log("RewardedAd full screen content closed.");
      // Reload ad for next use
      _isAdLoaded = false;
      LoadAd();
    };

    // Raised when the ad failed to open full screen content
    ad.OnAdFullScreenContentFailed += (AdError error) =>
  {
      Debug.LogError($"RewardedAd failed to open full screen content with error: {error.GetMessage()}");
      // Reload ad
      _isAdLoaded = false;
      LoadAd();
    };
  }

  /// <summary>
  /// Simulate ad reload for testing on unsupported platforms
  /// </summary>
  private void SimulateAdReload()
  {
    _isAdLoaded = true;
    if (adButton != null)
    {
      adButton.interactable = true;
    }
  }

  /// <summary>
  /// Grant the reward based on the reward type
  /// </summary>
  private void GrantReward()
  {
    switch (rewardType)
    {
      case RewardType.Coins:
        GrantCoinsReward();
        break;
      case RewardType.Elixir:
        GrantElixirReward();
        break;
      case RewardType.Weapon1:
        GrantWeapon1Reward();
        break;
      case RewardType.Weapon2:
        GrantWeapon2Reward();
        break;
    }

    // Trigger events
    OnRewardGranted?.Invoke();
    int rewardValue = rewardType == RewardType.Coins ? coinRewardAmount : 
                     rewardType == RewardType.Elixir ? Mathf.RoundToInt(elixirFillAmount * 100) : 1;
    OnRewardGrantedWithDetails?.Invoke(rewardType, rewardValue);
  }

  /// <summary>
  /// Grant coins reward
  /// </summary>
  private void GrantCoinsReward()
  {
    // Try to use PlayFab integration if available
    var playFabIntegration = PlayFabGameIntegration.Instance;
    if (playFabIntegration != null)
  {
      playFabIntegration.AddCoins(coinRewardAmount);
      Debug.Log($"Rewarded {coinRewardAmount} coins via PlayFab");
    }
    else
    {
      // Fallback to PlayerPrefs
      int currentCoins = PlayerPrefs.GetInt("Coins", 0);
      PlayerPrefs.SetInt("Coins", currentCoins + coinRewardAmount);
      PlayerPrefs.Save();
      Debug.Log($"Rewarded {coinRewardAmount} coins via PlayerPrefs. Total: {currentCoins + coinRewardAmount}");
    }
  }

  /// <summary>
  /// Grant elixir reward (fill elixir bottle incrementally)
  /// </summary>
  private void GrantElixirReward()
  {
    // Try to use PlayFab integration if available
    var playFabIntegration = PlayFabGameIntegration.Instance;
    if (playFabIntegration != null)
    {
      playFabIntegration.AddElixirFill(elixirFillAmount);
      float currentFill = playFabIntegration.GetElixirFill();
      Debug.Log($"Added {elixirFillAmount * 100}% elixir fill via PlayFab. Current fill: {currentFill * 100}%");
    }
    else
    {
      // Fallback to PlayerPrefs
      float currentFill = PlayerPrefs.GetFloat("ElixirFill", 0f);
      float newFill = Mathf.Min(currentFill + elixirFillAmount, 1f); // Cap at 100%
      PlayerPrefs.SetFloat("ElixirFill", newFill);
      PlayerPrefs.Save();
      Debug.Log($"Added {elixirFillAmount * 100}% elixir fill via PlayerPrefs. Current fill: {newFill * 100}%");
    }
  }

  /// <summary>
  /// Grant First Special Weapon reward (direct unlock)
  /// </summary>
  private void GrantWeapon1Reward()
  {
    var playFabIntegration = PlayFabGameIntegration.Instance;
    if (playFabIntegration != null)
    {
      playFabIntegration.UnlockSpecialWeapon1();
      Debug.Log("First Special Weapon unlocked via PlayFab!");
    }
    else
    {
      PlayerPrefs.SetInt("SpecialWeapon1Unlocked", 1);
      PlayerPrefs.Save();
      Debug.Log("First Special Weapon unlocked via PlayerPrefs!");
    }
  }

  /// <summary>
  /// Grant Second Special Weapon reward (direct unlock)
  /// </summary>
  private void GrantWeapon2Reward()
  {
    var playFabIntegration = PlayFabGameIntegration.Instance;
    if (playFabIntegration != null)
    {
      playFabIntegration.UnlockSpecialWeapon2();
      Debug.Log("Second Special Weapon unlocked via PlayFab!");
    }
    else
    {
      PlayerPrefs.SetInt("SpecialWeapon2Unlocked", 1);
      PlayerPrefs.Save();
      Debug.Log("Second Special Weapon unlocked via PlayerPrefs!");
    }
  }

  /// <summary>
  /// Check if ad is ready to show
  /// </summary>
  public bool IsAdReady()
  {
    // On unsupported platforms with testing mode, return true if enabled
    if (!_isPlatformSupported)
    {
      return grantRewardsOnUnsupportedPlatforms && _isAdLoaded;
    }

    return _isAdLoaded && _rewardedAd != null && _rewardedAd.CanShowAd();
  }

  /// <summary>
  /// Check if current platform supports AdMob
  /// </summary>
  public bool IsPlatformSupported()
  {
    return _isPlatformSupported;
  }

  /// <summary>
  /// Set reward type programmatically
  /// </summary>
  public void SetRewardType(RewardType type)
  {
    rewardType = type;
  }

  void OnDestroy()
  {
    // Clean up the ad when the object is destroyed
    if (_rewardedAd != null)
    {
      _rewardedAd.Destroy();
    }
  }
}
