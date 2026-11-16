using UnityEngine;

namespace NonameGame
{
    public class Pendulum : MonoBehaviour
    {
        [Header("Настройки маятника")]
        [SerializeField] private float maxAngle = 45f;
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private float timeOffset = 0f;
        [SerializeField] private Vector3 rotationAxis = Vector3.forward;

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

            float angle = Mathf.Sin((t + timeOffset) * speed) * maxAngle;
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
