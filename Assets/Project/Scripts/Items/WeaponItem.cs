using Fusion;
using UnityEngine;

namespace NonameGame
{
    public class WeaponItem : NetworkBehaviour
    {
        [Header("Projectiles (scene refs, 3 шт)")]
        [SerializeField] private Bullet[] projectiles;

        [Networked] public NetworkBool IsHeld { get; set; }
        [Networked] public PlayerRef HeldBy { get; set; }
        [Networked] public int Ammo { get; set; }

        private Rigidbody _rb;
        private Collider _col;

        public int MaxAmmo => projectiles != null ? projectiles.Length : 0;
        public bool HasAmmo => Ammo > 0;

        public override void Spawned()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();

            if (projectiles != null)
            {
                foreach (var p in projectiles)
                {
                    if (p != null)
                        p.SetOwnerWeapon(this);
                }
            }

            if (HasStateAuthority)
                Ammo = MaxAmmo;
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
            HeldBy = player;
        }

        public void ForceDrop()
        {
            IsHeld = false;
            HeldBy = default;

            if (_rb != null)
                _rb.isKinematic = false;
            if (_col != null)
                _col.enabled = true;
        }

        public Bullet GetAvailableProjectile()
        {
            if (projectiles == null || Ammo <= 0)
                return null;

            foreach (var p in projectiles)
            {
                if (p != null && p.IsInPool)
                    return p;
            }

            return null;
        }

        public void OnProjectileFired(Bullet p)
        {
            if (!HasStateAuthority)
                return;
            Ammo = Mathf.Max(0, Ammo - 1);
        }

        public void OnProjectileReturned(Bullet p)
        {
            if (!HasStateAuthority)
                return;
            Ammo = Mathf.Min(MaxAmmo, Ammo + 1);
        }
    }
}