using UnityEngine;
using System.Collections.Generic;
using Fusion;

namespace NonameGame
{
    public class NetworkGravityArea : NetworkBehaviour
    {
        [Header("Настройки воздушного потока")]
        [Tooltip("Модификатор скорости. Ось Y — это скорость подъема (м/с). Оси X и Z — множители торможения бега (обычно от 0.9 до 1).")]
        public Vector3 gravityForce = new Vector3(0.95f, 12f, 0.95f);

        private List<Rigidbody> rigidbodies = new List<Rigidbody>();

        public override void Spawned()
        {
            // Железно проверяем, что коллайдер зоны вентилятора является триггером
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        public override void FixedUpdateNetwork()
        {
            rigidbodies.RemoveAll(item => item == null);

            if (Runner.IsServer && rigidbodies.Count > 0)
            {
                for (int i = 0; i < rigidbodies.Count; i++)
                {
                    Rigidbody rb = rigidbodies[i];
                    if (rb == null) continue;

                    //NetworkObject netObj = rb.GetComponent<NetworkObject>();
                    //var characterManager = rb.GetComponent<PhysicsPlayerController>();

                    //if (netObj != null && characterManager != null)
                    {
                        // Слегка притормаживаем корову по бокам (X и Z) и принудительно выталкиваем вверх по Y
                        Vector3 currentVel = rb.linearVelocity;
                        rb.linearVelocity = new Vector3(
                            currentVel.x * gravityForce.x, 
                            gravityForce.y, 
                            currentVel.z * gravityForce.z
                        );
                    }
                }
            }
        }        

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null && !rigidbodies.Contains(rb)) 
                {
                    rigidbodies.Add(rb);
                    Debug.Log($"[Вентилятор] Игрок {other.gameObject.name} вошел в зону восходящего потока.");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null && rigidbodies.Contains(rb)) 
                {
                    rigidbodies.Remove(rb);
                    Debug.Log($"[Вентилятор] Игрок {other.gameObject.name} покинул зону потока.");
                }
            }
        }
    }
}
