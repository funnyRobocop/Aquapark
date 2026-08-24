using UnityEngine;

[Tooltip("Class that rotates an object on the provided axis.")]
public class SimpleRotator : MonoBehaviour
{
    [SerializeField, Tooltip("The euler angles that the object will rotate in world space.")]
    public Vector3 rotateEuler;

    [SerializeField, Tooltip("The space, world or local (self), that the object will rotate.")]
    public Space space;

    /// <summary>
    /// Reference to the object's transform.
    /// </summary>
    Transform cachedTransform;

    private void Awake()
    {
        cachedTransform = transform;
    }

    void Update()
    {
        cachedTransform.Rotate(rotateEuler * Time.deltaTime, space);
    }
}