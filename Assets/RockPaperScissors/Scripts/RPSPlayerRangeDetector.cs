using Mirror;
using UnityEngine;

namespace GameRPS
{
    public class RPSPlayerRangeDetector : NetworkBehaviour
    {
        private RPSPlayer owner;
        private RPSPlayer targetPlayer;

        private void Awake()
        {
            owner = GetComponent<RPSPlayer>();
        }
        void Start()
        {
            if (isServer && !isClient)
                GetComponent<Collider>().enabled = false;
        }

        [Client]
        private void OnTriggerEnter(Collider other)
        {
            if (!owner.isLocalPlayer) return;

            if (other.TryGetComponent(out RPSPlayer otherPlayer))
            {
                if (otherPlayer == owner) return;
                if (owner.state != RPSState.Idle) return;

                targetPlayer = otherPlayer;
                owner.canvasBattle.ShowChallengeButton(() => owner.CmdRequestChallenge(otherPlayer.netId), otherPlayer.netId);
            }
        }

        [Client]
        private void OnTriggerExit(Collider other)
        {
            if (!owner.isLocalPlayer) return;

            if (other.TryGetComponent(out RPSPlayer otherPlayer))
            {
                if (targetPlayer == otherPlayer)
                {
                    targetPlayer = null;
                    owner.canvasBattle.HideChallengeButton();
                }
            }
        }

    }
}