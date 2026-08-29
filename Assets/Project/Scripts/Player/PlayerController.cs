using Fusion;
using UnityEngine;

namespace NonameGame
{
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement specifics")]
        [Tooltip("Layers where the player can stand on")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.25f;
        [Tooltip("Base player speed")]
        public float movementSpeed = 14f;
        [Range(0.01f, 0.99f)]
        [Tooltip("Minimum input value to trigger movement")]
        public float movementThrashold = 0.01f;
        [Space(10)]

        [Tooltip("Speed up multiplier")]
        public float dampSpeedUp = 0.2f;
        [Tooltip("Speed down multiplier")]
        public float dampSpeedDown = 0.1f;


        [Header("Jump and gravity specifics")]
        [Tooltip("Jump velocity")]
        public float jumpVelocity = 20f;
        [Tooltip("Multiplier applied to gravity when the player is falling")]
        public float fallMultiplier = 1.7f;
        [Range(0f, 1f)]
        [Tooltip("Player friction against floor")]
        public float frictionAgainstFloor = 0.3f;
        [SerializeField] private float dashForce = 15f;
        [SerializeField] private float dashStunDuration = 0.25f;
        [Space(10)]

        [Header("Slope and step specifics")]
        [Tooltip("Distance from the player feet used to check if the player is touching a slope")]
        public float slopeCheckerThrashold = 0.51f;
        [Tooltip("Distance from the player center used to check if the player is touching a step")]
        public float stepCheckerThrashold = 0.6f;
        [Space(10)]

        [Range(1f, 89f)]
        [Tooltip("Max climbable slope angle")]
        public float maxClimbableSlopeAngle = 53.6f;
        [Tooltip("Max climbable step height")]
        public float maxStepHeight = 0.74f;
        [Space(10)]

        [Tooltip("Speed multiplier based on slope angle")]
        public AnimationCurve speedMultiplierOnAngle = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Range(0.01f, 1f)]
        [Tooltip("Multipler factor on climbable slope")]
        public float canSlideMultiplierCurve = 0.061f;
        [Range(0.01f, 1f)]
        [Tooltip("Multipler factor on non climbable slope")]
        public float cantSlideMultiplierCurve = 0.039f;
        [Range(0.01f, 1f)]
        [Tooltip("Multipler factor on step")]
        public float climbingStairsMultiplierCurve = 0.637f;
        [Space(10)]

        [Tooltip("Multipler factor for gravity")]
        public float gravityMultiplier = 6f;
        [Tooltip("Multipler factor for gravity used on change of normal")]
        public float gravityMultiplyerOnSlideChange = 3f;
        [Tooltip("Multipler factor for gravity used on non climbable slope")]
        public float gravityMultiplierIfUnclimbableSlope = 30f;
        [Space(10)]

        public bool lockOnSlope = false;

        [Tooltip("Character model")]
        public Transform cameraTarget;
        [Tooltip("Character rotation speed when the forward direction is changed")]
        public float characterModelRotationSmooth = 0.1f;
        [Space(10)]

        private Vector3 forward;
        private Vector3 globalForward;
        private Vector3 reactionForward;
        private Vector3 down;
        private Vector3 globalDown;
        private Vector3 reactionGlobalDown;
        private float currentSurfaceAngle;
        private bool currentLockOnSlope;
        private Vector3 groundNormal;
        private Vector3 prevGroundNormal;
        private float coyoteJumpMultiplier = 1f;
        private bool isGrounded = false;
        private bool isTouchingSlope = false;
        private bool isTouchingStep = false;
        private bool isJumping = false;
        private float targetAngle;
        private Rigidbody rigidbody;
        private CapsuleCollider collider;
        private float originalColliderHeight;
        private Vector3 currVelocity;
        private float turnSmoothVelocity;
        private Vector3 _networkCameraDirection;
        private bool netDashAnimationFlag;


        [Networked] private NetworkBool _hasDashedInAir { get; set; }
        [Networked] private TickTimer _dashTimer { get; set; }
        [Networked] private Vector3 _dashStoredDirection { get; set; }
        [Networked] public TickTimer stunTimer { get; set; }


        public bool GetGrounded() { return isGrounded; }
        public bool GetTouchingSlope() { return isTouchingSlope; }
        public bool GetTouchingStep() { return isTouchingStep; }
        public bool GetJumping() { return isJumping; }

        public override void Spawned()
        {
            rigidbody = this.GetComponent<Rigidbody>();
            collider = this.GetComponent<CapsuleCollider>();
            originalColliderHeight = collider.height;

            SetFriction(frictionAgainstFloor, true);
            currentLockOnSlope = lockOnSlope;

            var camera = FindAnyObjectByType<CameraManager>();
            if (camera != null)
                camera.InitForPlayer(cameraTarget);
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetworkInputData data))
            {
                if (isGrounded)
                {
                    _hasDashedInAir = false;
                }

                if (data.Move.magnitude > movementThrashold)
                {
                    Quaternion cameraYRotation = Quaternion.Euler(0f, data.CameraRotationY, 0f);
                    Vector3 camForward = cameraYRotation * Vector3.forward;
                    Vector3 camRight = cameraYRotation * Vector3.right;

                    _networkCameraDirection = (camForward * data.Move.y + camRight * data.Move.x).normalized;
                    targetAngle = Mathf.Atan2(data.Move.x, data.Move.y) * Mathf.Rad2Deg + data.CameraRotationY;
                }
                else
                {
                    _networkCameraDirection = Vector3.zero;
                }

                CheckGrounded();
                CheckStep();
                CheckSlopeAndDirections();

                if (!stunTimer.ExpiredOrNotRunning(Runner))
                {
                    netDashAnimationFlag = true;
                }
                else if (!_dashTimer.ExpiredOrNotRunning(Runner))
                {
                    Vector3 currentVel = rigidbody.linearVelocity;
                    rigidbody.linearVelocity = new Vector3(_dashStoredDirection.x * dashForce, currentVel.y, _dashStoredDirection.z * dashForce);
                }
                else
                {
                    if (data.Move.magnitude > movementThrashold)
                    {
                        Vector3 targetVelocity = _networkCameraDirection * movementSpeed;
                        targetVelocity.y = rigidbody.linearVelocity.y;
                        rigidbody.linearVelocity = Vector3.SmoothDamp(rigidbody.linearVelocity, targetVelocity, ref currVelocity, dampSpeedUp);
                    }
                    else
                    {
                        Vector3 targetVelocity = Vector3.zero;
                        targetVelocity.y = rigidbody.linearVelocity.y;
                        rigidbody.linearVelocity = Vector3.SmoothDamp(rigidbody.linearVelocity, targetVelocity, ref currVelocity, dampSpeedDown);
                    }

                    float angle = Mathf.SmoothDampAngle(cameraTarget.eulerAngles.y, targetAngle, ref turnSmoothVelocity, characterModelRotationSmooth);
                    transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
                    cameraTarget.rotation = Quaternion.Euler(0f, angle, 0f);

                    if (data.SpacePressed && isGrounded && ((isTouchingSlope && currentSurfaceAngle <= maxClimbableSlopeAngle) || !isTouchingSlope))
                    {
                        rigidbody.linearVelocity += Vector3.up * jumpVelocity;
                        isJumping = true;
                    }

                    if (rigidbody.linearVelocity.y < 0 && !isGrounded) coyoteJumpMultiplier = fallMultiplier;
                    else
                    {
                        isJumping = false;
                        coyoteJumpMultiplier = 1f;
                    }

                    if (data.SpacePressed && !isGrounded && !_hasDashedInAir)
                    {
                        _hasDashedInAir = true;
                        _dashTimer = TickTimer.CreateFromSeconds(Runner, dashStunDuration);
                        _dashStoredDirection = (_networkCameraDirection != Vector3.zero) ? _networkCameraDirection : cameraTarget.forward;
                        rigidbody.linearVelocity = Vector3.zero;
                        Vector3 impulseVector = _dashStoredDirection;
                        impulseVector.y = 0.2f;
                        rigidbody.AddForce(impulseVector.normalized * dashForce, ForceMode.Impulse);

                        netDashAnimationFlag = true;
                    }
                }

                ApplyGravity();
            }
        }

        private void CheckGrounded()
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);
        }

        private void CheckStep()
        {
            bool tmpStep = false;
            Vector3 bottomStepPos = transform.position - new Vector3(0f, originalColliderHeight / 2f, 0f) + new Vector3(0f, 0.05f, 0f);

            RaycastHit stepLowerHit;
            if (Physics.Raycast(bottomStepPos, globalForward, out stepLowerHit, stepCheckerThrashold, groundMask))
            {
                RaycastHit stepUpperHit;
                if (RoundValue(stepLowerHit.normal.y) == 0 &&
                !Physics.Raycast(bottomStepPos + new Vector3(0f, maxStepHeight, 0f), globalForward, out stepUpperHit, stepCheckerThrashold + 0.05f, groundMask))
                {
                    tmpStep = true;
                }
            }

            RaycastHit stepLowerHit45;
            if (Physics.Raycast(bottomStepPos, Quaternion.AngleAxis(45, transform.up) * globalForward, out stepLowerHit45, stepCheckerThrashold, groundMask))
            {
                RaycastHit stepUpperHit45;
                if (RoundValue(stepLowerHit45.normal.y) == 0 && !Physics.Raycast(bottomStepPos + new Vector3(0f, maxStepHeight, 0f), Quaternion.AngleAxis(45, Vector3.up) * globalForward, out stepUpperHit45, stepCheckerThrashold + 0.05f, groundMask))
                {
                    tmpStep = true;
                }
            }

            RaycastHit stepLowerHitMinus45;
            if (Physics.Raycast(bottomStepPos, Quaternion.AngleAxis(-45, transform.up) * globalForward, out stepLowerHitMinus45, stepCheckerThrashold, groundMask))
            {
                RaycastHit stepUpperHitMinus45;
                if (RoundValue(stepLowerHitMinus45.normal.y) == 0 && !Physics.Raycast(bottomStepPos + new Vector3(0f, maxStepHeight, 0f), Quaternion.AngleAxis(-45, Vector3.up) * globalForward, out stepUpperHitMinus45, stepCheckerThrashold + 0.05f, groundMask))
                {
                    tmpStep = true;
                }
            }

            isTouchingStep = tmpStep;
        }

        private void CheckSlopeAndDirections()
        {
            prevGroundNormal = groundNormal;

            RaycastHit slopeHit;
            if (Physics.SphereCast(transform.position, slopeCheckerThrashold, Vector3.down, out slopeHit, originalColliderHeight / 2f + 0.5f, groundMask))
            {
                groundNormal = slopeHit.normal;

                if (slopeHit.normal.y == 1)
                {
                    forward = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                    globalForward = forward;
                    reactionForward = forward;

                    SetFriction(frictionAgainstFloor, true);
                    currentLockOnSlope = lockOnSlope;

                    currentSurfaceAngle = 0f;
                    isTouchingSlope = false;
                }
                else
                {
                    Vector3 tmpGlobalForward = transform.forward.normalized;
                    Vector3 tmpForward = new Vector3(tmpGlobalForward.x, Vector3.ProjectOnPlane(transform.forward.normalized, slopeHit.normal).normalized.y, tmpGlobalForward.z);
                    Vector3 tmpReactionForward = new Vector3(tmpForward.x, tmpGlobalForward.y - tmpForward.y, tmpForward.z);

                    if (currentSurfaceAngle <= maxClimbableSlopeAngle && !isTouchingStep)
                    {
                        forward = tmpForward * ((speedMultiplierOnAngle.Evaluate(currentSurfaceAngle / 90f) * canSlideMultiplierCurve) + 1f);
                        globalForward = tmpGlobalForward * ((speedMultiplierOnAngle.Evaluate(currentSurfaceAngle / 90f) * canSlideMultiplierCurve) + 1f);
                        reactionForward = tmpReactionForward * ((speedMultiplierOnAngle.Evaluate(currentSurfaceAngle / 90f) * canSlideMultiplierCurve) + 1f);

                        SetFriction(frictionAgainstFloor, true);
                        currentLockOnSlope = lockOnSlope;
                    }
                    else if (isTouchingStep)
                    {
                        forward = tmpForward * ((speedMultiplierOnAngle.Evaluate(currentSurfaceAngle / 90f) * climbingStairsMultiplierCurve) + 1f);
                        globalForward = tmpGlobalForward * ((speedMultiplierOnAngle.Evaluate(currentSurfaceAngle / 90f) * climbingStairsMultiplierCurve) + 1f);
                        reactionForward = tmpReactionForward * ((speedMultiplierOnAngle.Evaluate(currentSurfaceAngle / 90f) * climbingStairsMultiplierCurve) + 1f);

                        SetFriction(frictionAgainstFloor, true);
                        currentLockOnSlope = true;
                    }
                    else
                    {
                        forward = tmpForward * ((speedMultiplierOnAngle.Evaluate(currentSurfaceAngle / 90f) * cantSlideMultiplierCurve) + 1f);
                        globalForward = tmpGlobalForward * ((speedMultiplierOnAngle.Evaluate(currentSurfaceAngle / 90f) * cantSlideMultiplierCurve) + 1f);
                        reactionForward = tmpReactionForward * ((speedMultiplierOnAngle.Evaluate(currentSurfaceAngle / 90f) * cantSlideMultiplierCurve) + 1f);

                        SetFriction(0f, true);
                        currentLockOnSlope = lockOnSlope;
                    }

                    currentSurfaceAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                    isTouchingSlope = true;
                }

                down = Vector3.Project(Vector3.down, slopeHit.normal);
                globalDown = Vector3.down.normalized;
                reactionGlobalDown = Vector3.up.normalized;
            }
            else
            {
                groundNormal = Vector3.zero;

                forward = Vector3.ProjectOnPlane(transform.forward, slopeHit.normal).normalized;
                globalForward = forward;
                reactionForward = forward;

                down = Vector3.down.normalized;
                globalDown = Vector3.down.normalized;
                reactionGlobalDown = Vector3.up.normalized;

                SetFriction(frictionAgainstFloor, true);
                currentLockOnSlope = lockOnSlope;
            }
        }

        private void ApplyGravity()
        {
            Vector3 gravity = Vector3.zero;

            if (currentLockOnSlope || isTouchingStep) gravity = down * gravityMultiplier * -Physics.gravity.y * coyoteJumpMultiplier;
            else gravity = globalDown * gravityMultiplier * -Physics.gravity.y * coyoteJumpMultiplier;

            if (groundNormal.y != 1 && groundNormal.y != 0 && isTouchingSlope && prevGroundNormal != groundNormal)
            {
                gravity *= gravityMultiplyerOnSlideChange;
            }

            if (groundNormal.y != 1 && groundNormal.y != 0 && (currentSurfaceAngle > maxClimbableSlopeAngle && !isTouchingStep))
            {
                if (currentSurfaceAngle > 0f && currentSurfaceAngle <= 30f) gravity = globalDown * gravityMultiplierIfUnclimbableSlope * -Physics.gravity.y;
                else if (currentSurfaceAngle > 30f && currentSurfaceAngle <= 89f) gravity = globalDown * gravityMultiplierIfUnclimbableSlope / 2f * -Physics.gravity.y;
            }

            rigidbody.AddForce(gravity);
        }

        private void SetFriction(float _frictionWall, bool _isMinimum)
        {
            if (collider == null || collider.material == null) return;
            collider.material.dynamicFriction = 0.6f * _frictionWall;
            collider.material.staticFriction = 0.6f * _frictionWall;

            if (_isMinimum) collider.material.frictionCombine = PhysicsMaterialCombine.Minimum;
            else collider.material.frictionCombine = PhysicsMaterialCombine.Maximum;
        }

        private float RoundValue(float _value)
        {
            float unit = (float)Mathf.Round(_value);
            if (_value - unit < 0.000001f && _value - unit > -0.000001f) return unit;
            else return _value;
        }
    }
}
