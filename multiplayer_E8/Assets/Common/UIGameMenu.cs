using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Starter
{
    /// <summary>
    /// Shows in-game menu, handles player connecting/disconnecting to the network game and cursor locking.
    /// 
    /// Integration with Lobby System:
    /// - If MainMenuController.ComingFromLobby is true, the connection UI is hidden
    ///   since players are already connected through the lobby.
    /// - Only disconnect/back to menu options are shown.
    /// </summary>
    public class UIGameMenu : MonoBehaviour
    {
        [Header("Start Game Setup")]
        [Tooltip("Specifies which game mode player should join - e.g. Platformer, ThirdPersonCharacter")]
        public string GameModeIdentifier;
        public NetworkRunner RunnerPrefab;
        public int MaxPlayerCount = 8;

        [Header("Debug")]
        [Tooltip("For debug purposes it is possible to force single-player game (starts faster)")]
        public bool ForceSinglePlayer;

        [Header("UI Setup")]
        public CanvasGroup PanelGroup;
        public TMP_InputField RoomText;
        public TMP_InputField NicknameText;
        public TextMeshProUGUI StatusText;
        public GameObject StartGroup;
        public GameObject DisconnectGroup;

        [Header("Lobby Integration")]
        [Tooltip("If true, hide this menu when players came from lobby")]
        public bool HideWhenFromLobby = true;

        private NetworkRunner _runnerInstance;
        private static string _shutdownStatus;
        private bool _cameFromLobby = false;

        private void Start()
        {
            // Check if we came from the lobby system
            _cameFromLobby = Lobby.MainMenuController.ComingFromLobby;
            
            if (_cameFromLobby && Lobby.MainMenuController.ActiveRunner != null)
            {
                // Use the runner from the lobby
                _runnerInstance = Lobby.MainMenuController.ActiveRunner;
                
                Debug.Log("[UIGameMenu] Using runner from lobby system");

                // Setup shutdown listener
                SetupRunnerEvents();

                // Hide the panel initially since we're already connected
                if (HideWhenFromLobby && PanelGroup != null)
                {
                    PanelGroup.gameObject.SetActive(false);
                }

                if (StatusText != null)
                {
                    StatusText.text = "Battle Started!";
                }
            }
            else
            {
                // Fallback: Check if there is ALREADY a runner in the scene (e.g. from Scene Load / TieBreaker)
                if (_runnerInstance == null)
                {
                    var existingRunners = FindObjectsOfType<NetworkRunner>();
                    foreach(var r in existingRunners)
                    {
                        if(r.IsRunning) 
                        {
                            _runnerInstance = r;
                            Debug.Log("[UIGameMenu] Found existing running Runner in scene. Hiding menu.");
                            
                            SetupRunnerEvents();
                            
                            if (PanelGroup != null) 
                                PanelGroup.gameObject.SetActive(false);
                                
                            if (StatusText != null)
                                StatusText.text = "Sudden Death!";
                            break;
                        }
                    }
                }
            }
        }

        private void SetupRunnerEvents()
        {
             if(_runnerInstance == null) return;
             var events = _runnerInstance.GetComponent<NetworkEvents>();
             if (events != null)
             {
                 events.OnShutdown.RemoveListener(OnShutdown); // Prevent duplicates
                 events.OnShutdown.AddListener(OnShutdown);
             }
        }

        public async void StartGame()
        {
            // Don't allow starting new game if we came from lobby
            if (_cameFromLobby)
            {
                Debug.Log("[UIGameMenu] Cannot start new game - came from lobby");
                return;
            }

            await Disconnect();

            PlayerPrefs.SetString("PlayerName", NicknameText.text);

            _runnerInstance = Instantiate(RunnerPrefab);

            // Add listener for shutdowns so we can handle unexpected shutdowns
            var events = _runnerInstance.GetComponent<NetworkEvents>();
            events.OnShutdown.AddListener(OnShutdown);

            var sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));

            var startArguments = new StartGameArgs()
            {
                GameMode = Application.isEditor && ForceSinglePlayer ? GameMode.Single : GameMode.Shared,
                SessionName = RoomText.text,
                PlayerCount = MaxPlayerCount,
                // We need to specify a session property for matchmaking to decide where the player wants to join.
                // Otherwise players from Platformer scene could connect to ThirdPersonCharacter game etc.
                SessionProperties = new Dictionary<string, SessionProperty> {["GameMode"] = GameModeIdentifier},
                Scene = sceneInfo,
            };

            StatusText.text = startArguments.GameMode == GameMode.Single ? "Starting single-player..." : "Connecting...";

            var startTask = _runnerInstance.StartGame(startArguments);
            await startTask;

            if (startTask.Result.Ok)
            {
                StatusText.text = "Waiting for other player...";
                PanelGroup.gameObject.SetActive(false);
            }
            else
            {
                StatusText.text = $"Connection Failed: {startTask.Result.ShutdownReason}";
            }
        }

        public async void DisconnectClicked()
        {
            await Disconnect();
        }

        public async void BackToMenu()
        {
            await Disconnect();

            // Clear lobby state
            Lobby.MainMenuController.ComingFromLobby = false;
            
            SceneManager.LoadScene(0);
        }

        public void TogglePanelVisibility()
        {
            if (PanelGroup.gameObject.activeSelf && _runnerInstance == null)
                return; // Panel cannot be hidden if the game is not running

            PanelGroup.gameObject.SetActive(!PanelGroup.gameObject.activeSelf);
        }

        private void OnEnable()
        {
            var nickname = PlayerPrefs.GetString("PlayerName");
            if (string.IsNullOrEmpty(nickname))
            {
                nickname = "Player" + Random.Range(10000, 100000);
            }

            if (NicknameText != null)
            {
                NicknameText.text = nickname;
            }

            // Try to load previous shutdown status
            if (StatusText != null)
            {
                StatusText.text = _shutdownStatus != null ? _shutdownStatus : string.Empty;
            }
            _shutdownStatus = null;
        }

        private void Update()
        {
            // Enter/Esc key is used for locking/unlocking cursor in game view.
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePanelVisibility();
            }

            if (PanelGroup != null && PanelGroup.gameObject.activeSelf)
            {
                // If we came from lobby, only show disconnect options
                if (_cameFromLobby)
                {
                    if (StartGroup != null) StartGroup.SetActive(false);
                    if (DisconnectGroup != null) DisconnectGroup.SetActive(true);
                    if (RoomText != null) RoomText.interactable = false;
                    if (NicknameText != null) NicknameText.interactable = false;
                }
                else
                {
                    if (StartGroup != null) StartGroup.SetActive(_runnerInstance == null);
                    if (DisconnectGroup != null) DisconnectGroup.SetActive(_runnerInstance != null);
                    if (RoomText != null) RoomText.interactable = _runnerInstance == null;
                    if (NicknameText != null) NicknameText.interactable = _runnerInstance == null;
                }

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public async Task Disconnect()
        {
            if (_runnerInstance == null)
                return;

            if (StatusText != null)
            {
                StatusText.text = "Disconnecting...";
            }
            
            if (PanelGroup != null)
            {
                PanelGroup.interactable = false;
            }

            // Remove shutdown listener since we are disconnecting deliberately
            var events = _runnerInstance.GetComponent<NetworkEvents>();
            if (events != null)
            {
                events.OnShutdown.RemoveListener(OnShutdown);
            }

            await _runnerInstance.Shutdown();
            
            // If the runner was from lobby, don't destroy it here (let Unity handle it)
            if (!_cameFromLobby)
            {
                // Runner will be destroyed by shutdown
            }
            
            _runnerInstance = null;

            // Clear lobby reference
            Lobby.MainMenuController.ActiveRunner = null;

            // Reset of scene network objects is needed, reload the whole scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnShutdown(NetworkRunner runner, ShutdownReason reason)
        {
            // Unexpected shutdown happened (e.g. Host disconnected)

            // Save status into static variable, it will be used in OnEnable after scene load
            _shutdownStatus = $"Shutdown: {reason}";
            Debug.LogWarning(_shutdownStatus);

            // Clear lobby state
            Lobby.MainMenuController.ComingFromLobby = false;
            Lobby.MainMenuController.ActiveRunner = null;

            // Reset of scene network objects is needed, reload the whole scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
