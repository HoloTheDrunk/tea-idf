using UnityEngine;

namespace Player
{
    public class CameraController : MonoBehaviour
    {
        [Header("Settings")] //
        public float mouseSensitivity = 5;

        private float _mouseX, _mouseY;

        [Header("Player info")] //
        public Transform player;

        public float playerHeight;

        private float _tiltGoal;
        private float _curTilt;

        public float TiltGoal
        {
            get => _tiltGoal;
            set => _tiltGoal = value;
        }

        // Start is called before the first frame update
        void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        void LateUpdate()
        {
            transform.position = player.position + Vector3.up * playerHeight;
            CameraControl();
            Tilt();
        }

        private void CameraControl()
        {
            _mouseX += Input.GetAxis("Mouse X") * mouseSensitivity;
            _mouseY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            _mouseY = Mathf.Clamp(_mouseY, -89f, 89f);

            transform.rotation = Quaternion.Euler(_mouseY, _mouseX, 0);
        }

        private void Tilt()
        {
            _curTilt = Mathf.Lerp(_curTilt, _tiltGoal, 0.1f);
            transform.RotateAround(player.position, player.forward, _curTilt);
        }
    }
}