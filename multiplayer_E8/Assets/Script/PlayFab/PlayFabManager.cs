using System;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

namespace Starter.PlayFabIntegration
{
    /// <summary>
    /// Central manager for all PlayFab operations.
    /// Handles authentication, player data sync, and leaderboard operations.
    /// This is a singleton that persists across scenes.
    /// </summary>
    public class PlayFabManager : MonoBehaviour
    {
        public static PlayFabManager Instance { get; private set; }

        [Header("PlayFab Configuration")]
        [Tooltip("Your PlayFab Title ID from the PlayFab Dashboard")]
        [SerializeField] private string titleId = "YOUR_TITLE_ID"; // Replace with your Title ID

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
                
                // Load Weapons - Parse JSON
                string weaponsJson = PlayerPrefs.GetString("Guest_OwnedWeapons", "{}");
                var wrapper = JsonUtility.FromJson<IntListWrapper>(weaponsJson);
                CurrentPlayerData.OwnedWeapons = wrapper != null ? wrapper.List : new List<int>();

                // Load Selected Weapon
                CurrentPlayerData.SelectedWeapon = PlayerPrefs.GetString("Guest_SelectedWeapon", "");

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
            "OwnedWeapons", // <-- Add this key to fetch owned weapons
            "SelectedWeapon"
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

                        // Load Owned Weapons
                        if (result.Data.TryGetValue("OwnedWeapons", out var weaponsData))
                        {
                            var wrapper = JsonUtility.FromJson<IntListWrapper>(weaponsData.Value);
                            CurrentPlayerData.OwnedWeapons = wrapper != null ? wrapper.List : new List<int>();
                        }
                        else
                        {
                            CurrentPlayerData.OwnedWeapons = new List<int>();
                        }

                        // Load Selected Weapon
                        if (result.Data.TryGetValue("SelectedWeapon", out var selWeapon))
                        {
                            CurrentPlayerData.SelectedWeapon = selWeapon.Value;
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
                
                // Save Weapons
                string weaponsJson = JsonUtility.ToJson(new IntListWrapper { List = CurrentPlayerData.OwnedWeapons });
                PlayerPrefs.SetString("Guest_OwnedWeapons", weaponsJson);

                // Save Selected Weapon
                PlayerPrefs.SetString("Guest_SelectedWeapon", CurrentPlayerData.SelectedWeapon ?? "");
                
                PlayerPrefs.Save();
                Log("Guest Mode: Data saved locally to PlayerPrefs.");
                
                onComplete?.Invoke();
                return;
            }

            // --- REGISTERED USER SAVE (CLOUD) ---
            string ownedWeaponsJson = JsonUtility.ToJson(new IntListWrapper { List = CurrentPlayerData.OwnedWeapons });

            var data = new Dictionary<string, string>
            {
                { "Email", CurrentPlayerData.Email ?? "" },
                { "PlayerName", CurrentPlayerData.PlayerName ?? "" },
                { "ReferralCode", CurrentPlayerData.ReferralCode ?? "" },
                { "Ranking", CurrentPlayerData.Ranking.ToString() },
                { "Coins", CurrentPlayerData.Coins.ToString() },
                { "TotalGamesPlayed", CurrentPlayerData.TotalGamesPlayed.ToString() },
                { "TotalWins", CurrentPlayerData.TotalWins.ToString() },
                { "OwnedWeapons", ownedWeaponsJson }, // NOW INCLUDED
                { "SelectedWeapon", CurrentPlayerData.SelectedWeapon ?? "" } // Save selected weapon too
            };

            PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest { Data = data },
                result =>
                {
                    Log($"Cloud Save Success. Coins: {CurrentPlayerData.Coins}, Weapons: {CurrentPlayerData.OwnedWeapons.Count}");
                    onComplete?.Invoke();
                },
                error =>
                {
                    string errorMsg = GetErrorMessage(error);
                    onError?.Invoke(errorMsg);
                });
        }

        // Wrapper class for JSON serialization
        [Serializable]
        public class IntListWrapper
        {
            public List<int> List = new List<int>();
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
        /// <summary>
        /// Add coins to the player
        /// </summary>
        public void AddCoins(int amount, Action onComplete = null)
        {
            // IF GUEST: Add coins LOCALLY only (for this session/device) so they can buy weapons
            if (IsGuestSession)
            {
                // We use a local pref for guest coins if needed, or just update the in-memory value
                CurrentPlayerData.Coins += amount;
                Log($"Guest Mode: Added {amount} coins locally. Total: {CurrentPlayerData.Coins}");
                
                // Save locally so it persists if they restart app on same device (optional, but good UX)
                PlayerPrefs.SetInt("Guest_Coins", CurrentPlayerData.Coins);
                PlayerPrefs.Save();
                
                // Trigger UI update
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
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                UpdateLeaderboard(1);
            }
        }
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
                            Position = entry.Position + 1,
                            PlayFabId = entry.PlayFabId,
                            DisplayName = entry.DisplayName ?? "Anonymous",
                            Score = entry.StatValue,
                            IsCurrentPlayer = entry.PlayFabId == PlayFabId
                        });
                    }

                    onComplete?.Invoke(entries);
                    OnLeaderboardLoaded?.Invoke(entries);
                },
                error =>
                {
                    string errorMsg = GetErrorMessage(error);
                    LogError($"Failed to load leaderboard: {errorMsg}");
                    OnLeaderboardError?.Invoke(errorMsg);
                });
        }

        #endregion

        #region Referral System

        /// <summary>
        /// Apply a referral code
        /// </summary>
        public void ApplyReferralCode(string referralCode, Action onSuccess = null, Action<string> onError = null)
        {
            if (!IsLoggedIn)
            {
                onError?.Invoke("Not logged in");
                return;
            }

            if (referralCode == CurrentPlayerData.ReferralCode)
            {
                onError?.Invoke("Cannot use your own referral code");
                return;
            }

            // In a real implementation, you would validate the referral code server-side
            // using CloudScript. For now, we'll simulate the reward.
            Log($"Applying referral code: {referralCode}");

            // Reward the player for using a referral code
            AddCoins(100, () =>
            {
                Log("Referral bonus applied: +100 coins");
                onSuccess?.Invoke();
            });
        }

        #endregion

        #region Helpers

        private string GetErrorMessage(PlayFabError error)
        {
            if (error == null) return "Unknown error";
            
            switch (error.Error)
            {
                case PlayFabErrorCode.InvalidEmailAddress:
                    return "Invalid email address";
                case PlayFabErrorCode.InvalidPassword:
                    return "Invalid password (minimum 6 characters)";
                case PlayFabErrorCode.EmailAddressNotAvailable:
                    return "Email already registered";
                case PlayFabErrorCode.AccountNotFound:
                    return "Account not found";
                case PlayFabErrorCode.InvalidEmailOrPassword:
                    return "Invalid email or password";
                case PlayFabErrorCode.InvalidUsernameOrPassword:
                    return "Invalid username or password";
                default:
                    return error.ErrorMessage ?? error.Error.ToString();
            }
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[PlayFab] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[PlayFab] {message}");
        }

        #endregion
    }

    /// <summary>
    /// Data class for player information stored in PlayFab
    /// </summary>
    [Serializable]
    public class PlayFabPlayerData
    {
        public string PlayFabId;
        public string Email;
        public string PlayerName;
        public string ReferralCode;
        public int Ranking;
        public int Coins;
        public int TotalGamesPlayed;
        public int TotalWins;
        public List<int> OwnedWeapons = new List<int>();
        public string SelectedWeapon; // Added for weapon persistence
        public float WinRate => TotalGamesPlayed > 0 ? (float)TotalWins / TotalGamesPlayed * 100f : 0f;
    }

    /// <summary>
    /// Leaderboard entry data
    /// </summary>
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

