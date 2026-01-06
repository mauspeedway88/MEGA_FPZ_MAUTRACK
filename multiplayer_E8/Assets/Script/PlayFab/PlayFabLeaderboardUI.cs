using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Starter.PlayFabIntegration
{
    /// <summary>
    /// UI Component for displaying PlayFab leaderboard.
    /// Handles rendering leaderboard entries and highlighting the current player.
    /// </summary>
    public class PlayFabLeaderboardUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject leaderboardPanel;
        [SerializeField] private Transform entriesContainer;
        [SerializeField] private GameObject entryPrefab;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Current Player Info")]
        [SerializeField] private GameObject playerRankPanel;
        [SerializeField] private TextMeshProUGUI playerRankText;
        [SerializeField] private TextMeshProUGUI playerScoreText;

        [Header("Leaderboard Settings")]
        [SerializeField] private string leaderboardName = "Rankings";
        [SerializeField] private int maxEntries = 50;
        [SerializeField] private bool showAroundPlayer = false;

        [Header("Entry Colors")]
        [SerializeField] private Color currentPlayerColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color silverColor = new Color(0.75f, 0.75f, 0.75f);
        [SerializeField] private Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f);

        private PlayFabManager _playFabManager;
        private List<GameObject> _entryObjects = new List<GameObject>();
        private bool _eventsSubscribed = false;

        private void Start()
        {
            SetupButtonListeners();
            
            // Use this gameObject as the panel if not assigned
            if (leaderboardPanel == null)
            {
                leaderboardPanel = gameObject;
            }
            
            // Hide initially
            leaderboardPanel.SetActive(false);
        }

        private void OnEnable()
        {
            // Try to subscribe to events when enabled
            EnsurePlayFabManagerAndSubscribe();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void EnsurePlayFabManagerAndSubscribe()
        {
            if (_playFabManager == null)
            {
                _playFabManager = PlayFabManager.Instance;
            }
            
            if (_playFabManager != null && !_eventsSubscribed)
            {
                SubscribeToEvents();
            }
        }

        private void SetupButtonListeners()
        {
            if (refreshButton != null)
                refreshButton.onClick.AddListener(RefreshLeaderboard);

            if (closeButton != null)
                closeButton.onClick.AddListener(HideLeaderboard);
        }

        private void SubscribeToEvents()
        {
            if (_playFabManager != null && !_eventsSubscribed)
            {
                _playFabManager.OnLeaderboardLoaded += OnLeaderboardLoaded;
                _playFabManager.OnLeaderboardError += OnLeaderboardError;
                _eventsSubscribed = true;
                Debug.Log("[LeaderboardUI] Subscribed to PlayFab events");
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_playFabManager != null && _eventsSubscribed)
            {
                _playFabManager.OnLeaderboardLoaded -= OnLeaderboardLoaded;
                _playFabManager.OnLeaderboardError -= OnLeaderboardError;
                _eventsSubscribed = false;
            }
        }

        #region Public Methods

        /// <summary>
        /// Show the leaderboard panel and fetch data
        /// </summary>
        public void ShowLeaderboard()
        {
            Debug.Log("[LeaderboardUI] ShowLeaderboard called");
            
            // Make sure we have a panel reference
            if (leaderboardPanel == null)
            {
                leaderboardPanel = gameObject;
            }
            
            leaderboardPanel.SetActive(true);
            gameObject.SetActive(true); // Ensure this component is also active
            // Ensure we're subscribed to events
            EnsurePlayFabManagerAndSubscribe();
            
            RefreshLeaderboard();
        }

        /// <summary>
        /// Hide the leaderboard panel
        /// </summary>
        public void HideLeaderboard()
        {
            Debug.Log("[LeaderboardUI] HideLeaderboard called");
            
            if (leaderboardPanel != null)
            {
                leaderboardPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Refresh leaderboard data from PlayFab
        /// </summary>
        public void RefreshLeaderboard()
        {
            Debug.Log("[LeaderboardUI] RefreshLeaderboard called");
            
            // Try to get PlayFabManager
            if (_playFabManager == null)
            {
                _playFabManager = PlayFabManager.Instance;
            }
            
            if (_playFabManager == null)
            {
                Debug.LogError("[LeaderboardUI] PlayFabManager not found!");
                ShowError("PlayFab not initialized");
                return;
            }

            // Make sure events are subscribed
            if (!_eventsSubscribed)
            {
                SubscribeToEvents();
            }

            // Check if logged in
            if (!_playFabManager.IsLoggedIn)
            {
                Debug.LogWarning("[LeaderboardUI] Not logged into PlayFab - showing demo data");
                ShowError("Please login to view leaderboard");
                ShowLoading(false);
                return;
            }

            Debug.Log($"[LeaderboardUI] Fetching leaderboard: {leaderboardName}");
            ShowLoading(true);
            ClearEntries();

            if (showAroundPlayer)
            {
                _playFabManager.GetLeaderboardAroundPlayer(leaderboardName, maxEntries);
            }
            else
            {
                _playFabManager.GetLeaderboard(leaderboardName, maxEntries);
            }
        }

        #endregion

        #region Event Handlers

        private void OnLeaderboardLoaded(List<LeaderboardEntry> entries)
        {
            Debug.Log($"[LeaderboardUI] Leaderboard loaded with {entries?.Count ?? 0} entries");
            
            ShowLoading(false);
            ClearEntries();
            
            if (entries == null || entries.Count == 0)
            {
                ShowStatus("No players on leaderboard yet. Play some games to get ranked!");
                return;
            }
            
            PopulateLeaderboard(entries);
            UpdatePlayerRankDisplay();
        }

        private void OnLeaderboardError(string error)
        {
            Debug.LogError($"[LeaderboardUI] Leaderboard error: {error}");
            ShowLoading(false);
            ShowError($"Failed to load: {error}");
        }

        #endregion

        #region UI Updates

        private void PopulateLeaderboard(List<LeaderboardEntry> entries)
        {
            if (entriesContainer == null)
            {
                Debug.LogWarning("[LeaderboardUI] Missing entriesContainer reference");
                ShowError("UI not configured properly");
                return;
            }
            
            if (entryPrefab == null)
            {
                Debug.LogWarning("[LeaderboardUI] Missing entryPrefab reference - creating simple text entries");
                // Create simple text entries if no prefab
                foreach (var entry in entries)
                {
                    var entryGO = new GameObject($"Entry_{entry.Position}");
                    entryGO.transform.SetParent(entriesContainer, false);
                    
                    var text = entryGO.AddComponent<TextMeshProUGUI>();
                    text.text = $"#{entry.Position}  {entry.DisplayName}  -  {entry.Score}";
                    text.fontSize = 18;
                    text.color = entry.IsCurrentPlayer ? currentPlayerColor : Color.white;
                    
                    var rectTransform = entryGO.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(400, 30);
                    
                    _entryObjects.Add(entryGO);
                }
                
                ShowStatus($"Showing {entries.Count} players");
                return;
            }

            foreach (var entry in entries)
            {
                var entryObj = Instantiate(entryPrefab, entriesContainer);
                entryObj.SetActive(true);
                _entryObjects.Add(entryObj);

                SetupEntryUI(entryObj, entry);
            }

            ShowStatus($"Showing {entries.Count} players");
        }

        private void SetupEntryUI(GameObject entryObj, LeaderboardEntry entry)
        {
            // Find UI components in the entry prefab
            var rankText = entryObj.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>();
            var nameText = entryObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var scoreText = entryObj.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
            var background = entryObj.GetComponent<Image>();

            // Try alternative component names
            if (rankText == null) rankText = FindChildWithComponent<TextMeshProUGUI>(entryObj, "Rank");
            if (nameText == null) nameText = FindChildWithComponent<TextMeshProUGUI>(entryObj, "Name");
            if (scoreText == null) scoreText = FindChildWithComponent<TextMeshProUGUI>(entryObj, "Score");

            // Set rank with medal emoji for top 3
            if (rankText != null)
            {
                string rankDisplay = entry.Position switch
                {
                    1 => "🥇 1",
                    2 => "🥈 2",
                    3 => "🥉 3",
                    _ => $"#{entry.Position}"
                };
                rankText.text = rankDisplay;
            }

            // Set player name
            if (nameText != null)
            {
                nameText.text = entry.DisplayName;
                if (entry.IsCurrentPlayer)
                {
                    nameText.text += " (You)";
                }
            }

            // Set score
            if (scoreText != null)
            {
                scoreText.text = entry.Score.ToString();
            }

            // Set background color
            if (background != null)
            {
                Color bgColor = normalColor;
                
                if (entry.IsCurrentPlayer)
                {
                    bgColor = currentPlayerColor;
                }
                else
                {
                    bgColor = entry.Position switch
                    {
                        1 => new Color(goldColor.r, goldColor.g, goldColor.b, 0.3f),
                        2 => new Color(silverColor.r, silverColor.g, silverColor.b, 0.3f),
                        3 => new Color(bronzeColor.r, bronzeColor.g, bronzeColor.b, 0.3f),
                        _ => new Color(normalColor.r, normalColor.g, normalColor.b, 0.1f)
                    };
                }

                background.color = bgColor;
            }
        }

        private T FindChildWithComponent<T>(GameObject parent, string nameContains) where T : Component
        {
            foreach (Transform child in parent.transform)
            {
                if (child.name.ToLower().Contains(nameContains.ToLower()))
                {
                    var component = child.GetComponent<T>();
                    if (component != null) return component;
                }
            }
            return null;
        }

        private void UpdatePlayerRankDisplay()
        {
            if (playerRankPanel == null) return;

            if (_playFabManager != null && _playFabManager.IsLoggedIn && _playFabManager.CurrentPlayerData != null)
            {
                playerRankPanel.SetActive(true);

                if (playerRankText != null)
                {
                    // Find player position in cached leaderboard
                    var cachedEntries = _playFabManager.CachedLeaderboard;
                    var playerEntry = cachedEntries?.Find(e => e.IsCurrentPlayer);
                    Debug.LogWarning(playerEntry != null);
                    if (playerEntry != null)
                    {
                        playerRankText.text = $"Your Rank: #{playerEntry.Position}";
                    }
                    else
                    {
                        playerRankText.text = "Your Rank: Not ranked";
                    }
                }

                if (playerScoreText != null)
                {
                    playerScoreText.text = $"Total Wins: {_playFabManager.CurrentPlayerData.TotalWins}";
                }
            }
            else
            {
                playerRankPanel.SetActive(false);
            }
        }

        private void ClearEntries()
        {
            foreach (var obj in _entryObjects)
            {
                if (obj != null) Destroy(obj);
            }
            _entryObjects.Clear();
        }

        private void ShowLoading(bool show)
        {
            Debug.Log($"[LeaderboardUI] ShowLoading: {show}");
            
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(show);
            }

            if (entriesContainer != null)
            {
                entriesContainer.gameObject.SetActive(!show);
            }

            if (statusText != null)
            {
                if (show)
                {
                    statusText.text = "Loading leaderboard...";
                    statusText.color = Color.white;
                }
            }
        }

        private void ShowError(string error)
        {
            Debug.LogError($"[LeaderboardUI] Error: {error}");
            
            if (statusText != null)
            {
                statusText.text = error;
                statusText.color = new Color(1f, 0.5f, 0.5f); // Light red
            }
            
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(false);
            }
        }

        private void ShowStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = Color.white;
            }
        }

        #endregion
    }
}

