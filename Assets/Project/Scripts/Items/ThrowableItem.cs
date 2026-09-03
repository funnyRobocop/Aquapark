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
            // Пока держим — кинематик (позиция ставится с игрока)
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

        private void OnCollisionEnter(Collision collision)
        {
            if (!HasStateAuthority)
                return;

            // Удар по игроку только пока летит после броска
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

                    // После удара по игроку можно оставить airborne или сбросить:
                    // IsAirborneThrown = false;
                }
            }

            // Касание земли / окружения — больше не "снаряд"
            if (!collision.collider.GetComponentInParent<PlayerRaceData>())
            {
                // небольшая проверка: не сбрасываем от другого предмета в воздухе, если хочешь — добавь слой Ground
                IsAirborneThrown = false;
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!HasStateAuthority)
                return;

            // Надёжнее гасить airborne при контакте с полом
            if (IsAirborneThrown && IsGroundLayer(collision.collider))
                IsAirborneThrown = false;
        }

        private bool IsGroundLayer(Collider c)
        {
            // При желании замени на LayerMask
            return !c.GetComponentInParent<PlayerRaceData>() && !c.GetComponentInParent<ThrowableItem>();
        }
    }
}
