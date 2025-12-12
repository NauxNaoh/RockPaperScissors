using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.CullingGroup;

namespace GameRPS
{
    public enum CanvasState { None, Challenger, Opponent, Battle, End }
    public class CanvasBattle : MonoBehaviour
    {
        [Header("challenger")]
        public GameObject panelChallenger;
        public TextMeshProUGUI tmpChallenger;

        [Header("opponent")]
        public GameObject panelOpponent;
        public TextMeshProUGUI tmpOpponent;
        public Button btnReject;
        public Button btnAccept;

        [Header("Battle")]
        public GameObject panelBattle;
        public TextMeshProUGUI tmpBattle;
        public Button btnRock;
        public Button btnPaper;
        public Button btnScissor;
        public GameObject gobjBtns;

        [Header("end")]
        public GameObject panelEnd;
        public TextMeshProUGUI tmpEnd;
        public Button btnOK;

        [Header("challenge")]
        public Button btnChallenge;
        public TextMeshProUGUI tmpChallengeLabel;

        public CanvasState state = CanvasState.None;
        private Action<bool> actionAccept;
        private Action actionRock;
        private Action actionPaper;
        private Action actionScissors;

        public Action<CanvasState> onStateChanged;

        private void Awake()
        {
            SetState(CanvasState.None);
            RegisterEventInitialized();
        }

        void RegisterEventInitialized()
        {
            Reset();
            btnReject.onClick.AddListener(() => actionAccept?.Invoke(false));
            btnAccept.onClick.AddListener(() => actionAccept?.Invoke(true));
            btnRock.onClick.AddListener(() => actionRock?.Invoke());
            btnPaper.onClick.AddListener(() => actionPaper?.Invoke());
            btnScissor.onClick.AddListener(() => actionScissors?.Invoke());
        }

        public void SetState(CanvasState canvasState)
        {
            this.state = canvasState;

            panelChallenger.SetActive(state == CanvasState.Challenger);
            panelOpponent.SetActive(state == CanvasState.Opponent);
            panelBattle.SetActive(state == CanvasState.Battle);
            panelEnd.SetActive(state == CanvasState.End);

            onStateChanged?.Invoke(canvasState);
        }

        public void SetDataChallenger(string message)
        {
            tmpChallenger.text = message;
        }

        public void SetDataOpponent(string message, Action<bool> cbAccept)
        {
            tmpOpponent.text = message;
            actionAccept = cbAccept;
        }

        public void SetDataBattle(string message, Action cbRock, Action cbPaper, Action cbScissors)
        {
            gobjBtns.SetActive(true);
            actionRock = cbRock;
            actionPaper = cbPaper;
            actionScissors = cbScissors;
            tmpBattle.text = message;
        }

        public void SetBattleWaitingOther(string message)
        {
            gobjBtns.SetActive(false);
            actionRock = null;
            actionPaper = null;
            actionScissors = null;
            tmpBattle.text = message;
        }

        public void SetDataEnd(string message, Action action)
        {
            tmpEnd.text = message;
            gobjBtns.SetActive(true);
            btnOK.onClick.RemoveAllListeners();
            btnOK.onClick.AddListener(() => action?.Invoke());
        }

        private void Reset()
        {
            btnReject.onClick.RemoveAllListeners();
            btnAccept.onClick.RemoveAllListeners();
            btnRock.onClick.RemoveAllListeners();
            btnPaper.onClick.RemoveAllListeners();
            btnScissor.onClick.RemoveAllListeners();
            actionAccept = null;
            actionRock = null;
            actionPaper = null;
            actionScissors = null;
        }

        public void ShowChallengeButton(System.Action onClick, uint idPlayer)
        {
            tmpChallengeLabel.text = $"Challenge Player {idPlayer}";
            btnChallenge.onClick.RemoveAllListeners();
            btnChallenge.onClick.AddListener(() =>
            {
                HideChallengeButton();   // ?N NGAY L?P T?C
                onClick();               // G?i command challenge player
            });

            btnChallenge.gameObject.SetActive(true);
        }

        public void HideChallengeButton()
        {
            btnChallenge.gameObject.SetActive(false);
        }
    }
}