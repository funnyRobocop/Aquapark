using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fusion;

namespace NonameGame
{
    public class Bumper : MonoBehaviour
    {
        [Header("Настройки бампера")]
        [SerializeField] private float bounceForce = 18f;
        [SerializeField] private float upwardBoost = 0.45f;
        [SerializeField] private float cooldownTime = 0.5f;
        [SerializeField] private string playerTag = "Player";

        private readonly HashSet<NetworkId> _cooldownIds = new HashSet<NetworkId>();
        private CapsuleCollider _capsuleCollider;

        private void Awake()
        {
            _capsuleCollider = GetComponent<CapsuleCollider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            TryBounce(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryBounce(other);
        }

        private void TryBounce(Collider other)
        {
            if (!other.CompareTag(playerTag))
                return;

            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null)
                return;

            if (!netObj.HasStateAuthority)
                return;

            if (_cooldownIds.Contains(netObj.Id))
                return;

            var rb = other.attachedRigidbody;
            if (rb == null)
                rb = other.GetComponentInParent<Rigidbody>();
            if (rb == null)
                return;

            _cooldownIds.Add(netObj.Id);

            // Определяем точку отталкивания: либо ближайшая точка на оси капсулы, либо центр объекта
            Vector3 originPoint = _capsuleCollider != null
                ? GetClosestPointOnCapsuleAxis(_capsuleCollider, other.bounds.center)
                : transform.position;

            Vector3 bounceDir = other.bounds.center - originPoint;
            bounceDir.y = 0f;
            if (bounceDir.sqrMagnitude < 0.001f)
                bounceDir = transform.forward;
            bounceDir.Normalize();
            bounceDir.y = upwardBoost;
            bounceDir.Normalize();

            rb.linearVelocity = Vector3.zero;

            var target = other.GetComponentInParent<PlayerRaceData>();
            if (target != null && target.Object != null)
                target.RPC_ApplyPush(bounceDir * bounceForce);

            StartCoroutine(ReleaseCooldown(netObj.Id, cooldownTime));

            var playerController = other.GetComponentInParent<PlayerController>();
            if (playerController != null)
                playerController.SetStunTimer(cooldownTime);
        }

        /// <summary>
        /// Возвращает ближайшую к targetWorldPos точку на центральной оси CapsuleCollider в мировых координатах.
        /// </summary>
        private Vector3 GetClosestPointOnCapsuleAxis(CapsuleCollider capsule, Vector3 targetWorldPos)
        {
            // Переводим позицию игрока в локальные координаты объекта капсулы
            Vector3 localTarget = capsule.transform.InverseTransformPoint(targetWorldPos);
            Vector3 center = capsule.center;

            // Определяем локальную ось капсулы (0 = X, 1 = Y, 2 = Z)
            Vector3 axis = Vector3.up;
            float radiusScale = 1f;
            float heightScale = 1f;
            Vector3 lossy = capsule.transform.lossyScale;

            switch (capsule.direction)
            {
                case 0: // X-Axis
                    axis = Vector3.right;
                    heightScale = Mathf.Abs(lossy.x);
                    radiusScale = Mathf.Max(Mathf.Abs(lossy.y), Mathf.Abs(lossy.z));
                    break;
                case 1: // Y-Axis
                    axis = Vector3.up;
                    heightScale = Mathf.Abs(lossy.y);
                    radiusScale = Mathf.Max(Mathf.Abs(lossy.x), Mathf.Abs(lossy.z));
                    break;
                case 2: // Z-Axis
                    axis = Vector3.forward;
                    heightScale = Mathf.Abs(lossy.z);
                    radiusScale = Mathf.Max(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y));
                    break;
            }

            // Половина длины цилиндрической части оси (от центра до центра полусферы)
            float worldHeight = capsule.height * heightScale;
            float worldRadius = capsule.radius * radiusScale;
            float halfSegmentWorld = Mathf.Max(0f, worldHeight * 0.5f - worldRadius);
            float halfSegmentLocal = heightScale > 0.0001f ? halfSegmentWorld / heightScale : 0f;

            // Проецируем локальную позицию игрока на отрезок оси [-halfSegmentLocal, +halfSegmentLocal]
            float projection = Vector3.Dot(localTarget - center, axis);
            float clampedProjection = Mathf.Clamp(projection, -halfSegmentLocal, halfSegmentLocal);
            Vector3 closestLocalPoint = center + axis * clampedProjection;

            // Возвращаем точку обратно в мировые координаты
            return capsule.transform.TransformPoint(closestLocalPoint);
        }

        private IEnumerator ReleaseCooldown(NetworkId id, float delay)
        {
            yield return new WaitForSeconds(delay);
            _cooldownIds.Remove(id);
        }
    }
}
