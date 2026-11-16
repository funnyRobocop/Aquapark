using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace NonameGame
{
    public class Trampoline : MonoBehaviour
    {
        [Header("Настройки батута")]
        [SerializeField] private float bounceStrength = 2f;
        [SerializeField] private float minBounceForce = 16f;
        [SerializeField] private float maxBounceForce = 24f;
        [SerializeField] private float controlLockTime = 0.35f;
        [SerializeField] private string playerTag = "Player";

        [Header("Направление вылета")]
        [Tooltip("Локальная ось объекта, перпендикулярная поверхности батута (обычно (0, 1, 0))")]
        [SerializeField] private Vector3 localBounceAxis = Vector3.up;
        [Tooltip("Дополнительный вертикальный подброс, чтобы игрок не цеплялся за край следующего батута")]
        [SerializeField] private float extraUpwardLift = 0.25f;

        private readonly List<Rigidbody> _rigidbodies = new List<Rigidbody>();
        private readonly List<float> _velocities = new List<float>();

        private void Start()
        {
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag(playerTag))
                return;

            var rb = collision.rigidbody;
            if (rb == null)
                rb = collision.transform.GetComponentInParent<Rigidbody>();

            if (rb == null)
                return;

            var netObj = rb.GetComponentInParent<NetworkObject>();
            if (netObj == null || !netObj.HasStateAuthority)
                return;

            // Берем скорость падения из списка (если сработал верхний триггер) или из текущей скорости столкновения
            float fallingSpeed = Mathf.Abs(collision.relativeVelocity.y);
            int index = _rigidbodies.IndexOf(rb);
            if (index >= 0)
            {
                fallingSpeed = Mathf.Max(fallingSpeed, Mathf.Abs(_velocities[index]));
            }

            float calculatedForce = bounceStrength * fallingSpeed;
            float finalForce = Mathf.Clamp(calculatedForce, minBounceForce, maxBounceForce);

            // Вычисляем вектор строго перпендикулярно наклону шляпки батута
            Vector3 bounceDir = transform.TransformDirection(localBounceAxis.normalized);
            bounceDir += Vector3.up * extraUpwardLift;
            bounceDir.Normalize();

            Vector3 launchVelocity = bounceDir * finalForce;

            var controller = rb.GetComponentInParent<PlayerController>();
            if (controller != null)
            {
                controller.LaunchFromTrampoline(launchVelocity, controlLockTime);
            }
            else
            {
                rb.linearVelocity = launchVelocity;
            }
        }

        public void Add(Rigidbody rb, float velocityY)
        {
            int index = _rigidbodies.IndexOf(rb);
            if (index < 0)
            {
                _rigidbodies.Add(rb);
                _velocities.Add(velocityY);
            }
            else
            {
                _velocities[index] = velocityY;
            }
        }

        public void Remove(Rigidbody rb)
        {
            int index = _rigidbodies.IndexOf(rb);
            if (index < 0)
                return;

            _rigidbodies.RemoveAt(index);
            _velocities.RemoveAt(index);
        }

        // Визуализация направления вылета в окне Scene
        private void OnDrawGizmosSelected()
        {
            Vector3 dir = transform.TransformDirection(localBounceAxis.normalized);
            dir = (dir + Vector3.up * extraUpwardLift).normalized;

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, dir * 3f);
            Gizmos.DrawSphere(transform.position + dir * 3f, 0.2f);
        }
    }
}
