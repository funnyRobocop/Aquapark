using Fusion;
using UnityEngine;

namespace NonameGame
{
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float movementThreshold = 0.1f;
        [SerializeField] private float dampSpeedUp = 0.12f;
        [SerializeField] private float dampSpeedDown = 0.08f;
        [SerializeField] private float airControl = 0.65f;

        [Header("Jump / Dash")]
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float dashForce = 15f;
        [SerializeField] private float dashDuration = 0.22f;
        [SerializeField] private float dashUpBoost = 0.2f;
        [SerializeField] private float fallMultiplier = 2.2f;

        [Header("Ground")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.28f;
        [SerializeField] private LayerMask groundMask = ~0;

        [Header("Step")]
        [SerializeField] private float stepCheckerThreshold = 0.6f;
        [SerializeField] private float maxStepHeight = 0.45f;

        [Header("Slope")]
        [SerializeField] private float slopeCheckerThreshold = 0.45f;
        [SerializeField] private float maxClimbableSlopeAngle = 55f;
        [SerializeField] private float gravityMultiplier = 3.5f;
        [SerializeField] private float gravityMultiplierOnSlideChange = 2.5f;
        [SerializeField] private float gravityMultiplierIfUnclimbableSlope = 20f;
        [SerializeField] private bool lockOnSlope = false;

        [Header("Friction")]
        [SerializeField] private float frictionAgainstFloor = 0.3f;

        [Header("References")]
        [SerializeField] private Transform cameraTarget;

        private Rigidbody _rb;
        private CapsuleCollider _col;
        private float _originalColliderHeight;

        private Vector3 _forward;
        private Vector3 _globalForward;
        private Vector3 _down;
        private Vector3 _globalDown;
        private Vector3 _groundNormal = Vector3.up;
        private Vector3 _prevGroundNormal = Vector3.up;

        private float _currentSurfaceAngle;
        private bool _currentLockOnSlope;
        private bool _isGrounded;
        private bool _isTouchingSlope;
        private bool _isTouchingStep;

        private Vector3 _moveDir;
        private float _targetAngle;
        private Vector3 _smoothVel;
        private float _coyoteJumpMultiplier = 1f;

        [Networked] private NetworkBool _hasDashedInAir { get; set; }
        [Networked] private TickTimer _dashTimer { get; set; }
        [Networked] public TickTimer _stunTimer { get; set; }

        public bool IsGrounded => _isGrounded;

        public void SetStunTimer(float duration)
        {
            if (!HasStateAuthority)
                return;

            _stunTimer = TickTimer.CreateFromSeconds(Runner, duration);
        }

        public override void Spawned()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<CapsuleCollider>();
            _originalColliderHeight = _col.height;

            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            SetFriction(frictionAgainstFloor, true);
            _currentLockOnSlope = lockOnSlope;

            if (HasStateAuthority && cameraTarget != null)
            {
                var cam = FindAnyObjectByType<CameraManager>();
                if (cam != null)
                    cam.InitForPlayer(cameraTarget);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (!GetInput(out NetworkInputData data))
                return;

            // Направление от камеры
            if (data.Move.magnitude > movementThreshold)
            {
                Quaternion camRot = Quaternion.Euler(0f, data.CameraRotationY, 0f);
                Vector3 camF = camRot * Vector3.forward;
                Vector3 camR = camRot * Vector3.right;
                camF.y = 0f;
                camR.y = 0f;

                _moveDir = (camF * data.Move.y + camR * data.Move.x).normalized;
                _targetAngle = Mathf.Atan2(data.Move.x, data.Move.y) * Mathf.Rad2Deg + data.CameraRotationY;
            }
            else
            {
                _moveDir = Vector3.zero;
            }

            CheckGrounded();
            CheckStep();
            CheckSlopeAndDirections();

            if (_isGrounded)
                _hasDashedInAir = false;

            if (!_stunTimer.ExpiredOrNotRunning(Runner))
            {
                //netDashAnimationFlag = true;
            }
            else if (!_dashTimer.ExpiredOrNotRunning(Runner))
            {
                Vector3 v = _rb.linearVelocity;
                Vector3 dashDir = _moveDir.sqrMagnitude > 0.01f ? _moveDir : transform.forward;
                _rb.linearVelocity = new Vector3(dashDir.x * dashForce, v.y, dashDir.z * dashForce);
            }
            else
            {
                Move();
                Rotate();
                TryJumpAndDash(data);
            }

            ApplyGravity();

            // Гасим паразитное кручение
            Vector3 ang = _rb.angularVelocity;
            ang.y *= 0.5f;
            _rb.angularVelocity = ang;
        }

        private void Move()
        {
            float speed = moveSpeed;
            if (!_isGrounded)
                speed *= airControl;

            Vector3 targetVel;

            if (_moveDir.sqrMagnitude > 0.001f)
            {
                // Движение вдоль поверхности (Nappin-style forward), а не только XZ
                Vector3 alongSurface = _forward.sqrMagnitude > 0.001f
                    ? _forward.normalized
                    : _moveDir;

                // На ровной земле forward уже горизонтальный; на склоне — вдоль склона
                if (!_isTouchingSlope && !_isTouchingStep)
                    alongSurface = _moveDir;

                // Если step — чуть больше «влезаем» на выступ за счёт forward из CheckSlope
                targetVel = alongSurface * speed;

                // Не вдавливаем в вертикальную стену (не step)
                if (!_isTouchingStep && IsBlockedByWall())
                {
                    Vector3 horizontal = targetVel;
                    horizontal.y = 0f;
                    // оставляем только составляющую не в стену — упрощённо режем скорость
                    targetVel = Vector3.ProjectOnPlane(targetVel, GetWallNormal());
                }
            }
            else
            {
                targetVel = Vector3.zero;
            }

            targetVel.y = _rb.linearVelocity.y;

            float damp = _moveDir.sqrMagnitude > 0.001f ? dampSpeedUp : dampSpeedDown;
            _rb.linearVelocity = Vector3.SmoothDamp(_rb.linearVelocity, targetVel, ref _smoothVel, damp);
        }

        private void Rotate()
        {
            if (_moveDir.sqrMagnitude < 0.1f)
                return;

            Vector3 horizontalVel = _rb.linearVelocity;
            horizontalVel.y = 0f;
            if (horizontalVel.sqrMagnitude < 0.35f)
                return;

            Quaternion targetRot = Quaternion.Euler(0f, _targetAngle, 0f);
            _rb.MoveRotation(Quaternion.RotateTowards(
                _rb.rotation,
                targetRot,
                rotationSpeed * Runner.DeltaTime));

            if (cameraTarget != null)
                cameraTarget.rotation = Quaternion.Euler(0f, _targetAngle, 0f);
        }

        private void TryJumpAndDash(NetworkInputData data)
        {
            if (!data.SpacePressed)
                return;

            // Прыжок с земли / пологого склона
            if (_isGrounded && (!_isTouchingSlope || _currentSurfaceAngle <= maxClimbableSlopeAngle))
            {
                Vector3 v = _rb.linearVelocity;
                v.y = jumpForce;
                _rb.linearVelocity = v;
                return;
            }

            // Dash в воздухе
            if (!_isGrounded && !_hasDashedInAir)
            {
                _hasDashedInAir = true;
                _dashTimer = TickTimer.CreateFromSeconds(Runner, dashDuration);

                Vector3 dir = _moveDir.sqrMagnitude > 0.01f ? _moveDir : transform.forward;
                dir.y = dashUpBoost;

                _rb.linearVelocity = Vector3.zero;
                _rb.AddForce(dir.normalized * dashForce, ForceMode.Impulse);
            }
        }

        // ================== NAPPIN-STYLE CHECKS ==================

        private void CheckGrounded()
        {
            Vector3 origin = groundCheck != null
                ? groundCheck.position
                : transform.position + Vector3.down * (_originalColliderHeight * 0.5f - 0.05f);

            _isGrounded = Physics.CheckSphere(origin, groundCheckRadius, groundMask);
        }

        private void CheckStep()
        {
            bool tmpStep = false;
            Vector3 bottomStepPos = transform.position
                - new Vector3(0f, _originalColliderHeight / 2f, 0f)
                + new Vector3(0f, 0.05f, 0f);

            Vector3 dir = _globalForward.sqrMagnitude > 0.01f ? _globalForward : transform.forward;
            dir.y = 0f;
            dir.Normalize();

            // 0°
            if (Physics.Raycast(bottomStepPos, dir, out RaycastHit low, stepCheckerThreshold, groundMask))
            {
                if (RoundValue(low.normal.y) == 0f &&
                    !Physics.Raycast(bottomStepPos + Vector3.up * maxStepHeight, dir, stepCheckerThreshold + 0.05f, groundMask))
                {
                    tmpStep = true;
                }
            }

            // +45°
            Vector3 dir45 = Quaternion.AngleAxis(45f, Vector3.up) * dir;
            if (Physics.Raycast(bottomStepPos, dir45, out RaycastHit low45, stepCheckerThreshold, groundMask))
            {
                if (RoundValue(low45.normal.y) == 0f &&
                    !Physics.Raycast(bottomStepPos + Vector3.up * maxStepHeight, dir45, stepCheckerThreshold + 0.05f, groundMask))
                {
                    tmpStep = true;
                }
            }

            // -45°
            Vector3 dirM45 = Quaternion.AngleAxis(-45f, Vector3.up) * dir;
            if (Physics.Raycast(bottomStepPos, dirM45, out RaycastHit lowM45, stepCheckerThreshold, groundMask))
            {
                if (RoundValue(lowM45.normal.y) == 0f &&
                    !Physics.Raycast(bottomStepPos + Vector3.up * maxStepHeight, dirM45, stepCheckerThreshold + 0.05f, groundMask))
                {
                    tmpStep = true;
                }
            }

            _isTouchingStep = tmpStep;
        }

        private void CheckSlopeAndDirections()
        {
            _prevGroundNormal = _groundNormal;

            float castDist = _originalColliderHeight / 2f + 0.5f;

            if (Physics.SphereCast(transform.position, slopeCheckerThreshold, Vector3.down,
                    out RaycastHit slopeHit, castDist, groundMask))
            {
                _groundNormal = slopeHit.normal;
                _currentSurfaceAngle = Vector3.Angle(Vector3.up, slopeHit.normal);

                if (slopeHit.normal.y >= 0.99f)
                {
                    // Ровный пол
                    _forward = Quaternion.Euler(0f, _targetAngle, 0f) * Vector3.forward;
                    _globalForward = _forward;

                    SetFriction(frictionAgainstFloor, true);
                    _currentLockOnSlope = lockOnSlope;
                    _isTouchingSlope = false;
                    _currentSurfaceAngle = 0f;
                }
                else
                {
                    // Движение вдоль склона в сторону targetAngle / moveDir
                    Vector3 desired = _moveDir.sqrMagnitude > 0.01f
                        ? _moveDir
                        : transform.forward;

                    Vector3 alongSlope = Vector3.ProjectOnPlane(desired, slopeHit.normal).normalized;

                    if (_currentSurfaceAngle <= maxClimbableSlopeAngle || _isTouchingStep)
                    {
                        _forward = alongSlope;
                        _globalForward = Vector3.ProjectOnPlane(desired, Vector3.up).normalized;

                        SetFriction(frictionAgainstFloor, true);
                        _currentLockOnSlope = _isTouchingStep || lockOnSlope;
                    }
                    else
                    {
                        // Слишком круто — скользим
                        _forward = alongSlope * 0.3f;
                        _globalForward = _forward;

                        SetFriction(0f, true);
                        _currentLockOnSlope = lockOnSlope;
                    }

                    _isTouchingSlope = true;
                }

                _down = Vector3.Project(Vector3.down, slopeHit.normal);
                _globalDown = Vector3.down;
            }
            else
            {
                _groundNormal = Vector3.up;
                _forward = _moveDir.sqrMagnitude > 0.01f ? _moveDir : transform.forward;
                _globalForward = _forward;
                _down = Vector3.down;
                _globalDown = Vector3.down;

                SetFriction(frictionAgainstFloor, true);
                _currentLockOnSlope = lockOnSlope;
                _isTouchingSlope = false;
                _currentSurfaceAngle = 0f;
            }
        }

        private void ApplyGravity()
        {
            // Падение быстрее
            if (_rb.linearVelocity.y < 0f && !_isGrounded)
                _coyoteJumpMultiplier = fallMultiplier;
            else
                _coyoteJumpMultiplier = 1f;

            Vector3 gravity;
            if (_currentLockOnSlope || _isTouchingStep)
                gravity = _down * gravityMultiplier * -Physics.gravity.y * _coyoteJumpMultiplier;
            else
                gravity = _globalDown * gravityMultiplier * -Physics.gravity.y * _coyoteJumpMultiplier;

            // Перегиб склона
            if (_isTouchingSlope && _prevGroundNormal != _groundNormal &&
                _groundNormal.y > 0.01f && _groundNormal.y < 0.99f)
            {
                gravity *= gravityMultiplierOnSlideChange;
            }

            // Крутой склон — сильнее тянем вниз
            if (_isTouchingSlope && _currentSurfaceAngle > maxClimbableSlopeAngle && !_isTouchingStep)
            {
                gravity = _globalDown * gravityMultiplierIfUnclimbableSlope * -Physics.gravity.y;
            }

            _rb.AddForce(gravity, ForceMode.Acceleration);
        }

        private void SetFriction(float friction, bool minimum)
        {
            if (_col == null || _col.material == null)
                return;

            _col.material.dynamicFriction = 0.6f * friction;
            _col.material.staticFriction = 0.6f * friction;
            _col.material.frictionCombine = minimum
                ? PhysicsMaterialCombine.Minimum
                : PhysicsMaterialCombine.Maximum;
        }

        private bool IsBlockedByWall()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 dir = _moveDir.sqrMagnitude > 0.01f ? _moveDir : transform.forward;
            dir.y = 0f;
            return Physics.Raycast(origin, dir, out RaycastHit hit, 0.55f, groundMask)
                   && hit.normal.y < 0.3f
                   && !_isTouchingStep;
        }

        private Vector3 GetWallNormal()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 dir = _moveDir.sqrMagnitude > 0.01f ? _moveDir : transform.forward;
            dir.y = 0f;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, 0.55f, groundMask))
                return hit.normal;
            return -dir;
        }

        private static float RoundValue(float value)
        {
            float unit = Mathf.Round(value);
            if (Mathf.Abs(value - unit) < 0.000001f)
                return unit;
            return value;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
        }
    }
}
