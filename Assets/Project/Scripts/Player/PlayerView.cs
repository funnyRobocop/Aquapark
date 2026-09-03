using Fusion;
using UnityEngine;


namespace NonameGame
{
    public class PlayerView : NetworkBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Animator animator;
        [SerializeField] private NetworkMecanimAnimator networkAnimator;
        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerGrab grab;
        [SerializeField] private Rigidbody rb;

        [Header("Tuning")]
        [SerializeField] private float speedDamp = 0.1f;
        [SerializeField] private float runSpeedThreshold = 0.5f;
        [SerializeField] private float fallYThreshold = -1.5f;

        // Чтобы не спамить trigger каждый тик
        private bool _wasGrounded = true;
        private bool _wasHolding;
        private bool _dashTriggered;
        private bool _pushTriggered;

        // Хеши — быстрее строк
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsHoldingHash = Animator.StringToHash("IsHolding");
        private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int DashHash = Animator.StringToHash("Dash");
        private static readonly int PushHash = Animator.StringToHash("Push");
        private static readonly int ThrowHash = Animator.StringToHash("Throw");

        public override void Spawned()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (networkAnimator == null)
                networkAnimator = GetComponentInChildren<NetworkMecanimAnimator>();
            if (controller == null)
                controller = GetComponentInParent<PlayerController>();
            if (grab == null)
                grab = GetComponentInParent<PlayerGrab>();
            if (rb == null)
                rb = GetComponentInParent<Rigidbody>();
        }

        public override void Render()
        {
            // Можно обновлять и на proxies для гладкости Speed по velocity,
            // но triggers/сеть — лучше с authority через NetworkMecanimAnimator
            if (animator == null || rb == null)
                return;

            // Speed — локально по velocity (на remote тоже ок, меньше лага)
            Vector3 horizontal = rb.linearVelocity;
            horizontal.y = 0f;
            float speedNorm = Mathf.Clamp01(horizontal.magnitude / Mathf.Max(runSpeedThreshold, 0.01f));
            animator.SetFloat(SpeedHash, speedNorm, speedDamp, Time.deltaTime);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || animator == null || controller == null)
                return;

            bool grounded = controller.IsGrounded;
            bool holding = grab != null && grab.IsHolding;
            float vy = rb != null ? rb.linearVelocity.y : 0f;
            bool falling = !grounded && vy < fallYThreshold;

            SetBool(IsGroundedHash, grounded);
            SetBool(IsHoldingHash, holding);
            SetBool(IsFallingHash, falling);

            // Прыжок: только что оторвался от земли вверх
            if (_wasGrounded && !grounded && vy > 0.5f)
                SetTrigger(JumpHash);

            _wasGrounded = grounded;
            _wasHolding = holding;
        }

        // ===== Вызывать из геймплейных скриптов =====

        public void PlayDash()
        {
            if (!HasStateAuthority) return;
            SetTrigger(DashHash);
        }

        public void PlayPush()
        {
            if (!HasStateAuthority) return;
            SetTrigger(PushHash);
        }

        public void PlayThrow()
        {
            if (!HasStateAuthority) return;
            SetTrigger(ThrowHash);
        }

        private void SetBool(int hash, bool value)
        {
            if (networkAnimator != null)
                networkAnimator.Animator.SetBool(hash, value);
            else
                animator.SetBool(hash, value);
        }

        private void SetTrigger(int hash)
        {
            // NetworkMecanimAnimator.SetTrigger — правильный сетевой путь
            if (networkAnimator != null)
                networkAnimator.SetTrigger(hash);
            else
                animator.SetTrigger(hash);
        }
    }
}
