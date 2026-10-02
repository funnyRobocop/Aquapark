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

        private Rigidbody _rb;
        private Collider _col;

        public override void Spawned()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();
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
        }

        public void Throw(Vector3 force)
        {
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

            if (_rb != null)
                _rb.isKinematic = false;

            if (_col != null)
                _col.enabled = true;
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
                if (player != null)
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