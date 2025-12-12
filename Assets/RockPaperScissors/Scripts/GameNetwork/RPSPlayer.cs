using Mirror;
using UnityEngine;

namespace GameRPS
{
    [System.Flags]
    public enum RPSChoice : byte { None = 0, Rock = 1, Paper = 2, Scissors = 3 }
    public enum RPSState : byte { Idle = 0, WaitingConfirm = 1, Selecting = 2, Completed = 3 }

    [RequireComponent(typeof(RPSPlayerController))]
    public class RPSPlayer : NetworkBehaviour
    {
        [Header("UI Prefab")]
        [SerializeField] private CanvasBattle prefabCanvasBattle;

        [Header("References")]
        [SerializeField] private TextMesh playerNameMesh;
        [SerializeField] private TextMesh playerStateMesh;
        [SerializeField] private MeshRenderer playerMesh;

        [Header("Sync Vars")]
        [SyncVar(hook = nameof(OnNameChanged))]
        public string playerName;

        [SyncVar(hook = nameof(OnColorChanged))]
        public Color playerColor = Color.white;

        [SyncVar(hook = nameof(OnStateChanged))]
        public RPSState state = RPSState.Idle;

        [SyncVar]
        public uint opponentNetId = 0;

        [SyncVar(hook = nameof(OnChoiceChanged))]
        public RPSChoice myChoice = RPSChoice.None;

        [SyncVar]
        public bool isChallenger = false;

        public CanvasBattle canvasBattle;

        [Server]
        public void ServerInitialize()
        {
            playerName = $"Player {netId}";
            playerColor = Random.ColorHSV();
            state = RPSState.Idle;
            opponentNetId = 0;
            myChoice = RPSChoice.None;
            isChallenger = false;
        }

        void OnNameChanged(string _, string newName)
        {
            if (playerNameMesh != null)
                playerNameMesh.text = isLocalPlayer ? "You" : newName;
        }

        void OnColorChanged(Color _, Color newColor)
        {
            if (playerMesh != null)
                playerMesh.material.color = newColor;
        }

        void OnStateChanged(RPSState oldState, RPSState newState)
        {
            UpdateStateTextLocal();
        }

        void OnChoiceChanged(RPSChoice oldChoice, RPSChoice newChoice)
        {
            UpdateStateTextLocal();
        }

        void UpdateStateTextLocal()
        {
            if (playerStateMesh == null) return;

            switch (state)
            {
                case RPSState.Idle:
                    playerStateMesh.text = "";
                    break;

                case RPSState.WaitingConfirm:
                    playerStateMesh.text = "Waiting...";
                    break;

                case RPSState.Selecting:
                    if (myChoice == RPSChoice.None)
                    {
                        playerStateMesh.text = "Selecting...";
                    }
                    else
                    {
                        playerStateMesh.text = $"Picked {myChoice}";
                    }
                    break;

                case RPSState.Completed:
                    playerStateMesh.text = "";
                    break;
            }
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            canvasBattle = Instantiate(prefabCanvasBattle);
        }

        [Server]
        void SetState(RPSState newState)
        {
            state = newState;
            RpcUpdatePlayerState();
        }

        [ClientRpc]
        void RpcUpdatePlayerState()
        {
            UpdateStateTextLocal();
        }

        [Command]
        public void CmdRequestChallenge(uint targetNetId)
        {
            if (state != RPSState.Idle) return;

            if (!NetworkServer.spawned.TryGetValue(targetNetId, out var obj)) return;
            if (!obj.TryGetComponent(out RPSPlayer target)) return;

            Server_StartChallenge(target);
        }

        [Server]
        void Server_StartChallenge(RPSPlayer target)
        {
            if (state != RPSState.Idle || target.state != RPSState.Idle) return;

            isChallenger = true;
            opponentNetId = target.netId;
            SetState(RPSState.WaitingConfirm);

            target.isChallenger = false;
            target.opponentNetId = netId;
            target.SetState(RPSState.WaitingConfirm);

            // UI Notifications
            Target_ShowChallengerUI(connectionToClient, target.netId);
            target.Target_ShowOpponentUI(target.connectionToClient, netId);
        }

        [TargetRpc]
        void Target_ShowChallengerUI(NetworkConnectionToClient conn, uint oppId)
        {
            canvasBattle.SetDataChallenger($"You challenged Player {oppId}");
            canvasBattle.SetState(CanvasState.Challenger);
        }

        [TargetRpc]
        void Target_ShowOpponentUI(NetworkConnectionToClient conn, uint challengerId)
        {
            canvasBattle.SetDataOpponent($"Player {challengerId} challenged you!", CmdRespondToChallenge);
            canvasBattle.SetState(CanvasState.Opponent);
        }

        [Command]
        void CmdRespondToChallenge(bool accept)
        {
            if (state != RPSState.WaitingConfirm) return;

            if (!NetworkServer.spawned.TryGetValue(opponentNetId, out var id)) return;
            if (!id.TryGetComponent(out RPSPlayer opponent)) return;

            if (!accept)
            {
                Target_ShowRejected(connectionToClient);
                opponent.Target_ShowRejected(opponent.connectionToClient);

                ResetBattleState();
                opponent.ResetBattleState();
                return;
            }

            // Start battle
            SetState(RPSState.Selecting);
            opponent.SetState(RPSState.Selecting);

            myChoice = RPSChoice.None;
            opponent.myChoice = RPSChoice.None;

            Target_StartSelection(connectionToClient);
            opponent.Target_StartSelection(opponent.connectionToClient);
        }

        [TargetRpc]
        void Target_ShowRejected(NetworkConnectionToClient _) =>
            canvasBattle.SetState(CanvasState.None);


        [TargetRpc]
        void Target_StartSelection(NetworkConnectionToClient _)
        {
            canvasBattle.SetDataBattle(
                "Choose Rock / Paper / Scissors",
                () => CmdSelectChoice(RPSChoice.Rock),
                () => CmdSelectChoice(RPSChoice.Paper),
                () => CmdSelectChoice(RPSChoice.Scissors)
            );
            canvasBattle.SetState(CanvasState.Battle);
        }

        [Command]
        void CmdSelectChoice(RPSChoice choice)
        {
            if (state != RPSState.Selecting) return;
            if (myChoice != RPSChoice.None) return;

            myChoice = choice;
            Target_ShowWaiting(connectionToClient, choice);

            if (!NetworkServer.spawned.TryGetValue(opponentNetId, out var id)) return;
            if (!id.TryGetComponent(out RPSPlayer other)) return;
            if (other.myChoice == RPSChoice.None) return;

            Server_ResolveBattle(this, other);
        }

        [TargetRpc]
        void Target_ShowWaiting(NetworkConnectionToClient _, RPSChoice choice)
        {
            canvasBattle.SetBattleWaitingOther($"You picked {choice}, waiting opponent...");
            canvasBattle.SetState(CanvasState.Battle);
        }

        [Server]
        void Server_ResolveBattle(RPSPlayer a, RPSPlayer b)
        {
            int result = Compare(a.myChoice, b.myChoice);

            a.Target_ShowResult(a.connectionToClient, result, a.myChoice, b.myChoice);
            b.Target_ShowResult(b.connectionToClient, -result, b.myChoice, a.myChoice);

            a.ResetBattleState();
            b.ResetBattleState();
        }

        [Server]
        int Compare(RPSChoice a, RPSChoice b)
        {
            if (a == b) return 0;
            if ((a == RPSChoice.Rock && b == RPSChoice.Scissors) ||
                (a == RPSChoice.Scissors && b == RPSChoice.Paper) ||
                (a == RPSChoice.Paper && b == RPSChoice.Rock))
                return 1;
            return -1;
        }

        [TargetRpc]
        void Target_ShowResult(NetworkConnectionToClient _, int result, RPSChoice you, RPSChoice opp)
        {
            string msg = result switch
            {
                0 => $"Draw!\nYou [{you}] vs Opp [{opp}]",
                1 => $"You WIN!\nYou [{you}] vs Opp [{opp}]",
                -1 => $"You LOSE!\nYou [{you}] vs Opp [{opp}]",
                _ => ""
            };

            canvasBattle.SetDataEnd(msg, () => canvasBattle.SetState(CanvasState.None));
            canvasBattle.SetState(CanvasState.End);
        }

        [Server]
        void ResetBattleState()
        {
            state = RPSState.Idle;
            opponentNetId = 0;
            myChoice = RPSChoice.None;
            isChallenger = false;

            RpcUpdatePlayerState();
        }
    }
}
