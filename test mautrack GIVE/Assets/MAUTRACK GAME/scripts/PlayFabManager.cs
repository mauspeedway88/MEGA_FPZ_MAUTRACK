using System;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

namespace Mautrack.PlayFabIntegration
{
    /// <summary>
    /// Central manager for all PlayFab operations.
    /// Handles authentication, player data sync (Coins, Cars), and leaderboard operations.
    /// This is a singleton that persists across scenes.
    /// </summary>
    public class PlayFabManager : MonoBehaviour
    {
        public static PlayFabManager Instance { get; private set; }

        [Header("PlayFab Configuration")]
        [Tooltip("Your PlayFab Title ID from the PlayFab Dashboard")]
        [SerializeField] private string titleId = "18E21D"; // Auto-configured from Source project

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // Events for UI and game systems to subscribe to
        public event Action OnLoginSuccess;
        public event Action<string> OnLoginFailed;
        public event Action OnLogout;
        public event Action<PlayFabPlayerData> OnPlayerDataLoaded;
        public event Action<string> OnPlayerDataError;
        public event Action<List<LeaderboardEntry>> OnLeaderboardLoaded;
        public event Action<string> OnLeaderboardError;

        // Player session state
        public bool IsLoggedIn { get; private set; }
        public string PlayFabId { get; private set; }
        public string SessionTicket { get; private set; }
        public PlayFabPlayerData CurrentPlayerData { get; private set; }

        // Cache for leaderboard data
        public List<LeaderboardEntry> CachedLeaderboard { get; private set; } = new List<LeaderboardEntry>();

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePlayFab();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializePlayFab()
        {
            if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId))
            {
                PlayFabSettings.staticSettings.TitleId = titleId;
            }

            CurrentPlayerData = new PlayFabPlayerData();
            
            // Try auto-login with stored credentials or guest login
            TryAutoLogin();
            
            Log("PlayFab Manager initialized");
        }

        #region Authentication

        /// <summary>
        /// Register a new user with email and password
        /// </summary>
        public void RegisterWithEmail(string email, string password, string displayName, Action onSuccess = null, Action<string> onError = null)
        {
            Log($"Registering new user: {email}");

            var request = new RegisterPlayFabUserRequest
            {
                Email = email,
                Password = password,
                DisplayName = displayName,
                RequireBothUsernameAndEmail = false
            };

            PlayFabClientAPI.RegisterPlayFabUser(request,
                result =>
                {
                    Log("Registration successful!");
                    PlayFabId = result.PlayFabId;
                    SessionTicket = result.SessionTicket;
                    IsLoggedIn = true;
                    IsGuestSession = false;

                    // Save credentials for auto-login
                    SaveCredentials(email, password);
                    
                    // Mark email login method
                    PlayerPrefs.SetString("PlayFab_LoginMethod", "email");
                    PlayerPrefs.Save();

                    // Initialize player data
                    CurrentPlayerData.Email = email;
                    CurrentPlayerData.PlayerName = displayName;
                    CurrentPlayerData.PlayFabId = PlayFabId;
                    
                    // Generate referral code for new user
                    CurrentPlayerData.ReferralCode = GenerateReferralCode(PlayFabId);

                    // Save initial data to PlayFab
                    SavePlayerData();

                    onSuccess?.Invoke();
                    OnLoginSuccess?.Invoke();
                },
                error =>
                {
                    string errorMsg = GetErrorMessage(error);
                    LogError($"Registration failed: {errorMsg}");
                    onError?.Invoke(errorMsg);
                    OnLoginFailed?.Invoke(errorMsg);
                });
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        public void LoginWithEmail(string email, string password, Action onSuccess = null, Action<string> onError = null)
        {
            Log($"Logging in user: {email}");

            var request = new LoginWithEmailAddressRequest
            {
                Email = email,
                Password = password,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true,
                    GetUserData = true,
                    GetUserReadOnlyData = true
                }
            };

            PlayFabClientAPI.LoginWithEmailAddress(request,
                result =>
                {
                    Log("Login successful!");
                    PlayFabId = result.PlayFabId;
                    SessionTicket = result.SessionTicket;
                    IsLoggedIn = true;
                    IsGuestSession = false;

                    // Save credentials for auto-login
                    SaveCredentials(email, password);
                    
                    // Mark email login method
                    PlayerPrefs.SetString("PlayFab_LoginMethod", "email");
                    PlayerPrefs.Save();

                    // Load player data
                    LoadPlayerData(onSuccess);
                },
                error =>
                {
                    string errorMsg = GetErrorMessage(error);
                    LogError($"Login failed: {errorMsg}");
                    onError?.Invoke(errorMsg);
                    OnLoginFailed?.Invoke(errorMsg);
                });
        }

        /// <summary>
        /// Login as a guest (device-based authentication)
        /// </summary>
        public void LoginAsGuest(Action onSuccess = null, Action<string> onError = null)
        {
            Log("Logging in as guest");

            string deviceId = GetDeviceId();

#if UNITY_ANDROID && !UNITY_EDITOR
            var request = new LoginWithAndroidDeviceIDRequest
            {
                AndroidDeviceId = deviceId,
                CreateAccount = true,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true,
                    GetUserData = true
                }
            };
            
            PlayFabClientAPI.LoginWithAndroidDeviceID(request,
                result => OnGuestLoginSuccess(result.PlayFabId, result.SessionTicket, result.NewlyCreated, onSuccess),
                error => OnGuestLoginFailed(error, onError));
#elif UNITY_IOS && !UNITY_EDITOR
            var request = new LoginWithIOSDeviceIDRequest
            {
                DeviceId = deviceId,
                CreateAccount = true,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true,
                    GetUserData = true
                }
            };
            
            PlayFabClientAPI.LoginWithIOSDeviceID(request,
                result => OnGuestLoginSuccess(result.PlayFabId, result.SessionTicket, result.NewlyCreated, onSuccess),
                error => OnGuestLoginFailed(error, onError));
#else
            var request = new LoginWithCustomIDRequest
            {
                CustomId = deviceId,
                CreateAccount = true,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true,
                    GetUserData = true
                }
            };

            PlayFabClientAPI.LoginWithCustomID(request,
                result => OnGuestLoginSuccess(result.PlayFabId, result.SessionTicket, result.NewlyCreated, onSuccess),
                error => OnGuestLoginFailed(error, onError));
#endif
        }

        private void OnGuestLoginSuccess(string playFabId, string sessionTicket, bool newlyCreated, Action onSuccess)
        {
            Log($"Guest login successful! Newly created: {newlyCreated}");
            PlayFabId = playFabId;
            SessionTicket = sessionTicket;
            IsLoggedIn = true;
            IsGuestSession = true;

            // Mark guest login method
            PlayerPrefs.SetString("PlayFab_LoginMethod", "guest");
            PlayerPrefs.Save();

            if (newlyCreated)
            {
                // Initialize new guest account
                CurrentPlayerData.PlayFabId = PlayFabId;
                CurrentPlayerData.PlayerName = "Guest" + UnityEngine.Random.Range(1000, 9999);
                CurrentPlayerData.ReferralCode = GenerateReferralCode(PlayFabId);
                // Can grant starter car here if needed
                CurrentPlayerData.OwnedCars = new List<int> { 0 }; // Default car ID 0
                
                SavePlayerData();
                onSuccess?.Invoke();
                OnLoginSuccess?.Invoke();
            }
            else
            {
                LoadPlayerData(onSuccess);
            }
        }

        private void OnGuestLoginFailed(PlayFabError error, Action<string> onError)
        {
            string errorMsg = GetErrorMessage(error);
            LogError($"Guest login failed: {errorMsg}");
            onError?.Invoke(errorMsg);
            OnLoginFailed?.Invoke(errorMsg);
        }

        // Flag to check if current user is a guest (limited features)
        public bool IsGuestSession { get; private set; }
        
        // Event triggered when no stored credentials are found (New User)
        public event Action OnRequireUserLogin;

        /// <summary>
        /// Try to auto-login with stored credentials or guest login
        /// Priority: Check PlayerPrefs for previous login method.
        /// If NO method is found (First run), we do NOTHING and ask for UI input.
        /// </summary>
        private void TryAutoLogin()
        {
            string loginMethod = PlayerPrefs.GetString("PlayFab_LoginMethod", "");
            string storedEmail = PlayerPrefs.GetString("PlayFab_Email", "");
            string storedPassword = PlayerPrefs.GetString("PlayFab_Password", "");

            Log($"Attempting auto-login. Method: '{loginMethod}'");

            if (string.IsNullOrEmpty(loginMethod))
            {
                // -- NEW USER CASE --
                // No previous login method found. 
                // Do NOT auto-login. Notify UI to show "Login / Guest" options.
                Log("No stored login method. Waiting for user input...");
                OnRequireUserLogin?.Invoke();
                return;
            }

            if (loginMethod == "guest")
            {
                // Previous session was Guest. Auto-login as Guest.
                Log("Auto-login: Guest");
                LoginAsGuest(
                     onSuccess: () => 
                     {
                         // Callback handled in OnGuestLoginSuccess
                     },
                     onError: (error) => 
                     {
                         // If guest login fails, we might want to ask user again
                         OnRequireUserLogin?.Invoke();
                     }
                );
            }
            else if (loginMethod == "email")
            {
                // Previous session was Email. Auto-login with Email.
                if (!string.IsNullOrEmpty(storedEmail) && !string.IsNullOrEmpty(storedPassword))
                {
                    Log("Auto-login: Email");
                    LoginWithEmail(storedEmail, storedPassword,
                        onSuccess: () => { },
                        onError: (error) => 
                        {
                             // If email login fails (pword changed?), ask user
                             OnRequireUserLogin?.Invoke();
                        }
                    );
                }
                else
                {
                    OnRequireUserLogin?.Invoke();
                }
            }
            else
            {
                // Unknown method, fallback to asking user
                OnRequireUserLogin?.Invoke();
            }
        }

        /// <summary>
        /// Logout and clear stored credentials
        /// </summary>
        public void Logout()
        {
            Log("Logging out");
            
            IsLoggedIn = false;
            PlayFabId = null;
            SessionTicket = null;
            CurrentPlayerData = new PlayFabPlayerData();

            // Clear stored credentials and login method
            PlayerPrefs.DeleteKey("PlayFab_Email");
            PlayerPrefs.DeleteKey("PlayFab_Password");
            PlayerPrefs.DeleteKey("PlayFab_LoginMethod");
            PlayerPrefs.Save();

            OnLogout?.Invoke();
        }

        private void SaveCredentials(string email, string password)
        {
            PlayerPrefs.SetString("PlayFab_Email", email);
            PlayerPrefs.SetString("PlayFab_Password", password);
            PlayerPrefs.Save();
        }

        private string GetDeviceId()
        {
            string deviceId = PlayerPrefs.GetString("PlayFab_DeviceId", "");
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = SystemInfo.deviceUniqueIdentifier;
                if (deviceId == SystemInfo.unsupportedIdentifier)
                {
                    deviceId = Guid.NewGuid().ToString();
                }
                PlayerPrefs.SetString("PlayFab_DeviceId", deviceId);
                PlayerPrefs.Save();
            }
            return deviceId;
        }

        #endregion

        #region Player Data

        /// <summary>
        /// Load player data from PlayFab
        /// </summary>
        [Serializable]
        public class StringListWrapper
        {
            public List<string> List = new List<string>();
        }

        // Wrapper class for JSON serialization
        [Serializable]
        public class IntListWrapper
        {
            public List<int> List = new List<int>();
        }

        public void LoadPlayerData(Action onComplete = null)
        {
            if (!IsLoggedIn)
            {
                LogError("Cannot load player data: Not logged in");
                return;
            }

            // --- GUEST MODE LOAD ---
            if (IsGuestSession)
            {
                Log("Guest Mode: Loading local data...");
                CurrentPlayerData.Coins = PlayerPrefs.GetInt("Guest_Coins", 0);
                CurrentPlayerData.Ranking = PlayerPrefs.GetInt("Guest_Ranking", 1000);
                CurrentPlayerData.PlayerName = PlayerPrefs.GetString("Guest_PlayerName", "Guest");
                
                // Load Cars - Parse JSON
                string carsJson = PlayerPrefs.GetString("Guest_OwnedCars", "{}");
                var wrapper = JsonUtility.FromJson<IntListWrapper>(carsJson);
                CurrentPlayerData.OwnedCars = wrapper != null ? wrapper.List : new List<int>();

                // Load Selected Car
                CurrentPlayerData.SelectedCar = PlayerPrefs.GetInt("Guest_SelectedCar", 0);

                onComplete?.Invoke();
                OnPlayerDataLoaded?.Invoke(CurrentPlayerData);
                OnLoginSuccess?.Invoke();
                return;
            }

            Log("Loading player data from PlayFab");

            var request = new GetUserDataRequest
            {
                Keys = new List<string>
                {
                    "Email",
                    "PlayerName",
                    "ReferralCode",
                    "Ranking",
                    "Coins",
                    "TotalGamesPlayed",
                    "TotalWins",
                    "OwnedCars", // <-- MIGRATED: Fetch owned CARS
                    "SelectedCar" // <-- MIGRATED: Fetch selected CAR
                }
            };

            PlayFabClientAPI.GetUserData(request,
                result =>
                {
                    Log("Player data loaded successfully");

                    CurrentPlayerData.PlayFabId = PlayFabId;

                    if (result.Data != null)
                    {
                        if (result.Data.TryGetValue("Email", out var email))
                            CurrentPlayerData.Email = email.Value;
                        if (result.Data.TryGetValue("PlayerName", out var name))
                            CurrentPlayerData.PlayerName = name.Value;
                        if (result.Data.TryGetValue("ReferralCode", out var refCode))
                            CurrentPlayerData.ReferralCode = refCode.Value;
                        if (result.Data.TryGetValue("Ranking", out var ranking))
                            int.TryParse(ranking.Value, out CurrentPlayerData.Ranking);
                        if (result.Data.TryGetValue("Coins", out var coins))
                            int.TryParse(coins.Value, out CurrentPlayerData.Coins);
                        if (result.Data.TryGetValue("TotalGamesPlayed", out var games))
                            int.TryParse(games.Value, out CurrentPlayerData.TotalGamesPlayed);
                        if (result.Data.TryGetValue("TotalWins", out var wins))
                            int.TryParse(wins.Value, out CurrentPlayerData.TotalWins);

                        // Load Owned Cars
                        if (result.Data.TryGetValue("OwnedCars", out var carsData))
                        {
                            var wrapper = JsonUtility.FromJson<IntListWrapper>(carsData.Value);
                            CurrentPlayerData.OwnedCars = wrapper != null ? wrapper.List : new List<int>();
                        }
                        else
                        {
                            CurrentPlayerData.OwnedCars = new List<int>();
                        }

                        // Load Selected Car
                        if (result.Data.TryGetValue("SelectedCar", out var selCar))
                        {
                            int.TryParse(selCar.Value, out CurrentPlayerData.SelectedCar);
                        }
                    }

                    // Sync with local PlayerPrefs for backward compatibility
                    SyncToPlayerPrefs();

                    onComplete?.Invoke();
                    OnPlayerDataLoaded?.Invoke(CurrentPlayerData);
                    OnLoginSuccess?.Invoke();
                },
                error =>
                {
                    string errorMsg = GetErrorMessage(error);
                    LogError($"Failed to load player data: {errorMsg}");
                    OnPlayerDataError?.Invoke(errorMsg);
                });
        }

        /// <summary>
        /// Save current player data to PlayFab
        /// </summary>
        public void SavePlayerData(Action onComplete = null, Action<string> onError = null)
        {
            if (!IsLoggedIn)
            {
                onError?.Invoke("Not logged in");
                return;
            }

            // --- GUEST MODE SAVE ---
            if (IsGuestSession)
            {
                // Save everything to PlayerPrefs
                PlayerPrefs.SetInt("Guest_Coins", CurrentPlayerData.Coins);
                PlayerPrefs.SetInt("Guest_Ranking", CurrentPlayerData.Ranking);
                PlayerPrefs.SetString("Guest_PlayerName", CurrentPlayerData.PlayerName);
                
                // Save Cars
                string carsJson = JsonUtility.ToJson(new IntListWrapper { List = CurrentPlayerData.OwnedCars });
                PlayerPrefs.SetString("Guest_OwnedCars", carsJson);

                // Save Selected Car
                PlayerPrefs.SetInt("Guest_SelectedCar", CurrentPlayerData.SelectedCar);
                
                PlayerPrefs.Save();
                Log("Guest Mode: Data saved locally to PlayerPrefs.");
                
                onComplete?.Invoke();
                return;
            }

            // --- REGISTERED USER SAVE (CLOUD) ---
            string ownedCarsJson = JsonUtility.ToJson(new IntListWrapper { List = CurrentPlayerData.OwnedCars });

            var data = new Dictionary<string, string>
            {
                { "Email", CurrentPlayerData.Email ?? "" },
                { "PlayerName", CurrentPlayerData.PlayerName ?? "" },
                { "ReferralCode", CurrentPlayerData.ReferralCode ?? "" },
                { "Ranking", CurrentPlayerData.Ranking.ToString() },
                { "Coins", CurrentPlayerData.Coins.ToString() },
                { "TotalGamesPlayed", CurrentPlayerData.TotalGamesPlayed.ToString() },
                { "TotalWins", CurrentPlayerData.TotalWins.ToString() },
                { "OwnedCars", ownedCarsJson }, // NOW INCLUDED
                { "SelectedCar", CurrentPlayerData.SelectedCar.ToString() } // Save selected Car too
            };

            PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest { Data = data },
                result =>
                {
                    Log($"Cloud Save Success. Coins: {CurrentPlayerData.Coins}, Cars: {CurrentPlayerData.OwnedCars.Count}");
                    onComplete?.Invoke();
                },
                error =>
                {
                    string errorMsg = GetErrorMessage(error);
                    onError?.Invoke(errorMsg);
                });
        }

        /// <summary>
        /// Update specific player data fields
        /// </summary>
        public void UpdatePlayerData(string key, string value, Action onComplete = null)
        {
            if (!IsLoggedIn) return;

            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> { { key, value } }
            };

            PlayFabClientAPI.UpdateUserData(request,
                result =>
                {
                    Log($"Updated {key} = {value}");
                    onComplete?.Invoke();
                },
                error => LogError($"Failed to update {key}: {GetErrorMessage(error)}"));
        }

        /// <summary>
        /// Update player's display name
        /// </summary>
        public void UpdateDisplayName(string displayName)
        {
            if (!IsLoggedIn || string.IsNullOrEmpty(displayName)) return;

            var request = new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = displayName
            };

            PlayFabClientAPI.UpdateUserTitleDisplayName(request,
                result => Log($"Display name updated to: {displayName}"),
                error => LogError($"Failed to update display name: {GetErrorMessage(error)}"));
        }

        /// <summary>
        /// Add coins to the player
        /// </summary>
        public void AddCoins(int amount, Action onComplete = null)
        {
            // IF GUEST: Add coins LOCALLY only (for this session/device) so they can buy cars
            if (IsGuestSession)
            {
                CurrentPlayerData.Coins += amount;
                Log($"Guest Mode: Added {amount} coins locally. Total: {CurrentPlayerData.Coins}");
                
                PlayerPrefs.SetInt("Guest_Coins", CurrentPlayerData.Coins);
                PlayerPrefs.Save();
                
                OnPlayerDataLoaded?.Invoke(CurrentPlayerData); 
                onComplete?.Invoke();
                return;
            }

            // IF REGISTERED: Full Cloud Sync
            CurrentPlayerData.Coins += amount;
            UpdatePlayerData("Coins", CurrentPlayerData.Coins.ToString(), () =>
            {
                SyncToPlayerPrefs();
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Record a game result
        /// </summary>
        public void RecordGameResult(bool won, int coinsEarned = 0)
        {
            if (IsGuestSession)
            {
                Log("Guest Mode: Game result ignored. No stats or coins awarded.");
                return;
            }

            CurrentPlayerData.TotalGamesPlayed++;
            if (won) CurrentPlayerData.TotalWins++;
            CurrentPlayerData.Coins += coinsEarned;

            // Calculate new ranking (simple ELO-like system)
            int rankChange = won ? 25 : -15;
            CurrentPlayerData.Ranking = Mathf.Max(0, CurrentPlayerData.Ranking + rankChange);

            SavePlayerData();
            
            // Update leaderboard
            UpdateLeaderboard(CurrentPlayerData.Ranking);
        }

        /// <summary>
        /// Sync PlayFab data to PlayerPrefs for backward compatibility
        /// </summary>
        private void SyncToPlayerPrefs()
        {
            PlayerPrefs.SetString("PlayerName", CurrentPlayerData.PlayerName ?? "");
            PlayerPrefs.SetInt("Coins", CurrentPlayerData.Coins);
            PlayerPrefs.SetInt("Ranking", CurrentPlayerData.Ranking);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Generate a unique referral code
        /// </summary>
        private string GenerateReferralCode(string playFabId)
        {
            string baseCode = playFabId.Length >= 6 ? playFabId.Substring(0, 6) : playFabId;
            return "REF" + baseCode.ToUpper() + UnityEngine.Random.Range(100, 999);
        }

        #endregion

        #region Leaderboard

        /// <summary>
        /// Update the player's score on the leaderboard
        /// </summary>
        public void UpdateLeaderboard(int score, string leaderboardName = "Rankings")
        {
            if (!IsLoggedIn) return;

            Log($"Updating leaderboard '{leaderboardName}' with score: {score}");

            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                        StatisticName = leaderboardName,
                        Value = score
                    }
                }
            };

            PlayFabClientAPI.UpdatePlayerStatistics(request,
                result => Log("Leaderboard updated successfully"),
                error => LogError($"Failed to update leaderboard: {GetErrorMessage(error)}"));
        }

        /// <summary>
        /// Get the global leaderboard
        /// </summary>
        public void GetLeaderboard(string leaderboardName = "Rankings", int maxResults = 100, Action<List<LeaderboardEntry>> onComplete = null)
        {
            Log($"Loading leaderboard: {leaderboardName}");

            var request = new GetLeaderboardRequest
            {
                StatisticName = leaderboardName,
                StartPosition = 0,
                MaxResultsCount = maxResults
            };

            PlayFabClientAPI.GetLeaderboard(request,
                result =>
                {
                    Log($"Leaderboard loaded: {result.Leaderboard.Count} entries");
                    
                    CachedLeaderboard.Clear();
                    foreach (var entry in result.Leaderboard)
                    {
                        CachedLeaderboard.Add(new LeaderboardEntry
                        {
                            Position = entry.Position + 1, // 1-based for display
                            PlayFabId = entry.PlayFabId,
                            DisplayName = entry.DisplayName ?? "Anonymous",
                            Score = entry.StatValue,
                            IsCurrentPlayer = entry.PlayFabId == PlayFabId
                        });
                    }

                    onComplete?.Invoke(CachedLeaderboard);
                    OnLeaderboardLoaded?.Invoke(CachedLeaderboard);
                },
                error =>
                {
                    string errorMsg = GetErrorMessage(error);
                    LogError($"Failed to load leaderboard: {errorMsg}");
                    OnLeaderboardError?.Invoke(errorMsg);
                });
        }

        /// <summary>
        /// Get leaderboard around the current player
        /// </summary>
        public void GetLeaderboardAroundPlayer(string leaderboardName = "Rankings", int maxResults = 10, Action<List<LeaderboardEntry>> onComplete = null)
        {
            if (!IsLoggedIn)
            {
                GetLeaderboard(leaderboardName, maxResults, onComplete);
                return;
            }

            Log($"Loading leaderboard around player: {leaderboardName}");

            var request = new GetLeaderboardAroundPlayerRequest
            {
                StatisticName = leaderboardName,
                MaxResultsCount = maxResults
            };

            PlayFabClientAPI.GetLeaderboardAroundPlayer(request,
                result =>
                {
                    Log($"Leaderboard loaded: {result.Leaderboard.Count} entries");
                    
                    var entries = new List<LeaderboardEntry>();
                    foreach (var entry in result.Leaderboard)
                    {
                        entries.Add(new LeaderboardEntry
                        {
                            Position = entry.Position + 1, // 1-based for display
                            PlayFabId = entry.PlayFabId,
                            DisplayName = entry.DisplayName ?? "Anonymous",
                            Score = entry.StatValue,
                            IsCurrentPlayer = entry.PlayFabId == PlayFabId
                        });
                    }
                    
                    onComplete?.Invoke(entries);
                },
                error =>
                {
                    string errorMsg = GetErrorMessage(error);
                    LogError($"Failed to load leaderboard around player: {errorMsg}");
                });
        }

        #endregion

        #region Helpers

        private void Log(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[PlayFabManager] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[PlayFabManager] {message}");
        }

        private string GetErrorMessage(PlayFabError error)
        {
            if (error == null) return "Unknown Error";
            return $"{error.ErrorMessage}";
        }

        #endregion
    }

    [Serializable]
    public class PlayFabPlayerData
    {
        public string Email;
        public string PlayerName = "Player";
        public string PlayFabId;
        public string ReferralCode;
        public int Ranking = 1000;
        public int Coins = 0;
        public int TotalGamesPlayed = 0;
        public int TotalWins = 0;
        
        // MIGRATED: Car Specific
        public List<int> OwnedCars = new List<int>();
        public int SelectedCar = 0;
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public int Position;
        public string PlayFabId;
        public string DisplayName;
        public int Score;
        public bool IsCurrentPlayer;
    }
}
