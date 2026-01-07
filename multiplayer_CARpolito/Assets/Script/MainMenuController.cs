using Fusion;
using Starter.PlayFabIntegration;
using Starter.Shooter;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Starter.Lobby
{
    /// <summary>
    /// Unified Main Menu Controller that integrates:
    /// - Public Random Matchmaking (2-Player Lobby)
    /// - Private Match (Password Lobby)
    /// - Seamless transition to combat scene
    /// 
    /// This replaces the simple UIMainMenu and provides the full lobby experience.
    /// </summary>
    public class MainMenuController : MonoBehaviour, INetworkRunnerCallbacks
    {
        public const int MAX_PLAYERS = 2;
        private const string PRIVATE_SESSION_PREFIX = "PRIVATE_";
        public enum MatchMode
        {
            None,
            PublicMatch,
            PrivateMatch
        }

        [Header("Network")]
        [SerializeField] private NetworkRunner runnerPrefab;
        
        [Header("Scene Configuration")]
        [Tooltip("Scene index to load after lobby (3 = 03_Shooter)")]
        public int combatSceneIndex = 3; // 03_Shooter scene - CHANGE IN INSPECTOR IF NEEDED

        [Header("UI - Main Menu Panel")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private Button findMatchButton;
        [SerializeField] private Button privateMatchButton;
        [SerializeField] private Button quitButton;

        [Header("UI - Password Panel")]
        [SerializeField] private GameObject passwordPanel;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TextMeshProUGUI passwordInstructionText;
        [SerializeField] private Button joinPrivateButton;
        [SerializeField] private Button passwordBackButton;
        [SerializeField] private TextMeshProUGUI passwordErrorText;

        [Header("UI - Searching Panel (Public Match)")]
        [SerializeField] private GameObject searchingPanel;
        [SerializeField] private TextMeshProUGUI searchingText;
        [SerializeField] private Button cancelSearchButton;

        [Header("UI - Waiting Panel (Private Match)")]
        [SerializeField] private GameObject waitingPanel;
        [SerializeField] private TextMeshProUGUI waitingText;
        [SerializeField] private TextMeshProUGUI roomCodeText;
        [SerializeField] private Button cancelWaitButton;

        [Header("UI - Lobby Panel (Both Players Connected)")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private TextMeshProUGUI lobbyTitleText;
        [SerializeField] private TextMeshProUGUI lobbyModeText;
        [Header("Lobby - Weapon Selection")]
        [SerializeField] private Transform weaponContainer;
        [SerializeField] private GameObject weaponItemPrefab;
        [SerializeField] private List<WeaponData> ownedWeapons = new List<WeaponData>();
        private int _selectedWeaponId = -1;
        private List<GameObject> _weaponUIItems = new List<GameObject>();

        [Header("UI - Player 1 Slot")]
        [SerializeField] private Image player1Background;
        [SerializeField] private TextMeshProUGUI player1NameText;
        [SerializeField] private TextMeshProUGUI player1StatusText;

        [Header("UI - Player 2 Slot")]
        [SerializeField] private Image player2Background;
        [SerializeField] private TextMeshProUGUI player2NameText;
        [SerializeField] private TextMeshProUGUI player2StatusText;

        [Header("UI - Ready Controls")]
        [SerializeField] private Button startBattleButton;
        [SerializeField] private TextMeshProUGUI startBattleButtonText;
        [SerializeField] private TextMeshProUGUI lobbyStatusText;
        [SerializeField] private Button leaveLobbyButton;

        [Header("UI - Transition Panel")]
        [SerializeField] private GameObject transitionPanel;
        [SerializeField] private TextMeshProUGUI transitionText;

        [Header("PlayFab Integration")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Button openShopBtn;
        [SerializeField] private Button exitshopBtn;

        [Header("Ads & Rewards")]
        [SerializeField] private Button adElixirButton;
        [SerializeField] private Button adCoinsButton;
        [SerializeField] private Button adWeapon1Button;
        [SerializeField] private Button adWeapon2Button;

        [Header("Colors")]
        [SerializeField] private Color connectedColor = new Color(0.2f, 0.7f, 0.2f, 1f);
        [SerializeField] private Color readyColor = new Color(0.1f, 0.9f, 0.1f, 1f);
        [SerializeField] private Color waitingColor = new Color(0.7f, 0.5f, 0.1f, 1f);
        [SerializeField] private Color emptySlotColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        [Header("PlayFab Integration")]
        [SerializeField] private PlayFabAuthUI playFabAuthUI;
        [SerializeField] private Button profileButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Image profileDisplayImage; // The image component to show the avatar
        [SerializeField] private TextMeshProUGUI coinsDisplayText;
        [SerializeField] private TextMeshProUGUI rankingDisplayText;
        [SerializeField] private TextMeshProUGUI playerNameDisplayText;

        [SerializeField] private PlayFabLeaderboardUI leaderboardUI;

        // Static reference to pass runner between scenes
        public static NetworkRunner ActiveRunner { get; set; }
        public static bool ComingFromLobby { get; set; }

        // State
        private NetworkRunner _runner;
        private MatchMode _currentMode = MatchMode.None;
        private string _currentRoomCode = "";
        private bool _isConnecting = false;
        private bool _localPlayerReady = false;
        private Dictionary<PlayerRef, bool> _playerReadyStates = new Dictionary<PlayerRef, bool>();
        private bool _isBattleStarting = false;
        private Coroutine _animationCoroutine;
        
        // PlayFab
        private PlayFabManager _playFabManager;
        private PlayFabGameIntegration _playFabIntegration;

        // --- SINGLETON & DEBUG RESTORED ---
        private static MainMenuController _instance;
        public static MainMenuController Instance => _instance;
        private string _debugLog = "";

        private void Awake()
        {
            _instance = this;
        }

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(_debugLog))
                GUI.Label(new Rect(10, 10, 1200, 800), _debugLog, new GUIStyle { fontSize = 23, normal = { textColor = Color.yellow }, wordWrap = true });

            // [RESTORED] DEBUG BUTTON - GUARANTEED METHOD
            if (GUI.Button(new Rect(Screen.width - 220, 10, 200, 100), "DEBUG: ADD COINS"))
            {
                 LogToScreen("DEBUG BUTTON CLICKED.");
                 OnAdButtonClicked("Coins");
            }
        }

        public static void LogToScreen(string msg)
        {
            if (_instance != null)
            {
                _instance._debugLog = msg + "\n" + _instance._debugLog;
                if (_instance._debugLog.Length > 2000) _instance._debugLog = _instance._debugLog.Substring(0, 2000);
            }
            Debug.Log(msg);
        }

        public void GrantCoinsDebug()
        {
            LogToScreen("[MainMenu] GrantCoinsDebug Called via External Redirection");
            OnAdButtonClicked("Coins");
        }
        // ----------------------------------

        #region Unity Lifecycle

        private void Start()
        {
            LogToScreen("MAIN MENU START. Checking EventSystem...");
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                 var es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
                 if (es == null) LogToScreen("CRITICAL ERROR: NO EVENT SYSTEM FOUND IN SCENE!");
                 else LogToScreen($"EventSystem found: {es.name}, Enabled: {es.enabled}");
            }
            else
            {
                 LogToScreen("EventSystem.current is VALID.");
            }
            // [MODIFICATION] FORCED REMOVAL - EXECUTE FIRST
            // We do this BEFORE HideAllPanels to ensure GameObject.Find works if they start active.
            // [NUCLEAR OPTION] Resources.FindObjectsOfTypeAll finds EVERYTHING (Active or Inactive)
            try
            {
                var allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                string[] targets = new string[] { "Ad Weapon 1", "Ad Weapon 2", "Ads Elixir", "Ad Weapon 1(Clone)", "Ads Elixir(Clone)" };
                
                foreach (var go in allGameObjects)
                {
                    // Filter out Assets (prefabs in project) -> allow only Scene objects
                    if (!go.scene.IsValid()) continue;

                    foreach (var target in targets)
                    {
                        if (go.name == target)
                        {
                            go.SetActive(false);
                            LogToScreen($"[NUCLEAR] DISABLED: {go.name}");
                        }
                    }
                }

                // [TEXT CONTENT SCAN - TMPro]
                var allTMPs = Resources.FindObjectsOfTypeAll<TMPro.TextMeshProUGUI>();
                foreach(var txt in allTMPs)
                {
                    if (!txt.gameObject.scene.IsValid()) continue;

                    if (txt.text.Contains("Ad Weapon") || txt.text.Contains("Ads Elixir"))
                    {
                        var parentBtn = txt.GetComponentInParent<Button>();
                        if (parentBtn != null) 
                        {
                            parentBtn.interactable = false;
                            parentBtn.gameObject.SetActive(false);
                            LogToScreen($"[TEXT-TMP] DISABLED PARENT OF: {txt.text}");
                        }
                        else
                        {
                            txt.transform.parent.gameObject.SetActive(false); // Hide container
                            LogToScreen($"[TEXT-TMP] DISABLED CONTAINER OF: {txt.text}");
                        }
                    }
                }

                // [TEXT CONTENT SCAN - Legacy Text]
                var allLegacyTexts = Resources.FindObjectsOfTypeAll<Text>();
                foreach(var txt in allLegacyTexts)
                {
                     if (!txt.gameObject.scene.IsValid()) continue;

                    if (txt.text.Contains("Ad Weapon") || txt.text.Contains("Ads Elixir"))
                    {
                        var parentBtn = txt.GetComponentInParent<Button>();
                        if (parentBtn != null) 
                        {
                            parentBtn.interactable = false;
                            parentBtn.gameObject.SetActive(false);
                            LogToScreen($"[TEXT-LEGACY] DISABLED PARENT OF: {txt.text}");
                        }
                        else
                        {
                            txt.transform.parent.gameObject.SetActive(false);
                            LogToScreen($"[TEXT-LEGACY] DISABLED CONTAINER OF: {txt.text}");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                LogToScreen($"Error in cleanup: {e.Message}");
            }
            
            // [FAILSAFE] dynamic search for Ads Coins if reference is missing
            if (adCoinsButton == null)
            {
                LogToScreen("[MainMenu] adCoinsButton reference is NULL. Searching...");
                var allBtns = Resources.FindObjectsOfTypeAll<Button>();
                foreach (var btn in allBtns)
                {
                    if (!btn.gameObject.scene.IsValid()) continue;
                    
                    // Check by Name
                    if (btn.name == "Ads Coins" || btn.name == "Ad Coins")
                    {
                        adCoinsButton = btn;
                        LogToScreen($"[MainMenu] FOUND 'Ads Coins' by name: {btn.name}");
                        break;
                    }

                    // Check by Text
                    var txt = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (txt != null && txt.text.Contains("Ads Coins"))
                    {
                        adCoinsButton = btn;
                        // [FIX] Disable Raycast on Text to ensure Button receives the click
                        txt.raycastTarget = false; 
                        LogToScreen($"[MainMenu] FOUND 'Ads Coins' by text. RaycastTarget disabled on text.");
                        break;
                    }
                }
            }
            
            // Explicit Reference Cleanup (Keep this as backup)
            if (adWeapon1Button != null) adWeapon1Button.gameObject.SetActive(false);
            if (adWeapon2Button != null) adWeapon2Button.gameObject.SetActive(false);
            if (adElixirButton != null) adElixirButton.gameObject.SetActive(false);

            SetupButtonListeners();
            // Start by hiding everything - wait for Auth status
            HideAllPanels();

            InitializePlayFab();
            
            // Clear any previous session data
            ActiveRunner = null;
            ComingFromLobby = false;
        }

        private void InitializePlayFab()
        {
            _playFabManager = PlayFabManager.Instance;
            _playFabIntegration = PlayFabGameIntegration.Instance;
            
            if (_playFabManager != null)
            {
                _playFabManager.OnLoginSuccess += OnPlayFabLoginSuccess;
                _playFabManager.OnPlayerDataLoaded += OnPlayFabDataLoaded;
                _playFabManager.OnLogout += OnPlayFabLogout;
                _playFabManager.OnRequireUserLogin += OnRequireUserLogin;
                
                // If already logged in, update UI and show menu
                if (_playFabManager.IsLoggedIn)
                {
                    OnPlayFabLoginSuccess();
                }
            }
        }
        
        private void OnRequireUserLogin()
        {
            Debug.Log("[MainMenu] User login required. Showing Auth UI.");
            if (playFabAuthUI != null)
            {
                playFabAuthUI.ShowLoginPanel();
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            
            // Unsubscribe from PlayFab events
            if (_playFabManager != null)
            {
                _playFabManager.OnLoginSuccess -= OnPlayFabLoginSuccess;
                _playFabManager.OnPlayerDataLoaded -= OnPlayFabDataLoaded;
                _playFabManager.OnLoginSuccess -= OnPlayFabLoginSuccess;
                _playFabManager.OnPlayerDataLoaded -= OnPlayFabDataLoaded;
                _playFabManager.OnLogout -= OnPlayFabLogout;
                _playFabManager.OnRequireUserLogin -= OnRequireUserLogin;
            }
        }

        private void Update()
        {
            if (_runner != null && _runner.IsRunning && !_isBattleStarting)
            {
                UpdateLobbyState();
            }
            
            // Update coins display in real-time (for when coins change from ads, etc.)
            UpdateCoinsDisplay();
        }

        private float _lastCoinsUpdate = 0f;
        private void UpdateCoinsDisplay()
        {
            // Throttle updates to every 0.5 seconds
            if (Time.time - _lastCoinsUpdate < 0.5f) return;
            _lastCoinsUpdate = Time.time;
            
            if (coinsDisplayText != null)
            {
                int coins = _playFabIntegration != null ? _playFabIntegration.GetCoins() : PlayerPrefs.GetInt("Coins", 0);
                coinsDisplayText.text = $"{coins}";
            }
        }

        #endregion

        #region Setup
        private void SetupButtonListeners()
        {
            // Main Menu
            if (findMatchButton != null)
                findMatchButton.onClick.AddListener(OnFindMatchClicked);
            if (privateMatchButton != null)
                privateMatchButton.onClick.AddListener(OnPrivateMatchClicked);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            // Password Panel
            if (joinPrivateButton != null)
                joinPrivateButton.onClick.AddListener(OnJoinPrivateClicked);
            if (passwordBackButton != null)
                passwordBackButton.onClick.AddListener(OnBackClicked);

            // Searching/Waiting
            if (cancelSearchButton != null)
                cancelSearchButton.onClick.AddListener(OnCancelClicked);
            if (cancelWaitButton != null)
                cancelWaitButton.onClick.AddListener(OnCancelClicked);

            // Lobby
            if (startBattleButton != null)
                startBattleButton.onClick.AddListener(OnStartBattleClicked);
            if (leaveLobbyButton != null)
                leaveLobbyButton.onClick.AddListener(OnLeaveLobbyClicked);

            // PlayFab - Profile & Leaderboard
            if (profileButton != null)
                profileButton.onClick.AddListener(OnProfileButtonClicked);
            if (leaderboardButton != null)
                leaderboardButton.onClick.AddListener(OnLeaderboardButtonClicked);
            //Shop
            if (openShopBtn != null)
                openShopBtn.onClick.AddListener(OnOpenShopButtonClicked);
            if (exitshopBtn != null)
                exitshopBtn.onClick.AddListener(OnExitShopButtonClicked);

            // Ads & Rewards
            if (adElixirButton != null)
                adElixirButton.onClick.AddListener(() => OnAdButtonClicked("Elixir"));
            
            if (adCoinsButton != null)
            {
                // [IMPORTANT FIX] Take control away from broken external scripts
                var externalScript = adCoinsButton.GetComponent("RewardedCoinsButton") as MonoBehaviour;
                if (externalScript != null)
                {
                    LogToScreen("[MainMenu] Found broken RewardedCoinsButton. Destroying component to take control.");
                    Destroy(externalScript);
                }
                
                adCoinsButton.onClick.RemoveAllListeners(); // Clear any previous junk
                adCoinsButton.onClick.AddListener(() => OnAdButtonClicked("Coins"));
                adCoinsButton.interactable = true;
                LogToScreen("[MainMenu] Took control of AdCoinsButton.");
            }

            if (adWeapon1Button != null)
                adWeapon1Button.onClick.AddListener(() => OnAdButtonClicked("Weapon1"));
            if (adWeapon2Button != null)
                adWeapon2Button.onClick.AddListener(() => OnAdButtonClicked("Weapon2"));
        }

        private void LoadPlayerName()
        {
            if (nicknameInput != null)
            {
                // Try to get name from PlayFab first, then PlayerPrefs
                string savedName = "";
                
                if (_playFabManager != null && _playFabManager.IsLoggedIn && _playFabManager.CurrentPlayerData != null)
                {
                    savedName = _playFabManager.CurrentPlayerData.PlayerName;
                }
                
                if (string.IsNullOrEmpty(savedName))
                {
                    savedName = PlayerPrefs.GetString("PlayerName", "");
                }
                
                if (string.IsNullOrEmpty(savedName))
                {
                    savedName = "Player" + Random.Range(1000, 9999);
                }
                
                nicknameInput.text = savedName;
            }
        }

        #region PlayFab Integration

        private void OnPlayFabLoginSuccess()
        {
            UpdatePlayFabUI();
            LoadPlayerName();
            LoadLocalProfileImage();
            ShowMainMenu(); // Navigate to main menu after successful login
        }

        private void OnPlayFabDataLoaded(PlayFabPlayerData data)
        {
            UpdatePlayFabUI();
            
            // Update nickname input with PlayFab name
            if (nicknameInput != null && !string.IsNullOrEmpty(data.PlayerName))
            {
                nicknameInput.text = data.PlayerName;
            }
            PopulateWeaponCarousel();
        }

        private void OnPlayFabLogout()
        {
            UpdatePlayFabUI();
        }

        private void UpdatePlayFabUI()
        {
            var data = _playFabManager?.CurrentPlayerData;
            
            if (_playFabManager != null && _playFabManager.IsLoggedIn && data != null)
            {
                if (coinsDisplayText != null)
                    coinsDisplayText.text = $"{data.Coins}";
                
                if (rankingDisplayText != null)
                    rankingDisplayText.text = $"Ranking: {data.Ranking}";
                
                if (playerNameDisplayText != null)
                    playerNameDisplayText.text = data.PlayerName ?? "Player";
            }
            else
            {
                // Use local data
                if (coinsDisplayText != null)
                    coinsDisplayText.text = $"{PlayerPrefs.GetInt("Coins", 0)}";
                if (rankingDisplayText != null)
                    rankingDisplayText.text = $"Ranking: {PlayerPrefs.GetInt("Ranking", 1000)}";
                
                if (playerNameDisplayText != null)
                    playerNameDisplayText.text = PlayerPrefs.GetString("PlayerName", "Player");
            }
        }

        /// <summary>
        /// Show the profile panel
        /// </summary>
        public void ShowProfile()
        {
            Debug.Log("[MainMenu] ShowProfile called");
            
            if (playFabAuthUI != null)
            {
                playFabAuthUI.ShowProfilePanel();
            }
            else
            {
                Debug.LogWarning("[MainMenu] PlayFabAuthUI not assigned! Please assign it in the inspector.");
            }
        }

        /// <summary>
        /// Show the leaderboard panel
        /// </summary>
        public void ShowLeaderboard()
        {
           
            if (leaderboardUI != null)
            {
                leaderboardUI.ShowLeaderboard();
            }
            else
            {
                Debug.LogWarning("[MainMenu] LeaderboardUI not found in scene. Please add PlayFabLeaderboardUI component.");
            }
        }
        public void ShowShop()
        {
            if(shopPanel != null)
            {
                shopPanel.SetActive(true);
                mainMenuPanel.SetActive(false);
            }
        }
        public void CloseShop()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
                mainMenuPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Called when profile button is clicked
        /// </summary>
        private void OnProfileButtonClicked()
        {
            Debug.Log("[MainMenu] Profile button clicked");
            ShowProfile();
        }

        /// <summary>
        /// Called when leaderboard button is clicked
        /// </summary>
        private void OnLeaderboardButtonClicked()
        {
            Debug.Log("[MainMenu] Leaderboard button clicked");
            ShowLeaderboard();
        }
        /// <summary>
        /// Called when Shop button is clicked
        /// </summary>
        private void OnOpenShopButtonClicked()
        {
            Debug.Log("[MainMenu] Shop button clicked");
            ShowShop();
        }
        /// <summary>
        /// Called when Shop button is clicked
        /// </summary>
        private void OnExitShopButtonClicked()
        {
            Debug.Log("[MainMenu] Shop button clicked");
            CloseShop();
        }

        private void OnAdButtonClicked(string rewardType)
        {
            LogToScreen($"[MainMenu] Ad button clicked for reward: {rewardType}");
            
            if (rewardType == "Coins")
            {
                LogToScreen("[MainMenu] Processing Coin Reward (Simulated)...");
                int rewardAmount = 50; // Requested by User

                // 1. PlayFab Add
                if (_playFabManager != null && _playFabManager.IsLoggedIn)
                {
                    LogToScreen("Adding coins via PlayFab...");
                    _playFabManager.AddCoins(rewardAmount, () =>
                    {
                        LogToScreen($"SUCCESS: Added {rewardAmount} Coins (Cloud).");
                        UpdateStatusMessage($"¡+{rewardAmount} MONEDAS (CLOUD)!");
                        UpdatePlayFabUI(); // Refresh UI immediately
                    });
                }
                else
                {
                    // 2. Local Fallback
                    LogToScreen("Adding coins via PlayerPrefs...");
                    int currentCoins = PlayerPrefs.GetInt("Coins", 0);
                    PlayerPrefs.SetInt("Coins", currentCoins + rewardAmount);
                    PlayerPrefs.Save();
                    LogToScreen($"SUCCESS: Added {rewardAmount} Coins (Local). Total: {currentCoins + rewardAmount}");
                    UpdateStatusMessage($"¡+{rewardAmount} MONEDAS (LOCAL)!");
                    if (coinsDisplayText != null) coinsDisplayText.text = (currentCoins + rewardAmount).ToString();
                }
            }
            else
            {
                LogToScreen($"Reward type '{rewardType}' not implemented yet.");
            }
        }

        /// <summary>
        /// Check if player is logged into PlayFab
        /// </summary>
        public bool IsPlayFabLoggedIn => _playFabManager?.IsLoggedIn ?? false;

        #endregion

        private void SavePlayerName()
        {
            if (nicknameInput != null && !string.IsNullOrEmpty(nicknameInput.text))
            {
                string playerName = nicknameInput.text;
                PlayerPrefs.SetString("PlayerName", playerName);
                PlayerPrefs.Save();
                
                if (_playFabManager != null && _playFabManager.IsLoggedIn)
                {
                    _playFabManager.CurrentPlayerData.PlayerName = playerName;
                    _playFabManager.SavePlayerData();
                }
            }
        }

        private void LoadLocalProfileImage()
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, "ProfileAvatar.png");
            if (System.IO.File.Exists(path))
            {
                try 
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(bytes))
                    {
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        if (profileDisplayImage != null) profileDisplayImage.sprite = sprite;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[MainMenu] Failed to load profile image: {e.Message}");
                }
            }
        }

        #endregion

        #region Panel Management

        private void HideAllPanels()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (passwordPanel != null) passwordPanel.SetActive(false);
            if (searchingPanel != null) searchingPanel.SetActive(false);
            if (waitingPanel != null) waitingPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            if (transitionPanel != null) transitionPanel.SetActive(false);
        }

        private void ShowMainMenu()
        {
            StopAnimation();
            HideAllPanels();
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            
            _currentMode = MatchMode.None;
            _currentRoomCode = "";
            _localPlayerReady = false;
            _playerReadyStates.Clear();
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ShowPasswordPanel()
        {
            HideAllPanels();
            if (passwordPanel != null) passwordPanel.SetActive(true);
            if (passwordInput != null) passwordInput.text = "";
            if (passwordErrorText != null) passwordErrorText.gameObject.SetActive(false);
        }

        private void ShowSearchingPanel()
        {
            HideAllPanels();
            if (searchingPanel != null) searchingPanel.SetActive(true);
            StartAnimation("SEARCHING FOR ANOTHER PLAYER", searchingText);
        }

        private void ShowWaitingPanel()
        {
            HideAllPanels();
            if (waitingPanel != null) waitingPanel.SetActive(true);
            if (roomCodeText != null) roomCodeText.text = $"Room Code: {_currentRoomCode}";
            StartAnimation("WAITING FOR OPPONENT", waitingText);
        }

        private void ShowLobbyPanel()
        {
            PopulateWeaponCarousel();
            StopAnimation();
            HideAllPanels();
            if (lobbyPanel != null) lobbyPanel.SetActive(true);

            if (lobbyTitleText != null)
                lobbyTitleText.text = "BATTLE LOBBY";

            if (lobbyModeText != null)
            {
                lobbyModeText.text = _currentMode == MatchMode.PublicMatch 
                    ? "Public Match" 
                    : $"Private Room: {_currentRoomCode}";
            }
        }

        private void ShowTransitionPanel()
        {
            StopAnimation();
            HideAllPanels();
            if (transitionPanel != null) transitionPanel.SetActive(true);
            if (transitionText != null) transitionText.text = "BATTLE STARTING!";
        }

        private void ShowPasswordError(string message)
        {
            if (passwordErrorText != null)
            {
                passwordErrorText.text = message;
                passwordErrorText.gameObject.SetActive(true);
            }
        }

        #endregion

        #region Animation

        private void StartAnimation(string baseText, TextMeshProUGUI targetText)
        {
            StopAnimation();
            _animationCoroutine = StartCoroutine(AnimateText(baseText, targetText));
        }

        private void StopAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
        }

        private IEnumerator AnimateText(string baseText, TextMeshProUGUI targetText)
        {
            int dots = 0;
            while (true)
            {
                if (targetText != null)
                {
                    targetText.text = baseText + new string('.', dots);
                }
                dots = (dots + 1) % 4;
                yield return new WaitForSeconds(0.4f);
            }
        }


        #endregion

        private TextMeshProUGUI statusText;

        private void CreateDebugResetButton()
        {
            if (GameObject.Find("DebugResetWeaponBtn")) return;

            GameObject canvasObj = GameObject.Find("Canvas") ?? GameObject.FindObjectOfType<Canvas>()?.gameObject;
            if (canvasObj == null) return;

            GameObject btnObj = new GameObject("DebugResetWeaponBtn");
            btnObj.transform.SetParent(canvasObj.transform, false);

            // Position: Bottom Right
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-20, 20);
            rect.sizeDelta = new Vector2(200, 50);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.8f, 0.2f, 0.2f, 1f); // Red

            Button btn = btnObj.AddComponent<Button>();
            
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "TEST: RESET WEAPON";
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            btn.onClick.AddListener(() =>
            {
                var pm = PlayFabManager.Instance;
                if (pm != null && pm.CurrentPlayerData != null)
                {
                    pm.CurrentPlayerData.OwnedWeapons.Clear(); // Remove all weapons
                    pm.CurrentPlayerData.SelectedWeapon = "";
                    UpdateStatusMessage("DEBUG: Armas eliminadas.\n¡Intenta Jugar ahora!");
                    Debug.Log("DEBUG: OwnedWeapons cleared via Button.");
                }
            });
        }

        private void CreateStatusTextFallback()
        {
            if (statusText != null) return;

            GameObject canvasObj = GameObject.Find("Canvas") ?? GameObject.FindObjectOfType<Canvas>()?.gameObject;
            if (canvasObj == null) return;

            GameObject txtObj = new GameObject("GlobalStatusText");
            txtObj.transform.SetParent(canvasObj.transform, false);
            
            statusText = txtObj.AddComponent<TextMeshProUGUI>();
            statusText.fontSize = 36;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = Color.red;
            statusText.raycastTarget = false;
            statusText.fontStyle = FontStyles.Bold;

            RectTransform rect = txtObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 50);
            rect.sizeDelta = new Vector2(800, 150);
        }

        private void UpdateStatusMessage(string message)
        {
            if (statusText == null) CreateStatusTextFallback();

            if (statusText != null)
            {
                statusText.text = message;
                statusText.gameObject.SetActive(true);
                statusText.transform.SetAsLastSibling(); // Ensure on top
                
                CancelInvoke(nameof(HideStatusText));
                Invoke(nameof(HideStatusText), 3f);
            }
        }

        private void HideStatusText()
        {
            if (statusText != null) statusText.gameObject.SetActive(false);
        }

        #region Button Handlers

        private void OnFindMatchClicked()
        {
            LogToScreen("Find Match Button CLICKED.");
            if (_isConnecting) return;

            // Enforce "Must have weapons to play" rule
            if (_playFabManager != null && (_playFabManager.CurrentPlayerData.OwnedWeapons == null || _playFabManager.CurrentPlayerData.OwnedWeapons.Count == 0))
            {
                Debug.LogWarning("[MainMenu] Cannot play without weapons! Please visit the Shop.");
                UpdateStatusMessage("¡NECESITAS UN ARMA PARA PELEAR!\nVe a la Tienda y equípate.");
                return;
            }

            // BETTING LOGIC: Entry Fee 50 Coins
            int entryFee = 50;
            if (_playFabManager != null && _playFabManager.CurrentPlayerData.Coins < entryFee)
            {
                UpdateStatusMessage($"¡APUESTA OBLIGATORIA!\nNecesitas {entryFee} Monedas para entrar.");
                return;
            }

            if (Starter.PlayFabIntegration.PlayFabGameIntegration.Instance != null)
            {
                Starter.PlayFabIntegration.PlayFabGameIntegration.Instance.SpendCoins(entryFee);
            }

            SavePlayerName();
            _currentMode = MatchMode.PublicMatch;
            ShowSearchingPanel();
            ConnectToLobby("");
        }

        private void OnPrivateMatchClicked()
        {
            SavePlayerName();
            ShowPasswordPanel();
        }

        private void OnJoinPrivateClicked()
        {
            if (_isConnecting) return;

            // Enforce "Must have weapons to play" rule for Private Matches too
            if (_playFabManager != null && (_playFabManager.CurrentPlayerData.OwnedWeapons == null || _playFabManager.CurrentPlayerData.OwnedWeapons.Count == 0))
            {
                ShowPasswordError("You need weapons to fight! Visit the Shop.");
                return;
            }

            string password = passwordInput != null ? passwordInput.text.Trim() : "";

            if (string.IsNullOrEmpty(password))
            {
                ShowPasswordError("Please enter a password!");
                return;
            }
            if (password.Length < 2)
            {
                ShowPasswordError("Password must be at least 2 characters!");
                return;
            }
            if (password.Length > 20)
            {
                ShowPasswordError("Password must be 20 characters or less!");
                return;
            }

            _currentMode = MatchMode.PrivateMatch;
            _currentRoomCode = password.ToUpper();
            ShowWaitingPanel();
            ConnectToLobby(PRIVATE_SESSION_PREFIX + _currentRoomCode);
        }

        private void OnBackClicked()
        {
            ShowMainMenu();
        }

        private async void OnCancelClicked()
        {
            await DisconnectAndReturn();
        }

        private void OnStartBattleClicked()
        {
            if (_runner == null || !_runner.IsRunning) return;

            _localPlayerReady = !_localPlayerReady;
            
            // Update local state
            _playerReadyStates[_runner.LocalPlayer] = _localPlayerReady;
            
            // Send to all players
            SendReadyState(_localPlayerReady);
            
            Debug.Log($"[MainMenu] Local player ready: {_localPlayerReady}");
        }

        private async void OnLeaveLobbyClicked()
        {
            await DisconnectAndReturn();
        }

        private void OnQuitClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#endif
        }

        #endregion

        #region Network Connection

        private async void ConnectToLobby(string sessionName)
        {
            _isConnecting = true;

            // Cleanup existing runner
            if (_runner != null)
            {
                _runner.RemoveCallbacks(this);
                await _runner.Shutdown();
                Destroy(_runner.gameObject);
                _runner = null;
            }

            // Create new runner
            _runner = Instantiate(runnerPrefab);
            _runner.AddCallbacks(this);

            var sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));

            var gameModeName = _currentMode == MatchMode.PublicMatch ? "PublicMatch2v2" : "PrivateMatch";

            var startArgs = new StartGameArgs()
            {
                GameMode = GameMode.Shared,
                SessionName = sessionName, // Empty for public, password-based for private
                PlayerCount = MAX_PLAYERS,
                SessionProperties = new Dictionary<string, SessionProperty>
                {
                    ["GameMode"] = gameModeName
                },
                Scene = sceneInfo,
                SceneManager = _runner.GetComponent<INetworkSceneManager>()
            };

            Debug.Log($"[MainMenu] Connecting... Mode: {_currentMode}, Session: {sessionName}");

            var result = await _runner.StartGame(startArgs);

            _isConnecting = false;

            if (result.Ok)
            {
                Debug.Log("[MainMenu] Connected successfully!");
            }
            else
            {
                Debug.LogError($"[MainMenu] Connection failed: {result.ShutdownReason}");
                
                if (_currentMode == MatchMode.PrivateMatch)
                {
                    ShowPasswordPanel();
                    ShowPasswordError($"Connection failed: {result.ShutdownReason}");
                }
                else
                {
                    ShowMainMenu();
                }
            }
        }

        private async System.Threading.Tasks.Task DisconnectAndReturn()
        {
            _isConnecting = false;
            _isBattleStarting = false;

            if (_runner != null)
            {
                _runner.RemoveCallbacks(this);
                await _runner.Shutdown();
                Destroy(_runner.gameObject);
                _runner = null;
            }

            ShowMainMenu();
        }

        #endregion

        #region Lobby State Management

        private void UpdateLobbyState()
        {
            int playerCount = GetPlayerCount();

            if (playerCount < MAX_PLAYERS)
            {
                // Still waiting for opponent
                if (_currentMode == MatchMode.PublicMatch && !searchingPanel.activeSelf)
                {
                    ShowSearchingPanel();
                }
                else if (_currentMode == MatchMode.PrivateMatch && !waitingPanel.activeSelf)
                {
                    ShowWaitingPanel();
                }
            }
            else
            {
                // Both players connected
                if (!lobbyPanel.activeSelf && !transitionPanel.activeSelf)
                {
                    ShowLobbyPanel();
                }

                UpdatePlayerSlots();
                UpdateStartButton();
                UpdateLobbyStatus();
                CheckAllReady();
            }
        }

        private int GetPlayerCount()
        {
            if (_runner == null) return 0;
            int count = 0;
            foreach (var player in _runner.ActivePlayers)
            {
                count++;
            }
            return count;
        }

        private void UpdatePlayerSlots()
        {
            var players = new List<PlayerRef>();
            foreach (var player in _runner.ActivePlayers)
            {
                players.Add(player);
            }

            // Player 1
            if (players.Count >= 1)
            {
                bool isLocal = players[0] == _runner.LocalPlayer;
                bool isReady = _playerReadyStates.ContainsKey(players[0]) && _playerReadyStates[players[0]];
                UpdateSlot(player1Background, player1NameText, player1StatusText,
                    GetDisplayName(isLocal), true, isReady);
            }

            // Player 2
            if (players.Count >= 2)
            {
                bool isLocal = players[1] == _runner.LocalPlayer;
                bool isReady = _playerReadyStates.ContainsKey(players[1]) && _playerReadyStates[players[1]];
                UpdateSlot(player2Background, player2NameText, player2StatusText,
                    GetDisplayName(isLocal), true, isReady);
            }
        }

        private void UpdateSlot(Image bg, TextMeshProUGUI nameText, TextMeshProUGUI statusText,
            string name, bool connected, bool ready)
        {
            if (bg != null)
            {
                bg.color = !connected ? emptySlotColor : (ready ? readyColor : connectedColor);
            }
            if (nameText != null)
            {
                nameText.text = name;
            }
            if (statusText != null)
            {
                statusText.text = !connected ? "" : (ready ? "READY!" : "Connected");
            }
        }

        private void UpdateStartButton()
        {
            if (startBattleButton == null) return;

            var colors = startBattleButton.colors;
            
            if (_localPlayerReady)
            {
                if (startBattleButtonText != null) startBattleButtonText.text = "WAITING...";
                colors.normalColor = waitingColor;
            }
            else
            {
                if (startBattleButtonText != null) startBattleButtonText.text = "START BATTLE";
                colors.normalColor = connectedColor;
            }
            
            startBattleButton.colors = colors;
        }

        private void UpdateLobbyStatus()
        {
            if (lobbyStatusText == null) return;

            int readyCount = 0;
            foreach (var kvp in _playerReadyStates)
            {
                if (kvp.Value) readyCount++;
            }

            if (readyCount == 0)
                lobbyStatusText.text = "Both players must press START BATTLE";
            else if (readyCount == 1)
                lobbyStatusText.text = "Waiting for opponent...";
            else
                lobbyStatusText.text = "BATTLE STARTING!";
        }

        private void CheckAllReady()
        {
            if (_isBattleStarting) return;
            if (GetPlayerCount() < MAX_PLAYERS) return;

            int readyCount = 0;
            foreach (var kvp in _playerReadyStates)
            {
                if (kvp.Value) readyCount++;
            }

            if (readyCount >= MAX_PLAYERS)
            {
                StartBattle();
            }
        }

        private string GetDisplayName(bool isLocal)
        {
            if (isLocal)
            {
                return PlayerPrefs.GetString("PlayerName", "You") + " (You)";
            }
            return "Opponent";
        }

        #endregion

        #region Ready State Sync (Using ReliableData)

        private void SendReadyState(bool ready)
        {
            if (_runner == null) return;

            // Create message: [PlayerRef ID (4 bytes)][Ready state (1 byte)]
            byte[] data = new byte[5];
            System.BitConverter.GetBytes(_runner.LocalPlayer.PlayerId).CopyTo(data, 0);
            data[4] = ready ? (byte)1 : (byte)0;

            // Send to all players
            foreach (var player in _runner.ActivePlayers)
            {
                if (player != _runner.LocalPlayer)
                {
                    _runner.SendReliableDataToPlayer(player, new Fusion.Sockets.ReliableKey(), data);
                }
            }
        }

        #endregion

        #region Battle Start

        private void StartBattle()
        {
            if (_isBattleStarting) return;
            _isBattleStarting = true;

            Debug.Log("[MainMenu] All players ready! Starting battle...");
            ShowTransitionPanel();

            // Store runner reference for the combat scene
            ActiveRunner = _runner;
            ComingFromLobby = true;

            // Don't destroy the runner - it will be used in the combat scene
            if (_runner != null)
            {
                _runner.RemoveCallbacks(this);
                DontDestroyOnLoad(_runner.gameObject);
            }

            StartCoroutine(LoadCombatScene());
        }

        private IEnumerator LoadCombatScene()
        {
            yield return new WaitForSeconds(1.5f);

            if (_runner != null && _runner.IsRunning)
            {
                // Only master client can load scenes in Shared mode
                if (_runner.IsSharedModeMasterClient)
                {
                    Debug.Log("[MainMenu] Master client loading combat scene...");
                    _runner.LoadScene(SceneRef.FromIndex(combatSceneIndex));
                }
                else
                {
                    Debug.Log("[MainMenu] Waiting for master client to load scene...");
                    // Non-master clients will be automatically transitioned by Fusion
                }
            }
        }

        #endregion

        #region INetworkRunnerCallbacks

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[MainMenu] Player joined: {player}");
            
            // Initialize ready state for new player
            if (!_playerReadyStates.ContainsKey(player))
            {
                _playerReadyStates[player] = false;
            }

            // If we're already ready, send our state to the new player
            if (_localPlayerReady && player != runner.LocalPlayer)
            {
                SendReadyState(_localPlayerReady);
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[MainMenu] Player left: {player}");
            
            _playerReadyStates.Remove(player);
            
            // Cancel battle start if it was in progress
            if (_isBattleStarting)
            {
                _isBattleStarting = false;
                ShowLobbyPanel();
            }
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, Fusion.Sockets.ReliableKey key, System.ArraySegment<byte> data)
        {
            if (data.Count >= 5)
            {
                int playerId = System.BitConverter.ToInt32(data.Array, data.Offset);
                bool ready = data.Array[data.Offset + 4] == 1;

                // Find the player ref with this ID
                foreach (var p in runner.ActivePlayers)
                {
                    if (p.PlayerId == playerId)
                    {
                        _playerReadyStates[p] = ready;
                        Debug.Log($"[MainMenu] Received ready state from player {p}: {ready}");
                        break;
                    }
                }
            }
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"[MainMenu] Shutdown: {shutdownReason}");
            if (!_isBattleStarting)
            {
                ShowMainMenu();
            }
        }

        // Unused callbacks
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, Fusion.Sockets.NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, Fusion.Sockets.NetAddress remoteAddress, Fusion.Sockets.NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, Fusion.Sockets.ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        #endregion

        #region Lobby Weapon Management

        public void PopulateWeaponCarousel()
        {
            // Clear previous items
            foreach (var item in _weaponUIItems)
                Destroy(item);
            _weaponUIItems.Clear();

            if (_playFabManager == null || _playFabManager.CurrentPlayerData == null) return;

            var ownedWeaponIds = _playFabManager.CurrentPlayerData.OwnedWeapons;
            if (ownedWeaponIds.Count == 0) return;

            for (int i = 0; i < ownedWeaponIds.Count; i++)
            {
                int weaponId = ownedWeaponIds[i];
                var weaponData = GetWeaponDataById(weaponId);
                if (weaponData == null) continue;

                GameObject itemGO = Instantiate(weaponItemPrefab, weaponContainer);
                _weaponUIItems.Add(itemGO);

                var itemScript = itemGO.GetComponent<WeaponItem>();
                if (itemScript != null)
                {
                    itemScript.Setup(weaponData, OnWeaponSelected);
                }

                // Automatically select the first weapon
                if (i == 0)
                {
                    OnWeaponSelected(weaponId);
                }
            }
        }

        private void OnWeaponSelected(int weaponId)
        {
            _selectedWeaponId = weaponId;
            PlayerPrefs.SetInt("SelectedWeaponId", weaponId); // save globally
            PlayerPrefs.Save();
            // Highlight selected weapon
            foreach (var itemGO in _weaponUIItems)
            {
                var itemScript = itemGO.GetComponent<WeaponItem>();
                var bg = itemGO.GetComponent<Image>();
                if (itemScript != null && bg != null)
                {
                    bg.color = (itemScript.WeaponId == weaponId) ? Color.green : Color.white;
                }
            }

            var weaponData = GetWeaponDataById(weaponId);
            if (weaponData != null)
            {
                Debug.LogWarning($"Selected weapon: {weaponData.WeaponName}");
            }
        }

        private WeaponData GetWeaponDataById(int id)
        {
            // Assuming you have a master list of all WeaponData assets
            foreach (var weapon in ownedWeapons)
            {
                if (weapon.WeaponID == id) return weapon;
            }
            Debug.LogWarning($"WeaponData not found for ID: {id}");
            return null;
        }

        #endregion

    }
}

