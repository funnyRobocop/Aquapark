using Fusion;
using UnityEngine;

namespace NonameGame
{
    public class WeaponItem : NetworkBehaviour
    {
        [Networked] public NetworkBool IsHeld { get; set; }
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
    }
}