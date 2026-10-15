using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;

namespace NonameGame
{
    // ─────────────────────────────────────────────────────────────────────────
    // Анимационная архитектура для Fusion 2 Shared Mode
    //
    // Проблема NetworkMecanimAnimator + триггеры:
    //   Триггер сбрасывается Unity сразу же после обработки. К моменту
    //   передачи snapshot он уже = 0, другие клиенты его не видят.
    //   Также FixedUpdateNetwork — симуляционный тик, а не кадр рендера;
    //   если гонять Animator.SetBool там, анимации дёргаются при ресимуляции.
    //
    // Решение:
    //   1. Непрерывные состояния (Speed, IsGrounded, …) — [Networked] bool/float,
    //      читаем и передаём в Animator только в Render().
    //   2. Одноразовые события (Jump, Dash, …) — [Networked] int-счётчик.
    //      Owner инкрементирует его; на всех клиентах Render() замечает
    //      изменение и вызывает animator.SetTrigger ровно один раз.
    //   3. NetworkMecanimAnimator больше не нужен для синхронизации.
    //      Его можно оставить на объекте (не ломает), но Sync Settings
    //      можно выставить в Nothing — мы сами управляем параметрами.
    // ─────────────────────────────────────────────────────────────────────────
    public class PlayerView : NetworkBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerGrab grab;
        [SerializeField] private PlayerWeapon weapon;
        [SerializeField] private PlayerPipeSlide pipeSlide;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Dictionary<PlayerSkin, Transform> holdPointsDict;
        [SerializeField] private Transform stunVFXPoint;

        [Header("Tuning")]
        [SerializeField] private float speedDamp = 0.1f;
        [SerializeField] private float runSpeedThreshold = 0.5f;
        [SerializeField] private float fallYThreshold = -1.5f;

        [Inject] private VFXManager _vfxManager;
        [Inject] private IPlayerSkinLoader _playerSkinLoader;

        // ── Networked: непрерывные состояния ──────────────────────────────────
        [Networked] private NetworkBool NetGrounded  { get; set; }
        [Networked] private NetworkBool NetHolding   { get; set; }
        [Networked] private NetworkBool NetArmed     { get; set; }
        [Networked] private NetworkBool NetFalling   { get; set; }
        [Networked] private NetworkBool NetFlying    { get; set; }
        [Networked] private float       NetSpeed     { get; set; }

        // ── Networked: одноразовые события (счётчики) ─────────────────────────
        // Render() сравнивает с локальной копией; при изменении → SetTrigger
        [Networked] private int NetJump  { get; set; }
        [Networked] private int NetDash  { get; set; }
        [Networked] private int NetPush  { get; set; }
        [Networked] private int NetThrow { get; set; }
        [Networked] private int NetShoot { get; set; }

        // ── Networked: скин ───────────────────────────────────────────────────
        [Networked, OnChangedRender(nameof(OnSkinChanged))]
        public int SkinId { get; set; }

        // ── Локальные зеркала счётчиков для Render() ─────────────────────────
        private int _prevJump, _prevDash, _prevPush, _prevThrow, _prevShoot;

        // ── Хэши параметров Animator ──────────────────────────────────────────
        private static readonly int SpeedHash      = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsHoldingHash  = Animator.StringToHash("IsHolding");
        private static readonly int IsArmedHash    = Animator.StringToHash("IsArmed");
        private static readonly int IsFallingHash  = Animator.StringToHash("IsFalling");
        private static readonly int IsFlyingHash   = Animator.StringToHash("IsFlying");
        private static readonly int JumpHash       = Animator.StringToHash("Jump");
        private static readonly int DashHash       = Animator.StringToHash("Dash");
        private static readonly int PushHash       = Animator.StringToHash("Push");
        private static readonly int ThrowHash      = Animator.StringToHash("Throw");
        private static readonly int ShootHash      = Animator.StringToHash("Shoot");

        // ─────────────────────────────────────────────────────────────────────
        public void InitializeBeforeSpawn(int requestedId)
        {
            SkinId = requestedId;
        }

        public override void Spawned()
        {
            if (controller == null) controller   = GetComponent<PlayerController>();
            if (grab      == null) grab          = GetComponent<PlayerGrab>();
            if (weapon    == null) weapon        = GetComponent<PlayerWeapon>();
            if (pipeSlide == null) pipeSlide     = GetComponent<PlayerPipeSlide>();
            if (rb        == null) rb            = GetComponent<Rigidbody>();

            // Синхронизируем локальные зеркала со snapshot (для поздних клиентов)
            _prevJump  = NetJump;
            _prevDash  = NetDash;
            _prevPush  = NetPush;
            _prevThrow = NetThrow;
            _prevShoot = NetShoot;

            InitSkin();
        }

        // ── Скин ─────────────────────────────────────────────────────────────
        private void OnSkinChanged() => InitSkin();

        public void InitSkin()
        {
            var skinType = (PlayerSkin)SkinId;
            var skinData = _playerSkinLoader.PlayerSkinAll[skinType];
            SpawnPlayerMesh(skinData, transform.GetChild(0));

            if (holdPointsDict.ContainsKey(skinType))
                grab.SetHoldPoint(holdPointsDict[skinType]);
        }

        public void SpawnPlayerMesh(PlayerSkinData activeSkinData, Transform spawnPoint)
        {
            activeSkinData.PrefabReference.InstantiateAsync(spawnPoint).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject skin = handle.Result;
                    var skinAnimator = skin.GetComponent<Animator>();
                    animator.avatar = skinAnimator.avatar;
                    skinAnimator.enabled = false;
                }
            };
        }

        // ── Owner: пишем сетевое состояние в FixedUpdateNetwork ──────────────
        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || animator == null)
                return;

            bool flying  = pipeSlide != null && pipeSlide.IsSliding;
            bool grounded = !flying && controller != null && controller.IsGrounded;
            bool holding = grab   != null && grab.IsHolding;
            bool armed   = weapon != null && weapon.IsArmed;

            float vy      = rb != null ? rb.linearVelocity.y : 0f;
            bool falling  = !flying && !grounded && vy < fallYThreshold;

            float speedNorm = 0f;
            if (!flying && GetInput(out NetworkInputData data) && data.Move.sqrMagnitude > 0.01f && grounded)
                speedNorm = 1f;

            // Пишем в сетевые свойства — Fusion реплицирует их на все клиенты
            NetGrounded = grounded;
            NetHolding  = holding;
            NetArmed    = armed;
            NetFalling  = falling;
            NetFlying   = flying;
            NetSpeed    = speedNorm;
        }

        // ── Все клиенты: читаем сетевое состояние и двигаем Animator ─────────
        // Render() вызывается каждый Unity-кадр, ПОСЛЕ интерполяции snapshot.
        public override void Render()
        {
            if (animator == null)
                return;

            // Непрерывные параметры
            animator.SetFloat(SpeedHash,      NetSpeed,   speedDamp, Time.deltaTime);
            animator.SetBool(IsGroundedHash,  NetGrounded);
            animator.SetBool(IsHoldingHash,   NetHolding);
            animator.SetBool(IsArmedHash,     NetArmed);
            animator.SetBool(IsFallingHash,   NetFalling);
            animator.SetBool(IsFlyingHash,    NetFlying);

            // Одноразовые события: инкремент счётчика = «сыграть анимацию 1 раз»
            if (NetJump  != _prevJump)  { animator.SetTrigger(JumpHash);  _prevJump  = NetJump; }
            if (NetDash  != _prevDash)  { animator.SetTrigger(DashHash);  _prevDash  = NetDash; }
            if (NetPush  != _prevPush)  { animator.SetTrigger(PushHash);  _prevPush  = NetPush; }
            if (NetThrow != _prevThrow) { animator.SetTrigger(ThrowHash); _prevThrow = NetThrow; }
            if (NetShoot != _prevShoot) { animator.SetTrigger(ShootHash); _prevShoot = NetShoot; }
        }

        // ── Публичные методы: вызываются из PlayerController и др. ────────────
        // Только owner; инкрементируют счётчик → Fusion реплицирует → Render()

        public void PlayJump()
        {
            if (animator.GetBool(IsHoldingHash)) return;
            if (!HasStateAuthority) return;
            NetJump++;
        }

        public void PlayDash()
        {
            if (animator.GetBool(IsHoldingHash)) return;
            if (!HasStateAuthority) return;
            NetDash++;
        }

        public void PlayPush()
        {
            if (!HasStateAuthority) return;
            NetPush++;
        }

        public void PlayThrow()
        {
            if (!HasStateAuthority) return;
            NetThrow++;
        }

        public void PlayShoot()
        {
            if (!HasStateAuthority) return;
            NetShoot++;
        }

        public void PlayStunVFX()
        {
            _vfxManager.PlayStunVFX(stunVFXPoint.position);
        }

        public void SetFlying(bool value)
        {
            // Если нужен мгновенный отклик до следующего FixedUpdateNetwork,
            // пишем напрямую; FixedUpdateNetwork всё равно перезапишет.
            if (!HasStateAuthority) return;
            NetFlying = value;
            if (value)
            {
                NetFalling  = false;
                NetGrounded = false;
            }
        }
    }
}
