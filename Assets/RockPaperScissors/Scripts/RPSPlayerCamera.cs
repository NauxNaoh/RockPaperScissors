using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameRPS
{
    public class RPSPlayerCamera : NetworkBehaviour
    {
        private Camera mainCam;
        public Vector3 rotation = new Vector3(60f, 0f, 0f);
        public Vector3 offset = new Vector3(0, 10, -5);

        private void Awake()
        {
            mainCam = Camera.main;
        }

        public override void OnStartLocalPlayer()
        {
            if (mainCam != null)
            {
                mainCam.orthographic = false;

                mainCam.transform.position = transform.position + offset;
                mainCam.transform.rotation = Quaternion.Euler(rotation);
                //mainCam.transform.SetParent(transform);

                //mainCam.transform.localPosition = offset;
                //mainCam.transform.localEulerAngles = rotation;
            }
            else
                Debug.LogWarning("PlayerCamera: Could not find a camera in scene with 'MainCamera' tag.");
        }

        private void LateUpdate()
        {
            if (!isLocalPlayer || mainCam == null) return;

            Vector3 targetPos = transform.position + offset;
            mainCam.transform.position = targetPos;
        }

        void OnApplicationQuit()
        {
            ReleaseCamera();
        }

        public override void OnStopLocalPlayer()
        {
            ReleaseCamera();
        }

        void OnDisable()
        {
            ReleaseCamera();
        }

        void OnDestroy()
        {
            ReleaseCamera();
        }

        void ReleaseCamera()
        {
            if (mainCam != null && mainCam.transform.parent == transform)
            {
                mainCam.transform.SetParent(null);
                mainCam.orthographic = true;
                mainCam.orthographicSize = 15f;
                mainCam.transform.localPosition = new Vector3(0f, 70f, 0f);
                mainCam.transform.localEulerAngles = new Vector3(90f, 0f, 0f);

                if (mainCam.gameObject.scene != SceneManager.GetActiveScene())
                    SceneManager.MoveGameObjectToScene(mainCam.gameObject, SceneManager.GetActiveScene());
            }
        }
    }
}