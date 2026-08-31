using UnityEngine;
using Fusion;

namespace NonameGame
{
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkGateTrap : NetworkBehaviour
    {
        [Header("Настройки вращения ворот")]
        [Tooltip("Целевой угол открытия в градусах")]
        [SerializeField] private float targetAngle = 90f;   
        [Tooltip("Локальная ось, вокруг которой вращаются ворота (например, Vector3.right)")]
        [SerializeField] private Vector3 rotationAxis = Vector3.right; 

        [Header("Тайминги фаз (в секундах)")]
        [SerializeField] private float cycleDuration = 4f;   // Длительность всего цикла
        [SerializeField] private float riseDuration = 0.15f; // Время резкого хлопка (взлета)
        [SerializeField] private float stayDuration = 0.8f;  // Сколько ворота стоят открытыми
        [SerializeField] private float fallDuration = 0.8f;  // Время плавного закрытия
        [SerializeField] private float timeOffset = 0f;      // Смещение фазы времени для асинхронности

        private Quaternion _startRotation;
        private Rigidbody _rigidbody;

        public override void Spawned()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _startRotation = transform.localRotation;
        }

        public override void FixedUpdateNetwork()
        {
            if (_rigidbody == null) return;

            if (Runner.IsServer)
            {
                float syncedTime = Runner.SimulationTime + timeOffset;
                float timeInCycle = syncedTime % cycleDuration;
                float progress = 0f;

                if (timeInCycle < riseDuration)
                {
                    float t = timeInCycle / riseDuration;
                    progress = Mathf.SmoothStep(0f, 1f, t);
                }
                else if (timeInCycle < riseDuration + stayDuration)
                {
                    progress = 1f;
                }
                else if (timeInCycle < riseDuration + stayDuration + fallDuration)
                {
                    float t = (timeInCycle - (riseDuration + stayDuration)) / fallDuration;
                    progress = Mathf.SmoothStep(1f, 0f, t);
                }
                else
                {
                    progress = 0f;
                }

                float currentAngle = targetAngle * progress;
                Quaternion targetLocalRotation = _startRotation * Quaternion.AngleAxis(currentAngle, rotationAxis.normalized);

                Quaternion targetWorldRotation = transform.parent != null 
                    ? transform.parent.rotation * targetLocalRotation 
                    : targetLocalRotation;

                _rigidbody.MoveRotation(targetWorldRotation);
            }
        }
    }
}
