using UnityEngine;

public class CrowdBounce : MonoBehaviour
{
    [SerializeField] private Vector3 targetPositionOffset = new Vector3(0f, 0.3f, 0f);
    [SerializeField] private Vector3 targetRotationOffset = new Vector3(0f, -0.02f, 0f);
    [SerializeField] private float cycleDuration = 0.67f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Start()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    private void Update()
    {
        if (cycleDuration <= 0f) return;

        float phase = (Time.unscaledTime / cycleDuration) * Mathf.PI * 2f;
        float t = (1f - Mathf.Cos(phase)) * 0.5f;

        transform.localPosition = startPosition + targetPositionOffset * t;
        transform.localRotation = startRotation * Quaternion.Euler(targetRotationOffset * t);
    }
}
