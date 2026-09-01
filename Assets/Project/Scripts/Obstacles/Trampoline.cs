using System.Collections.Generic;
using Fusion;
using UnityEngine;


namespace NonameGame
{
    public class Trampoline : MonoBehaviour
    {
        [Header("Настройки батута")]
        [SerializeField] private float bounceStrength = 2f;
        [SerializeField] private float minBounceForce = 14f;
        [SerializeField] private float maxBounceForce = 24f;
        [SerializeField] private string playerTag = "Player";

        private readonly List<Rigidbody> _rigidbodies = new List<Rigidbody>();
        private readonly List<float> _velocities = new List<float>();

        private void Start()
        {
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag(playerTag))
                return;

            var rb = collision.rigidbody;
            if (rb == null)
                rb = collision.transform.GetComponentInParent<Rigidbody>();

            if (rb == null || !_rigidbodies.Contains(rb))
                return;

            var netObj = rb.GetComponentInParent<NetworkObject>();
            if (netObj == null)
                return;

            // Shared Mode: только владелец своего персонажа
            if (!netObj.HasStateAuthority)
                return;

            int index = _rigidbodies.IndexOf(rb);
            float fallingVelocityY = _velocities[index];

            float calculatedForce = bounceStrength * Mathf.Abs(fallingVelocityY);
            float finalForce = Mathf.Clamp(calculatedForce, minBounceForce, maxBounceForce);

            rb.linearVelocity = Vector3.zero;
            rb.AddForce(transform.up * finalForce, ForceMode.Impulse);
        }

        public void Add(Rigidbody rb, float velocityY)
        {
            int index = _rigidbodies.IndexOf(rb);
            if (index < 0)
            {
                _rigidbodies.Add(rb);
                _velocities.Add(velocityY);
            }
            else
            {
                _velocities[index] = velocityY;
            }
        }

        public void Remove(Rigidbody rb)
        {
            int index = _rigidbodies.IndexOf(rb);
            if (index < 0)
                return;

            _rigidbodies.RemoveAt(index);
            _velocities.RemoveAt(index);
        }
    }
}
