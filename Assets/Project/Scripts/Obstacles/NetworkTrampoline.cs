using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fusion;

namespace NonameGame
{
    public class NetworkTrampoline : MonoBehaviour
    {
        [Header("Настройки батута")]
        [Tooltip("Множитель силы отскока. Перемножается на скорость падения коровы.")]
        public float bounceStrength = 2f; 

        /*[Tooltip("Время оглушения (блокировки WASD) в полете, чтобы корова взлетела строго вверх")]
        [SerializeField] private float stunTime = 0.45f;*/

        [Header("ОГРАНИЧЕНИЯ ВЫСОТЫ (Fall Guys Стандарт)")]
        [Tooltip("Минимальный пинок вверх, если корова наступила на батут почти без скорости")]
        [SerializeField] private float minBounceForce = 14f;
        
        [Tooltip("МАКСИМАЛЬНЫЙ ПОТОЛОК СИЛЫ ВЗЛЕТА. Защищает от улетания в космос при повторных прыжках!")]
        [SerializeField] private float maxBounceForce = 24f; // Выставите от 20 до 25 под вашу карту!

        private List<Rigidbody> rigidbodies = new List<Rigidbody>();
        private List<float> velocities = new List<float>();
        //private HashSet<Rigidbody> _activeBounces = new HashSet<Rigidbody>();

        private void Start()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                Rigidbody playerRb = collision.transform.GetComponent<Rigidbody>();
                
                if (playerRb != null && rigidbodies.Contains(playerRb) /*&& !_activeBounces.Contains(playerRb)*/)
                {
                    //_activeBounces.Add(playerRb);

                    NetworkObject netObj = playerRb.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        if (netObj.HasInputAuthority || netObj.Runner.IsServer)
                        {
                            var characterManager = playerRb.GetComponent<PlayerController>();
                            if (characterManager != null)
                            {
                                // 1. Включаем сетевой таймер оглушения
                                //characterManager.stunTimer = TickTimer.CreateFromSeconds(netObj.Runner, stunTime);

                                // 2. Извлекаем скорость падения из списков Nappin
                                int targetIndex = rigidbodies.IndexOf(playerRb);
                                float fallingVelocityY = velocities[targetIndex];

                                // 3. Обнуляем текущую скорость Rigidbody перед толчком
                                playerRb.linearVelocity = Vector3.zero;

                                // 4. РАСЧЕТ СИЛЫ С ЖЕСТКИМ ОГРАНИЧЕНИЕМ ПОТОЛКА:
                                // Сначала считаем динамическую силу по формуле Nappin
                                float calculatedForce = bounceStrength * Mathf.Abs(fallingVelocityY);
                                
                                // ЖЕЛЕЗОБЕТОННЫЙ МАНЕВР LERPMINMAX:
                                // Функция Clamp намертво зажимает силу взлета в наши границы!
                                // Корова никогда не прыгнет выше, чем maxBounceForce
                                float finalVerticalForce = Mathf.Clamp(calculatedForce, minBounceForce, maxBounceForce);

                                Vector3 bounceImpulse = transform.up * finalVerticalForce;

                                // 5. Стреляем импульсом силы PhysX в небо!
                                playerRb.AddForce(bounceImpulse, ForceMode.Impulse);

                                // Включаем флаг анимации махов копытами
                                //characterManager.netDashAnimationFlag = true;

                                Debug.Log($"[Батут Ограничитель] Взлет зафиксирован! Расчетная сила была: {calculatedForce}, Применена сжатая сила: {finalVerticalForce} (Макс лимит: {maxBounceForce})");
                            }
                        }
                    }

                    //StartCoroutine(ReleasePlayerRoutine(playerRb, stunTime));
                }
            }
        }

        /*private IEnumerator ReleasePlayerRoutine(Rigidbody rb, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_activeBounces.Contains(rb))
            {
                _activeBounces.Remove(rb);
            }
        }*/

        #region Handle list Nappin (Без изменений)
        public void Add(Rigidbody _rb, float _velocity_y)
        {
            if (!rigidbodies.Contains(_rb))
            {
                rigidbodies.Add(_rb);
                velocities.Add(_velocity_y);
            }
            else
            {
                int index = rigidbodies.IndexOf(_rb);
                velocities[index] = _velocity_y;
            }
        }

        public void Remove(Rigidbody _rb)
        {
            if (rigidbodies.Contains(_rb))
            {
                int index = rigidbodies.IndexOf(_rb);
                rigidbodies.RemoveAt(index);
                velocities.RemoveAt(index);
            }
        }
        #endregion
    }
}
