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

        [Header("References")]
        [SerializeField] private PlayerView _view;

        [Networked] private NetworkId _heldItemId { get; set; }
        [Networked] private NetworkBool _isHolding { get; set; }

        private bool _wasGrabHeld;

        // Локальный меш в СВОИХ руках (authority)
        private GameObject _localVisual;

        // Локальный меш в чужих руках (proxy) — один на этого игрока
        private GameObject _remoteHeldVisual;
        private NetworkId _remoteHeldItemId;

        public bool IsHolding => _isHolding;
        public Transform HoldPoint => holdPoint;

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
            // 1) Свой предмет в руках
            if (HasStateAuthority && _isHolding && _localVisual != null && holdPoint != null)
            {
                _localVisual.transform.SetPositionAndRotation(holdPoint.position, holdPoint.rotation);
            }

            // 2) Чужие руки — показываем/убираем visual по IsHeld
            UpdateRemoteHeldVisual();
        }

        private void UpdateRemoteHeldVisual()
        {
            // Свои руки обрабатываем через _localVisual
            if (HasStateAuthority)
                return;

            if (holdPoint == null || Runner == null)
                return;

            // Этот PlayerGrab принадлежит какому-то игроку — ищем, держит ли ОН что-то
            PlayerRef owner = Object.InputAuthority;
            if (owner == PlayerRef.None)
                owner = Object.StateAuthority;

            ThrowableItem heldItem = FindHeldItemByPlayer(owner);

            if (heldItem != null)
            {
                // Нужен visual
                if (_remoteHeldVisual == null || _remoteHeldItemId != heldItem.Object.Id)
                {
                    DestroyRemoteHeldVisual();
                    _remoteHeldVisual = CreateVisualCopy(heldItem.gameObject);
                    _remoteHeldItemId = heldItem.Object.Id;
                }

                if (_remoteHeldVisual != null)
                {
                    _remoteHeldVisual.transform.SetPositionAndRotation(holdPoint.position, holdPoint.rotation);
                }
            }
            else
            {
                DestroyRemoteHeldVisual();
            }
        }

        private ThrowableItem FindHeldItemByPlayer(PlayerRef player)
        {
            foreach (var item in Runner.GetAllBehaviours<ThrowableItem>())
            {
                if (item != null && item.IsHeld && item.HeldBy == player)
                    return item;
            }
            return null;
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

            Vector3 spawnPos = holdPoint != null
                ? holdPoint.position
                : transform.position + transform.forward * 1.1f + Vector3.up * 1.1f;
            Quaternion spawnRot = holdPoint != null ? holdPoint.rotation : transform.rotation;

            DestroyLocalVisual();
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

            _view.PlayThrow();
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

            SetRenderersEnabled(item.gameObject, false);
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
            _localVisual = CreateVisualCopy(item.gameObject);
        }

        private GameObject CreateVisualCopy(GameObject source)
        {
            if (source == null || holdPoint == null)
                return null;

            var copy = Instantiate(source, holdPoint);
            copy.name = source.name + "_HeldVisual";

            foreach (var nb in copy.GetComponentsInChildren<NetworkBehaviour>(true))
                Destroy(nb);
            foreach (var no in copy.GetComponentsInChildren<NetworkObject>(true))
                Destroy(no);
            foreach (var nt in copy.GetComponentsInChildren<NetworkTransform>(true))
                Destroy(nt);
            foreach (var rb in copy.GetComponentsInChildren<Rigidbody>(true))
                Destroy(rb);
            foreach (var col in copy.GetComponentsInChildren<Collider>(true))
                Destroy(col);

            copy.transform.SetParent(holdPoint);
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;

            SetRenderersEnabled(copy, true);
            return copy;
        }

        private void DestroyLocalVisual()
        {
            if (_localVisual != null)
            {
                Destroy(_localVisual);
                _localVisual = null;
            }
        }

        private void DestroyRemoteHeldVisual()
        {
            if (_remoteHeldVisual != null)
            {
                Destroy(_remoteHeldVisual);
                _remoteHeldVisual = null;
            }
            _remoteHeldItemId = default;
        }

        private static void SetRenderersEnabled(GameObject go, bool enabled)
        {
            if (go == null) return;
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
            DestroyRemoteHeldVisual();
        }
    }
}