using UnityEngine;
using UnityEngine.UI;
using GoogleMobileAds.Api;
using Mautrack.PlayFabIntegration;
using System;

namespace Mautrack.Ads
{
    public enum RewardType
    {
        Coins,
        Item // Extended for future
    }

    public class GamAdsManager : MonoBehaviour
    {
        [Header("AdMob Configuration")]
        [SerializeField] string _androidAdUnitId = "ca-app-pub-6016513053121401/1716293301";
        [SerializeField] string _iOSAdUnitId = "ca-app-pub-6016513053121401/1716293301";
        string _adUnitId = null;

        [Header("Reward Configuration")]
        [SerializeField] RewardType rewardType = RewardType.Coins;
        [SerializeField] int coinRewardAmount = 100;

        [Header("References")]
        [SerializeField] Button adButton; 

        // Events for UI updates
        public event Action OnRewardGranted;
        public event Action<RewardType, int> OnRewardGrantedWithDetails;

        private RewardedAd _rewardedAd;
        private bool _isAdLoaded = false;
        private bool _isPlatformSupported = false;

        [Header("Debug")]
        [SerializeField] bool grantRewardsOnUnsupportedPlatforms = true; // Enabled for Editor testing

        void Awake()
        {
            #if UNITY_IOS || UNITY_ANDROID
            _isPlatformSupported = true;
            #if UNITY_IOS
            _adUnitId = _iOSAdUnitId;
            #elif UNITY_ANDROID
                _adUnitId = _androidAdUnitId;
            #endif
            #elif UNITY_EDITOR
            _isPlatformSupported = true;
            _adUnitId = _androidAdUnitId; 
            #else
            _isPlatformSupported = false;
            _adUnitId = null;
            #endif
        }

        void Start()
        {
            var pm = PlayFabManager.Instance;
            if (pm != null && pm.IsGuestSession)
            {
                Debug.Log("GamAdsManager: Guest Mode Active - Ads enabled for temporary session coins.");
            }

            if (adButton != null)
            {
                adButton.onClick.RemoveAllListeners();
                adButton.onClick.AddListener(ShowAd);
            }

            if (_isPlatformSupported)
            {
                LoadAd();
            }
            else if (grantRewardsOnUnsupportedPlatforms)
            {
                _isAdLoaded = true;
                if (adButton != null) adButton.interactable = true;
            }
            else
            {
                if (adButton != null) adButton.interactable = false;
            }
        }

        public void LoadAd()
        {
            if (!_isPlatformSupported) return;
            if (string.IsNullOrEmpty(_adUnitId)) return;

            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }

            Debug.Log("Loading AdMob Rewarded Ad...");
            var adRequest = new AdRequest();

            RewardedAd.Load(_adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError($"RewardedAd failed to load: {error?.GetMessage()}");
                    _isAdLoaded = false;
                    if (adButton != null) adButton.interactable = false;
                    Invoke(nameof(LoadAd), 5f);
                    return;
                }

                Debug.Log("RewardedAd loaded!");
                _rewardedAd = ad;
                _isAdLoaded = true;
                if (adButton != null) adButton.interactable = true;
                RegisterEventHandlers(ad);
            });
        }

        public void ShowAd()
        {
            if (!_isPlatformSupported)
            {
                if (grantRewardsOnUnsupportedPlatforms)
                {
                    GrantReward();
                    _isAdLoaded = false;
                    Invoke(nameof(SimulateAdReload), 1f);
                }
                return;
            }

            if (!_isAdLoaded || _rewardedAd == null)
            {
                LoadAd();
                return;
            }

            if (_rewardedAd.CanShowAd())
            {
                _rewardedAd.Show((Reward reward) =>
                {
                    GrantReward();
                });
            }
            else
            {
                LoadAd();
            }
        }

        private void RegisterEventHandlers(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                _isAdLoaded = false;
                LoadAd();
            };
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                _isAdLoaded = false;
                LoadAd();
            };
        }

        private void SimulateAdReload()
        {
            _isAdLoaded = true;
            if (adButton != null) adButton.interactable = true;
        }

        private void GrantReward()
        {
            switch (rewardType)
            {
                case RewardType.Coins:
                    GrantCoinsReward();
                    break;
            }

            OnRewardGranted?.Invoke();
            OnRewardGrantedWithDetails?.Invoke(rewardType, coinRewardAmount);
        }

        private void GrantCoinsReward()
        {
            var pm = PlayFabManager.Instance;
            if (pm != null)
            {
                pm.AddCoins(coinRewardAmount);
                Debug.Log($"Rewarded {coinRewardAmount} coins via PlayFabManager");
            }
            else
            {
                // Fallback safe mode (shouldn't happen if manager exists)
                int currentCoins = PlayerPrefs.GetInt("Coins", 0);
                PlayerPrefs.SetInt("Coins", currentCoins + coinRewardAmount);
                PlayerPrefs.Save();
                Debug.Log($"Rewarded {coinRewardAmount} coins via PlayerPrefs (Fallback).");
            }
        }
    }
}
