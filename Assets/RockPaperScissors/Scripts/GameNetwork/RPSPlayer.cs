using Mirror;
using UnityEngine;

namespace GameRPS
{
    public class RPSPlayer : NetworkBehaviour
    {
        [SerializeField] private TextMesh playerNameMesh;
        [SerializeField] private MeshRenderer playerMesh;


        [SyncVar(hook = nameof(OnNameChanged))]
        public string playerName;
        [SyncVar(hook = nameof(OnColorChanged))]
        public Color playerColor = Color.white;
        [SyncVar(hook = nameof(OnRpsBattleChange))]
        public RPSBattle rpsBattle;


        #region Init
        [Server]
        public void ServerInitialize()
        {
            playerName = $"Player {netIdentity.netId}";
            playerColor = new(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            rpsBattle = new();
        }

        void OnNameChanged(string old, string newName)
        {
            playerNameMesh.text = newName;
        }

        void OnColorChanged(Color old, Color newColor)
        {
            playerNameMesh.color = newColor;
            playerMesh.material.color = newColor;
        }

        [ClientRpc]
        public void RpcNotifyPlayerJoined(NetworkIdentity identity)
        {
            if (identity.isLocalPlayer)
                Debug.Log($"[Client] You joined server");
            else
            {
                RPSPlayer player = identity.GetComponent<RPSPlayer>();
                Debug.Log($"[Client] {player.playerName} joined");
            }
        }
        #endregion



        #region Challenge
        [Client]
        private void Update()
        {
            if (!isLocalPlayer) return;

            if (Input.GetKeyDown(KeyCode.O) && rpsBattle.isNoBattle)
            {
                Debug.Log("You challenge random player");
                CmdFindOpponent();
                return;
            }

            if (rpsBattle.isNoBattle) return;
            if (rpsBattle.state == RPSState.WaitingConfirm
                && rpsBattle.opponent != null
                && rpsBattle.opponent.netId == netId)
            {
                if (Input.GetKeyDown(KeyCode.Y))
                    CmdChoiceAccept();
                else if (Input.GetKeyDown(KeyCode.N))
                    CmdChoiceReject();
            }
        }

        [Command]
        void CmdFindOpponent()
        {
            if (RPSNetworkManager.singleton.numPlayers < 2) return;

            foreach (var conn in NetworkServer.connections)
            {
                NetworkIdentity identityFound = conn.Value.identity;
                if (identityFound == netIdentity) continue;

                CmdCreateChallenge(identityFound);
                return;
            }
        }

        [Server]
        public void CmdCreateChallenge(NetworkIdentity target)
        {
            if (!rpsBattle.isNoBattle && rpsBattle.state != RPSState.Idle) return;

            var targetPlayer = target.GetComponent<RPSPlayer>();
            if (targetPlayer == null
                || !targetPlayer.rpsBattle.isNoBattle
                || targetPlayer.rpsBattle.state != RPSState.Idle) return;

            var newBattle = new RPSBattle(RPSState.WaitingConfirm, challenger: this, opponent: targetPlayer);
            rpsBattle = newBattle;
            targetPlayer.rpsBattle = newBattle;

            targetPlayer.RpcReceiveChallenge(targetPlayer.connectionToClient, netIdentity);
        }

        [TargetRpc]
        void RpcReceiveChallenge(NetworkConnectionToClient conn, NetworkIdentity challengerIdentity)
        {
            Debug.Log($"[Client] You have challenge from Player {challengerIdentity.netId}. Press [Y]Confirm or [N]Reject");
        }

        [Command]
        void CmdChoiceAccept()
        {
            var oldChallConn = rpsBattle.challenger.connectionToClient;
            var oldOppoConn = rpsBattle.opponent.connectionToClient;

            rpsBattle.state = RPSState.Selecting;

            var newBattle = new RPSBattle(rpsBattle);
            rpsBattle.challenger.rpsBattle = newBattle;
            rpsBattle = newBattle;

            RpcNotifySelectRockPaperScissors(oldChallConn);
            RpcNotifySelectRockPaperScissors(oldOppoConn);
        }

        [TargetRpc]
        void RpcNotifySelectRockPaperScissors(NetworkConnectionToClient conn)
        {
            Debug.Log($"[Client] Battle start. Press to choose [R]Rock or [P]Paper or [S]Scissors");
        }

        [Command]
        void CmdChoiceReject()
        {
            var oldChallConn = rpsBattle.challenger.connectionToClient;
            var oldOppoConn = rpsBattle.opponent.connectionToClient;

            var newBattle = new RPSBattle();
            rpsBattle.challenger.rpsBattle = newBattle;
            rpsBattle = newBattle;

            RpcNotifyRejectBattle(oldChallConn);
            RpcNotifyRejectBattle(oldOppoConn);
        }

        [TargetRpc]
        void RpcNotifyRejectBattle(NetworkConnectionToClient conn)
        {
            Debug.Log($"Battle rejected!!");
        }



        void OnRpsBattleChange(RPSBattle old, RPSBattle newBattle)
        {

        }
        #endregion
    }
}