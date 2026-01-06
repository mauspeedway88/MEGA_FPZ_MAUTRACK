using UnityEngine;
using System;
using Starter.Shooter;

namespace Starter.PlayFabIntegration
{
    /// <summary>
    /// Helper component for integrating PlayFab with game scenes.
    /// Handles game results, coin rewards, and data synchronization.
    /// Add this to a persistent object in your game scenes.
    /// </summary>
    public class PlayFabGameIntegration : MonoBehaviour
    {
        public static PlayFabGameIntegration Instance { get; private set; }

        [Header("Coin Rewards")]
        [SerializeField] private int winCoins = 50;
        [SerializeField] private int loseCoins = 10;
        [SerializeField] private int chickenKillCoins = 5;

        [Header("Ranking Points")]
        [SerializeField] private int winRankingPoints = 25;
        [SerializeField] private int loseRankingPoints = -15;

        [Header("Debug")]
        [SerializeField] private bool enableDebugUI = false;

        // Events for game systems to react to
        public event Action<int> OnCoinsChanged;
        public event Action<int> OnRankingChanged;
        public event Action<float> OnElixirFillChanged; // Changed to float for percentage (0.0 to 1.0)
        public event Action OnSpecialWeaponUnlocked; // Fired when bottle is exchanged for weapon

        private PlayFabManager _playFabManager;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            _playFabManager = PlayFabManager.Instance;

            // Initialize local PlayerPrefs with PlayFab data if logged in
            if (_playFabManager != null && _playFabManager.IsLoggedIn)
            {
                SyncLocalData();
            }
        }

        #region Public API

        /// <summary>
        /// Report game result and update PlayFab data
        /// </summary>
        /// <param name="won">Whether the local player won</param>
        /// <param name="chickenKills">Number of chickens killed (for bonus coins)</param>
        public void ReportGameResult(bool won, int chickenKills = 0)
        {
            int coinsEarned = CalculateCoinsEarned(won, chickenKills);
            
            if (_playFabManager != null && _playFabManager.IsLoggedIn)
            {
                // Update PlayFab
                _playFabManager.RecordGameResult(won, coinsEarned);
                Debug.Log($"[PlayFabGameIntegration] Reported game result to PlayFab: Won={won}, Coins={coinsEarned}");
            }
            else
            {
                // Fallback to local storage
                UpdateLocalData(won, coinsEarned);
                Debug.Log($"[PlayFabGameIntegration] Stored result locally: Won={won}, Coins={coinsEarned}");
            }

            OnCoinsChanged?.Invoke(GetCoins());
            OnRankingChanged?.Invoke(GetRanking());
        }

        /// <summary>
        /// Add coins to the player (for rewards, ads, etc.)
        /// </summary>
        public void AddCoins(int amount)
        {
            if (_playFabManager != null && _playFabManager.IsLoggedIn)
            {
                _playFabManager.AddCoins(amount);
            }
            else
            {
                int currentCoins = PlayerPrefs.GetInt("Coins", 0);
                PlayerPrefs.SetInt("Coins", currentCoins + amount);
                PlayerPrefs.Save();
            }

            OnCoinsChanged?.Invoke(GetCoins());
        }

        /// <summary>
        /// Spend coins (returns false if not enough)
        /// </summary>
        public bool SpendCoins(int amount)
        {
            int currentCoins = GetCoins();
            if (currentCoins < amount) return false;

            if (_playFabManager != null && _playFabManager.IsLoggedIn)
            {
                _playFabManager.CurrentPlayerData.Coins -= amount;
                _playFabManager.SavePlayerData();
            }
            else
            {
                PlayerPrefs.SetInt("Coins", currentCoins - amount);
                PlayerPrefs.Save();
            }

            OnCoinsChanged?.Invoke(GetCoins());
            return true;
        }

        /// <summary>
        /// Get current coins (from PlayFab or local)
        /// </summary>
        public int GetCoins()
        {
            if (_playFabManager != null && _playFabManager.IsLoggedIn)
            {
                return _playFabManager.CurrentPlayerData?.Coins ?? PlayerPrefs.GetInt("Coins", 0);
            }
            return PlayerPrefs.GetInt("Coins", 0);
        }

        /// <summary>
        /// Add elixir fill to the bottle (for rewards, ads, etc.)
        /// Each ad fills about 1/3 (33.33%) of the bottle. 3 ads = full bottle.
        /// </summary>
        /// <param name="fillAmount">Amount to fill (0.0 to 1.0, where 1.0 = 100% full)</param>
        public void AddElixirFill(float fillAmount)
        {
            float currentFill = GetElixirFill();
            float newFill = Mathf.Min(currentFill + fillAmount, 1f); // Cap at 100%

            PlayerPrefs.SetFloat("ElixirFill", newFill);
            PlayerPrefs.Save();

            OnElixirFillChanged?.Invoke(newFill);
            Debug.Log($"[PlayFabGameIntegration] Added {fillAmount * 100}% elixir fill. Current: {newFill * 100}%");
        }

        /// <summary>
        /// Get current elixir fill percentage (0.0 to 1.0)
        /// </summary>
        public float GetElixirFill()
        {
            return PlayerPrefs.GetFloat("ElixirFill", 0f);
        }

        /// <summary>
        /// Get elixir fill as percentage (0 to 100)
        /// </summary>
        public int GetElixirFillPercent()
        {
            return Mathf.RoundToInt(GetElixirFill() * 100f);
        }

        /// <summary>
        /// Check if elixir bottle is full (ready to exchange for special weapon)
        /// </summary>
        public bool IsElixirBottleFull()
        {
            return GetElixirFill() >= 1f;
        }

        /// <summary>
        /// Exchange full elixir bottle for special weapon (Third Special Weapon)
        /// This weapon can ONLY be obtained this way (by watching 3 ads to fill the bottle)
        /// </summary>
        /// <returns>True if exchange was successful, false if bottle is not full or weapon already unlocked</returns>
        public bool ExchangeElixirBottleForWeapon()
        {
            if (!IsElixirBottleFull())
            {
                Debug.LogWarning("[PlayFabGameIntegration] Cannot exchange: Elixir bottle is not full!");
                return false;
            }

            if (IsSpecialWeaponUnlocked())
            {
                Debug.LogWarning("[PlayFabGameIntegration] Cannot exchange: Special weapon already unlocked!");
                return false;
            }

            // Reset elixir fill to 0
            PlayerPrefs.SetFloat("ElixirFill", 0f);
            
            // Mark special weapon as unlocked
            PlayerPrefs.SetInt("SpecialWeaponUnlocked", 1);
            PlayerPrefs.Save();

            OnElixirFillChanged?.Invoke(0f);
            OnSpecialWeaponUnlocked?.Invoke();

            Debug.Log("[PlayFabGameIntegration] Elixir bottle exchanged for special weapon!");
            return true;
        }

        /// <summary>
        /// Check if special weapon (Third Special Weapon) has been unlocked
        /// </summary>
        public bool IsSpecialWeaponUnlocked()
        {
            return PlayerPrefs.GetInt("SpecialWeaponUnlocked", 0) == 1;
        }

        /// <summary>
        /// Unlock First Special Weapon (direct unlock after watching one ad)
        /// </summary>
        public void UnlockSpecialWeapon1()
        {
            PlayerPrefs.SetInt("SpecialWeapon1Unlocked", 1);
            PlayerPrefs.Save();
            Debug.Log("[PlayFabGameIntegration] First Special Weapon unlocked!");
        }

        /// <summary>
        /// Check if First Special Weapon is unlocked
        /// </summary>
        public bool IsSpecialWeapon1Unlocked()
        {
            return PlayerPrefs.GetInt("SpecialWeapon1Unlocked", 0) == 1;
        }

        /// <summary>
        /// Unlock Second Special Weapon (direct unlock after watching one ad)
        /// </summary>
        public void UnlockSpecialWeapon2()
        {
            PlayerPrefs.SetInt("SpecialWeapon2Unlocked", 1);
            PlayerPrefs.Save();
            Debug.Log("[PlayFabGameIntegration] Second Special Weapon unlocked!");
        }

        /// <summary>
        /// Check if Second Special Weapon is unlocked
        /// </summary>
        public bool IsSpecialWeapon2Unlocked()
        {
            return PlayerPrefs.GetInt("SpecialWeapon2Unlocked", 0) == 1;
        }

        /// <summary>
        /// Get how many ads watched (based on fill percentage)
        /// Each ad fills 33.33% (0.3333), so 3 ads = 100%
        /// </summary>
        public int GetAdsWatchedForElixir()
        {
            float fill = GetElixirFill();
            return Mathf.FloorToInt(fill / 0.3333f); // 0.3333 = 33.33% = 1/3 per ad
        }

        /// <summary>
        /// Get how many more ads needed to fill the bottle completely
        /// </summary>
        public int GetAdsNeededToFillBottle()
        {
            float fill = GetElixirFill();
            float remaining = 1f - fill;
            return Mathf.CeilToInt(remaining / 0.3333f); // 0.3333 = 33.33% = 1/3 per ad
        }

        /// <summary>
        /// Get current ranking (from PlayFab or local)
        /// </summary>
        public int GetRanking()
        {
            if (_playFabManager != null && _playFabManager.IsLoggedIn)
            {
                return _playFabManager.CurrentPlayerData?.Ranking ?? PlayerPrefs.GetInt("Ranking", 1000);
            }
            return PlayerPrefs.GetInt("Ranking", 1000);
        }

        /// <summary>
        /// Get player name (from PlayFab or local)
        /// </summary>
        public string GetPlayerName()
        {
            if (_playFabManager != null && _playFabManager.IsLoggedIn && _playFabManager.CurrentPlayerData != null)
            {
                return _playFabManager.CurrentPlayerData.PlayerName ?? PlayerPrefs.GetString("PlayerName", "Player");
            }
            return PlayerPrefs.GetString("PlayerName", "Player");
        }

        /// <summary>
        /// Update player name
        /// </summary>
        public void SetPlayerName(string name)
        {
            PlayerPrefs.SetString("PlayerName", name);
            PlayerPrefs.Save();

            if (_playFabManager != null && _playFabManager.IsLoggedIn)
            {
                _playFabManager.CurrentPlayerData.PlayerName = name;
                _playFabManager.SavePlayerData();
            }
        }

        /// <summary>
        /// Get player's referral code
        /// </summary>
        public string GetReferralCode()
        {
            if (_playFabManager != null && _playFabManager.IsLoggedIn)
            {
                return _playFabManager.CurrentPlayerData?.ReferralCode ?? "";
            }
            return "";
        }

        /// <summary>
        /// Check if player is logged in to PlayFab
        /// </summary>
        public bool IsLoggedIn => _playFabManager?.IsLoggedIn ?? false;

        /// <summary>
        /// Sync local PlayerPrefs data with PlayFab
        /// </summary>
        public void SyncLocalData()
        {
            if (_playFabManager != null && _playFabManager.IsLoggedIn && _playFabManager.CurrentPlayerData != null)
            {
                var data = _playFabManager.CurrentPlayerData;
                
                // Sync to local
                PlayerPrefs.SetString("PlayerName", data.PlayerName ?? "");
                PlayerPrefs.SetInt("Coins", data.Coins);
                PlayerPrefs.SetInt("Ranking", data.Ranking);
                // Initialize elixir fill if not set
                if (!PlayerPrefs.HasKey("ElixirFill"))
                {
                    PlayerPrefs.SetFloat("ElixirFill", 0f);
                }
                if (!PlayerPrefs.HasKey("SpecialWeaponUnlocked"))
                {
                    PlayerPrefs.SetInt("SpecialWeaponUnlocked", 0);
                }
                PlayerPrefs.Save();

                Debug.Log($"[PlayFabGameIntegration] Synced local data: Name={data.PlayerName}, Coins={data.Coins}, Ranking={data.Ranking}");
            }
        }

        /// <summary>
        /// Force save current data to PlayFab
        /// </summary>
        public void ForceSaveToPlayFab()
        {
            if (_playFabManager != null && _playFabManager.IsLoggedIn)
            {
                // Update PlayFab data with local values if they're newer
                _playFabManager.CurrentPlayerData.PlayerName = PlayerPrefs.GetString("PlayerName", _playFabManager.CurrentPlayerData.PlayerName);
                _playFabManager.SavePlayerData();
            }
        }

        #endregion

        #region Private Helpers

        private int CalculateCoinsEarned(bool won, int chickenKills)
        {
            int baseReward = won ? winCoins : loseCoins;
            int chickenBonus = chickenKills * chickenKillCoins;
            return baseReward + chickenBonus;
        }

        private void UpdateLocalData(bool won, int coinsEarned)
        {
            // Update coins
            int currentCoins = PlayerPrefs.GetInt("Coins", 0);
            PlayerPrefs.SetInt("Coins", currentCoins + coinsEarned);

            // Update ranking
            int currentRanking = PlayerPrefs.GetInt("Ranking", 1000);
            int rankChange = won ? winRankingPoints : loseRankingPoints;
            PlayerPrefs.SetInt("Ranking", Mathf.Max(0, currentRanking + rankChange));

            // Update stats
            int totalGames = PlayerPrefs.GetInt("TotalGamesPlayed", 0) + 1;
            PlayerPrefs.SetInt("TotalGamesPlayed", totalGames);

            if (won)
            {
                int totalWins = PlayerPrefs.GetInt("TotalWins", 0) + 1;
                PlayerPrefs.SetInt("TotalWins", totalWins);
            }

            PlayerPrefs.Save();
        }

        #endregion

        #region Debug UI

        private void OnGUI()
        {
            if (!enableDebugUI) return;

            GUILayout.BeginArea(new Rect(10, 10, 350, 300));
            GUILayout.Label("PlayFab Debug", GUI.skin.box);
            GUILayout.Label($"Logged In: {IsLoggedIn}");
            GUILayout.Label($"Coins: {GetCoins()}");
            GUILayout.Label($"Ranking: {GetRanking()}");
            GUILayout.Label($"Name: {GetPlayerName()}");
            GUILayout.Label($"Elixir Fill: {GetElixirFillPercent()}% ({GetAdsWatchedForElixir()}/5 ads)");
            GUILayout.Label($"Special Weapon: {(IsSpecialWeaponUnlocked() ? "UNLOCKED" : "Locked")}");
            
            if (GUILayout.Button("Add 13 Coins"))
            {
                AddCoins(13);
            }
            
            if (GUILayout.Button("Add Elixir Fill (20%)"))
            {
                AddElixirFill(0.2f);
            }
            
            if (IsElixirBottleFull() && !IsSpecialWeaponUnlocked())
            {
                if (GUILayout.Button("Exchange Bottle for Weapon"))
                {
                    ExchangeElixirBottleForWeapon();
                }
            }
            
            if (GUILayout.Button("Report Win"))
            {
                ReportGameResult(true, 5);
            }
            
            if (GUILayout.Button("Report Loss"))
            {
                ReportGameResult(false, 2);
            }
            
            GUILayout.EndArea();
        }

        #endregion
    }
}

