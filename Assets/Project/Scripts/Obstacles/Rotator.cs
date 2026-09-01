using UnityEngine;


namespace NonameGame
{
    public class Rotator : MonoBehaviour
    {
        [Header("Настройки вращения")]
        [SerializeField] private float rotationSpeed = 90f; // градусов в секунду
        [SerializeField] private float timeOffset = 0f;
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        private Quaternion _startRotation;
        private Rigidbody _rb;

        private void Awake()
        {
            _startRotation = transform.rotation;
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

            float currentAngle = (t + timeOffset) * rotationSpeed;
            Quaternion targetRotation = _startRotation * Quaternion.AngleAxis(currentAngle, rotationAxis.normalized);

            if (_rb != null)
                _rb.MoveRotation(targetRotation);
            else
                transform.rotation = targetRotation;
        }
    }
}
