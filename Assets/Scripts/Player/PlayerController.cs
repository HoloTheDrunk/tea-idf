using UnityEngine;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        // Constants
        private const float MovSpeedMult = 100f;
        private const float JumpForceMult = 10f;

        // Input
        private UserInput _userInput;

        [Header("Movement")] // Movement
        [Range(0f, 1000f)]
        public float movementSpeed;

        [Range(0f, 100f)] public float jumpForce;
        [HideInInspector] public bool isGrounded;

        private bool _onWall;
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
        }

        public void FixedUpdate()
        {
            isGrounded = IsGrounded();

            // Counter skid
            _rb.AddForce(-2 * _rb.velocity);

            transform.localRotation = Quaternion.Euler(0, transform.localEulerAngles.y, 0);
            if (isGrounded)
            {
                // Check possibility of jump if the user is trying to jump
                if (_userInput.jumping && _userInput.canJump)
                    _rb.AddForce(Vector3.up * (jumpForce * JumpForceMult), ForceMode.Impulse);
                _onWall = false;
            }
            else if (_onWall)
            {
                if (_userInput.jumping && _userInput.canJump)
                    _rb.AddForce(
                        (Vector3.up + transform.right * mainCamera.GetComponent<CameraController>().TiltGoal) *
                        (jumpForce * JumpForceMult), ForceMode.Impulse);
            }

            if (!_rb.useGravity)
            {
                _rb.AddForce(Physics.gravity * _rb.mass / 3f);
            }

            _rb.AddForce(transform.right * (_userInput.x * Time.fixedDeltaTime * movementSpeed * MovSpeedMult));
            _rb.AddForce(transform.forward * (_userInput.y * Time.fixedDeltaTime * movementSpeed * MovSpeedMult));
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
                    itemOutline.OutlineColor = Color.cyan; //new Color(255, 166, 13);
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
            if (!isGrounded)
            {
                bool rightRaycast = Physics.Raycast(transform.position, transform.right, out RaycastHit right,
                    0.75f,
                    1 << LayerMask.NameToLayer("Buildings"));
                bool leftRaycast = Physics.Raycast(transform.position, -transform.right, out RaycastHit left, 0.75f,
                    1 << LayerMask.NameToLayer("Buildings"));
                if (!_onWall)
                {
                    Vector3 cur = mainCamera.transform.localRotation.eulerAngles;
                    if (rightRaycast || leftRaycast)
                    {
                        if (leftRaycast && rightRaycast)
                        {
                            // In-between walls
                            mainCamera.GetComponent<CameraController>().TiltGoal = 0f;
                        }
                        else if (rightRaycast)
                        {
                            // Wall on the right only
                            mainCamera.GetComponent<CameraController>().TiltGoal = wallrideTilt;
                        }
                        else
                        {
                            // Wall ont the left only
                            mainCamera.GetComponent<CameraController>().TiltGoal = -wallrideTilt;
                        }

                        _onWall = true;
                        _rb.useGravity = false;
                    }
                    else
                    {
                        mainCamera.GetComponent<CameraController>().TiltGoal = 0f;
                        _onWall = false;
                        _rb.useGravity = true;
                    }
                }
                else
                {
                    if (!(rightRaycast || leftRaycast))
                    {
                        mainCamera.GetComponent<CameraController>().TiltGoal = 0f;
                        _onWall = false;
                        _rb.useGravity = true;
                    }
                }
            }
            else
            {
                mainCamera.GetComponent<CameraController>().TiltGoal = 0f;
                _onWall = false;
                _rb.useGravity = true;
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

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position - Vector3.up * transform.localScale.y * 1.1f);
            Gizmos.color = _onWall ? Color.green : Color.red;
            Gizmos.DrawSphere(transform.position, .6f);
        }
    }
}