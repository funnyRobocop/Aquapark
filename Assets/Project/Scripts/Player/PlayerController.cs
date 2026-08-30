using Fusion;
using UnityEngine;

namespace NonameGame
{
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float rotationSpeed = 720f; // градусов в секунду
        [SerializeField] private float jumpForce = 9f;
        [SerializeField] private float movementThreshold = 0.01f;
        [SerializeField] private float acceleration = 0.12f; // для SmoothDamp
        [SerializeField] private float deceleration = 0.08f;

        [Header("Dash")]
        [SerializeField] private float dashForce = 15f;
        [SerializeField] private float dashDuration = 0.22f;
        [SerializeField] private float dashUpBoost = 0.2f;

        [Header("Ground & Slope")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.28f;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float slopeLimit = 55f;
        [SerializeField] private float slopeCheckDistance = 1.2f;

        [Header("Extra Gravity")]
        [SerializeField] private float fallMultiplier = 1.7f;

        [Header("References")]
        [SerializeField] private Transform cameraTarget; // визуал / точка для камеры

        private Rigidbody _rb;
        private CapsuleCollider _col;
        private Vector3 _smoothVel;
        private Vector3 _moveDir;
        private float _targetAngle;
        private bool _isGrounded;
        private Vector3 _groundNormal = Vector3.up;

        [Networked] private NetworkBool _hasDashedInAir { get; set; }
        [Networked] private TickTimer _dashTimer { get; set; }

        public bool IsGrounded => _isGrounded;

        public override void Spawned()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<CapsuleCollider>();

            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

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

            CheckGround();

            if (_isGrounded)
                _hasDashedInAir = false;

            // Направление от камеры
            if (data.Move.magnitude > movementThreshold)
            {
                Quaternion camRot = Quaternion.Euler(0f, data.CameraRotationY, 0f);
                Vector3 camForward = camRot * Vector3.forward;
                Vector3 camRight = camRot * Vector3.right;
                camForward.y = 0f;
                camRight.y = 0f;

                _moveDir = (camForward * data.Move.y + camRight * data.Move.x).normalized;
                _targetAngle = Mathf.Atan2(data.Move.x, data.Move.y) * Mathf.Rad2Deg + data.CameraRotationY;
            }
            else
            {
                _moveDir = Vector3.zero;
            }

            // Во время рывка — фиксируем горизонтальную скорость
            if (!_dashTimer.ExpiredOrNotRunning(Runner))
            {
                Vector3 v = _rb.linearVelocity;
                Vector3 dashDir = _moveDir != Vector3.zero ? _moveDir : transform.forward;
                _rb.linearVelocity = new Vector3(dashDir.x * dashForce, v.y, dashDir.z * dashForce);
            }
            else
            {
                Move(data);
                Rotate();
                TryJumpAndDash(data);
            }

            ApplyExtraGravity();
        }

        private void Move(NetworkInputData data)
        {
            Vector3 targetVel;

            if (_moveDir.sqrMagnitude > 0.001f)
            {
                targetVel = _moveDir * moveSpeed;

                // Проекция на склон, чтобы не влипать
                if (!_isGrounded || Vector3.Angle(Vector3.up, _groundNormal) > 1f)
                {
                    targetVel = Vector3.ProjectOnPlane(targetVel, _groundNormal).normalized * moveSpeed;
                }
            }
            else
            {
                targetVel = Vector3.zero;
            }

            // Сохраняем текущую вертикальную скорость
            targetVel.y = _rb.linearVelocity.y;

            float smoothTime = _moveDir.sqrMagnitude > 0.001f ? acceleration : deceleration;
            _rb.linearVelocity = Vector3.SmoothDamp(_rb.linearVelocity, targetVel, ref _smoothVel, smoothTime);
        }

        private void Rotate()
        {
            if (_moveDir.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRot = Quaternion.Euler(0f, _targetAngle, 0f);
            _rb.MoveRotation(Quaternion.RotateTowards(
                _rb.rotation,
                targetRot,
                rotationSpeed * Runner.DeltaTime));

            // Визуал (если отдельный)
            if (cameraTarget != null)
                cameraTarget.rotation = Quaternion.Euler(0f, _targetAngle, 0f);
        }

        private void TryJumpAndDash(NetworkInputData data)
        {
            if (!data.SpacePressed)
                return;

            // Прыжок с земли
            if (_isGrounded)
            {
                Vector3 v = _rb.linearVelocity;
                v.y = jumpForce;
                _rb.linearVelocity = v;
                return;
            }

            // Рывок в воздухе (один раз)
            if (!_hasDashedInAir)
            {
                _hasDashedInAir = true;
                _dashTimer = TickTimer.CreateFromSeconds(Runner, dashDuration);

                Vector3 dir = _moveDir != Vector3.zero ? _moveDir : transform.forward;
                dir.y = dashUpBoost;

                _rb.linearVelocity = Vector3.zero;
                _rb.AddForce(dir.normalized * dashForce, ForceMode.Impulse);
            }
        }

        private void CheckGround()
        {
            Vector3 origin = groundCheck != null
                ? groundCheck.position
                : transform.position + Vector3.down * (_col.height * 0.5f - 0.05f);

            _isGrounded = Physics.CheckSphere(origin, groundCheckRadius, groundMask);

            // Нормаль поверхности
            _groundNormal = Vector3.up;
            if (Physics.SphereCast(transform.position, 0.35f, Vector3.down, out RaycastHit hit,
                    slopeCheckDistance, groundMask))
            {
                float angle = Vector3.Angle(Vector3.up, hit.normal);
                if (angle <= slopeLimit)
                    _groundNormal = hit.normal;
            }
        }

        private void ApplyExtraGravity()
        {
            if (_rb.linearVelocity.y < 0f && !_isGrounded)
            {
                _rb.AddForce(Physics.gravity * (fallMultiplier - 1f), ForceMode.Acceleration);
            }
        }

        private void OnDrawGizmosSelecЪted()
        {
            if (groundCheck == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
