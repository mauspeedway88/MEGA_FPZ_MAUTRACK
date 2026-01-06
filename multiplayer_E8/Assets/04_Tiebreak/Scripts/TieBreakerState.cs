using Fusion;
using System.Collections.Generic;

public class TieBreakerState : NetworkBehaviour
{
    public static PlayerRef[] TiedPlayers { get; set; } = new PlayerRef[2];

    [Networked, Capacity(2)]
    public NetworkArray<PlayerRef> NetworkedTiedPlayers => default;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            // Initialize with the two tied players
            for (int i = 0; i < TiedPlayers.Length && i < NetworkedTiedPlayers.Length; i++)
            {
                NetworkedTiedPlayers.Set(i, TiedPlayers[i]);
            }
        }
    }
}