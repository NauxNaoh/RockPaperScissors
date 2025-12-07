using Mirror;
using UnityEngine;

namespace GameRPS
{
    public enum RPSChoice : byte { None = 0, Rock = 1, Paper = 2, Scissors = 3 }
    public enum RPSState : byte { Idle = 0, WaitingConfirm = 1, Selecting = 2, Completed = 3 }

    public class RPSPlayer : NetworkBehaviour
    {
        [Header("Reference")]
        [SerializeField] private TextMesh playerNameMesh;
        [SerializeField] private MeshRenderer playerMesh;


        [SyncVar(hook = nameof(OnNameChanged))]
        public string playerName;

        [SyncVar(hook = nameof(OnColorChanged))]
        public Color playerColor = Color.white;

        [SyncVar(hook = nameof(OnStateChanged))]
        public RPSState state = RPSState.Idle;

        [SyncVar]
        public uint opponentNetId = 0;

        [SyncVar]
        public RPSChoice myChoice = RPSChoice.None;

        [SyncVar]
        public bool isChallenger = false;

        #region Initialization
        [Server]
        public void ServerInitialize()
        {
            playerName = $"Player {netIdentity.netId}";
            playerColor = new(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            state = RPSState.Idle;
            opponentNetId = 0;
            myChoice = RPSChoice.None;
            isChallenger = false;
        }

        void OnNameChanged(string old, string newName)
        {
            if (playerNameMesh != null) playerNameMesh.text = newName;
        }

        void OnColorChanged(Color old, Color newColor)
        {
            if (playerNameMesh != null)
                playerNameMesh.color = newColor;
            if (playerMesh != null && playerMesh.material != null)
                playerMesh.material.color = newColor;
        }

        [Client]
        void OnStateChanged(RPSState oldState, RPSState newState)
        {
            if (!isLocalPlayer) return;
            Debug.Log($"[Client] Player {netId} (You) state changed: {newState}");
        }


        [ClientRpc]
        public void RpcNotifyPlayerJoined(uint netID)
        {
            if (isLocalPlayer)
                Debug.Log($"[Client] You joined server");
            else
                Debug.Log($"[Client] Player {netID} joined");
        }
        #endregion



        #region Input
        private void Update()
        {
            if (!isLocalPlayer) return;

            if (Input.GetKeyDown(KeyCode.O) && state == RPSState.Idle)
            {
                CmdRequestFindOpponent();
                return;
            }


            if (state == RPSState.WaitingConfirm && !isChallenger)
            {
                if (Input.GetKeyDown(KeyCode.Y))
                {
                    CmdRespondToChallenge(true);
                }
                else if (Input.GetKeyDown(KeyCode.N))
                {
                    CmdRespondToChallenge(false);
                }
            }
            else if (state == RPSState.Selecting)
            {
                if (Input.GetKeyDown(KeyCode.R)) CmdSelectChoice(RPSChoice.Rock);
                else if (Input.GetKeyDown(KeyCode.P)) CmdSelectChoice(RPSChoice.Paper);
                else if (Input.GetKeyDown(KeyCode.S)) CmdSelectChoice(RPSChoice.Scissors);
            }
        }
        #endregion



        #region Challenge Handle
        [Command]
        void CmdRequestFindOpponent()
        {
            if (!isServer) return;

            if (RPSNetworkManager.Instance == null
                || RPSNetworkManager.Instance.PlayerCount < 2)
                return;

            RPSPlayer other = RPSNetworkManager.Instance.FindAnyOtherPlayer(this);
            if (other == null) return;

            CreateChallengeWith(other);
        }


        [Server]
        public void CreateChallengeWith(RPSPlayer target)
        {
            if (state != RPSState.Idle || target == null || target.state != RPSState.Idle) return;

            isChallenger = true;
            state = RPSState.WaitingConfirm;
            opponentNetId = target.netId;

            target.isChallenger = false;
            target.state = RPSState.WaitingConfirm;
            target.opponentNetId = netId;

            TargetRpcNotifyFoundOpponent(connectionToClient, opponentNetId);
            target.TargetRpcReceiveChallenge(target.connectionToClient, netId);
        }

        [TargetRpc]
        void TargetRpcNotifyFoundOpponent(NetworkConnectionToClient conn, uint oppId)
        {
            Debug.Log($"You challenge Player {oppId}");
        }

        [TargetRpc]
        void TargetRpcReceiveChallenge(NetworkConnectionToClient conn, uint challengerNetId)
        {
            Debug.Log($"[Client] You have challenge from Player {challengerNetId}. Press [Y]Confirm or [N]Reject");
        }

        [Command]
        void CmdRespondToChallenge(bool accept)
        {
            if (!isServer) return;
            if (state != RPSState.WaitingConfirm) return;

            if (!NetworkServer.spawned.TryGetValue(opponentNetId, out var challengerIdentity)) return;
            if (!challengerIdentity.TryGetComponent<RPSPlayer>(out var challenger))
            {
                ResetBattleState();
                return;
            }

            if (!accept)
            {
                TargetRpcBattleRejected(connectionToClient);
                challenger.TargetRpcBattleRejected(challenger.connectionToClient);

                ResetBattleState();
                challenger.ResetBattleState();
                return;
            }

            state = RPSState.Selecting;
            challenger.state = RPSState.Selecting;

            myChoice = RPSChoice.None;
            challenger.myChoice = RPSChoice.None;

            TargetRpcStartSelection(connectionToClient);
            challenger.TargetRpcStartSelection(challenger.connectionToClient);
        }

        [Server]
        void ResetBattleState()
        {
            state = RPSState.Idle;
            opponentNetId = 0;
            myChoice = RPSChoice.None;
            isChallenger = false;
        }

        [TargetRpc]
        void TargetRpcBattleRejected(NetworkConnectionToClient conn)
        {
            Debug.Log("[Client] Challenge rejected.");
        }

        [TargetRpc]
        void TargetRpcStartSelection(NetworkConnectionToClient conn)
        {
            Debug.Log($"[Client] Battle start. Press to choose [R]Rock, [P]Paper, [S]Scissors");
        }

        [Command]
        void CmdSelectChoice(RPSChoice choice)
        {
            if (!isServer) return;

            if (state != RPSState.Selecting) return;
            if (myChoice != RPSChoice.None) return;
            myChoice = choice;

            if (opponentNetId == 0) return;
            if (!NetworkServer.spawned.TryGetValue(opponentNetId, out var opponentIdentity)) return;
            if (!opponentIdentity.TryGetComponent<RPSPlayer>(out var oppPlayer)) return;
            if (oppPlayer.myChoice == RPSChoice.None) return;

            CheckResult(this, oppPlayer);
        }

        [Server]
        void CheckResult(RPSPlayer a, RPSPlayer b)
        {
            if (a.myChoice == RPSChoice.None || b.myChoice == RPSChoice.None) return;

            int result = CompareChoice(a.myChoice, b.myChoice);
            switch (result)
            {
                case 0:
                    a.TargetRpcShowDraw(a.connectionToClient);
                    b.TargetRpcShowDraw(b.connectionToClient);
                    break;
                case 1:
                    a.TargetRpcShowWin(a.connectionToClient);
                    b.TargetRpcShowLose(b.connectionToClient);
                    break;
                case -1:
                    a.TargetRpcShowLose(a.connectionToClient);
                    b.TargetRpcShowWin(b.connectionToClient);
                    break;
            }

            a.ResetBattleState();
            b.ResetBattleState();
        }

        [Server]
        int CompareChoice(RPSChoice a, RPSChoice b)
        {
            if (a == b) return 0;
            if ((a == RPSChoice.Rock && b == RPSChoice.Scissors) ||
                (a == RPSChoice.Scissors && b == RPSChoice.Paper) ||
                (a == RPSChoice.Paper && b == RPSChoice.Rock))
                return 1;
            return -1;
        }


        [TargetRpc]
        void TargetRpcShowDraw(NetworkConnectionToClient conn)
        {
            Debug.Log($"Battle result: Draw!");
        }

        [TargetRpc]
        void TargetRpcShowWin(NetworkConnectionToClient conn)
        {
            Debug.Log($"Battle result: You win!");
        }

        [TargetRpc]
        void TargetRpcShowLose(NetworkConnectionToClient conn)
        {
            Debug.Log($"Battle result: You lose!");
        }
        #endregion
    }
}