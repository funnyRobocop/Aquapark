using Fusion;
using UnityEngine;

namespace NonameGame
{
    public class ThrowableItem : NetworkBehaviour
    {
        [Header("Hold / Throw")]
        [SerializeField] private float throwForce = 14f;
        [SerializeField] private float throwUpForce = 3f;

        [Header("Hit Player")]
        [SerializeField] private float hitForce = 16f;
        [SerializeField] private float hitUpForce = 3f;
        [SerializeField] private float stunTime = 0.35f;

        [Networked] public NetworkBool IsHeld { get; set; }
        [Networked] public NetworkBool IsAirborneThrown { get; set; }
        [Networked] public PlayerRef HeldBy { get; set; }
        /// <summary>Кто бросил предмет — сохраняется до приземления для подсчёта ThrowCount.</summary>
        [Networked] public PlayerRef ThrownBy { get; set; }

        private Rigidbody _rb;
        private Collider _col;
        private NetworkTransform _nt;

        private Vector3 _initialPosition;

        public override void Spawned()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();
            _nt = GetComponent<NetworkTransform>();
            _initialPosition = transform.position;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_ResetToInitial() => ResetToInitial();

        public void ResetToInitial()
        {
            _nt.Teleport(_initialPosition);
            var rb = GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        }

        public override void FixedUpdateNetwork()
        {
            if (IsHeld)
            {
                if (_rb != null && !_rb.isKinematic)
                    _rb.isKinematic = true;

                if (_col != null)
                    _col.enabled = false;
            }
            else
            {
                if (_rb != null && _rb.isKinematic)
                    _rb.isKinematic = false;

                if (_col != null)
                    _col.enabled = true;
            }
        }

        public void PickUp(PlayerRef player)
        {
            IsHeld = true;
            IsAirborneThrown = false;
            HeldBy = player;
            ThrownBy = default;
        }

        public void Throw(Vector3 force, PlayerRef throwner)
        {
            ThrownBy = throwner;       // запомнить до сброса
            IsHeld = false;
            IsAirborneThrown = true;
            HeldBy = default;

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(force, ForceMode.VelocityChange);
            }
        }

        public void ForceDrop()
        {
            IsHeld = false;
            IsAirborneThrown = false;
            HeldBy = default;
            ThrownBy = default;

            if (_rb != null)
                _rb.isKinematic = false;

            if (_col != null)
                _col.enabled = true;
        }

        /// <summary>
        /// Рассылается всем; каждый клиент проверяет — он ли бросал — и прибавляет ThrowCount + проигрывает эффект.
        /// </summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_NotifyThrowHit(PlayerRef thrower, Vector3 hitPosition)
        {
            if (Runner.LocalPlayer != thrower) return;

            var local = PlayerRaceData.LocalInstance;
            if (local == null)
                return;
                
            local.PlayHitFeedback(hitPosition);   // ← эффект + звук у кидавшего

            if (InGameManager.Instance != null &&
                InGameManager.Instance.gameState == InGameManager.GameState.Game)
            {
                local.ThrowCount++;
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_ApplyPush(Vector3 force)
        {
            if (IsHeld)
                return;

            if (_rb == null)
                _rb = GetComponent<Rigidbody>();

            if (_rb == null)
                return;

            if (_rb.isKinematic)
                _rb.isKinematic = false;

            _rb.AddForce(force, ForceMode.Impulse);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!HasStateAuthority)
                return;

            if (IsAirborneThrown)
            {
                var player = collision.collider.GetComponentInParent<PlayerRaceData>();
                // Не применять силу и не считать бросок по самому бросающему
                if (player != null && player.Object.InputAuthority != ThrownBy)
                {
                    Vector3 dir = player.transform.position - transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude < 0.001f)
                        dir = transform.forward;
                    dir.Normalize();
                    dir += Vector3.up * (hitUpForce / Mathf.Max(hitForce, 0.01f));

                    player.RPC_ApplyPush(dir.normalized * hitForce);

                    if (collision.collider.TryGetComponent<PlayerController>(out var playerController))
                        playerController.SetStunTimer(stunTime);

                    // Уведомляем бросавшего — он локально прибавит ThrowCount и проиграет эффект
                    if (ThrownBy != default)
                        RPC_NotifyThrowHit(ThrownBy, player.transform.position);
                }
            }

            if (!collision.collider.GetComponentInParent<PlayerRaceData>())
            {
                IsAirborneThrown = false;
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!HasStateAuthority)
                return;

            if (IsAirborneThrown && IsGroundLayer(collision.collider))
                IsAirborneThrown = false;
        }

        private bool IsGroundLayer(Collider c)
        {
            return !c.GetComponentInParent<PlayerRaceData>() && !c.GetComponentInParent<ThrowableItem>();
        }
    }
}