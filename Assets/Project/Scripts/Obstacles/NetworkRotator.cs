using UnityEngine;
using Fusion;

namespace NonameGame
{
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkRotator : NetworkBehaviour
    {
        [Header("Настройки вращения")]
        [Tooltip("Скорость вращения в градусах в секунду")]
        [SerializeField] private float rotationSpeed;
        
        [Tooltip("Смещение фазы времени для асинхронности")]
        [SerializeField] private float timeOffset = 0f;

        private Rigidbody _rigidbody;
        private Quaternion _startRotation;

        public override void Spawned()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _startRotation = transform.rotation;
        }

        public override void FixedUpdateNetwork()
        {
            if (_rigidbody == null) return;

            if (Runner.IsServer)
            {
                float syncedTime = Runner.SimulationTime + timeOffset;

                float currentAngle = syncedTime * rotationSpeed;

                Quaternion targetRotation = _startRotation * Quaternion.Euler(0f, currentAngle, 0f);

                _rigidbody.MoveRotation(targetRotation);
            }
        }
    }
}
