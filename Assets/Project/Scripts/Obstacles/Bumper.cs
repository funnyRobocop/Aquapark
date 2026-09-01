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

            Vector3 bounceDir = other.transform.position - transform.position;
            bounceDir.y = 0f;
            if (bounceDir.sqrMagnitude < 0.001f)
                bounceDir = transform.forward;
            bounceDir.Normalize();
            bounceDir.y = upwardBoost;
            bounceDir.Normalize();

            rb.linearVelocity = Vector3.zero;
            rb.AddForce(bounceDir * bounceForce, ForceMode.Impulse);

            StartCoroutine(ReleaseCooldown(netObj.Id, cooldownTime));
        }

        private IEnumerator ReleaseCooldown(NetworkId id, float delay)
        {
            yield return new WaitForSeconds(delay);
            _cooldownIds.Remove(id);
        }
    }
}
