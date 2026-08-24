using Fusion;
using UnityEngine;

[Tooltip("Behaviour that makes the object oscillate between two points on a sine curve.")]
public class OscillatingNetworkBehaviour : NetworkBehaviour
{
    [Networked(), Tooltip("The oscillating value that makes the object move on a sine curve.")]
    public float OscillatingValue { get; set; }

    [SerializeField, Tooltip("The rate at which the object oscillates.")]
    float oscillateRate;

    [SerializeField, Tooltip("The minimum local position of the object.")]
    Vector3 localMin;

    [SerializeField, Tooltip("The maximum local position of the object.")]
    Vector3 localMax;

    [SerializeField, Tooltip("The axis, in local space, at which the object spins.")]
    public Vector3 spinAxis;

    [SerializeField, Tooltip("The rate at which the object spins; use 0 if it does not spin.")]
    public float spinRate;

    /// <summary>
    /// Cached reference to the Transform.
    /// </summary>
    Transform cachedTransform;

    private void Awake()
    {
        cachedTransform = transform;
    }

    public override void Spawned()
    {
        base.Spawned();

        
        if (HasStateAuthority)
        {
            // Randomizes an intial starting point between 0 and 2PI.
            OscillatingValue = Mathf.PI * 2 * Random.value;
        }
    }

    public override void FixedUpdateNetwork()
    {
        OscillatingValue += Runner.DeltaTime * oscillateRate;

        float percent = 0.5f + 0.5f * Mathf.Sin(OscillatingValue);

        cachedTransform.localPosition = Vector3.Lerp(localMin, localMax, percent);
        cachedTransform.Rotate(spinAxis, spinRate * Runner.DeltaTime, Space.Self);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.parent.TransformPoint(localMin), transform.parent.TransformPoint(localMax));
    }
}
