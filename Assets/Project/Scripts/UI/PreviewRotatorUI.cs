using UnityEngine;
using UnityEngine.EventSystems;


namespace NonameGame
{
    public class PreviewRotatorUI : MonoBehaviour, IDragHandler
    {
        [SerializeField] private Transform preview;

        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 0.4f; // Чувствительность вращения

        public void OnDrag(PointerEventData eventData)
        {
            if (preview == null) return;

            float mouseDeltaX = eventData.delta.x;

            preview.Rotate(Vector3.up, -mouseDeltaX * rotationSpeed);
        }
    }
}
