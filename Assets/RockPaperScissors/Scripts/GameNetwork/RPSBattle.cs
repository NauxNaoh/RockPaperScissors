using System;

namespace GameRPS
{
    public enum RPSChoice : byte { None, Rock, Paper, Scissors }
    public enum RPSState : byte { Idle, WaitingConfirm, Selecting, Completed }

    [Serializable]
    public class RPSBattle
    {
        public RPSState state;
        public RPSPlayer challenger;
        public RPSChoice challengerChoice;
        public RPSPlayer opponent;
        public RPSChoice opponentChoice;

        public bool isNoBattle => challenger == null && opponent == null;

        public RPSBattle()
        {
            state = RPSState.Idle;
            this.challenger = null;
            this.challengerChoice = RPSChoice.None;
            this.opponent = null;
            this.opponentChoice = RPSChoice.None;
        }

        public RPSBattle(RPSState state = RPSState.Idle, RPSPlayer challenger = null, RPSChoice challengerChoice = RPSChoice.None, RPSPlayer opponent = null, RPSChoice opponentChoice = RPSChoice.None)
        {
            this.state = state;
            this.challenger = challenger;
            this.challengerChoice = challengerChoice;
            this.opponent = opponent;
            this.opponentChoice = opponentChoice;
        }

        public RPSBattle(RPSBattle battleRef)
        {
            this.state = battleRef.state;
            this.challenger = battleRef.challenger;
            this.challengerChoice = battleRef.challengerChoice;
            this.opponent = battleRef.opponent;
            this.opponentChoice = battleRef.opponentChoice;
        }
    }
}