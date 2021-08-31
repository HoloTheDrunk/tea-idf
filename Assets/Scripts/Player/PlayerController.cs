using Actionable;
using UnityEngine;

namespace Player
{
    internal struct RaycastResult
    {
        public RaycastHit HitInfo;
        public bool Status;
    }

    public class PlayerController : MonoBehaviour
    {
        // Constants
        private const float MovSpeedMult = 1000f;
        private const float JumpForceMult = 10f;

        // Speed
        [Header("Movement")] // Movement
        [Range(0f, 1000f)]
        public float movementSpeed;

        [Range(0, 20)] public float minMaxRunSpeed;
        private float _curMaxRunSpeed;

        // Jump
        [Range(0f, 100f)] public float jumpForce;
        [HideInInspector] public bool isGrounded;
        public float jumpCooldown;
        [SerializeField]
        private float jumpTimer;

        [Tooltip("Percent values between 0 and 100.")]
        public Vector3 dragVector;

        public float wallrideTilt = 15f;

        [Header("Camera")] // Camera
        public Camera mainCamera;

        public AnimationCurve fovCurve;

        // Items
        private Transform _aimedAtItem;

        // Physics
        private Rigidbody _rb;
        
        // Wall hits
        private RaycastResult _rightWall;
        private RaycastResult _backWall;
        private RaycastResult _leftWall;

        // Input
        private UserInput _userInput;


        public void Start()
        {
            _rb = GetComponent<Rigidbody>();

            _userInput = GetComponent<UserInput>();
            jumpTimer = jumpCooldown;

            _leftWall = new RaycastResult();
            _rightWall = new RaycastResult();
            _backWall = new RaycastResult();
        }

        public void Update()
        {
            mainCamera.fieldOfView +=
                (60 + 60 * fovCurve.Evaluate(_rb.velocity.magnitude / 100) - mainCamera.fieldOfView) * 0.2f;

            Transform mainCameraTransform = mainCamera.transform;
            bool hitItem = Physics.Raycast(mainCameraTransform.position, mainCameraTransform.forward,
                out RaycastHit hit, 5f,
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
                    UseAction useAction = hit.transform.gameObject.GetComponent<UseAction>();
                    if (useAction != null)
                        useAction.triggered = true;
                }
            }
            else
            {
                CleanOutlines();
            }
        }

        public void FixedUpdate()
        {
            isGrounded = IsGrounded();
            jumpTimer = Mathf.Clamp(jumpTimer - Time.fixedDeltaTime, -1f, jumpCooldown);

            bool isTouchingWall;

            // transform.localRotation = Quaternion.Euler(0, transform.localEulerAngles.y, 0);
            if (isGrounded)
            {
                if (_userInput.jumping) Jump(Vector3.up * (jumpForce * JumpForceMult));

                (_leftWall.Status, _rightWall.Status, _backWall.Status) = (false, false, false);
                isTouchingWall = false;
            }
            else
            {
                _rightWall.Status = Physics.Raycast(transform.position, transform.right, out _rightWall.HitInfo,
                    0.75f, 1 << LayerMask.NameToLayer("Buildings"));
                _leftWall.Status = Physics.Raycast(transform.position, -transform.right, out _leftWall.HitInfo,
                    0.75f, 1 << LayerMask.NameToLayer("Buildings"));
                _backWall.Status = Physics.Raycast(transform.position, -transform.forward, out _backWall.HitInfo,
                    0.75f, 1 << LayerMask.NameToLayer("Buildings"));

                isTouchingWall = _leftWall.Status || _rightWall.Status || _backWall.Status;

                if (isTouchingWall && _userInput.jumping)
                {
                    Vector3 wallNormalAddition = (_leftWall.Status ? _leftWall.HitInfo.normal : Vector3.zero) +
                                                 (_rightWall.Status ? _rightWall.HitInfo.normal : Vector3.zero) +
                                                 (_backWall.Status ? _backWall.HitInfo.normal : Vector3.zero);
                    Jump((transform.forward + transform.up + wallNormalAddition) * (jumpForce * JumpForceMult));
                }
            }

            // _userInput.x = Mathf.Abs(_rb.velocity.x) > baseMaxRunSpeed ? 0f : _userInput.x;
            // _userInput.y = Mathf.Abs(_rb.velocity.z) > baseMaxRunSpeed ? 0f : _userInput.y;

            _rb.AddForce(transform.right * (_userInput.x * Time.fixedDeltaTime * movementSpeed * MovSpeedMult));
            _rb.AddForce(transform.forward * (_userInput.y * Time.fixedDeltaTime * movementSpeed * MovSpeedMult));

            if (_rb.velocity.magnitude > _curMaxRunSpeed)
            {
                _rb.AddForce(_rb.velocity.normalized * (_curMaxRunSpeed - _rb.velocity.magnitude));
            }
            else if (_rb.velocity.magnitude > _curMaxRunSpeed * .95f)
            {
                _curMaxRunSpeed *= 1.1f;
            }
            else if (_rb.velocity.magnitude < _curMaxRunSpeed * .95f)
            {
                _curMaxRunSpeed *= _curMaxRunSpeed * .95f > minMaxRunSpeed ? .95f : 1f;
            }

            // Credit to:
            // https://answers.unity.com/questions/233850/rigidbody-making-drag-affect-only-horizontal-speed.html
            Vector3 vel = _rb.velocity;
            vel.x *= 1f - dragVector.x / (100f * (isGrounded ? 1f : 2f));
            if (vel.y < 0f && isTouchingWall) vel.y *= 1f - dragVector.y / 100f;

            vel.z *= 1f - dragVector.z / (100f * (isGrounded ? 1f : 2f));
            _rb.velocity = vel;
        }

        public void LateUpdate()
        {
            CameraController mainCameraController = mainCamera.GetComponent<CameraController>();
            mainCameraController.TiltGoal = (isGrounded, _leftWall.Status, _rightWall.Status) switch
            {
                (false, true, false) => -wallrideTilt,
                (false, false, true) => wallrideTilt,
                _ => 0f
            };

            // Rotate to follow camera rotation
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x,
                mainCamera.transform.rotation.eulerAngles.y,
                transform.rotation.eulerAngles.z);
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position - Vector3.up * transform.localScale.y * 1.1f);
            Gizmos.color =
                (_leftWall.Status, _rightWall.Status, _backWall.Status) switch
                {
                    (false, false, false) => Color.red,
                    (false, false, true) => Color.magenta,
                    (true, false, _) => Color.blue,
                    (false, true, _) => Color.green,
                    (true, true, _) => Color.yellow
                };
            Gizmos.DrawSphere(transform.position + transform.up * 1.5f, .5f);
        }

        private void CleanOutlines()
        {
            if (_aimedAtItem != null) Destroy(_aimedAtItem.gameObject.GetComponent<Outline>());
        }

        private bool IsGrounded()
        {
            return Physics.Raycast(transform.position, Vector3.down, transform.localScale.y * 1.1f);
        }

        private void Jump(Vector3 direction)
        {
            if (jumpTimer < 0f)
            {
                jumpTimer = jumpCooldown;
                _rb.AddForce(direction, ForceMode.Impulse);
            }
        }
    }
}