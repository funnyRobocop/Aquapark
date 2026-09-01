using UnityEngine;
using System.Collections.Generic;
using Fusion;

namespace NonameGame
{
    public class GravityArea : MonoBehaviour
    {
        [Header("Настройки воздушного потока")]
        [Tooltip("X/Z — множители горизонтальной скорости (0.9–1). Y — целевая вертикальная скорость подъёма (м/с).")]
        [SerializeField] private Vector3 gravityForce = new Vector3(0.95f, 12f, 0.95f);

        [SerializeField] private string playerTag = "Player";

        private readonly List<Rigidbody> _rigidbodies = new List<Rigidbody>();

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        private void FixedUpdate()
        {
            _rigidbodies.RemoveAll(rb => rb == null);

            for (int i = 0; i < _rigidbodies.Count; i++)
            {
                var rb = _rigidbodies[i];
                if (rb == null)
                    continue;

                var netObj = rb.GetComponentInParent<NetworkObject>();
                if (netObj == null || !netObj.HasStateAuthority)
                    continue;

                Vector3 v = rb.linearVelocity;
                rb.linearVelocity = new Vector3(
                    v.x * gravityForce.x,
                    gravityForce.y,
                    v.z * gravityForce.z
                );
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag))
                return;

            var rb = other.attachedRigidbody;
            if (rb == null)
                rb = other.GetComponentInParent<Rigidbody>();

            if (rb != null && !_rigidbodies.Contains(rb))
                _rigidbodies.Add(rb);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag))
                return;

            var rb = other.attachedRigidbody;
            if (rb == null)
                rb = other.GetComponentInParent<Rigidbody>();

            if (rb != null)
                _rigidbodies.Remove(rb);
        }
    }
}
