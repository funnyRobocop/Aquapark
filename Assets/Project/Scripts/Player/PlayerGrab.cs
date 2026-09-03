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
        [SerializeField] private Transform holdPoint;

        [Header("Throw")]
        [SerializeField] private float throwForce = 14f;
        [SerializeField] private float throwUpForce = 3f;

        [Header("Hide while held")]
        [SerializeField] private Vector3 hidePosition = new Vector3(0f, -500f, 0f);

        [Networked] private NetworkId _heldItemId { get; set; }
        [Networked] private NetworkBool _isHolding { get; set; }

        private bool _wasGrabHeld;
        private GameObject _localVisual; // локальная копия только у себя

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
                // Реальный предмет держим «в изгнании»
                KeepItemHidden();

                if (released)
                    ThrowHeldItem();
            }
            else if (pressed)
            {
                TryGrab();
            }
        }

        public override void Render()
        {
            // Локальный меш в руке — только у владельца
            if (!HasStateAuthority)
                return;

            if (_isHolding && _localVisual != null && holdPoint != null)
            {
                _localVisual.transform.SetPositionAndRotation(holdPoint.position, holdPoint.rotation);
            }
        }

        private void TryGrab()
        {
            ThrowableItem item = FindItemInCone();
            if (item == null)
                return;

            if (!item.Object.HasStateAuthority)
                item.Object.RequestStateAuthority();

            HideAndHold(item);
            item.PickUp(Object.InputAuthority);

            _heldItemId = item.Object.Id;
            _isHolding = true;

            SpawnLocalVisual(item);
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

            Vector3 spawnPos = holdPoint != null ? holdPoint.position : transform.position + transform.forward * 1.1f + Vector3.up * 1.1f;
            Quaternion spawnRot = holdPoint != null ? holdPoint.rotation : transform.rotation;

            // Убираем локальный меш
            DestroyLocalVisual();

            // Возвращаем реальный предмет в руку и бросаем
            RestoreItemForThrow(item, spawnPos, spawnRot);

            Vector3 dir = transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f)
                dir = Vector3.forward;
            dir.Normalize();
            dir += Vector3.up * (throwUpForce / Mathf.Max(throwForce, 0.01f));
            dir.Normalize();

            item.Throw(dir * throwForce);
            ClearHold();
        }

        private void HideAndHold(ThrowableItem item)
        {
            var rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            var col = item.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            var nt = item.GetComponent<NetworkTransform>();
            if (nt != null)
            {
                nt.enabled = false;
                nt.Teleport(hidePosition, Quaternion.identity);
            }
            else
            {
                item.transform.position = hidePosition;
            }

            // Опционально выключить рендеры оригинала
            SetRenderersEnabled(item.gameObject, false);
        }

        private void KeepItemHidden()
        {
            if (!Runner.TryFindObject(_heldItemId, out var obj))
                return;

            var item = obj.GetBehaviour<ThrowableItem>();
            if (item == null)
                return;

            if ((item.transform.position - hidePosition).sqrMagnitude > 0.01f)
            {
                var nt = item.GetComponent<NetworkTransform>();
                if (nt != null && nt.enabled)
                    nt.enabled = false;

                item.transform.position = hidePosition;
            }
        }

        private void RestoreItemForThrow(ThrowableItem item, Vector3 pos, Quaternion rot)
        {
            SetRenderersEnabled(item.gameObject, true);

            var col = item.GetComponent<Collider>();
            if (col != null)
                col.enabled = true;

            var rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            var nt = item.GetComponent<NetworkTransform>();
            if (nt != null)
            {
                nt.enabled = true;
                nt.Teleport(pos, rot);
            }
            else
            {
                item.transform.SetPositionAndRotation(pos, rot);
            }
        }

        private void SpawnLocalVisual(ThrowableItem item)
        {
            DestroyLocalVisual();

            // Копия только меша — без сетевых компонент
            _localVisual = Instantiate(item.gameObject, holdPoint != null ? holdPoint : transform);

            // Снять всё сетевое / физику с копии
            foreach (var nb in _localVisual.GetComponentsInChildren<NetworkBehaviour>(true))
                Destroy(nb);

            foreach (var no in _localVisual.GetComponentsInChildren<NetworkObject>(true))
                Destroy(no);

            foreach (var nt in _localVisual.GetComponentsInChildren<NetworkTransform>(true))
                Destroy(nt);

            foreach (var rb in _localVisual.GetComponentsInChildren<Rigidbody>(true))
                Destroy(rb);

            foreach (var col in _localVisual.GetComponentsInChildren<Collider>(true))
                Destroy(col);

            _localVisual.transform.SetParent(holdPoint != null ? holdPoint : transform);
            _localVisual.transform.localPosition = Vector3.zero;
            _localVisual.transform.localRotation = Quaternion.identity;

            SetRenderersEnabled(_localVisual, true);
        }

        private void DestroyLocalVisual()
        {
            if (_localVisual != null)
            {
                Destroy(_localVisual);
                _localVisual = null;
            }
        }

        private static void SetRenderersEnabled(GameObject go, bool enabled)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.enabled = enabled;
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
                if (item == null || item.Object == null || item.IsHeld)
                    continue;

                Vector3 toItem = item.transform.position - transform.position;
                toItem.y = 0f;
                if (toItem.sqrMagnitude < 0.001f)
                    continue;

                if (Vector3.Angle(forward, toItem.normalized) > grabAngle * 0.5f)
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
            DestroyLocalVisual();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            DestroyLocalVisual();
        }
    }
}