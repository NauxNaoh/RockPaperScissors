using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace GameRPS
{ 
    public class RPSNetworkManager : NetworkManager
    {
        public static RPSNetworkManager Instance;

        public uint PlayerCount;

        private Dictionary<NetworkConnection, RPSPlayer> players = new();

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
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnServerAddPlayer(conn);

            RPSPlayer player = conn.identity.GetComponent<RPSPlayer>();
            players[conn] = player;
            player.ServerInitialize();
            player.RpcNotifyPlayerJoined(conn.identity);
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            players.Remove(conn);
            base.OnServerDisconnect(conn);
        }


        public static void AddBattle(RPSBattle battle)
        {

        }
    }
}