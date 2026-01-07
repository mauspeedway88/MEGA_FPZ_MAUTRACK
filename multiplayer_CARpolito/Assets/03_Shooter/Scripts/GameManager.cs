using UnityEngine;
using Fusion;
using System.Linq; // Necesario para ordenar listas
using System.Collections.Generic;
using TMPro;
using TMPro;
using Starter.PlayFabIntegration;
using UnityEngine.SceneManagement; // Creating scene transition 


namespace Starter.Shooter
{
    /// <summary>
    /// Handles player connections (spawning of Player instances).
    /// </summary>
    public sealed class GameManager : NetworkBehaviour
    {
        public Player PlayerPrefab;
        public NetworkObject Room;
        public int Coins;
        public TextMeshProUGUI Coins_Text;

        [Header("PlayFab Integration")]
        [SerializeField] private bool enablePlayFabSync = true;

        [Networked]
        public NetworkLinkedList<Player> PLayers => default;
        
        [Tooltip("If true, players won't respawn and the match ends on death.")]
        public bool IsTieBreaker = false;
        
        // Persist bet amount between scenes
        public static int PendingBet = 0;

        public Player LocalPlayer { get; private set; }

        private SpawnPoint[] _spawnPoints;
        private bool _cameFromLobby;
        private PlayFabGameIntegration _playFabIntegration;

        // removed local _tieBreakerWinTriggered

        [Networked]
        public NetworkBool TieBreakerGameEnded { get; set; }

        /// <summary>
        /// Método Genérico (Requerido por Player.cs para Respawn).
        /// Devuelve un punto aleatorio cualquiera con su rotación.
        /// </summary>
        public (Vector3 position, Quaternion rotation) GetSpawnPositionAndRotation()
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                _spawnPoints = FindObjectsOfType<SpawnPoint>().OrderBy(sp => sp.name).ToArray();
            }

            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                var spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
                var randomPositionOffset = Random.insideUnitCircle * spawnPoint.Radius;
                Vector3 position = spawnPoint.transform.position + new Vector3(randomPositionOffset.x, 0f, randomPositionOffset.y);
                
                // USE SPAWN POINT ROTATION
                return (position, spawnPoint.transform.rotation);
            }

            // Fallback
            return (transform.position + new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f)), Quaternion.identity);
        }
        
        /// <summary>
        /// Legacy method for backwards compatibility - returns only position
        /// </summary>
        public Vector3 GetSpawnPosition()
        {
            var (position, _) = GetSpawnPositionAndRotation();
            return position;
        }

        /// <summary>
        /// Método Específico (Usado al iniciar la partida).
        /// Usa el ID del jugador para garantizar que aparezcan en puntos diferentes.
        /// Devuelve posición Y rotación del spawn point.
        /// </summary>
        public (Vector3 position, Quaternion rotation) GetSpawnPositionAndRotationForPlayer(PlayerRef player)
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                // Ordenamos por nombre para que todos los clientes tengan la misma lista (Punto A, Punto B...)
                _spawnPoints = FindObjectsOfType<SpawnPoint>().OrderBy(sp => sp.name).ToArray();
            }

            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                // Usamos el ID del jugador para elegir el índice.
                // Jugador 0 -> Índice 0. Jugador 1 -> Índice 1.
                int spawnIndex = player.PlayerId % _spawnPoints.Length;
                
                var spawnPoint = _spawnPoints[spawnIndex];
                
                // Pequeño offset para que no aparezcan clavados en el centro
                var randomPositionOffset = Random.insideUnitCircle * spawnPoint.Radius * 0.5f;
                Vector3 position = spawnPoint.transform.position + new Vector3(randomPositionOffset.x, 0f, randomPositionOffset.y);
                
                // USE SPAWN POINT ROTATION - This allows designers to set orientation in Unity Editor
                Quaternion rotation = spawnPoint.transform.rotation;
                
                return (position, rotation);
            }

            // Fallback: separación manual si no hay spawn points
            float xOffset = (player.PlayerId % 2 == 0) ? -5f : 5f;
            return (transform.position + new Vector3(xOffset, 0f, 0f), Quaternion.identity);
        }
        
        /// <summary>
        /// Legacy method for backwards compatibility - returns only position
        /// </summary>
        public Vector3 GetSpawnPositionForPlayer(PlayerRef player)
        {
            var (position, _) = GetSpawnPositionAndRotationForPlayer(player);
            return position;
        }

        public override void Spawned()
        {
            // Inicializar puntos ordenados
            _spawnPoints = FindObjectsOfType<SpawnPoint>().OrderBy(sp => sp.name).ToArray();
            
            // Initialize PlayFab integration
            _playFabIntegration = PlayFabGameIntegration.Instance;
            if (_playFabIntegration == null && enablePlayFabSync)
            {
                var integrationGO = new GameObject("PlayFabGameIntegration");
                _playFabIntegration = integrationGO.AddComponent<PlayFabGameIntegration>();
            }
            
            // Sync coins from PlayFab
            if (_playFabIntegration != null && enablePlayFabSync)
            {
                Coins = _playFabIntegration.GetCoins();
            }
            
            // Check if we came from lobby
            _cameFromLobby = Starter.Lobby.MainMenuController.ComingFromLobby;
            
            Debug.Log($"[Shooter GameManager] Spawned. LocalPlayer: {Runner.LocalPlayer}, CameFromLobby: {_cameFromLobby}, ActivePlayers: {Runner.ActivePlayers.Count()}");

            // CRITICAL FIX: PREVENT SPAWNING IN INFO SCENE (05_infoscene)
            // If this is the info scene, we only want UI/Data, NO player avatars.
            string currentSceneName = SceneManager.GetActiveScene().name;
            if (currentSceneName == "05_infoscene")
            {
                Debug.Log("[GameManager] INFO SCENE DETECTED. Skipping Player Spawn. UI Mode Only.");
                // We stop here for spawning, but allowed PlayFab init above for Coins UI.
                return;
            }

            // IMPORTANTE: Aquí usamos la lógica específica por jugador para el inicio.
            // Esto asegura que al entrar, uno vaya a un lado y el otro al otro.
            var (spawnPos, spawnRot) = GetSpawnPositionAndRotationForPlayer(Runner.LocalPlayer);

            Debug.Log($"[GameManager] Spawning player {Runner.LocalPlayer.PlayerId} at position: {spawnPos}, rotation: {spawnRot.eulerAngles}");

            LocalPlayer = Runner.Spawn(PlayerPrefab, spawnPos, spawnRot, Runner.LocalPlayer);
            Runner.SetPlayerObject(Runner.LocalPlayer, LocalPlayer.Object);
            
            if (!PLayers.Contains(LocalPlayer))
            {
                PLayers.Add(LocalPlayer);
            }

            // Check if we're in single player mode
            bool isSinglePlayer = Runner.GameMode == GameMode.Single;

            if (_cameFromLobby || IsTieBreaker)
            {
                // Coming from lobby OR TieBreaker mode - start immediately
                LocalPlayer.gameObject.SetActive(true);
                if (Room != null)
                {
                    Room.gameObject.SetActive(false);
                }
                
                // Clear lobby state if it was set
                if (_cameFromLobby)
                    Starter.Lobby.MainMenuController.ComingFromLobby = false;
            }
            else if (isSinglePlayer)
            {
                LocalPlayer.gameObject.SetActive(true);
                if (Room != null) Room.gameObject.SetActive(false);
            }
            else
            {
                // Legacy flow
                LocalPlayer.gameObject.SetActive(false);

                if (Runner.ActivePlayers.Count() == 1)
                {
                    if (Room != null) Room.gameObject.SetActive(true);
                }
            }
        }

        private void Update()
        {
            // Get coins from PlayFab if available, otherwise from PlayerPrefs
            if (_playFabIntegration != null && enablePlayFabSync)
            {
                Coins = _playFabIntegration.GetCoins();
            }
            else
            {
                Coins = PlayerPrefs.GetInt("Coins");
            }
            
            if (Coins_Text != null)
            {
                Coins_Text.text = Coins.ToString();
            }
        }

        public void AddCoins(int amount)
        {
            if (_playFabIntegration != null && enablePlayFabSync)
            {
                _playFabIntegration.AddCoins(amount);
                Coins = _playFabIntegration.GetCoins();
            }
            else
            {
                Coins = PlayerPrefs.GetInt("Coins", 0) + amount;
                PlayerPrefs.SetInt("Coins", Coins);
                PlayerPrefs.Save();
            }
        }

        public override void FixedUpdateNetwork()
        {
            bool isSinglePlayer = Runner.GameMode == GameMode.Single;
            if (!_cameFromLobby && !isSinglePlayer && Runner.ActivePlayers.Count() >= 2)
            {
                // Prevent re-enabling players if TieBreaker win sequence started (Networked check)
                if (IsTieBreaker && TieBreakerGameEnded) return;

                // CRITICAL FIX: Also check if BattleTimer says the game is over.
                // This prevents re-spawning/re-enabling players during the 3-second Victory transition.
                var timer = FindObjectOfType<BattleTimer>();
                if (timer != null && timer.Object != null && timer.Object.IsValid && timer.BattleEnded) return;

                foreach (var player in PLayers)
                {
                    if (player != null && !player.gameObject.activeSelf)
                    {
                        player.gameObject.SetActive(true);
                    }
                }
                if (Room != null && Room.gameObject.activeSelf)
                {
                    Room.gameObject.SetActive(false);
                }
            }
            
            // TIE BREAKER: End Game Logic
            if (IsTieBreaker)
            {
                int aliveCount = 0;
                Player winner = null;

                foreach (var player in PLayers)
                {
                    if (player != null && player.Health.IsAlive)
                    {
                        aliveCount++;
                        winner = player;
                    }
                }

                // If only 1 survivor (and we had players), declare winner
                // Relaxed check: just need 1 alive and a valid winner.
                // SERVER ONLY: Only server decides win to avoid sync issues.
                if (HasStateAuthority && aliveCount == 1 && winner != null && !TieBreakerGameEnded)
                {
                     // Double check we actually have players (don't auto-win if < 2 players in TieBreaker)
                     if(PLayers.Count >= 2)
                     {
                        Debug.LogWarning($"[GameManager] TIE BREAKER: Survivor found ({winner.Nickname}) with {aliveCount} alive. Triggering Win Sequence.");
                        TieBreakerGameEnded = true;
                        Debug.Log($"[GameManager] TieBreakerGameEnded set to TRUE. Starting Coroutine...");
                        StartCoroutine(Coroutine_HandleTieBreakerWin(winner));
                     }
                     else
                     {
                        Debug.LogWarning($"[GameManager] TieBreaker Check: Found winner but PLayers.Count = {PLayers.Count} (Must be >= 2). Waiting for players.");
                     }
                }
                else if (HasStateAuthority && TieBreakerGameEnded)
                {
                    // Debug trace to see if we are entering here correctly
                    // Debug.Log("[GameManager] TieBreakerGameEnded is TRUE. Waiting for scene load...");
                }
                
                // Debug log every 60 ticks (approx 1s) to trace state if nothing happens
                if (Runner.Tick % 60 == 0)
                {
                   // Debug.Log($"[GameManager] TieBreaker State: Alive={aliveCount}, Total={PLayers.Count}, Triggered={_tieBreakerWinTriggered}");
                }
            }
        }
        
        private System.Collections.IEnumerator Coroutine_HandleTieBreakerWin(Player winner)
        {
            // 1. Wait a moment to appreciate the final kill/smoke (optional, kept per previous preference)
            yield return new WaitForSeconds(1.0f);
            
            Debug.Log($"[GameManager] TIE BREAKER WINNER: {winner.Nickname} - Triggering Victory UI.");

            // SAVE NAMES FOR INFO SCENE
            GameResults.Reset();
            
            string wName = "Winner";
            string lName = "Loser";

            if (winner != null)
            {
                wName = string.IsNullOrEmpty(winner.Nickname) ? $"Player {winner.Object.InputAuthority.PlayerId}" : winner.Nickname;
            }

            // Find the other player (loser) among the networked players list
            foreach (var p in PLayers)
            {
                if (p != null && p != winner && p.Object != null && p.Object.IsValid)
                {
                    lName = string.IsNullOrEmpty(p.Nickname) ? $"Player {p.Object.InputAuthority.PlayerId}" : p.Nickname;
                    break;
                }
            }
            
            // Fallback for loser if not found in list
            if (lName == "Loser")
            {
                var allPlayers = FindObjectsOfType<Player>(true);
                foreach (var p in allPlayers)
                {
                    if (p != null && p != winner)
                    {
                        lName = string.IsNullOrEmpty(p.Nickname) ? $"Player {p.Object.InputAuthority.PlayerId}" : p.Nickname;
                        break;
                    }
                }
            }

            GameResults.WinnerName = wName;
            GameResults.LoserName = lName;

            Debug.Log($"[GameManager] Names Captured - Winner: {wName}, Loser: {lName}. Sending Sync RPC...");

            // SYNC NAMES TO ALL CLIENTS
            RPC_SyncGameResults(wName, lName);


            // 2. Disable logic/visuals for all players (Networked sync)
            RPC_DisableAllPlayers();

            // 3. Trigger Victory UI via BattleTimer
            var timer = FindObjectOfType<BattleTimer>();
            if(timer != null && timer.Object != null && timer.Object.IsValid)
            {
                // This updates the [Networked] var, causing all clients to see "VICTORY" / "DEFEAT"
                // BattleTimer will also handle the scene load to 05_infoscene after 2 seconds.
                timer.BattleEnded = true;
                timer.Winner = winner.Object.InputAuthority;
                timer.CurrentPhase = BattleTimer.GamePhase.GameOver;
                
                // Set the duration for result display before BattleTimer loads the next scene
                timer.BattleEndTimer = TickTimer.CreateFromSeconds(Runner, 2.5f); 
            }
            
            // 4. Award Coins locally if applicable
            if (winner == LocalPlayer)
            {
                AddCoins(PendingBet * 2);
            }

            Debug.Log("[GameManager] TieBreaker handled. Handing over to BattleTimer for UI and Scene Load.");
            yield break;
        }




        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]

        public void RPC_DisableAllPlayers()
        {
            Debug.Log("[GameManager] RPC Received: Disabling all players");
            
            // 1. Disable Players
            var allPlayers = FindObjectsOfType<Player>();
            foreach(var p in allPlayers)
            {
                if(p != null && p.gameObject != null)
                {
                    p.gameObject.SetActive(false);
                }
            }
            

        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_SyncGameResults(string winner, string loser)
        {
            Debug.Log($"[GameManager] RPC_SyncGameResults Received - Winner: {winner}, Loser: {loser}");
            GameResults.WinnerName = winner;
            GameResults.LoserName = loser;
            
            // CRITICAL: Save to disk so Scene 05 can always find them
            PlayerPrefs.SetString("LastWinner", winner);
            PlayerPrefs.SetString("LastLoser", loser);
            PlayerPrefs.Save();
        }


        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            LocalPlayer = null;
        }
    }
}