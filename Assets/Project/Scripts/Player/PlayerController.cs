using Fusion;
using UnityEngine;

namespace NonameGame
{
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed           = 10f;
        [SerializeField] private float rotationSpeed      = 540f;
        [SerializeField] private float movementThreshold  = 0.15f;
        [SerializeField] private float dampSpeedUp        = 0.06f;
        [SerializeField] private float dampSpeedDown      = 0.04f;
        [SerializeField] private float airControl         = 0.6f;

        [Header("Jump / Dash")]
        [SerializeField] private float jumpForce      = 9f;
        [SerializeField] private float dashForce      = 15f;
        [SerializeField] private float dashDuration   = 0.22f;
        [SerializeField] private float dashUpBoost    = 0.2f;
        [SerializeField] private float fallMultiplier = 2.8f;

        // ── COYOTE TIME ──────────────────────────────────────────────────
        // Позволяет прыгнуть через coyoteTime секунд после того, как игрок сошёл с края
        [Header("Coyote Time")]
        [SerializeField] private float coyoteTime = 0.12f;
        private float _coyoteTimer;

        [Header("Ground")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float     groundCheckRadius = 0.28f;
        [SerializeField] private LayerMask groundMask        = ~0;

        [Header("Step")]
        [SerializeField] private float stepCheckerThreshold = 0.65f;
        [SerializeField] private float maxStepHeight        = 0.45f;

        [Header("Slope")]
        [SerializeField] private float maxClimbableSlopeAngle          = 55f;
        [SerializeField] private float slopeCheckerThreshold           = 0.45f;
        [SerializeField] private float gravityMultiplier                = 2.2f;
        [SerializeField] private float gravityMultiplierOnSlideChange   = 2.5f;
        [SerializeField] private float gravityMultiplierIfUnclimbableSlope = 20f;
        // FIX: false = гравитация всегда прямо вниз (как у Nappin).
        // true = гравитация вдоль нормали склона — вызывает застревание.
        [SerializeField] private bool lockOnSlope = false;

        // ── SLOPE SPEED CURVE (port from Nappin) ────────────────────────
        // Компенсирует снижение скорости на склоне (чем круче склон — тем выше буст).
        [Header("Slope Speed Curve")]
        [SerializeField] private AnimationCurve speedMultiplierOnAngle   = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Range(0f, 1f)]
        [SerializeField] private float          slopeSpeedMultiplier     = 0.06f;

        [Header("Friction")]
        [SerializeField] private float frictionAgainstFloor = 0.95f;

        [Header("References")]
        [SerializeField] private Transform  cameraTarget;
        [SerializeField] private PlayerView _view;

        // ── privates ────────────────────────────────────────────────────
        private Rigidbody        _rb;
        private CapsuleCollider  _col;
        private float            _originalColliderHeight;

        private Vector3 _forward;
        private Vector3 _globalForward;
        private Vector3 _down;
        private Vector3 _globalDown;

        private Vector3 _groundNormal     = Vector3.up;
        private Vector3 _prevGroundNormal = Vector3.up;
        private float   _currentSurfaceAngle;
        private bool    _currentLockOnSlope;

        private bool _isGrounded;
        private bool _physGrounded;   // FIX: физический groundCheck без coyote
        private bool _isTouchingSlope;
        private bool _isTouchingStep;

        private Vector3 _moveDir;
        private float   _targetAngle;
        private Vector3 _smoothVel;

        private float _coyoteJumpMultiplier = 1f;

        // После прыжка/даша принудительно держим IsGrounded=false N тиков,
        // чтобы Animator успел запустить анимацию до перехода в Grounded.
        private int _airForceTicks = 0;
        private const int AirForceTicksAfterJump = 10;

        private NetworkBool  _hasDashedInAir { get; set; }
        private TickTimer    _dashTimer      { get; set; }
        public  TickTimer    _stunTimer      { get; set; }
        private Vector3      _dashDir        { get; set; }

        public bool IsGrounded => _isGrounded && _airForceTicks <= 0;

        // ----------------------------------------------------------------

        public void SetStunTimer(float duration)
        {
            if (!HasStateAuthority) return;
            _stunTimer = TickTimer.CreateFromSeconds(Runner, duration);
        }

        public override void Spawned()
        {
            _rb  = GetComponent<Rigidbody>();
            _col = GetComponent<CapsuleCollider>();
            _originalColliderHeight = _col.height;

            _rb.constraints  = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            SetFriction(frictionAgainstFloor, true);
            _currentLockOnSlope = lockOnSlope;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (!GetInput(out NetworkInputData data)) return;
            if (InGameManager.Instance == null ||
                InGameManager.Instance.gameState == InGameManager.GameState.ShowResults) return;

            // Направление от камеры
            if (data.Move.magnitude > movementThreshold)
            {
                Quaternion camRot = Quaternion.Euler(0f, data.CameraRotationY, 0f);
                Vector3 camF = camRot * Vector3.forward; camF.y = 0f;
                Vector3 camR = camRot * Vector3.right;   camR.y = 0f;
                _moveDir     = (camF * data.Move.y + camR * data.Move.x).normalized;
                _targetAngle = Mathf.Atan2(data.Move.x, data.Move.y) * Mathf.Rad2Deg + data.CameraRotationY;
            }
            else
            {
                _moveDir = Vector3.zero;
            }

            CheckGrounded();
            CheckStep();
            CheckSlopeAndDirections();

            if (_airForceTicks > 0) _airForceTicks--;
            if (_isGrounded) _hasDashedInAir = false;

            if (!_stunTimer.ExpiredOrNotRunning(Runner))
            {
                // стан — ничего не делаем
            }
            else if (!_dashTimer.ExpiredOrNotRunning(Runner))
            {
                Vector3 v = _rb.linearVelocity;
                _rb.linearVelocity = new Vector3(_dashDir.x * dashForce, v.y, _dashDir.z * dashForce);
            }
            else
            {
                Move();
                Rotate();
                TryJumpAndDash(data);
            }

            ApplyGravity();

            Vector3 ang = _rb.angularVelocity;
            ang.y *= 0.5f;
            _rb.angularVelocity = ang;
        }

        // ================================================================
        //  MOVE
        // ================================================================
        private void Move()
        {
            float speed = moveSpeed;
            if (!_isGrounded) speed *= airControl;

            Vector3 targetVel;

            if (_moveDir.sqrMagnitude > 0.001f)
            {
                // FIX: на подъёмном склоне и без прыжка — используем _forward целико
                // (c Y-компонентой вдоль склона, как у Nappin), а не заменяем Y текущей скоростью.
                bool onClimbableSlope = _isTouchingSlope
                                     && !_isTouchingStep
                                     && _currentSurfaceAngle <= maxClimbableSlopeAngle;

                if (onClimbableSlope && _rb.linearVelocity.y <= 0.1f)
                {
                    // Склон: _forward содержит XZ от moveDir + Y вдоль поверхности
                    // Не перезаписываем Y — пускаем движение вдоль склона
                    targetVel = _forward * speed;
                }
                else
                {
                    // Плоско, ступенька, воздух: сохраняем Y для прыжка/падения
                    Vector3 horizontal = _isTouchingSlope ? _globalForward : _moveDir;
                    targetVel   = horizontal * speed;
                    targetVel.y = _rb.linearVelocity.y;
                }

                if (!_isTouchingStep && IsBlockedByWall())
                    targetVel = Vector3.ProjectOnPlane(targetVel, GetWallNormal());
            }
            else
            {
                targetVel   = Vector3.zero;
                targetVel.y = _rb.linearVelocity.y;
            }

            float damp = _moveDir.sqrMagnitude > 0.001f ? dampSpeedUp : dampSpeedDown;
            _rb.linearVelocity = Vector3.SmoothDamp(_rb.linearVelocity, targetVel, ref _smoothVel, damp);
        }

        private void Rotate()
        {
            if (_moveDir.sqrMagnitude < 0.1f) return;
            Vector3 horizontalVel = _rb.linearVelocity; horizontalVel.y = 0f;
            if (horizontalVel.sqrMagnitude < 0.35f) return;

            Quaternion targetRot = Quaternion.Euler(0f, _targetAngle, 0f);
            _rb.MoveRotation(Quaternion.RotateTowards(_rb.rotation, targetRot, rotationSpeed * Runner.DeltaTime));
            if (cameraTarget != null)
                cameraTarget.rotation = Quaternion.Euler(0f, _targetAngle, 0f);
        }

        private void TryJumpAndDash(NetworkInputData data)
        {
            if (!data.SpacePressed) return;

            // === TEMP DEBUG — убрать после диагностики ===
            /*Debug.Log($"[JUMP] physGrounded={_physGrounded} coyote={_coyoteTimer:F3} " +
                      $"isGrounded={_isGrounded} slope={_isTouchingSlope} " +
                      $"slopeAngle={_currentSurfaceAngle:F1} velY={_rb.linearVelocity.y:F2} " +
                      $"stunOk={_stunTimer.ExpiredOrNotRunning(Runner)} " +
                      $"dashOk={_dashTimer.ExpiredOrNotRunning(Runner)}");*/
            // ====================================================

            // FIX: SphereCast с большим радиусом ловит вертикальное ребро
            // на стыке пола и склона → _currentSurfaceAngle ≈ 80–90° →
            // прыжок блокировался даже стоя на ровном полу.
            //
            // Решение: если CheckSphere говорит «стоим на земле» (_physGrounded)
            // → прыгаем ВСЕГДА, без учёта SphereCast slope-детектора.
            // Coyote-прыжок: те же права в течение coyoteTime после схода с края.
            // Slope-детектор используется только при блокировке на крутом склоне
            // когда _physGrounded = false (игрок уже в воздухе над склоном).

            if (_physGrounded || _coyoteTimer > 0f)
            {
                _smoothVel.y   = 0f;
                _airForceTicks = AirForceTicksAfterJump;
                Vector3 v = _rb.linearVelocity;
                v.y = jumpForce;
                _rb.linearVelocity = v;
                _coyoteTimer = 0f;
                if (_view != null) _view.PlayJump();
                return;
            }

            if (!_isGrounded && !_hasDashedInAir)
            {
                _hasDashedInAir = true;
                _airForceTicks  = AirForceTicksAfterJump;
                _dashTimer = TickTimer.CreateFromSeconds(Runner, dashDuration);
                Vector3 dir = _moveDir.sqrMagnitude > 0.01f ? _moveDir : transform.forward;
                dir.y = dashUpBoost;
                _rb.linearVelocity = Vector3.zero;
                _rb.AddForce(dir.normalized * dashForce, ForceMode.Impulse);
                _view.PlayDash();
                _dashDir = dir;
            }
        }

        // ================================================================
        //  CHECKS
        // ================================================================
        private void CheckGrounded()
        {
            Vector3 origin = groundCheck != null
                ? groundCheck.position
                : transform.position + Vector3.down * (_originalColliderHeight * 0.5f - 0.05f);

            _physGrounded = Physics.CheckSphere(origin, groundCheckRadius, groundMask);

            // FIX: coyote time — игрок считается на земле ещё coyoteTime секунд
            // после схода с края. Убрали условие velocity.y <= 0 — оно давало
            // ложный false когда физика слегка подбрасывала игрока у края.
            if (_physGrounded)
                _coyoteTimer = coyoteTime;
            else
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - Runner.DeltaTime);

            _isGrounded = _physGrounded || _coyoteTimer > 0f;
        }

        private void CheckStep()
        {
            bool tmpStep = false;
            Vector3 bottomStepPos = transform.position
                - new Vector3(0f, _originalColliderHeight / 2f, 0f)
                + new Vector3(0f, 0.05f, 0f);

            Vector3 dir = _globalForward.sqrMagnitude > 0.01f ? _globalForward : transform.forward;
            dir.y = 0f; dir.Normalize();

            void CheckDir(Vector3 d)
            {
                if (Physics.Raycast(bottomStepPos, d, out RaycastHit low, stepCheckerThreshold, groundMask))
                    if (RoundValue(low.normal.y) == 0f &&
                        !Physics.Raycast(bottomStepPos + Vector3.up * maxStepHeight, d, stepCheckerThreshold + 0.05f, groundMask))
                        tmpStep = true;
            }

            CheckDir(dir);
            CheckDir(Quaternion.AngleAxis( 45f, Vector3.up) * dir);
            CheckDir(Quaternion.AngleAxis(-45f, Vector3.up) * dir);

            _isTouchingStep = tmpStep;
        }

        private void CheckSlopeAndDirections()
        {
            _prevGroundNormal = _groundNormal;

            float castDist = _originalColliderHeight / 2f + 0.5f;
            if (!Physics.SphereCast(transform.position, slopeCheckerThreshold, Vector3.down,
                    out RaycastHit slopeHit, castDist, groundMask))
            {
                // Нет попадания — воздух
                _groundNormal     = Vector3.up;
                _forward          = _moveDir.sqrMagnitude > 0.01f ? _moveDir : transform.forward;
                _globalForward    = _forward;
                _down             = Vector3.down;
                _globalDown       = Vector3.down;
                SetFriction(frictionAgainstFloor, true);
                _currentLockOnSlope  = lockOnSlope;
                _isTouchingSlope     = false;
                _currentSurfaceAngle = 0f;
                return;
            }

            _groundNormal = slopeHit.normal;

            if (slopeHit.normal.y >= 0.99f)
            {
                // Ровный пол
                _forward = Quaternion.Euler(0f, _targetAngle, 0f) * Vector3.forward;
                _globalForward = _forward;
                SetFriction(frictionAgainstFloor, true);
                _currentLockOnSlope  = lockOnSlope;
                _isTouchingSlope     = false;
                _currentSurfaceAngle = 0f;
            }
            else
            {
                // Склон
                _currentSurfaceAngle = Vector3.Angle(Vector3.up, slopeHit.normal);

                // FIX: Nappin-style forward — XZ берём из moveDir (горизонталь),
                // Y — из ProjectOnPlane (Y-составляющая вдоль поверхности).
                // Обеспечивает одинаковую скорость вверх и вниз по склону.
                Vector3 desiredGF  = _moveDir.sqrMagnitude > 0.01f ? _moveDir : transform.forward;
                desiredGF.y = 0f;
                desiredGF.Normalize();

                Vector3 slopeAligned = Vector3.ProjectOnPlane(desiredGF, slopeHit.normal).normalized;

                // Кривая компенсации скорост�� (как у Nappin)
                float curveBoost = speedMultiplierOnAngle.Evaluate(_currentSurfaceAngle / 90f)
                                   * slopeSpeedMultiplier + 1f;

                if (_currentSurfaceAngle <= maxClimbableSlopeAngle || _isTouchingStep)
                {
                    // _forward: XZ из moveDir, Y из проекции на склон
                    _forward       = new Vector3(desiredGF.x, slopeAligned.y, desiredGF.z) * curveBoost;
                    _globalForward = desiredGF * curveBoost;
                    SetFriction(frictionAgainstFloor, true);
                    _currentLockOnSlope = _isTouchingStep || lockOnSlope;
                }
                else
                {
                    // Слишком крутой склон — скользим
                    _forward       = slopeAligned * 0.3f;
                    _globalForward = desiredGF   * 0.3f;
                    SetFriction(0f, true);
                    _currentLockOnSlope = lockOnSlope;
                }

                _isTouchingSlope = true;
            }

            // FIX: здесь _down используется только если lockOnSlope = true.
            // При lockOnSlope = false (default) гравитация всегда через _globalDown (чисто вниз).
            _down       = Vector3.Project(Vector3.down, slopeHit.normal);
            _globalDown = Vector3.down;
        }

        // ================================================================
        //  GRAVITY
        // ================================================================
        private void ApplyGravity()
        {
            if (_rb.linearVelocity.y < 0f && !_isGrounded)
                _coyoteJumpMultiplier = fallMultiplier;
            else
                _coyoteJumpMultiplier = 1f;

            Vector3 gravity;
            if (_currentLockOnSlope || _isTouchingStep)
                gravity = _down       * gravityMultiplier * -Physics.gravity.y * _coyoteJumpMultiplier;
            else
                gravity = _globalDown * gravityMultiplier * -Physics.gravity.y * _coyoteJumpMultiplier;

            // Перегиб склона
            if (_isTouchingSlope && _prevGroundNormal != _groundNormal &&
                _groundNormal.y > 0.01f && _groundNormal.y < 0.99f &&
                _rb.linearVelocity.y <= 0f)  // FIX: только при движении вниз
            {
                gravity *= gravityMultiplierOnSlideChange;
            }

            // Крутой склон — супергравитацию применяем ТОЛЬКО если игрок физически НЕ на земле.
            // Если _physGrounded=true, значит CheckSphere видит плоский пол, а SphereCast
            // просто зацепил край соседнего склона — не давим 196 м/с² вниз.
            if (!_physGrounded && _isTouchingSlope && _currentSurfaceAngle > maxClimbableSlopeAngle && !_isTouchingStep)
                gravity = _globalDown * gravityMultiplierIfUnclimbableSlope * -Physics.gravity.y;

            _rb.AddForce(gravity, ForceMode.Acceleration);
        }

        // ================================================================
        //  HELPERS
        // ================================================================
        private void SetFriction(float friction, bool minimum)
        {
            if (_col == null || _col.material == null) return;
            _col.material.dynamicFriction = 0.6f * friction;
            _col.material.staticFriction  = 0.6f * friction;
            _col.material.frictionCombine = minimum
                ? PhysicsMaterialCombine.Minimum
                : PhysicsMaterialCombine.Maximum;
        }

        private bool IsBlockedByWall()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 dir    = _moveDir.sqrMagnitude > 0.01f ? _moveDir : transform.forward;
            dir.y = 0f;
            return Physics.Raycast(origin, dir, out RaycastHit hit, 0.55f, groundMask)
                   && hit.normal.y < 0.3f && !_isTouchingStep;
        }

        private Vector3 GetWallNormal()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 dir    = _moveDir.sqrMagnitude > 0.01f ? _moveDir : transform.forward;
            dir.y = 0f;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, 0.55f, groundMask))
                return hit.normal;
            return -dir;
        }

        private static float RoundValue(float value)
        {
            float unit = Mathf.Round(value);
            return Mathf.Abs(value - unit) < 0.000001f ? unit : value;
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
