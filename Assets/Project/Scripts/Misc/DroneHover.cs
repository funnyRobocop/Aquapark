using UnityEngine;

namespace NonameGame
{
    public class DroneHover : MonoBehaviour
    {
        [Header("Настройки смещения (Позиция)")]
        [Tooltip("Максимальная амплитуда смещения по осям X, Y, Z")]
        public Vector3 positionAmount = new Vector3(0.2f, 0.15f, 0.2f);
        [Tooltip("Скорость покачивания по позициям")]
        public float positionSpeed = 1.5f;

        [Header("Настройки наклона (Вращение)")]
        [Tooltip("Максимальный угол наклона по осям X, Y, Z")]
        public Vector3 rotationAmount = new Vector3(5f, 3f, 5f);
        [Tooltip("Скорость изменения наклона")]
        public float rotationSpeed = 2f;

        // Стартовые позиции и вращение, относительно которых будет происходить движение
        private Vector3 startPosition;
        private Quaternion startRotation;

        // Индивидуальное смещение времени для каждого дрона, чтобы они не двигались синхронно
        private float seedX;
        private float seedY;
        private float seedZ;

        void Start()
        {
            // Запоминаем начальную позицию дрона
            startPosition = transform.localPosition;
            startRotation = transform.localRotation;

            // Генерируем случайное смещение, чтобы каждый дрон парил по-своему
            seedX = Random.Range(0f, 100f);
            seedY = Random.Range(100f, 200f);
            seedZ = Random.Range(200f, 300f);
        }

        void Update()
        {
            // Рассчитываем время для шума с учетом индивидуального сида
            float timeX = Time.time * positionSpeed + seedX;
            float timeY = Time.time * positionSpeed + seedY;
            float timeZ = Time.time * positionSpeed + seedZ;

            // Получаем плавные случайные значения от -1 до 1 с помощью шума Перлина
            float noiseX = Mathf.PerlinNoise(timeX, 0f) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(0f, timeY) * 2f - 1f;
            float noiseZ = Mathf.PerlinNoise(timeZ, timeZ) * 2f - 1f;

            // Считаем новое смещение позиции
            Vector3 offsetPosition = new Vector3(
                noiseX * positionAmount.x,
                noiseY * positionAmount.y,
                noiseZ * positionAmount.z
            );

            // Применяем позицию относительно стартовой точки
            transform.localPosition = startPosition + offsetPosition;

            // Рассчитываем шум для вращения (с другой скоростью и сидами)
            float rotTimeX = Time.time * rotationSpeed + seedZ;
            float rotTimeY = Time.time * rotationSpeed + seedX;
            float rotTimeZ = Time.time * rotationSpeed + seedY;

            float rotNoiseX = Mathf.PerlinNoise(rotTimeX, 0f) * 2f - 1f;
            float rotNoiseY = Mathf.PerlinNoise(0f, rotTimeY) * 2f - 1f;
            float rotNoiseZ = Mathf.PerlinNoise(rotTimeZ, rotTimeZ) * 2f - 1f;

            // Создаем кватернион поворота на основе шума
            Quaternion offsetRotation = Quaternion.Euler(
                rotNoiseX * rotationAmount.x,
                rotNoiseY * rotationAmount.y,
                rotNoiseZ * rotationAmount.z
            );

            // Применяем вращение относительно стартового
            transform.localRotation = startRotation * offsetRotation;
        }
    } 
}
