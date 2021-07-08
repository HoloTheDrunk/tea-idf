using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Player
{
	public class CameraController : MonoBehaviour
	{
		[Header("Settings")] //
		public float mouseSensitivity = 5;
		private float _mouseX, _mouseY;

		[Header("Player info")] //
		public Transform player;
		public float playerHeight;

		private bool _tilted = false;

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
		}

		private void CameraControl()
		{
			_mouseX += Input.GetAxis("Mouse X") * mouseSensitivity;
			_mouseY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
			_mouseY = Mathf.Clamp(_mouseY, -35, 60);

			transform.rotation = Quaternion.Euler(_mouseY, _mouseX, 0);
		}

		public void Tilt(int tilt)
		{
			if (!_tilted)
			{
				transform.RotateAround(transform.position, transform.forward, tilt * 15f);
				_tilted = true;
			}
			else
			{
				if (tilt == 0)
					_tilted = false;
			}
		}
	}
}