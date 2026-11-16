using UnityEngine;

namespace NonameGame
{
    public class GateTrap : MonoBehaviour
    {
        [Header("Настройки вращения")]
        [SerializeField] private float targetAngle = 90f;
        [SerializeField] private Vector3 rotationAxis = Vector3.right;

        [Header("Тайминги фаз (в секундах)")]
        [SerializeField] private float cycleDuration = 4f;
        [SerializeField] private float riseDuration = 0.15f;
        [SerializeField] private float stayDuration = 0.8f;
        [SerializeField] private float fallDuration = 0.8f;
        [SerializeField] private float timeOffset = 0f;

        private Quaternion _startLocalRotation;
        private Quaternion _prevLocalRotation;
        private Quaternion _currentLocalRotation;
        private Quaternion _visualBaseLocalRotation;
        private Rigidbody _rb;

        private void Awake()
        {
            _startLocalRotation = transform.localRotation;
            _prevLocalRotation = _startLocalRotation;
            _currentLocalRotation = _startLocalRotation;

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

            float timeInCycle = Mathf.Repeat(t + timeOffset, cycleDuration);
            float progress = EvaluateProgress(timeInCycle);

            float currentAngle = targetAngle * progress;
            Quaternion nextLocal = _startLocalRotation * Quaternion.AngleAxis(currentAngle, rotationAxis.normalized);

            _prevLocalRotation = _currentLocalRotation;
            _currentLocalRotation = nextLocal;

            ApplyRotation(_currentLocalRotation);
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
        }

        private float EvaluateProgress(float timeInCycle)
        {
            if (timeInCycle < riseDuration)
            {
                float t = timeInCycle / Mathf.Max(riseDuration, 0.0001f);
                return Mathf.SmoothStep(0f, 1f, t);
            }

            if (timeInCycle < riseDuration + stayDuration)
                return 1f;

            if (timeInCycle < riseDuration + stayDuration + fallDuration)
            {
                float t = (timeInCycle - riseDuration - stayDuration) / Mathf.Max(fallDuration, 0.0001f);
                return Mathf.SmoothStep(1f, 0f, t);
            }

            return 0f;
        }
    }
}
