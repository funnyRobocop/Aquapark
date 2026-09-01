using UnityEngine;
using Fusion;

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

            float timeInCycle = (t + timeOffset) % cycleDuration;
            float progress = EvaluateProgress(timeInCycle);

            float currentAngle = targetAngle * progress;
            Quaternion targetLocal = _startLocalRotation * Quaternion.AngleAxis(currentAngle, rotationAxis.normalized);

            if (_rb != null)
            {
                Quaternion targetWorld = transform.parent != null
                    ? transform.parent.rotation * targetLocal
                    : targetLocal;

                _rb.MoveRotation(targetWorld);
            }
            else
            {
                transform.localRotation = targetLocal;
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
