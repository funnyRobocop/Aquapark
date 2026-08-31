using Fusion;
using UnityEngine;


namespace NonameGame
{
    public class PlayerGrab : NetworkBehaviour
    {
        [Header("Cone")]
        [SerializeField] private float grabRadius = 1.8f;
        [SerializeField] private float grabAngle = 90f;
        [SerializeField] private LayerMask itemMask;
        [SerializeField] private Transform holdPoint; // пустой объект перед персонажем

        [Header("Hold")]
        [SerializeField] private float holdLerp = 25f;

        [Networked] private NetworkId _heldItemId { get; set; }
        [Networked] private NetworkBool _isHolding { get; set; }

        private bool _wasGrabHeld;

        public bool IsHolding => _isHolding;

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (!GetInput(out NetworkInputData data))
                return;

            bool held = data.GrabHeld;
            bool pressed = held && !_wasGrabHeld;
            bool released = !held && _wasGrabHeld;
            _wasGrabHeld = held;

            if (_isHolding)
            {
                UpdateHeldItemPosition();

                if (released)
                    ThrowHeldItem();
            }
            else
            {
                if (pressed)
                    TryGrab();
            }
        }

        private void TryGrab()
        {
            ThrowableItem item = FindItemInCone();
            if (item == null)
                return;

            // Берём authority над предметом
            if (!item.Object.HasStateAuthority)
                item.Object.RequestStateAuthority();

            item.PickUp(Object.InputAuthority);
            _heldItemId = item.Object.Id;
            _isHolding = true;
        }

        private void ThrowHeldItem()
        {
            if (!_isHolding)
                return;

            if (!Runner.TryFindObject(_heldItemId, out var obj))
            {
                ClearHold();
                return;
            }

            var item = obj.GetBehaviour<ThrowableItem>();
            if (item == null)
            {
                ClearHold();
                return;
            }

            Vector3 dir = transform.forward;
            dir.y = 0f;
            dir.Normalize();

            item.Throw(dir);
            ClearHold();
        }

        private void UpdateHeldItemPosition()
        {
            if (!Runner.TryFindObject(_heldItemId, out var obj))
            {
                ClearHold();
                return;
            }

            var item = obj.GetBehaviour<ThrowableItem>();
            if (item == null || !item.IsHeld)
            {
                ClearHold();
                return;
            }

            Vector3 targetPos = holdPoint != null
                ? holdPoint.position
                : transform.position + transform.forward * 1.1f + Vector3.up * 1.1f;

            Quaternion targetRot = holdPoint != null ? holdPoint.rotation : transform.rotation;

            // Т.к. kinematic — двигаем transform / NetworkTransform
            var nt = item.GetComponent<NetworkTransform>();
            if (nt != null)
            {
                // Плавное следование
                Vector3 p = Vector3.Lerp(item.transform.position, targetPos, Runner.DeltaTime * holdLerp);
                nt.Teleport(p, targetRot);
            }
            else
            {
                item.transform.position = Vector3.Lerp(item.transform.position, targetPos, Runner.DeltaTime * holdLerp);
                item.transform.rotation = targetRot;
            }
        }

        private ThrowableItem FindItemInCone()
        {
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Collider[] hits = Physics.OverlapSphere(origin, grabRadius, itemMask);

            ThrowableItem best = null;
            float bestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var item = hit.GetComponentInParent<ThrowableItem>();
                if (item == null || item.Object == null)
                    continue;

                // Уже у кого-то в руках
                if (item.IsHeld)
                    continue;

                Vector3 toItem = item.transform.position - transform.position;
                toItem.y = 0f;
                if (toItem.sqrMagnitude < 0.001f)
                    continue;

                float angle = Vector3.Angle(forward, toItem.normalized);
                if (angle > grabAngle * 0.5f)
                    continue;

                float dist = toItem.magnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = item;
                }
            }

            return best;
        }

        private void ClearHold()
        {
            _isHolding = false;
            _heldItemId = default;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(origin, grabRadius);

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Quaternion left = Quaternion.Euler(0f, -grabAngle * 0.5f, 0f);
            Quaternion right = Quaternion.Euler(0f, grabAngle * 0.5f, 0f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + left * forward * grabRadius);
            Gizmos.DrawLine(origin, origin + right * forward * grabRadius);
        }
    }
}