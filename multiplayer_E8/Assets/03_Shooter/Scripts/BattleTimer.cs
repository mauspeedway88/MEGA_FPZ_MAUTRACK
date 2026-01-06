using Fusion;
using Starter.PlayFabIntegration;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Starter.Shooter
{
    /// <summary>
    /// Handles the battle timer and tiebreaker (Sudden Death) mode.
    /// When time runs out with a tie, enters Sudden Death - first kill wins!
    /// </summary>
    public class BattleTimer : NetworkBehaviour
    {
        public enum GamePhase
        {
            Battle,         // Normal battle phase
            TiebreakCountdown,  // Countdown before sudden death
            SuddenDeath,    // First kill wins
            GameOver        // Winner declared
        }
        
        [Header("Timer Settings")]
        [Tooltip("Battle duration in seconds (8 minutes = 480)")]
        public float BattleDuration = 480f;
        
        [Header("Win Conditions")]
        [Tooltip("Number of kills needed to win in normal mode")]
        public int KillsToWin = 10;
        
        [Header("Sudden Death Settings")]
        [Tooltip("Countdown time before sudden death starts")]
        public float SuddenDeathCountdown = 3f;
        [Tooltip("Max duration for sudden death (0 = unlimited)")]
        public float SuddenDeathDuration = 60f;
        
        [Header("UI References")]
        public TextMeshProUGUI TimerText;
        public TextMeshProUGUI WinnerText;
        public GameObject WinnerPanel;
        public TextMeshProUGUI GamePhaseText;
        
        [Header("Results")]
        [Tooltip("Time to show results before returning to menu")]
        public float ResultsDisplayTime = 5f;
        
        [Networked]
        public TickTimer BattleEndTimer { get; set; }
        
        [Networked]
        public TickTimer SuddenDeathTimer { get; set; }
        
        [Networked]
        public NetworkBool BattleEnded { get; set; }
        
        [Networked]
        public PlayerRef Winner { get; set; }
        
        [Networked] public GamePhase CurrentPhase { get; set; }
        [Networked] public NetworkString<_32> WinnerName { get; set; }
        [Networked] public NetworkString<_32> LoserName { get; set; }

        [Networked] public NetworkDictionary<PlayerRef, int> KillsAtTie { get; }

        private GameManager _gameManager;
        private bool _initialized;
        private bool _resultReported;
        
        public override void Spawned()
        {
            _gameManager = FindObjectOfType<GameManager>();

            if (HasStateAuthority)
            {
                BattleEndTimer = TickTimer.CreateFromSeconds(Runner, BattleDuration);
                BattleEnded = false;
                Winner = PlayerRef.None;
                CurrentPhase = GamePhase.Battle;
                KillsAtTie.Clear();
                
                // FORCE ONE-SHOT MATCH ENDING
                KillsToWin = 1; 

                // TIEBREAKER SETUP
                if (_gameManager != null && _gameManager.IsTieBreaker)
                {
                    BattleDuration = 20f; // Force 20 seconds
                    BattleEndTimer = TickTimer.CreateFromSeconds(Runner, BattleDuration);
                    Debug.Log("[BattleTimer] TIEBREAKER MODE: Duration set to 20s");
                }
            }



            _initialized = true;
            
            if (WinnerPanel != null)
            {
                WinnerPanel.SetActive(false);
            }
            
            Debug.Log($"[BattleTimer] Started - Duration: {BattleDuration}s, KillsToWin: {KillsToWin}");
        }

        public override void Render()
        {
            // Sync from Networked variables to local static results & PlayerPrefs
            if (!string.IsNullOrEmpty(WinnerName.Value) && WinnerName.Value != "---")
            {
                // Only write if changed to avoid spam, or just overwrite (PlayerPrefs is fast enough for once per frame in menu, but let's be safe)
                if (GameResults.WinnerName != WinnerName.Value)
                {
                    Debug.Log($"[BattleTimer] Networked Name Change Detected: {WinnerName.Value} vs {LoserName.Value}");
                    
                    GameResults.WinnerName = WinnerName.Value;
                    GameResults.LoserName = LoserName.Value;
                    
                    PlayerPrefs.SetString("LastWinner", WinnerName.Value);
                    PlayerPrefs.SetString("LastLoser", LoserName.Value);
                    PlayerPrefs.Save();
                }
            }
        }
        
        public override void FixedUpdateNetwork()
        {
            if (!_initialized)
                return;
            
            if (!HasStateAuthority)
                return;
                
            switch (CurrentPhase)
            {
                case GamePhase.Battle:
                    UpdateBattlePhase();
                    break;
                    
                case GamePhase.TiebreakCountdown:
                    UpdateTiebreakCountdown();
                    break;
                    
                case GamePhase.SuddenDeath:
                    UpdateSuddenDeath();
                    break;
                    
                case GamePhase.GameOver:
                    UpdateGameOver();
                    break;
            }
        }
        
        private void UpdateBattlePhase()
        {
            // Check for winner by kills
            CheckWinByKills();
            
            // Check if time ran out
            if (BattleEndTimer.Expired(Runner) && !BattleEnded)
            {
                OnBattleTimeExpired();
            }
        }
        
        private void UpdateTiebreakCountdown()
        {
            if (SuddenDeathTimer.Expired(Runner))
            {
                StartSuddenDeath();
            }
        }
        
        private void UpdateSuddenDeath()
        {
            // Check for sudden death winner (first kill after tie)
            CheckSuddenDeathWinner();
            
            // Check sudden death time limit
            if (SuddenDeathDuration > 0 && SuddenDeathTimer.Expired(Runner))
            {
                // Time ran out in sudden death - declare winner by most recent kill or random
                DeclareSuddenDeathTimeout();
            }
        }
        
        private void UpdateGameOver()
        {
            if (BattleEndTimer.Expired(Runner))
            {
                ReturnToMainMenu();
            }
        }
        
        private void CheckWinByKills()
        {
            if (BattleEnded)
                return;
                
            foreach (var playerRef in Runner.ActivePlayers)
            {
                var playerObject = Runner.GetPlayerObject(playerRef);
                var player = playerObject != null ? playerObject.GetComponent<Player>() : null;
                
                if (player != null && player.Kills >= KillsToWin)
                {
                    DeclareWinner(playerRef);
                    return;
                }
            }
        }

        private void CheckSuddenDeathWinner()
        {
            foreach (var playerRef in Runner.ActivePlayers)
            {
                if (!KillsAtTie.ContainsKey(playerRef))
                    continue;

                var obj = Runner.GetPlayerObject(playerRef);
                var player = obj ? obj.GetComponent<Player>() : null;

                if (player == null)
                    continue;

                if (player.Kills > KillsAtTie[playerRef])
                {
                    DeclareWinner(playerRef);
                    return;
                }
            }
        }
        
        private void DeclareWinner(PlayerRef winner)
        {
            if (BattleEnded)
                return;
                
            BattleEnded = true;
            Winner = winner;
            CurrentPhase = GamePhase.GameOver;

            string wName = "---";
            string lName = "---";

            var allPlayers = FindObjectsOfType<Player>(true);
            foreach (var p in allPlayers)
            {
                if (p.Object != null && p.Object.InputAuthority == winner)
                {
                    wName = string.IsNullOrEmpty(p.Nickname) ? $"Player {winner.PlayerId}" : p.Nickname;
                }
                else if (p.Object != null && p.Object.InputAuthority != winner && p.Object.InputAuthority != PlayerRef.None)
                {
                    lName = string.IsNullOrEmpty(p.Nickname) ? $"Player {p.Object.InputAuthority.PlayerId}" : p.Nickname;
                }
            }

            // CLEAN NAMES
            if (wName.StartsWith("Guest", System.StringComparison.OrdinalIgnoreCase)) wName = "Guest";
            if (lName.StartsWith("Guest", System.StringComparison.OrdinalIgnoreCase)) lName = "Guest";

            // NETWORK SYNC (State Authority only)
            WinnerName = wName;
            LoserName = lName;

            // Update local results class too
            GameResults.WinnerName = wName;
            GameResults.LoserName = lName;
            
            // LOCAL PERSISTENCE
            PlayerPrefs.SetString("LastWinner", wName);
            PlayerPrefs.SetString("LastLoser", lName);
            PlayerPrefs.Save();
            
            BattleEndTimer = TickTimer.CreateFromSeconds(Runner, 4.0f); // More time for sync
            RPC_ReportResult(winner);
            
            Debug.Log($"[BattleTimer] Winner: {wName}, Loser: {lName}. Names saved to Networked properties.");
        }




        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ReportResult(PlayerRef winner)

        {
            if (_resultReported)
                return;
            _resultReported = true;
            
            var playFabIntegration = PlayFabGameIntegration.Instance;
            if (playFabIntegration != null && Runner != null)
            {
                bool localWon = winner == Runner.LocalPlayer;
                
                // Get local player's kills
                var localPlayerObj = Runner.GetPlayerObject(Runner.LocalPlayer);
                int kills = 0;
                if (localPlayerObj != null)
                {
                    var player = localPlayerObj.GetComponent<Player>();
                    if (player != null) kills = player.Kills;
                }
                
                playFabIntegration.ReportGameResult(localWon, kills);
                Debug.Log($"[BattleTimer] Reported to PlayFab: Won={localWon}, Kills={kills}");
            }
        }

        private void OnBattleTimeExpired()
        {
            // TIE BREAKER TIME EXPIRED LOGIC
            if (_gameManager != null && _gameManager.IsTieBreaker)
            {
                 Debug.Log("[BattleTimer] TieBreaker Time Expired. Evaluating winner by HEALTH.");
                 
                 Player winner = null;
                 float maxHealth = -1;
                 
                 foreach (var playerRef in Runner.ActivePlayers)
                 {
                     var obj = Runner.GetPlayerObject(playerRef);
                     var p = obj ? obj.GetComponent<Player>() : null;
                     if (p != null && p.Health.CurrentHealth > maxHealth)
                     {
                         maxHealth = p.Health.CurrentHealth;
                         winner = p;
                     }
                 }
                 
                 if (winner != null)
                 {
                     Debug.Log($"[BattleTimer] TimeUp Winner: {winner.Nickname} (Health: {maxHealth})");
                     
                     // 1. Disable ALL Players (Visual Disappearance)
                     foreach(var playerRef in Runner.ActivePlayers)
                     {
                        var obj = Runner.GetPlayerObject(playerRef);
                        if(obj != null) obj.gameObject.SetActive(false);
                     }
                     
                     // 2. Award coins if local
                     if (winner == _gameManager.LocalPlayer)
                     {
                         _gameManager.AddCoins(GameManager.PendingBet * 2);
                     }
                     
                     // 3. Declare Winner (Shows UI) and Wait for Scene Change
                     DeclareWinner(winner.Object.InputAuthority);
                 }
                 
                 return;
            }

            var players = Runner.ActivePlayers.ToArray();

            if (players.Length < 2)
                return;

            var p1 = Runner.GetPlayerObject(players[0])?.GetComponent<Player>();
            var p2 = Runner.GetPlayerObject(players[1])?.GetComponent<Player>();

            if (p1 == null || p2 == null)
                return;

            if (p1.Kills != p2.Kills)
            {
                DeclareWinner(p1.Kills > p2.Kills ? players[0] : players[1]);
            }
            else
            {
                StartTieBreaker(players);
            }
        }
        private void StartTieBreaker(PlayerRef[] tiedPlayers)
        {
            if (!Runner.IsSharedModeMasterClient)
                return;

            Debug.Log("[BattleTimer] Tie detected → loading TieBreaker scene");

            TieBreakerState.TiedPlayers = tiedPlayers;

            int sceneIndex = SceneUtility.GetBuildIndexByScenePath("04_Tiebreak");
            if (sceneIndex < 0)
            {
                Debug.LogError("[BattleTimer] CRITICAL: Scene '04_Tiebreak' not found in Build Settings! Cannot load TieBreaker.");
                return;
            }

            Runner.LoadScene(SceneRef.FromIndex(sceneIndex));
        }
        public static class TieBreakerState
        {
            public static PlayerRef[] TiedPlayers;
        }


        private void StartTiebreakCountdown(int score1, int score2)
        {
            CurrentPhase = GamePhase.TiebreakCountdown;
            KillsAtTie.Clear();

            foreach (var playerRef in Runner.ActivePlayers)
            {
                var obj = Runner.GetPlayerObject(playerRef);
                var player = obj ? obj.GetComponent<Player>() : null;

                if (player != null)
                    KillsAtTie.Add(playerRef, player.Kills);
            }
            SuddenDeathTimer = TickTimer.CreateFromSeconds(Runner, SuddenDeathCountdown);
            
            // Reset player health for sudden death
            RPC_PrepareSuddenDeath();
            
            Debug.Log($"[BattleTimer] Tiebreak countdown started. Scores at tie: {score1} vs {score2}");
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PrepareSuddenDeath()
        {
            if (!Runner.IsServer && !Runner.IsSharedModeMasterClient)
                return;

            foreach (var playerRef in Runner.ActivePlayers)
            {
                var obj = Runner.GetPlayerObject(playerRef);
                var player = obj ? obj.GetComponent<Player>() : null;

                if (player != null)
                    player.Health.Revive();
            }
        }



        private void StartSuddenDeath()
        {
            CurrentPhase = GamePhase.SuddenDeath;
            
            if (SuddenDeathDuration > 0)
            {
                SuddenDeathTimer = TickTimer.CreateFromSeconds(Runner, SuddenDeathDuration);
            }
            
            Debug.Log("[BattleTimer] SUDDEN DEATH! First kill wins!");
        }
        
        private void DeclareSuddenDeathTimeout()
        {
            // Sudden death timed out - this shouldn't happen often
            // Pick a winner randomly or by player index
            PlayerRef best = PlayerRef.None;
            int bestKills = -1;

            foreach (var playerRef in Runner.ActivePlayers)
            {
                var obj = Runner.GetPlayerObject(playerRef);
                var player = obj ? obj.GetComponent<Player>() : null;

                if (player != null && player.Kills > bestKills)
                {
                    bestKills = player.Kills;
                    best = playerRef;
                }
            }

            if (best != PlayerRef.None)
                DeclareWinner(best);
        }
        



        private void ReturnToMainMenu()
        {
            if (Runner != null && Runner.IsSharedModeMasterClient)
            {
                Debug.Log("[BattleTimer] Loading '05_infoscene'...");
                
                int infoSceneIndex = 5;
                int foundIndex = SceneUtility.GetBuildIndexByScenePath("05_infoscene");
                if (foundIndex >= 0) infoSceneIndex = foundIndex;

                Runner.LoadScene(SceneRef.FromIndex(infoSceneIndex));
            }
        }

        
        private void Update()
        {
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            if (!_initialized || Object == null || !Object.IsValid)
                return;
            
            // Update phase text
            if (GamePhaseText != null)
            {
                GamePhaseText.text = CurrentPhase switch
                {
                    GamePhase.Battle => "",
                    GamePhase.TiebreakCountdown => "GET READY...",
                    GamePhase.SuddenDeath => "⚔ SUDDEN DEATH ⚔",
                    GamePhase.GameOver => "",
                    _ => ""
                };
                
                // Make sudden death text pulse
                if (CurrentPhase == GamePhase.SuddenDeath)
                {
                    float pulse = 0.8f + Mathf.Sin(Time.time * 5f) * 0.2f;
                    GamePhaseText.transform.localScale = Vector3.one * pulse;
                }
                else
                {
                    GamePhaseText.transform.localScale = Vector3.one;
                }
            }
                
            // Update timer display
            if (TimerText != null && Runner != null)
            {
                // If GameOver (Victory/Defeat), hide the timer immediately
                if (CurrentPhase == GamePhase.GameOver)
                {
                    if(TimerText.gameObject.activeSelf) TimerText.gameObject.SetActive(false);
                    return; // Skip the rest of timer logic
                }
                
                // Ensure it's active otherwise
                if(!TimerText.gameObject.activeSelf) TimerText.gameObject.SetActive(true);

                float remainingTime = 0f;
                
                switch (CurrentPhase)
                {
                    case GamePhase.Battle:
                        remainingTime = BattleEndTimer.RemainingTime(Runner) ?? 0f;
                        break;
                    case GamePhase.TiebreakCountdown:
                        remainingTime = SuddenDeathTimer.RemainingTime(Runner) ?? 0f;
                        break;
                    case GamePhase.SuddenDeath:
                        if (SuddenDeathDuration > 0)
                            remainingTime = SuddenDeathTimer.RemainingTime(Runner) ?? 0f;
                        break;
                    // Phase GameOver handled above now
                }
                
                if (CurrentPhase == GamePhase.TiebreakCountdown)
                {
                    // Show countdown number
                    TimerText.text = Mathf.CeilToInt(remainingTime).ToString();
                    TimerText.fontSize = 72;
                }
                else if (CurrentPhase == GamePhase.SuddenDeath && SuddenDeathDuration <= 0)
                {
                    // No time limit in sudden death
                    TimerText.text = "∞";
                    TimerText.fontSize = 48;
                }
                else
                {
                    int minutes = Mathf.FloorToInt(remainingTime / 60f);
                    int seconds = Mathf.FloorToInt(remainingTime % 60f);
                    TimerText.text = $"{minutes:00}:{seconds:00}";
                    TimerText.fontSize = 36;
                }
                
                // Flash timer red when low
                if (CurrentPhase == GamePhase.Battle && remainingTime <= 30f)
                {
                    TimerText.color = Color.Lerp(Color.white, Color.red, Mathf.Sin(Time.time * 5f) * 0.5f + 0.5f);
                }
                else if (CurrentPhase == GamePhase.SuddenDeath)
                {
                    TimerText.color = Color.yellow;
                }
                else
                {
                    TimerText.color = Color.white;
                }
            }
            
            // Disable winner panel as requested by user - We go straight to scene 05
            if (WinnerPanel != null)
            {
                WinnerPanel.SetActive(false); 
            }

            else if (CurrentPhase == GamePhase.TiebreakCountdown && WinnerPanel != null)
            {
                // Show tie message during countdown
                WinnerPanel.SetActive(true);
                if (WinnerText != null)
                {
                    WinnerText.text = "TIE! SUDDEN DEATH!";
                    WinnerText.color = Color.yellow;
                }
            }
            else if (WinnerPanel != null && CurrentPhase != GamePhase.GameOver)
            {
                WinnerPanel.SetActive(false);
            }
        }
        
        public float GetRemainingTime()
        {
            if (Runner == null)
                return 0f;
            return BattleEndTimer.RemainingTime(Runner) ?? 0f;
        }
        
        public bool IsSuddenDeath()
        {
            return CurrentPhase == GamePhase.SuddenDeath;
        }
    }
}

