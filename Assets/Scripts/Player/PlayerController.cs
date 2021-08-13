using UnityEngine;

namespace Player
{
    struct RaycastResult
    {
        public RaycastHit hitInfo;
        public bool status;
    }

    public class PlayerController : MonoBehaviour
    {
        // Constants
        private const float MovSpeedMult = 1000f;
        private const float JumpForceMult = 10f;

        // Input
        private UserInput _userInput;

        [Header("Movement")] // Movement
        [Range(0f, 1000f)]
        public float movementSpeed;

        [Range(0, 20)] public float maxRunSpeed;

        [Range(0f, 100f)] public float jumpForce;
        [HideInInspector] public bool isGrounded;
        public float jumpCooldown;
        private float _jumpTimer;

        [Tooltip("Percent values between 0 and 100.")]
        public Vector3 dragVector;

        private RaycastResult _leftWall;
        private RaycastResult _rightWall;
        private RaycastResult _backWall;
        public float wallrideTilt = 15f;

        // Physics
        private Rigidbody _rb;

        // Items and inventory
        private Transform _aimedAtItem;

        [Header("Camera")] // Camera
        public Camera mainCamera;

        public AnimationCurve fovCurve;


        public void Start()
        {
            _rb = GetComponent<Rigidbody>();

            _userInput = GetComponent<UserInput>();
            _jumpTimer = jumpCooldown;

            _leftWall = new RaycastResult();
            _rightWall = new RaycastResult();
            _backWall = new RaycastResult();
        }

        public void FixedUpdate()
        {
            isGrounded = IsGrounded();
            _jumpTimer = Mathf.Clamp(_jumpTimer - Time.fixedDeltaTime, 0f, jumpCooldown);

            bool isTouchingWall = _leftWall.status || _rightWall.status || _backWall.status;

            // transform.localRotation = Quaternion.Euler(0, transform.localEulerAngles.y, 0);
            if (isGrounded)
            {
                if (_userInput.jumping)
                {
                    Jump(Vector3.up * (jumpForce * JumpForceMult));
                }

                (_leftWall.status, _rightWall.status, _backWall.status) = (false, false, false);
            }
            else
            {
                _rightWall.status = Physics.Raycast(transform.position, transform.right, out _rightWall.hitInfo,
                    0.75f, 1 << LayerMask.NameToLayer("Buildings"));
                _leftWall.status = Physics.Raycast(transform.position, -transform.right, out _leftWall.hitInfo,
                    0.75f, 1 << LayerMask.NameToLayer("Buildings"));
                _backWall.status = Physics.Raycast(transform.position, -transform.forward, out _backWall.hitInfo,
                    0.75f, 1 << LayerMask.NameToLayer("Buildings"));

                if (isTouchingWall && _userInput.jumping)
                {
                    Vector3 wallNormalAddition = (_leftWall.status ? _leftWall.hitInfo.normal : Vector3.zero) +
                                                 (_rightWall.status ? _rightWall.hitInfo.normal : Vector3.zero) +
                                                 (_backWall.status ? _backWall.hitInfo.normal : Vector3.zero);
                    Jump((transform.forward + transform.up + wallNormalAddition) * (jumpForce * JumpForceMult));
                }
            }

            _userInput.x = Mathf.Abs(_rb.velocity.x) > maxRunSpeed ? 0f : _userInput.x;
            _userInput.y = Mathf.Abs(_rb.velocity.z) > maxRunSpeed ? 0f : _userInput.y;

            _rb.AddForce(transform.right * (_userInput.x * Time.fixedDeltaTime * movementSpeed * MovSpeedMult));
            _rb.AddForce(transform.forward * (_userInput.y * Time.fixedDeltaTime * movementSpeed * MovSpeedMult));

            // Credit to:
            // https://answers.unity.com/questions/233850/rigidbody-making-drag-affect-only-horizontal-speed.html
            Vector3 vel = _rb.velocity;
            vel.x *= 1f - dragVector.x / (100f * (isGrounded ? 1f : 2f));
            if (vel.y < 0f && isTouchingWall)
            {
                vel.y *= 1f - dragVector.y / 100f;
            }

            vel.z *= 1f - dragVector.z / (100f * (isGrounded ? 1f : 2f));
            _rb.velocity = vel;
        }

        public void Update()
        {
            mainCamera.fieldOfView +=
                (60 + (60 * fovCurve.Evaluate(_rb.velocity.magnitude / 100)) - mainCamera.fieldOfView) * 0.2f;

            var mainCameraTransform = mainCamera.transform;
            bool hitItem = Physics.Raycast(mainCameraTransform.position, mainCameraTransform.forward, out var hit, 5f,
                1 << LayerMask.NameToLayer("Buttons"));
            if (hitItem)
            {
                _aimedAtItem = hit.transform;
                if (hit.transform.GetComponent<Outline>() is null)
                {
                    Outline itemOutline = hit.transform.gameObject.AddComponent<Outline>();
                    itemOutline.OutlineMode = Outline.Mode.OutlineVisible;
                    itemOutline.OutlineColor = Color.cyan;
                    itemOutline.OutlineWidth = 6;
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    hit.transform.gameObject.GetComponent<UseAction>().triggered = true;
                }
            }
            else
            {
                CleanOutlines();
            }
        }

        public void LateUpdate()
        {
            CameraController mainCameraController = mainCamera.GetComponent<CameraController>();
            if (!isGrounded)
            {
                if (_leftWall.status || _rightWall.status)
                {
                    mainCameraController.TiltGoal = (_leftWall.status, _rightWall.status) switch
                    {
                        (true, false) => -wallrideTilt,
                        (false, true) => wallrideTilt,
                        _ => 0f
                    };
                }
                else
                {
                    mainCameraController.TiltGoal = 0f;
                }
            }
            else
            {
                mainCameraController.TiltGoal = 0f;
            }

            // Rotate to follow camera rotation
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x,
                mainCamera.transform.rotation.eulerAngles.y,
                transform.rotation.eulerAngles.z);
        }

        private void CleanOutlines()
        {
            if (_aimedAtItem != null)
            {
                Destroy(_aimedAtItem.gameObject.GetComponent<Outline>());
            }
        }

        private bool IsGrounded()
        {
            return Physics.Raycast(transform.position, Vector3.down, transform.localScale.y * 1.1f);
        }

        private void Jump(Vector3 direction)
        {
            if (_jumpTimer < 0f)
            {
                _jumpTimer = jumpCooldown;
                _rb.AddForce(direction, ForceMode.Impulse);
            }
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position - Vector3.up * transform.localScale.y * 1.1f);
            Gizmos.color =
                (_leftWall.status, _rightWall.status, _backWall.status) switch
                {
                    (false, false, false) => Color.red,
                    (false, false, true) => Color.magenta,
                    (true, false, _) => Color.blue,
                    (false, true, _) => Color.green,
                    (true, true, _) => Color.yellow,
                };
            Gizmos.DrawSphere(transform.position + transform.up * 1.5f, .5f);
        }
    }
}