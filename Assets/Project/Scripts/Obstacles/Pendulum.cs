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

        [Header("Visual (опционально)")]
        [SerializeField] private Transform visualMesh;

        private Quaternion _startRotation;
        private Quaternion _prevRotation;
        private Quaternion _currentRotation;
        private Rigidbody _rb;

        private void Awake()
        {
            _startRotation = transform.rotation;
            _prevRotation = _startRotation;
            _currentRotation = _startRotation;

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
            Quaternion next = _startRotation * Quaternion.AngleAxis(angle, rotationAxis.normalized);

            _prevRotation = _currentRotation;
            _currentRotation = next;

            if (_rb != null)
                _rb.MoveRotation(_currentRotation);
            else
                transform.rotation = _currentRotation;
        }

        private void LateUpdate()
        {
            float alpha = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
            alpha = Mathf.Clamp01(alpha);

            Quaternion visualRot = Quaternion.Slerp(_prevRotation, _currentRotation, alpha);

            if (visualMesh != null)
                visualMesh.rotation = visualRot;
            else
                transform.rotation = visualRot;
        }
    }
}
