using UnityEngine;

namespace NonameGame
{
    public class AdaptiveCamera : MonoBehaviour
    {
        public Transform target;
        public float baseFOV = 35f;
        public float minFOV = 28f;
        public float maxFOV = 50f;

        Camera cam;

        void Awake() => cam = GetComponent<Camera>();

        void LateUpdate()
        {
            if (target == null) return;

            // Камера всегда смотрит на персонажа
            Vector3 pos = transform.position;
            pos.x = target.position.x;
            transform.position = pos;

            // Адаптация FOV
            float aspect = (float)Screen.width / Screen.height;
            float targetAspect = 16f / 9f;

            float fov = baseFOV;

            if (aspect > targetAspect)          // широкий экран
                fov = baseFOV * (aspect / targetAspect);
            else                                // высокий экран
                fov = baseFOV * (targetAspect / aspect);

            cam.fieldOfView = Mathf.Clamp(fov, minFOV, maxFOV);
        }
    }
}
