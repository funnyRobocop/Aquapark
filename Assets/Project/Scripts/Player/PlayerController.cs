using Fusion;
using UnityEngine;

namespace NonameGame
{
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float rotationSpeed = 12f;

        [Header("Ground Check")]
        [SerializeField] private float groundCheckRadius = 0.25f;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private Transform groundCheck; // пустой объект у ног

        private Rigidbody _rb;
        private NetworkTransform _nt;

        public override void Spawned()
        {
            _rb = GetComponent<Rigidbody>();
            _nt = GetComponent<NetworkTransform>();

            // На всякий случай
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public override void FixedUpdateNetwork()
        {
            // В Shared Mode логику движения делаем только тот, у кого State Authority
            if (!HasStateAuthority)
                return;

            if (GetInput(out NetworkInputData data) == false)
                return;

            // ===== Движение относительно камеры =====
            Vector3 camForward = Camera.main != null ? Camera.main.transform.forward : transform.forward;
            Vector3 camRight = Camera.main != null ? Camera.main.transform.right : transform.right;

            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = (camForward * data.Move.y + camRight * data.Move.x).normalized;

            Vector3 velocity = _rb.linearVelocity;
            velocity.x = moveDir.x * moveSpeed;
            velocity.z = moveDir.z * moveSpeed;
            // Y не трогаем — гравитация и прыжок сами

            _rb.linearVelocity = velocity;

            // ===== Поворот модели =====
            // ===== Поворот модели =====
            if (moveDir.sqrMagnitude > 0.05f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                // Крутим через Rigidbody, а не через transform напрямую
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, Runner.DeltaTime * rotationSpeed));
            }

            // ===== Прыжок =====
            if (data.Buttons.IsSet(NetworkInputData.BUTTON_JUMP) && IsGrounded())
            {
                velocity = _rb.linearVelocity;
                velocity.y = jumpForce;
                _rb.linearVelocity = velocity;
            }
        }

        private bool IsGrounded()
        {
            Vector3 pos = groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.9f;
            return Physics.CheckSphere(pos, groundCheckRadius, groundMask);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 pos = groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.9f;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pos, groundCheckRadius);
        }
    }
}
