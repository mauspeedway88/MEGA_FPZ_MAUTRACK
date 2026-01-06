using UnityEngine;
using Fusion;

namespace Starter.ThirdPersonCharacter
{
    /// <summary>
    /// Handles player connections (spawning of Player instances).
    /// 
    /// Integration with Lobby System:
    /// - If coming from lobby (MainMenuController.ComingFromLobby == true), 
    ///   players are already connected and this just spawns their characters.
    /// - If accessed directly (legacy flow), works as before with UIGameMenu.
    /// </summary>
    public sealed class GameManager : NetworkBehaviour
    {
        [Header("Player Setup")]
        public NetworkObject PlayerPrefab;
        public float SpawnRadius = 3f;

        [Header("Spawn Points (Optional)")]
        [Tooltip("If set, players will spawn at these specific points instead of random positions")]
        public Transform[] SpawnPoints;

        [Header("Auto Spawn Separation")]
        [Tooltip("Distance between auto-generated spawn positions when no spawn points are set")]
        public float SpawnSeparation = 5f;

        public override void Spawned()
        {
            Debug.Log($"[GameManager] Spawned. LocalPlayer: {Runner.LocalPlayer}, ComingFromLobby: {Starter.Lobby.MainMenuController.ComingFromLobby}");

            // Determine spawn position based on player ID
            Vector3 spawnPosition = GetSpawnPosition(Runner.LocalPlayer);
            Quaternion spawnRotation = GetSpawnRotation(Runner.LocalPlayer);

            // Spawn the player
            Runner.Spawn(PlayerPrefab, spawnPosition, spawnRotation, Runner.LocalPlayer);

            Debug.Log($"[GameManager] Player {Runner.LocalPlayer.PlayerId} spawned at {spawnPosition}");

            // If we came from lobby, clean up the static reference
            if (Starter.Lobby.MainMenuController.ComingFromLobby)
            {
                Starter.Lobby.MainMenuController.ComingFromLobby = false;
            }
        }

        private Vector3 GetSpawnPosition(PlayerRef player)
        {
            // Use player's PlayerId to determine spawn index (0 or 1 for 2-player game)
            int playerIndex = GetPlayerIndex(player);

            // Use designated spawn points if available
            if (SpawnPoints != null && SpawnPoints.Length > 0)
            {
                int spawnPointIndex = playerIndex % SpawnPoints.Length;
                
                if (SpawnPoints[spawnPointIndex] != null)
                {
                    Debug.Log($"[GameManager] Using spawn point {spawnPointIndex} for player {player.PlayerId}");
                    return SpawnPoints[spawnPointIndex].position;
                }
            }

            // Auto-generate spawn positions on opposite sides
            // Player 0 spawns on left (-X), Player 1 spawns on right (+X)
            float xOffset = (playerIndex == 0) ? -SpawnSeparation : SpawnSeparation;
            
            // Add small random offset to avoid exact same position if rejoining
            Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
            
            Vector3 spawnPos = transform.position + new Vector3(xOffset + randomOffset.x, 0f, randomOffset.y);
            
            Debug.Log($"[GameManager] Auto spawn position for player {player.PlayerId} (index {playerIndex}): {spawnPos}");
            return spawnPos;
        }

        private Quaternion GetSpawnRotation(PlayerRef player)
        {
            int playerIndex = GetPlayerIndex(player);

            // Use spawn point rotation if available
            if (SpawnPoints != null && SpawnPoints.Length > 0)
            {
                int spawnPointIndex = playerIndex % SpawnPoints.Length;
                if (SpawnPoints[spawnPointIndex] != null)
                {
                    return SpawnPoints[spawnPointIndex].rotation;
                }
            }

            // Auto-rotation: players face each other
            // Player 0 (left) faces right, Player 1 (right) faces left
            float yRotation = (playerIndex == 0) ? 90f : -90f;
            return Quaternion.Euler(0f, yRotation, 0f);
        }

        private int GetPlayerIndex(PlayerRef player)
        {
            // Get a consistent index based on all active players
            int index = 0;
            foreach (var p in Runner.ActivePlayers)
            {
                if (p == player)
                {
                    return index;
                }
                index++;
            }
            
            // Fallback: use PlayerId modulo 2
            return player.PlayerId % 2;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw spawn radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, SpawnRadius);

            // Draw auto spawn positions
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + new Vector3(-SpawnSeparation, 0, 0), 0.5f);
            Gizmos.DrawWireSphere(transform.position + new Vector3(SpawnSeparation, 0, 0), 0.5f);

            // Draw spawn points
            if (SpawnPoints != null)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < SpawnPoints.Length; i++)
                {
                    if (SpawnPoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(SpawnPoints[i].position, 0.5f);
                        // Draw direction arrow
                        Gizmos.DrawRay(SpawnPoints[i].position, SpawnPoints[i].forward * 2f);
                    }
                }
            }
        }
    }
}
