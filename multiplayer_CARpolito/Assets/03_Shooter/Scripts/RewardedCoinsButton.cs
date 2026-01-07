using UnityEngine;
using UnityEngine.UI;
using GoogleMobileAds.Api;
using Starter.PlayFabIntegration;
using System;

public class RewardedCoinsButton : MonoBehaviour
{
    [Header("AdMob Configuration")]
    [SerializeField] private string androidAdUnitId = "ca-app-pub-6016513053121401/1716293301";
    [SerializeField] private string iOSAdUnitId = "ca-app-pub-6016513053121401/1716293301";
    private string adUnitId;

    [Header("Reward Configuration")]
    [SerializeField] private int coinRewardAmount = 100;

    [Header("UI")]
    [SerializeField] private Button adButton;

    private RewardedAd rewardedAd;
    private bool isAdLoaded = false;
    [SerializeField] private bool grantRewardOnUnsupportedPlatforms = true;
    private bool isPlatformSupported = false;

    private void Awake()
    {
    #if UNITY_ANDROID || UNITY_IOS
        isPlatformSupported = true;
    #if UNITY_ANDROID
        adUnitId = androidAdUnitId;
    #elif UNITY_IOS
        adUnitId = iOSAdUnitId;
    #endif
    #elif UNITY_EDITOR || UNITY_STANDALONE || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
        isPlatformSupported = true;
        grantRewardOnUnsupportedPlatforms = true;
    #else
        isPlatformSupported = false;
    #endif
        // Debug Log
        if (Starter.Lobby.MainMenuController.FindObjectOfType<Starter.Lobby.MainMenuController>() != null)
             Starter.Lobby.MainMenuController.LogToScreen($"[RewardedCoins] Awake. Supported: {isPlatformSupported}, GrantUnsup: {grantRewardOnUnsupportedPlatforms}");
    }

    private void Start()
    {
        Starter.Lobby.MainMenuController.LogToScreen($"[RewardedCoins] Start. ButtonAssigned: {(adButton != null)}");

        if (adButton != null)
        {
            adButton.onClick.RemoveAllListeners();
            adButton.onClick.AddListener(ShowAd);
            // Ensure interactable at start before logic
            // adButton.interactable = false; 
            Starter.Lobby.MainMenuController.LogToScreen("[RewardedCoins] Listener Added to Button.");
        }

        if (isPlatformSupported)
            LoadAd();
        else if (grantRewardOnUnsupportedPlatforms)
        {
            isAdLoaded = true;
            if (adButton != null) {
                adButton.interactable = true;
                Starter.Lobby.MainMenuController.LogToScreen("[RewardedCoins] Force Enabled for Unsupported/Simulated.");
            }
        }
    }

    private void LoadAd()
    {
        if (!isPlatformSupported) return;
        if (string.IsNullOrEmpty(adUnitId)) return;

        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        var request = new AdRequest();
        RewardedAd.Load(adUnitId, request, (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"RewardedAd failed to load: {error?.GetMessage()}");
                isAdLoaded = false;
                if (adButton != null) adButton.interactable = false;
                Invoke(nameof(LoadAd), 5f); // retry
                return;
            }

            rewardedAd = ad;
            isAdLoaded = true;
            if (adButton != null) adButton.interactable = true;
            RegisterAdEvents(ad);
        });
    }

    private void RegisterAdEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            if (adButton != null) adButton.interactable = false;
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            isAdLoaded = false;
            LoadAd();
        };

        ad.OnAdFullScreenContentFailed += (error) =>
        {
            Debug.LogError($"Ad failed to show: {error.GetMessage()}");
            isAdLoaded = false;
            LoadAd();
        };
    }

    public void ShowAd()
    {
        Starter.Lobby.MainMenuController.LogToScreen("AdCoinsButton ShowAd CLICKED.");
        if (!isPlatformSupported)
        {
            if (grantRewardOnUnsupportedPlatforms)
            {
                GrantReward();
                Invoke(nameof(SimulateAdReload), 1f);
            }
            return;
        }

        if (!isAdLoaded || rewardedAd == null)
        {
            LoadAd();
            return;
        }

        if (rewardedAd.CanShowAd())
        {
            rewardedAd.Show((reward) => GrantReward());
        }
        else
        {
            LoadAd();
        }
    }

    private void GrantReward()
    {
        var manager = PlayFabManager.Instance;
        // Use the Status Message from MainMenuController if available
        var menuController = FindObjectOfType<Starter.Lobby.MainMenuController>();

        if (manager != null && manager.IsLoggedIn)
        {
            manager.AddCoins(coinRewardAmount, () =>
            {
                Debug.Log($"Granted {coinRewardAmount} coins via PlayFab");
                if (menuController != null) 
                    menuController.GetType().GetMethod("UpdateStatusMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(menuController, new object[] { $"¡+{coinRewardAmount} MONEDAS (CLOUD)!" });
            });
        }
        else
        {
            int currentCoins = PlayerPrefs.GetInt("Coins", 0);
            PlayerPrefs.SetInt("Coins", currentCoins + coinRewardAmount);
            PlayerPrefs.Save();
            Debug.Log($"Granted {coinRewardAmount} coins via PlayerPrefs. Total: {currentCoins + coinRewardAmount}");
            
             if (menuController != null) 
                    menuController.GetType().GetMethod("UpdateStatusMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(menuController, new object[] { $"¡+{coinRewardAmount} MONEDAS (LOCAL)!" });
        }
    }

    private void SimulateAdReload()
    {
        isAdLoaded = true;
        if (adButton != null) adButton.interactable = true;
    }

    private void OnDestroy()
    {
        if (rewardedAd != null) rewardedAd.Destroy();
    }
}
