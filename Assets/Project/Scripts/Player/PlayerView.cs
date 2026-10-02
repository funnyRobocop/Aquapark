using System.Collections.Generic;
using Fusion;
using UnityEngine;
using VContainer;

namespace NonameGame
{
    public class PlayerView : NetworkBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Animator animator;
        [SerializeField] private NetworkMecanimAnimator networkAnimator;
        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerGrab grab;
        [SerializeField] private PlayerWeapon weapon;
        [SerializeField] private PlayerPipeSlide pipeSlide;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Dictionary<PlayerSkinType, Transform> holdPointsDict;

        [Header("Tuning")]
        [SerializeField] private float speedDamp = 0.1f;
        [SerializeField] private float runSpeedThreshold = 0.5f;
        [SerializeField] private float fallYThreshold = -1.5f;

        private bool _wasHolding;
        private bool _dashTriggered;
        private bool _pushTriggered;

        [Inject] private IPlayerSkinLoader _playerSkinLoader;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsHoldingHash = Animator.StringToHash("IsHolding");
        private static readonly int IsArmedHash = Animator.StringToHash("IsArmed");
        private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");
        private static readonly int IsFlyingHash = Animator.StringToHash("IsFlying");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int DashHash = Animator.StringToHash("Dash");
        private static readonly int PushHash = Animator.StringToHash("Push");
        private static readonly int ThrowHash = Animator.StringToHash("Throw");
        private static readonly int ShootHash = Animator.StringToHash("Shoot");

        public PlayerSkinType SkinType { get; set; }

        public override void Spawned()
        {
            if (controller == null)
                controller = GetComponent<PlayerController>();
            if (grab == null)
                grab = GetComponent<PlayerGrab>();
            if (weapon == null)
                weapon = GetComponent<PlayerWeapon>();
            if (pipeSlide == null)
                pipeSlide = GetComponent<PlayerPipeSlide>();
            if (rb == null)
                rb = GetComponent<Rigidbody>();
        }

        public void InitSkin()
        {
            SkinType = (_playerSkinLoader as PlayerSkinLoader).TestSkinType;
            var skinPrefab = _playerSkinLoader.GetPlayerSkinPrefab((_playerSkinLoader as PlayerSkinLoader).TestSkinType);
            var skin = Instantiate(skinPrefab, transform.GetChild(0));
            var skinAnimator = skin.GetComponent<Animator>();
            animator.avatar = skinAnimator.avatar;
            skinAnimator.enabled = false;

            if (holdPointsDict.ContainsKey(SkinType))
                grab.SetHoldPoint(holdPointsDict[SkinType]);
        }

        /*public override void Render()
        {
            if (!HasStateAuthority || animator == null || rb == null)
                return;

            Vector3 horizontal = rb.linearVelocity;
            horizontal.y = 0f;
            float speedNorm = Mathf.Clamp01(horizontal.magnitude / Mathf.Max(runSpeedThreshold, 0.01f));
            animator.SetFloat(SpeedHash, speedNorm, speedDamp, Time.deltaTime);
        }*/

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || animator == null)
                return;

            bool flying = pipeSlide != null && pipeSlide.IsSliding;
            bool grounded = !flying && controller != null && controller.IsGrounded;
            bool holding = grab != null && grab.IsHolding;
            bool armed = weapon != null && weapon.IsArmed;

            float vy = rb != null ? rb.linearVelocity.y : 0f;
            bool falling = !flying && !grounded && vy < fallYThreshold;

            float speedNorm = 0f;
            if (!flying && GetInput(out NetworkInputData data) && data.Move.sqrMagnitude > 0.01f && grounded)
                speedNorm = 1f;

            animator.SetFloat(SpeedHash, speedNorm);
            animator.SetBool(IsGroundedHash, grounded);
            animator.SetBool(IsHoldingHash, holding);
            animator.SetBool(IsArmedHash, armed);
            animator.SetBool(IsFallingHash, falling);
            animator.SetBool(IsFlyingHash, flying);
        }

        public void PlayJump()
        {
            if (!HasStateAuthority) return;
            SetTrigger(JumpHash);
        }

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

        public void PlayShoot()
        {
            if (!HasStateAuthority) return;
            SetTrigger(ShootHash);
        }

        /// <summary>
        /// Полёт в трубе. Вызывается на всех клиентах из PlayerPipeSlide (после RPC).
        /// </summary>
        public void SetFlying(bool value)
        {
            SetBool(IsFlyingHash, value);
            if (value)
            {
                SetBool(IsFallingHash, false);
                SetBool(IsGroundedHash, false);
            }
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
            if (networkAnimator != null)
                networkAnimator.SetTrigger(hash);
            else
                animator.SetTrigger(hash);
        }
    }
}