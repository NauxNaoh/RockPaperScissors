using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace GameRPS
{
    public class RPSNetworkManager : NetworkManager
    {
        public static RPSNetworkManager Instance;

        public uint PlayerCount => (uint)NetworkServer.connections.Count;

        private readonly Dictionary<NetworkConnection, RPSPlayer> players = new();

        public override void OnStartHost()
        {
            base.OnStartHost();
            Debug.Log("[Server] Start host");
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Instance = this;
            players.Clear();
            Debug.Log("[Server] RPSNetworkManager started");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnServerAddPlayer(conn);

            RPSPlayer player = conn.identity.GetComponent<RPSPlayer>();
            if (player != null)
            {
                players[conn] = player;
                player.ServerInitialize();
            }
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            players.Remove(conn);
            
            base.OnServerDisconnect(conn);
        }


        public RPSPlayer FindAnyOtherPlayer(RPSPlayer self)
        {
            foreach (var kv in NetworkServer.spawned)
            {
                var player = kv.Value.GetComponent<RPSPlayer>();
                if (player == null || player == self) continue;
                if (player.state != RPSState.Idle) continue;
                return player;
            }
            return null;
        }
    }
}