using UnityEngine;

namespace Assets.Scripts.Player
{
	public class UserInput : MonoBehaviour
	{
		[HideInInspector] public float x, y;
		[HideInInspector] public bool jumping, sprinting, crouching;

		[HideInInspector] public bool canJump;
		public float jumpCooldown;
		private float _jumpTimer;

		public void FixedUpdate()
		{
			x = Input.GetAxisRaw("Horizontal");
			y = Input.GetAxisRaw("Vertical");
			jumping = Input.GetButton("Jump");
			crouching = Input.GetKey(KeyCode.LeftControl);

			if (_jumpTimer < 0)
			{
				canJump = true;
				if (jumping)
					_jumpTimer = jumpCooldown;
			}
			else
			{
				canJump = false;
				_jumpTimer -= Time.fixedDeltaTime;
			}
		}
	}
}