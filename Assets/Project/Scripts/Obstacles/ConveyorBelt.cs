using UnityEngine;

namespace NonameGame
{
    public class ConveyorBelt : MonoBehaviour
    {
        [Header("Настройки конвейера")]
        [Tooltip("Направление и скорость в локальных координатах объекта (например, (1, 0, 0) для оси X)")]
        [SerializeField] private Vector3 localDirection = Vector3.right;
        [SerializeField] private float speed = 6f;

        public Vector3 GetConveyorVelocity()
        {
            return transform.TransformDirection(localDirection.normalized) * speed;
        }

        private void OnCollisionStay(Collision collision)
        {
            // Проверяем, что столкновение произошло с ногами игрока (нормаль контакта направлена вверх)
            bool isStandingOnTop = false;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y < -0.5f)
                {
                    isStandingOnTop = true;
                    break;
                }
            }

            if (!isStandingOnTop) return;

            var player = collision.collider.GetComponentInParent<PlayerController>();
            if (player != null && player.HasStateAuthority)
            {
                player.SetExternalVelocity(GetConveyorVelocity());
            }
        }
    }
}