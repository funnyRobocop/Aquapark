using UnityEngine;

namespace NonameGame
{
    public class BoxingGlove : MonoBehaviour
    {
        [Header("Настройки вылета")]
        [Tooltip("Дистанция вылета по локальной оси Z (в метрах)")]
        [SerializeField] private float punchDistance = 5f;

        [Header("Тайминги фаз (в секундах)")]
        [SerializeField] private float cycleDuration = 4f;
        [SerializeField] private float extendDuration = 0.15f;
        [SerializeField] private float holdDuration = 0.35f;
        [SerializeField] private float retractDuration = 0.8f;
        [SerializeField] private float timeOffset = 0f;

        private Vector3 _startLocalPos;
        private Vector3 _prevLocalPos;
        private Vector3 _currentLocalPos;
        private Vector3 _visualBaseLocalPos;
        private Rigidbody _rb;

        private void Awake()
        {
            _startLocalPos = transform.localPosition;
            _prevLocalPos = _startLocalPos;
            _currentLocalPos = _startLocalPos;

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

            Vector3 nextLocal = _startLocalPos + Vector3.forward * (punchDistance * progress);

            _prevLocalPos = _currentLocalPos;
            _currentLocalPos = nextLocal;

            ApplyPosition(_currentLocalPos);
        }

        private void ApplyPosition(Vector3 localPos)
        {
            if (_rb != null)
            {
                Vector3 worldPos = transform.parent != null
                    ? transform.parent.TransformPoint(localPos)
                    : localPos;

                _rb.MovePosition(worldPos);
            }
        }

        private float EvaluateProgress(float timeInCycle)
        {
            if (timeInCycle < extendDuration)
            {
                float t = timeInCycle / Mathf.Max(extendDuration, 0.0001f);
                return 1f - (1f - t) * (1f - t);
            }

            if (timeInCycle < extendDuration + holdDuration)
                return 1f;

            if (timeInCycle < extendDuration + holdDuration + retractDuration)
            {
                float t = (timeInCycle - extendDuration - holdDuration) / Mathf.Max(retractDuration, 0.0001f);
                return Mathf.SmoothStep(1f, 0f, t);
            }

            return 0f;
        }
    }
}
