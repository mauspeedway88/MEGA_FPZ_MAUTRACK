using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace Starter.PlayFabIntegration
{
    /// <summary>
    /// UI Controller for PlayFab authentication.
    /// Handles login, registration, and account management UI panels.
    /// Attach this to your Canvas in the MainMenu scene.
    /// </summary>
    public class PlayFabAuthUI : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject authPanel;
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject profilePanel;
        [SerializeField] private GameObject mainGamePanel; // The main menu panel to show after login

        [Header("Login Panel")]
        [SerializeField] private TMP_InputField loginEmailInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button guestLoginButton;
        [SerializeField] private Button goToRegisterButton;
        [SerializeField] private TextMeshProUGUI loginErrorText;

        [Header("Register Panel")]
        [SerializeField] private TMP_InputField registerEmailInput;
        [SerializeField] private TMP_InputField registerPasswordInput;
        [SerializeField] private TMP_InputField registerConfirmPasswordInput;
        [SerializeField] private TMP_InputField registerDisplayNameInput;
        [SerializeField] private TMP_InputField referralCodeInput;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button goToLoginButton;
        [SerializeField] private TextMeshProUGUI registerErrorText;

        [Header("Profile Panel")]
        [SerializeField] private TextMeshProUGUI profileNameText;
        [SerializeField] private TextMeshProUGUI profileEmailText;
        [SerializeField] private TextMeshProUGUI profileRankingText;
        [SerializeField] private TextMeshProUGUI profileCoinsText;
        [SerializeField] private TextMeshProUGUI profileReferralCodeText;
        [SerializeField] private TextMeshProUGUI profileWinRateText;
        [SerializeField] private Button copyReferralButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button closeProfileButton;

        [Header("Loading")]
        [SerializeField] private TextMeshProUGUI loadingText;

        [Header("UI Colors")]
        [SerializeField] private Color errorColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.2f);

        // Events
        public event Action OnAuthenticationComplete;
        public event Action OnLoggedOut;

        private PlayFabManager _playFabManager;
        private bool _isInitialized;

        private void Start()
        {
            // Auto Build Check
            // We check a critical reference (loginPanel). If missing, we assume we need to build everything.
            if (loginPanel == null && Application.isPlaying)
            {
                Debug.Log("[PlayFabAuthUI] UI References missing. Auto-Building UI...");
                PlayFabUIGenerator.GenerateAuthUI(this);
            }

            InitializeUI();
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                SubscribeToEvents();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeUI()
        {
            // Find or wait for PlayFabManager
            _playFabManager = PlayFabManager.Instance;
            
            if (_playFabManager == null)
            {
                // Create PlayFabManager if it doesn't exist
                var managerGO = new GameObject("PlayFabManager");
                _playFabManager = managerGO.AddComponent<PlayFabManager>();
            }

            SetupButtonListeners();
            SubscribeToEvents();
            _isInitialized = true;

            // Update Guest Button Text with user's specific warning
            if (guestLoginButton != null)
            {
                var btnText = guestLoginButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = "Jugar como Invitado\n<size=20>(No ganarás monedas ni premios ni ranking)</size>";
                }
            }

            // Check if already logged in
            if (_playFabManager.IsLoggedIn)
            {
                OnLoginSuccessful();
            }
            // Else: Do nothing. The MainMenuController currently manages the initial flow via events.
        }

        private void SetupButtonListeners()
        {
            // Login panel buttons
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginClicked);
            if (guestLoginButton != null)
                guestLoginButton.onClick.AddListener(OnGuestLoginClicked);
            if (goToRegisterButton != null)
                goToRegisterButton.onClick.AddListener(ShowRegisterPanel);

            // Register panel buttons
            if (registerButton != null)
                registerButton.onClick.AddListener(OnRegisterClicked);
            if (goToLoginButton != null)
                goToLoginButton.onClick.AddListener(ShowLoginPanel);

            // Profile panel buttons
            if (copyReferralButton != null)
                copyReferralButton.onClick.AddListener(CopyReferralCode);
            if (logoutButton != null)
                logoutButton.onClick.AddListener(OnLogoutClicked);
            if (closeProfileButton != null)
                closeProfileButton.onClick.AddListener(CloseProfilePanel);
        }

        private void SubscribeToEvents()
        {
            if (_playFabManager != null)
            {
                _playFabManager.OnLoginSuccess += OnLoginSuccessful;
                _playFabManager.OnLoginFailed += OnLoginFailed;
                _playFabManager.OnLogout += OnLogoutComplete;
                _playFabManager.OnPlayerDataLoaded += OnPlayerDataLoaded;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_playFabManager != null)
            {
                _playFabManager.OnLoginSuccess -= OnLoginSuccessful;
                _playFabManager.OnLoginFailed -= OnLoginFailed;
                _playFabManager.OnLogout -= OnLogoutComplete;
                _playFabManager.OnPlayerDataLoaded -= OnPlayerDataLoaded;
            }
        }

        #region Panel Management

        private void HideAllPanels()
        {
            if (authPanel != null) authPanel.SetActive(false);
            if (loginPanel != null) loginPanel.SetActive(false);
            if (registerPanel != null) registerPanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (profilePanel != null) profilePanel.SetActive(false);
        }

        public void ShowLoginPanel()
        {
            HideAllPanels();
            if (authPanel != null) authPanel.SetActive(true);
            if (loginPanel != null) loginPanel.SetActive(true);
            ClearErrors();
            
            // Clear inputs
            if (loginEmailInput != null) loginEmailInput.text = "";
            if (loginPasswordInput != null) loginPasswordInput.text = "";
        }

        public void ShowRegisterPanel()
        {
            HideAllPanels();
            if (authPanel != null) authPanel.SetActive(true);
            if (registerPanel != null) registerPanel.SetActive(true);
            ClearErrors();
            
            // Clear inputs
            if (registerEmailInput != null) registerEmailInput.text = "";
            if (registerPasswordInput != null) registerPasswordInput.text = "";
            if (registerConfirmPasswordInput != null) registerConfirmPasswordInput.text = "";
            if (registerDisplayNameInput != null) registerDisplayNameInput.text = "";
            if (referralCodeInput != null) referralCodeInput.text = "";
        }

        private void ShowLoadingPanel(string message = "Loading...")
        {
            if (loginPanel != null) loginPanel.SetActive(false);
            if (registerPanel != null) registerPanel.SetActive(false);
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
                if (loadingText != null) loadingText.text = message;
            }
        }

        public void ShowProfilePanel()
        {
            Debug.Log("[PlayFabAuthUI] ShowProfilePanel called");
            
            // Ensure we have PlayFabManager
            if (_playFabManager == null)
            {
                _playFabManager = PlayFabManager.Instance;
            }
            
            if (_playFabManager == null)
            {
                Debug.LogError("[PlayFabAuthUI] PlayFabManager not found!");
                return;
            }
            
            if (!_playFabManager.IsLoggedIn)
            {
                Debug.LogWarning("[PlayFabAuthUI] Not logged in - showing login panel instead");
                ShowLoginPanel();
                return;
            }
            
            // Only hide auth-related panels, not the main game panel
            if (loginPanel != null) loginPanel.SetActive(false);
            if (registerPanel != null) registerPanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);
            
            if (profilePanel != null)
            {
                profilePanel.SetActive(true);
                gameObject.SetActive(true); // Ensure this component's GO is active
                UpdateProfileUI();
                Debug.Log("[PlayFabAuthUI] Profile panel shown");
            }
            else
            {
                Debug.LogError("[PlayFabAuthUI] Profile panel reference is null! Assign it in the inspector.");
            }
        }

        private void CloseProfilePanel()
        {
            Debug.Log("[PlayFabAuthUI] CloseProfilePanel called");
            if (profilePanel != null) profilePanel.SetActive(false);
            if (mainGamePanel != null) mainGamePanel.SetActive(true);
        }

        private void ClearErrors()
        {
            if (loginErrorText != null)
            {
                loginErrorText.text = "";
                loginErrorText.gameObject.SetActive(false);
            }
            if (registerErrorText != null)
            {
                registerErrorText.text = "";
                registerErrorText.gameObject.SetActive(false);
            }
        }

        private void ShowLoginError(string error)
        {
            if (loginErrorText != null)
            {
                loginErrorText.text = error;
                loginErrorText.color = errorColor;
                loginErrorText.gameObject.SetActive(true);
            }
        }

        private void ShowRegisterError(string error)
        {
            if (registerErrorText != null)
            {
                registerErrorText.text = error;
                registerErrorText.color = errorColor;
                registerErrorText.gameObject.SetActive(true);
            }
        }

        #endregion

        #region Button Handlers

        private void OnLoginClicked()
        {
            string email = loginEmailInput?.text.Trim() ?? "";
            string password = loginPasswordInput?.text ?? "";

            // Validation
            if (string.IsNullOrEmpty(email))
            {
                ShowLoginError("Please enter your email");
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowLoginError("Please enter a valid email");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowLoginError("Please enter your password");
                return;
            }

            ShowLoadingPanel("Logging in...");
            _playFabManager.LoginWithEmail(email, password,
                onSuccess: null,
                onError: error =>
                {
                    ShowLoginPanel();
                    ShowLoginError(error);
                });
        }

        private void OnGuestLoginClicked()
        {
            ShowLoadingPanel("Creating guest account...");
            _playFabManager.LoginAsGuest(
                onSuccess: null,
                onError: error =>
                {
                    ShowLoginPanel();
                    ShowLoginError(error);
                });
        }

        private void OnRegisterClicked()
        {
            string email = registerEmailInput?.text.Trim() ?? "";
            string password = registerPasswordInput?.text ?? "";
            string confirmPassword = registerConfirmPasswordInput?.text ?? "";
            string displayName = registerDisplayNameInput?.text.Trim() ?? "";
            string referralCode = referralCodeInput?.text.Trim() ?? "";

            // Validation
            if (string.IsNullOrEmpty(email))
            {
                ShowRegisterError("Please enter your email");
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowRegisterError("Please enter a valid email");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowRegisterError("Please enter a password");
                return;
            }

            if (password.Length < 6)
            {
                ShowRegisterError("Password must be at least 6 characters");
                return;
            }

            if (password != confirmPassword)
            {
                ShowRegisterError("Passwords do not match");
                return;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = "Player" + UnityEngine.Random.Range(1000, 9999);
            }

            if (displayName.Length < 3)
            {
                ShowRegisterError("Display name must be at least 3 characters");
                return;
            }

            ShowLoadingPanel("Creating account...");
            _playFabManager.RegisterWithEmail(email, password, displayName,
                onSuccess: () =>
                {
                    // Apply referral code if provided
                    if (!string.IsNullOrEmpty(referralCode))
                    {
                        _playFabManager.ApplyReferralCode(referralCode);
                    }
                },
                onError: error =>
                {
                    ShowRegisterPanel();
                    ShowRegisterError(error);
                });
        }

        private void OnLogoutClicked()
        {
            _playFabManager.Logout();
        }

        private void CopyReferralCode()
        {
            if (_playFabManager.CurrentPlayerData != null && !string.IsNullOrEmpty(_playFabManager.CurrentPlayerData.ReferralCode))
            {
                GUIUtility.systemCopyBuffer = _playFabManager.CurrentPlayerData.ReferralCode;
                Debug.Log("Referral code copied to clipboard!");
                
                // Visual feedback
                if (copyReferralButton != null)
                {
                    var originalText = copyReferralButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (originalText != null)
                    {
                        string original = originalText.text;
                        originalText.text = "Copied!";
                        StartCoroutine(ResetButtonText(originalText, original, 1.5f));
                    }
                }
            }
        }

        private System.Collections.IEnumerator ResetButtonText(TextMeshProUGUI text, string originalText, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (text != null) text.text = originalText;
        }

        #endregion

        #region Event Handlers

        private void OnLoginSuccessful()
        {
            Debug.Log("[PlayFabAuthUI] Login successful!");
            HideAllPanels();
            
            if (authPanel != null) authPanel.SetActive(false);
            if (mainGamePanel != null) mainGamePanel.SetActive(true);
            
            // Update profile data
            if (_playFabManager.CurrentPlayerData != null)
            {
                UpdateProfileUI();
            }

            OnAuthenticationComplete?.Invoke();
        }

        private void OnLoginFailed(string error)
        {
            Debug.LogError($"[PlayFabAuthUI] Login failed: {error}");
            // Error is shown by the specific login/register methods
        }

        private void OnLogoutComplete()
        {
            Debug.Log("[PlayFabAuthUI] Logged out");
            ShowLoginPanel();
            OnLoggedOut?.Invoke();
        }

        private void OnPlayerDataLoaded(PlayFabPlayerData data)
        {
            UpdateProfileUI();
        }

        #endregion

        #region Profile UI

        private void UpdateProfileUI()
        {
            Debug.Log("[PlayFabAuthUI] UpdateProfileUI called");
            
            var data = _playFabManager?.CurrentPlayerData;
            if (data == null)
            {
                Debug.LogWarning("[PlayFabAuthUI] No player data available");
                
                // Show placeholder data
                if (profileNameText != null) profileNameText.text = "Not logged in";
                if (profileEmailText != null) profileEmailText.text = "---";
                if (profileRankingText != null) profileRankingText.text = "Ranking: ---";
                if (profileCoinsText != null) profileCoinsText.text = "Coins: ---";
                if (profileReferralCodeText != null) profileReferralCodeText.text = "Your Code: ---";
                if (profileWinRateText != null) profileWinRateText.text = "Win Rate: ---";
                return;
            }

            Debug.Log($"[PlayFabAuthUI] Updating profile: Name={data.PlayerName}, Coins={data.Coins}, Ranking={data.Ranking}");

            if (profileNameText != null)
                profileNameText.text = data.PlayerName ?? "Unknown";
            
            if (profileEmailText != null)
                profileEmailText.text = data.Email ?? "Guest Account";
            
            if (profileRankingText != null)
                profileRankingText.text = $"Ranking: {data.Ranking}";
            
            if (profileCoinsText != null)
                profileCoinsText.text = $"Coins: {data.Coins}";
            
            if (profileReferralCodeText != null)
                profileReferralCodeText.text = $"Your Code: {data.ReferralCode ?? "N/A"}";
            
            if (profileWinRateText != null)
                profileWinRateText.text = $"Win Rate: {data.WinRate:F1}% ({data.TotalWins}/{data.TotalGamesPlayed})";
        }

        /// <summary>
        /// Public method to refresh the profile UI
        /// </summary>
        public void RefreshProfile()
        {
            _playFabManager?.LoadPlayerData();
        }

        #endregion

        #region Helpers

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if user is authenticated
        /// </summary>
        public bool IsAuthenticated => _playFabManager?.IsLoggedIn ?? false;

        /// <summary>
        /// Get current player name
        /// </summary>
        public string GetPlayerName()
        {
            return _playFabManager?.CurrentPlayerData?.PlayerName ?? PlayerPrefs.GetString("PlayerName", "Player");
        }

        /// <summary>
        /// Get current coins
        /// </summary>
        public int GetCoins()
        {
            return _playFabManager?.CurrentPlayerData?.Coins ?? PlayerPrefs.GetInt("Coins", 0);
        }

        /// <summary>
        /// Wait for auto-login to complete before showing login panel
        /// </summary>
        private IEnumerator WaitForAutoLogin()
        {
            // Wait a short time for auto-login to attempt
            yield return new WaitForSeconds(0.5f);
            
            // Check again if logged in (auto-login might have succeeded)
            if (_playFabManager != null && _playFabManager.IsLoggedIn)
            {
                OnLoginSuccessful();
            }
            else
            {
                // Auto-login didn't work, show login panel
                ShowLoginPanel();
            }
        }

        #endregion
    }
}

