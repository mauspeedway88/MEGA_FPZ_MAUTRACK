using Fusion;
using UnityEngine;
// removed static Starter.Shooter.BattleTimer; as it seems unused or specific

public class TieBreakerGameManager : NetworkBehaviour
{
    public TieBreakerPlayer PlayerPrefab;
    public Transform[] SpawnPoints;
    public BullseyeTarget Target;
    public float MatchDuration = 20f;
    
    [Networked] private TickTimer MatchTimer { get; set; }
    [Networked] private NetworkBool GameEnded { get; set; }

    public override void Spawned()
    {
        PlayerRef localPlayer = Runner.LocalPlayer;

        int index = HasStateAuthority ? 0 : 1;
        // Simple spawn point logic - might conflict if >2 players but fine for 1v1
        if (index < SpawnPoints.Length)
        {
            TieBreakerPlayer player = Runner.Spawn(
                PlayerPrefab,
                SpawnPoints[index].position,
                SpawnPoints[index].rotation,
                localPlayer
            );
            Runner.SetPlayerObject(localPlayer, player.Object);
        }

        if (HasStateAuthority)
        {
            MatchTimer = TickTimer.CreateFromSeconds(Runner, MatchDuration);
            GameEnded = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || GameEnded)
            return;

        if (MatchTimer.Expired(Runner))
        {
            Debug.Log("[TieBreakerGM] Time expired! Forcing winner.");
            GameEnded = true;
            
            if (Target != null)
            {
                Target.ForceDecideWinner();
            }
        }
    }
}
