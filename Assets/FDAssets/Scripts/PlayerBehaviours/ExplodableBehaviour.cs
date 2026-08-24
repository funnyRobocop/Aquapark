using UnityEngine;

[Tooltip("Component meant to fly off the player when they explode.  These objects do not need to be networked since they are on a different layer and will not collide with players.")]
public class ExplodableBehaviour : MonoBehaviour
{
    [SerializeField, Tooltip("Reference to the Rigidbody Component")]
    Rigidbody cachedRigidbody;

    [SerializeField, Tooltip("Reference to the Renderer Component")]
    Renderer cachedRenderer;

    [SerializeField, Tooltip("Reference to the Collider")]
    Collider cachedCollider;

    [SerializeField, Tooltip("Reference to the original parent")]
    Transform parent;

    [SerializeField, Tooltip("The original local position")]
    Vector3 originalLocalPosition;

    [SerializeField, Tooltip("The original local rotation")]
    Quaternion originalLocalRotation;

    [SerializeField, Tooltip("If true, this Explodable's renderer will be disabled when the player is assembled.")]
    bool hideOnAssembly;

    private void OnValidate()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        cachedRenderer = GetComponent<Renderer>();
        cachedCollider = GetComponent<Collider>();
        parent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
    }

    public void Explode(float explosionForce)
    {
        transform.SetParent(null, true);
        cachedRigidbody.isKinematic = false;
        cachedRenderer.enabled = true;
        cachedCollider.enabled = true;
        cachedRigidbody.AddExplosionForce(explosionForce, transform.position, 1f, 1f, ForceMode.VelocityChange);
    }

    public void Assemble()
    {
        transform.SetParent(parent);
        cachedRigidbody.isKinematic = true;
        cachedRenderer.enabled = !hideOnAssembly;
        transform.SetLocalPositionAndRotation(originalLocalPosition, originalLocalRotation);
    }
}
