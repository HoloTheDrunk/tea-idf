using UnityEngine;

namespace Player
{
	public class UserInput : MonoBehaviour
	{
		[HideInInspector] public float x, y;
		[HideInInspector] public bool jumping, sprinting, crouching;

		public void FixedUpdate()
		{
			x = Input.GetAxisRaw("Horizontal");
			y = Input.GetAxisRaw("Vertical");
			jumping = Input.GetButton("Jump");
			crouching = Input.GetKey(KeyCode.LeftControl);
		}
	}
}