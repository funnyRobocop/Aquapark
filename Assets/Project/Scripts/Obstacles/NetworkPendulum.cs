using UnityEngine;
using Fusion;

namespace NonameGame
{
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkPendulum : NetworkBehaviour
    {
        [Header("Настройки маятника")]
        [Tooltip("Максимальный угол раскачивания в градусах (в одну сторону)")]
        [SerializeField] private float maxAngle;
        
        [Tooltip("Скорость раскачивания")]
        [SerializeField] private float speed;
        
        [Tooltip("Смещение фазы времени, чтобы маятники качались асинхронно")]
        [SerializeField] private float timeOffset;

        [Header("Ось вращения")]
        [Tooltip("Вдоль какой локальной оси качается маятник? (X, Y или Z)")]
        [SerializeField] private Vector3 rotationAxis;

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

                // Вычисляем угол раскачивания по синусоиде (от -maxAngle до +maxAngle)
                float currentAngle = Mathf.Sin(syncedTime * speed) * maxAngle;

                Quaternion targetRotation = _startRotation * Quaternion.AngleAxis(currentAngle, rotationAxis.normalized);

                _rigidbody.MoveRotation(targetRotation);
            }
        }
    }
}
