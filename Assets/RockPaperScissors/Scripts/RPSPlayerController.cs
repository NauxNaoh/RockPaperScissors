using Mirror;
using UnityEngine;

namespace GameRPS
{
    public class RPSPlayerController : NetworkBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject prefabCanvasJoystick;
        [SerializeField] private Rigidbody rigidbody;
        [SerializeField] private float moveSpeed = 250f;
        private FloatingJoystick joystick;
        private bool initialized;
        private RPSPlayer playerRoot;

        public void Initialize()
        {
            initialized = true;
        }


        public void OnStateChanged(RPSState newState, CanvasState canvasState)
        {
            bool canMove = (newState == RPSState.Idle && canvasState == CanvasState.None);
            joystick.gameObject.SetActive(canMove);
        }


        #region Movement
        private void FixedUpdate()
        {
            if (!isLocalPlayer) return;
            if (!initialized) return;
            if (playerRoot.state != RPSState.Idle) return;

            joystick.gameObject.SetActive(true);
            Vector3 moveDir = GetMoveDirection();
            Vector3 newVelocity = moveDir * moveSpeed * Time.fixedDeltaTime;

            CmdChangePositionAndRotation(moveDir, newVelocity);
        }

        [Client]
        public Vector3 GetMoveDirection()
        {
            Vector2 joyInput = joystick.Direction;
            return new(joyInput.x, 0f, joyInput.y);
        }

        [Command]
        void CmdChangePositionAndRotation(Vector3 moveDir, Vector3 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            rigidbody.linearVelocity = velocity;
#else
            rigidbody.velocity = velocity;
#endif

            if (moveDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                rigidbody.MoveRotation(targetRot);
            }
        }
        #endregion


        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            var canvasObject = Instantiate(prefabCanvasJoystick);
            FloatingJoystick joy = canvasObject.GetComponentInChildren<FloatingJoystick>();
            RPSPlayer player = netIdentity.GetComponent<RPSPlayer>();


            if (joy == null || player == null) return;
            joystick = joy;
            playerRoot = player;
            Initialize();
        }
    }
}