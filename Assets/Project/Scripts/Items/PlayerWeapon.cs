using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace NonameGame
{
    public class PlayerWeapon : NetworkBehaviour
    {
        [Header("Grab cone")]
        [SerializeField] private float grabRadius = 1.8f;
        [SerializeField] private float grabAngle = 90f;
        [SerializeField] private LayerMask weaponMask;
        [SerializeField] private Transform holdPoint;
        [SerializeField] private Transform muzzlePoint;

        [Header("Hide while held")]
        [SerializeField] private Vector3 hidePosition = new Vector3(0f, -500f, 0f);

        [Header("Shoot")]
        [SerializeField] private float shootCooldown = 0.35f;
        [SerializeField] private float bulletSpeed = 28f;
        [SerializeField] private float bulletUpBoost = 0.05f;
        [SerializeField] private Bullet bulletPrefab;
        [SerializeField] private int poolSize = 12;

        [Header("References")]
        [SerializeField] private PlayerView _view;
        [SerializeField] private PlayerGrab playerGrab;

        [Networked] private NetworkId _heldWeaponId { get; set; }
        [Networked] private NetworkBool _isArmed { get; set; }
        [Networked] private TickTimer _shootCooldownTimer { get; set; }

        private bool _wasGrabHeld;
        private GameObject _localVisual;
        private GameObject _remoteHeldVisual;
        private NetworkId _remoteHeldWeaponId;

        private readonly Queue<Bullet> _pool = new Queue<Bullet>();
        private readonly List<Bullet> _allBullets = new List<Bullet>();

        public bool IsArmed => _isArmed;

        public override void Spawned()
        {
            if (playerGrab == null)
                playerGrab = GetComponent<PlayerGrab>();

            if (HasStateAuthority && bulletPrefab != null)
                BuildPool();
        }

        private void BuildPool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                var go = Instantiate(bulletPrefab.gameObject, transform);
                go.name = $"Bullet_{i}";
                var bullet = go.GetComponent<Bullet>();
                bullet.Init(this);
                _pool.Enqueue(bullet);
                _allBullets.Add(bullet);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (!GetInput(out NetworkInputData data))
                return;

            if (playerGrab != null && playerGrab.IsHolding)
                return;

            bool held = data.GrabHeld;
            bool pressed = held && !_wasGrabHeld;
            bool released = !held && _wasGrabHeld;
            _wasGrabHeld = held;

            if (_isArmed)
            {
                KeepWeaponHidden();

                if (released)
                    DropWeapon();
                else if (data.PushPressed)
                    TryShoot();
            }
            else if (pressed)
            {
                TryGrabWeapon();
            }
        }

        public override void Render()
        {
            if (HasStateAuthority && _isArmed && _localVisual != null && holdPoint != null)
            {
                _localVisual.transform.SetPositionAndRotation(holdPoint.position, holdPoint.rotation);
            }

            UpdateRemoteHeldVisual();
        }

        private void TryGrabWeapon()
        {
            WeaponItem weapon = FindWeaponInCone();
            if (weapon == null)
                return;

            if (!weapon.Object.HasStateAuthority)
                weapon.Object.RequestStateAuthority();

            HideAndHold(weapon);
            weapon.PickUp(Object.InputAuthority);

            _heldWeaponId = weapon.Object.Id;
            _isArmed = true;

            SpawnLocalVisual(weapon);
        }

        private void DropWeapon()
        {
            if (!_isArmed)
                return;

            if (!Runner.TryFindObject(_heldWeaponId, out var obj))
            {
                ClearHold();
                return;
            }

            var weapon = obj.GetBehaviour<WeaponItem>();
            if (weapon == null)
            {
                ClearHold();
                return;
            }

            Vector3 spawnPos = holdPoint != null
                ? holdPoint.position
                : transform.position + transform.forward * 1.1f + Vector3.up * 1.1f;
            Quaternion spawnRot = holdPoint != null ? holdPoint.rotation : transform.rotation;

            DestroyLocalVisual();
            RestoreWeapon(weapon, spawnPos, spawnRot);
            weapon.ForceDrop();
            ClearHold();
        }

        private void TryShoot()
        {
            if (!_shootCooldownTimer.ExpiredOrNotRunning(Runner))
                return;

            if (_pool.Count == 0)
                return;

            Vector3 origin = muzzlePoint != null
                ? muzzlePoint.position
                : (holdPoint != null
                    ? holdPoint.position
                    : transform.position + Vector3.up * 1.2f + transform.forward * 0.6f);

            Vector3 dir = transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f)
                dir = Vector3.forward;
            dir.Normalize();
            dir += Vector3.up * bulletUpBoost;
            dir.Normalize();

            Quaternion rot = Quaternion.LookRotation(dir);

            Bullet bullet = _pool.Dequeue();
            bullet.Fire(origin, rot, dir * bulletSpeed, Object.InputAuthority);

            _shootCooldownTimer = TickTimer.CreateFromSeconds(Runner, shootCooldown);

            if (_view != null)
                _view.PlayShoot();
        }

        public void ReturnBullet(Bullet bullet)
        {
            if (bullet == null)
                return;
            if (!_pool.Contains(bullet))
                _pool.Enqueue(bullet);
        }

        private void HideAndHold(WeaponItem weapon)
        {
            var rb = weapon.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            var col = weapon.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            var nt = weapon.GetComponent<NetworkTransform>();
            if (nt != null)
            {
                nt.enabled = false;
                nt.Teleport(hidePosition, Quaternion.identity);
            }
            else
            {
                weapon.transform.position = hidePosition;
            }

            SetRenderersEnabled(weapon.gameObject, false);
        }

        private void KeepWeaponHidden()
        {
            if (!Runner.TryFindObject(_heldWeaponId, out var obj))
                return;

            var weapon = obj.GetBehaviour<WeaponItem>();
            if (weapon == null)
                return;

            if ((weapon.transform.position - hidePosition).sqrMagnitude > 0.01f)
            {
                var nt = weapon.GetComponent<NetworkTransform>();
                if (nt != null && nt.enabled)
                    nt.enabled = false;
                weapon.transform.position = hidePosition;
            }

            SetRenderersEnabled(weapon.gameObject, false);
        }

        private void RestoreWeapon(WeaponItem weapon, Vector3 pos, Quaternion rot)
        {
            SetRenderersEnabled(weapon.gameObject, true);

            var col = weapon.GetComponent<Collider>();
            if (col != null)
                col.enabled = true;

            var rb = weapon.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            var nt = weapon.GetComponent<NetworkTransform>();
            if (nt != null)
            {
                nt.enabled = true;
                nt.Teleport(pos, rot);
            }
            else
            {
                weapon.transform.SetPositionAndRotation(pos, rot);
            }
        }

        private void SpawnLocalVisual(WeaponItem weapon)
        {
            DestroyLocalVisual();
            _localVisual = CreateVisualCopy(weapon.gameObject);
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

        private void UpdateRemoteHeldVisual()
        {
            if (HasStateAuthority)
                return;

            if (holdPoint == null || Runner == null)
                return;

            PlayerRef owner = Object.InputAuthority;
            if (owner == PlayerRef.None)
                owner = Object.StateAuthority;

            WeaponItem held = FindHeldWeaponByPlayer(owner);

            if (held != null)
            {
                if (_remoteHeldVisual == null || _remoteHeldWeaponId != held.Object.Id)
                {
                    DestroyRemoteHeldVisual();
                    _remoteHeldVisual = CreateVisualCopy(held.gameObject);
                    _remoteHeldWeaponId = held.Object.Id;
                }

                if (_remoteHeldVisual != null)
                    _remoteHeldVisual.transform.SetPositionAndRotation(holdPoint.position, holdPoint.rotation);
            }
            else
            {
                DestroyRemoteHeldVisual();
            }
        }

        private WeaponItem FindHeldWeaponByPlayer(PlayerRef player)
        {
            foreach (var w in Runner.GetAllBehaviours<WeaponItem>())
            {
                if (w != null && w.IsHeld && w.HeldBy == player)
                    return w;
            }
            return null;
        }

        private void DestroyRemoteHeldVisual()
        {
            if (_remoteHeldVisual != null)
            {
                Destroy(_remoteHeldVisual);
                _remoteHeldVisual = null;
            }
            _remoteHeldWeaponId = default;
        }

        private static void SetRenderersEnabled(GameObject go, bool enabled)
        {
            if (go == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.enabled = enabled;
        }

        private WeaponItem FindWeaponInCone()
        {
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Collider[] hits = Physics.OverlapSphere(origin, grabRadius, weaponMask);
            WeaponItem best = null;
            float bestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var weapon = hit.GetComponentInParent<WeaponItem>();
                if (weapon == null || weapon.Object == null || weapon.IsHeld)
                    continue;

                Vector3 to = weapon.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude < 0.001f)
                    continue;

                if (Vector3.Angle(forward, to.normalized) > grabAngle * 0.5f)
                    continue;

                float dist = to.magnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = weapon;
                }
            }

            return best;
        }

        private void ClearHold()
        {
            _isArmed = false;
            _heldWeaponId = default;
            DestroyLocalVisual();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            DestroyLocalVisual();
            DestroyRemoteHeldVisual();

            foreach (var b in _allBullets)
            {
                if (b != null)
                    Destroy(b.gameObject);
            }
            _allBullets.Clear();
            _pool.Clear();
        }
    }
}