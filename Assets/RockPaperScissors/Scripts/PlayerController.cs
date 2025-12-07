using UnityEngine;

namespace GameRPS
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 200f;
        private FloatingJoystick joystick;
        public bool initialized;
        public void Initialize(FloatingJoystick joy)
        {
            joystick = joy;
            if (joystick != null)
                initialized = true;
        }

        public Vector3 GetNewPos()
        {
            Vector2 joyInput = joystick.Direction;
            Vector3 moveDir = new(joyInput.x, 0f, joyInput.y);
            return moveDir * moveSpeed * Time.fixedDeltaTime;
        }
    }
}