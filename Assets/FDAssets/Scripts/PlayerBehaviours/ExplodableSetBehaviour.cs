using System;
using UnityEngine;

[Tooltip("Holds references to the ExplodableBehaviours that \"Explode\" and \"Assemble\" during gameplay.")]
public class ExplodableSetBehaviour : MonoBehaviour
{
    [SerializeField, Tooltip("Reference to the main renderer for the character that is turned off when they explode.")]
    Renderer mainMeshRenderer;

    [SerializeField, Tooltip("References to all of the objects that explode off a character.")]
    ExplodableBehaviour[] explodables;

    [SerializeField, Tooltip("The velocity at which objects explode off a character")]
    float explosionVelocity;

    private void OnValidate()
    {
        explodables = GetComponentsInChildren<ExplodableBehaviour>(true);
    }

    /// <summary>
    /// Method called when a character explodes.
    /// </summary>
    public void Explode()
    {
        mainMeshRenderer.enabled = false;
        foreach (var explodable in explodables)
            explodable.Explode(explosionVelocity);
    }

    /// <summary>
    /// Method called when a character should be reassembled.
    /// </summary>
    public void Assemble()
    {
        mainMeshRenderer.enabled = true;
        foreach (var explodable in explodables)
            explodable.Assemble();
    }

    /// <summary>
    /// Assigns the materials to the renderers in the player.
    /// </summary>
    /// <param name="materials"></param>
    public void AssignMaterials(Material[] materials)
    {
        mainMeshRenderer.sharedMaterials = materials;
        foreach (var explodables in explodables)
        {
            var renderer = explodables.GetComponentInChildren<MeshRenderer>(true);
            
            if (renderer == null)
                continue;

            // If the renderer only has one material, we assign the last material in the material array.
            if (renderer.sharedMaterials.Length == 1)
                renderer.sharedMaterial = materials[materials.Length - 1];
            else
                renderer.sharedMaterials = materials;
        }
    }
}
