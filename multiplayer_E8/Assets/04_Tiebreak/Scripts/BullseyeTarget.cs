using Fusion;
using System.Linq;
using UnityEngine;

public class BullseyeTarget : NetworkBehaviour
{
    public Transform Center;
    public NetworkObject HitMarkPrefab;
    public GameObject ImpactEffect;

    // Players who actually participated (fired a shell)
    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, NetworkBool> Participants => default;

    // Hit distances
    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, float> NetworkedHits => default;

    // Missed shots
    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, NetworkBool> NetworkedMissed => default;

    // =======================
    // SPAWN
    // =======================
    public override void Spawned()
    {
        if (!HasStateAuthority)
            return;

        Participants.Clear();
        NetworkedHits.Clear();
        NetworkedMissed.Clear();
    }

    // =======================
    // HIT (SERVER)
    // =======================
    public void RegisterHitServer(PlayerRef player, Vector3 hitPoint)
    {
        if (!HasStateAuthority) return;
        if (NetworkedHits.ContainsKey(player)) return;

        Participants.Set(player, true);

        float distance = Vector3.Distance(Center.position, hitPoint);
        NetworkedHits.Set(player, distance);

        Vector3 dir = (hitPoint - Center.position).normalized;
        Vector3 spawnPos = hitPoint + dir * 0.01f;

        if (HitMarkPrefab != null)
        {
            Runner.Spawn(
                HitMarkPrefab,
                spawnPos,
                Quaternion.LookRotation(dir)
            );
        }

        RPC_ShowHitEffect(player, hitPoint, distance);
        CheckWinner();
    }

    // =======================
    // MISS (SERVER)
    // =======================
    public void RegisterMissServer(PlayerRef player)
    {
        if (!HasStateAuthority) return;

        Participants.Set(player, true);
        NetworkedMissed.Set(player, true);

        RPC_ShowMissEffect(player);
        CheckWinner();
    }

    // =======================
    // VISUAL RPCs
    // =======================
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowHitEffect(PlayerRef player, Vector3 hitPoint, float distance)
    {
        Debug.Log($"[TieBreaker] Player {player.PlayerId} HIT at {distance}");

        if (ImpactEffect != null)
            Instantiate(ImpactEffect, hitPoint, Quaternion.identity);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowMissEffect(PlayerRef player)
    {
        Debug.Log($"[TieBreaker] Player {player.PlayerId} MISSED");
    }

    // =======================
    // WINNER LOGIC (SERVER)
    // =======================
    private void CheckWinner()
    {
        if (!HasStateAuthority) return;

        int totalParticipants = Participants.Count;
        if(totalParticipants == 1)
        {
            return;
        }
        int decisionsMade = NetworkedHits.Count + NetworkedMissed.Count;

        Debug.Log($"[TieBreaker] Decisions: {decisionsMade}/{totalParticipants}");

        if (totalParticipants == 0)
            return;

        if (decisionsMade < totalParticipants)
            return;

        PlayerRef winner = PlayerRef.None;
        float bestDistance = float.MaxValue;

        // Closest hit wins
        foreach (var hit in NetworkedHits)
        {
            if (hit.Value < bestDistance)
            {
                bestDistance = hit.Value;
                winner = hit.Key;
            }
        }

        // Everyone missed → pick any participant
        if (winner == PlayerRef.None)
        {
            foreach (var p in Participants)
            {
                winner = p.Key;
                break;
            }
        }

        if (winner != PlayerRef.None)
        {
            RPC_DeclareWinner(winner);
        }
    }

    public void ForceDecideWinner()
    {
        if (!HasStateAuthority) return;

        PlayerRef winner = PlayerRef.None;
        float bestDistance = float.MaxValue;

        // Check hits
        foreach (var hit in NetworkedHits)
        {
            if (hit.Value < bestDistance)
            {
                bestDistance = hit.Value;
                winner = hit.Key;
            }
        }

        // Check if we have a winner from hits
        if (winner != PlayerRef.None)
        {
            RPC_DeclareWinner(winner);
        }
        else if (Participants.Count > 0)
        {
            // Pick random if only misses
             RPC_DeclareWinner(Participants.First().Key);
        }
        else
        {
             Debug.Log("[TieBreaker] No participants, no winner.");
        }
    }

    // =======================
    // WINNER ANNOUNCEMENT
    // =======================
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DeclareWinner(PlayerRef winner)
    {
        Debug.Log($"[TieBreaker] WINNER: Player {winner.PlayerId}");
        // Hook UI here
    }
}
