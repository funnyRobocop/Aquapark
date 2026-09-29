using Fusion;
using UnityEngine;

namespace NonameGame
{
    public class Bullet : NetworkBehaviour
    {
        [Header("Pool")]
        [SerializeField] private Vector3 poolPosition = new Vector3(0f, -500f, 0f);

        [Header("Hit")]
        [SerializeField] private float hitForce = 12f;
        [SerializeField] private float hitUpForce = 2.5f;

        [Header("Return to pool")]
        [SerializeField] private float maxFlightTime = 6f;

        [Networked] public NetworkBool IsInPool { get; set; }
        [Networked] public NetworkBool IsAirborne { get; set; }
        [Networked] public PlayerRef FiredBy { get; set; }

        private Rigidbody _rb;
        private Collider _col;
        private NetworkTransform _nt;
        private TickTimer _flightTimer;
        private WeaponItem _ownerWeapon;

        public void SetOwnerWeapon(WeaponItem weapon)
        {
            _ownerWeapon = weapon;
        }

        public override void Spawned()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();
            _nt = GetComponent<NetworkTransform>();

            if (HasStateAuthority)
                ReturnToPoolImmediate();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (IsInPool)
            {
                if (_rb != null && !_rb.isKinematic)
                    _rb.isKinematic = true;
                if (_col != null && _col.enabled)
                    _col.enabled = false;
                return;
            }

            if (IsAirborne && _flightTimer.Expired(Runner))
                ReturnToPoolImmediate();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_Fire(Vector3 position, Quaternion rotation, Vector3 velocity, PlayerRef shooter)
        {
            if (!IsInPool)
                return;

            FiredBy = shooter;
            IsInPool = false;
            IsAirborne = true;
            _flightTimer = TickTimer.CreateFromSeconds(Runner, maxFlightTime);

            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            if (_col != null)
                _col.enabled = true;

            if (_nt != null)
            {
                _nt.enabled = true;
                _nt.Teleport(position, rotation);
            }
            else
            {
                transform.SetPositionAndRotation(position, rotation);
            }

            SetRenderersEnabled(true);

            if (_rb != null)
                _rb.linearVelocity = velocity;

            if (_ownerWeapon != null)
                _ownerWeapon.OnProjectileFired(this);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!HasStateAuthority || IsInPool)
                return;

            var player = collision.collider.GetComponentInParent<PlayerRaceData>();
            if (player != null)
            {
                if (player.Object != null && player.Object.InputAuthority == FiredBy)
                    return;

                Vector3 dir = player.transform.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.001f)
                    dir = transform.forward;
                dir.Normalize();
                dir += Vector3.up * (hitUpForce / Mathf.Max(hitForce, 0.01f));
                dir.Normalize();

                player.RPC_ApplyPush(dir * hitForce);
                ReturnToPoolImmediate();
                return;
            }

            if (IsAirborne)
            {
                IsAirborne = false;
                ReturnToPoolImmediate();
            }
        }

        private void ReturnToPoolImmediate()
        {
            IsInPool = true;
            IsAirborne = false;
            FiredBy = default;
            _flightTimer = default;

            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }

            if (_col != null)
                _col.enabled = false;

            if (_nt != null)
                _nt.Teleport(poolPosition, Quaternion.identity);
            else
                transform.position = poolPosition;

            SetRenderersEnabled(false);

            if (_ownerWeapon != null)
                _ownerWeapon.OnProjectileReturned(this);
        }

        private void SetRenderersEnabled(bool enabled)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.enabled = enabled;
        }
    }
}