using UnityEngine;

namespace NonameGame
{
    public class Rotator : MonoBehaviour
    {
        [Header("Настройки вращения")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float timeOffset = 0f;
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        private Quaternion _startLocalRotation;
        private Rigidbody _rb;

        private void Awake()
        {
            _startLocalRotation = transform.localRotation;

            _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                _rb.isKinematic = true;
                _rb.useGravity = false;
            }
        }

        private void FixedUpdate()
        {
            float t = SimulationTimeProvider.Instance != null
                ? SimulationTimeProvider.Time
                : Time.time;

            float angle = Mathf.Repeat((t + timeOffset) * rotationSpeed, 360f);
            Quaternion nextLocal = _startLocalRotation * Quaternion.AngleAxis(angle, rotationAxis.normalized);

            ApplyRotation(nextLocal);
        }

        private void ApplyRotation(Quaternion localRot)
        {
            if (_rb != null)
            {
                Quaternion worldRot = transform.parent != null
                    ? transform.parent.rotation * localRot
                    : localRot;

                _rb.MoveRotation(worldRot);
            }
            else
            {
                transform.localRotation = localRot;
            }
        }
    }
}
