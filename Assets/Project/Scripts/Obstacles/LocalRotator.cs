using UnityEngine;

namespace NonameGame
{
    public class LocalRotator : MonoBehaviour
    {
        [Header("Настройки локального вращения")]
        [Tooltip("Скорость вращения в градусах в секунду")]
        [SerializeField] private float rotationSpeed = 150f;

        [Tooltip("Вращение по часовой стрелке?")]
        [SerializeField] private bool clockWise = true;

        [Tooltip("Локальная ось, вокруг которой будет крутиться вентилятор")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up; // По умолчанию крутится вокруг оси Y (вверх)

        private void Update()
        {
            // Вычисляем направление знака вращения
            float directionSign = clockWise ? 1f : -1f;

            // Вычисляем угол поворота строго под частоту кадров (Time.deltaTime)
            float angleThisFrame = rotationSpeed * directionSign * Time.deltaTime;

            // Бесконечно вращаем графический меш вентилятора в локальных координатах
            transform.Rotate(rotationAxis.normalized, angleThisFrame, Space.Self);
        }
    }
}
