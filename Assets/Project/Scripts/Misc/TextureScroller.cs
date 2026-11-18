using UnityEngine;

public class TextureScroller : MonoBehaviour
{
    [SerializeField] private Material targetMaterial;
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.5f, 0f);

    private Vector2 currentOffset;
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");

    private void Start()
    {
        if (targetMaterial != null)
        {
            currentOffset = targetMaterial.GetTextureOffset(BaseMap);
        }
    }

    private void Update()
    {
        if (targetMaterial == null) return;

        currentOffset.x = Mathf.Repeat(currentOffset.x + scrollSpeed.x * Time.unscaledDeltaTime, 1f);
        currentOffset.y = Mathf.Repeat(currentOffset.y + scrollSpeed.y * Time.unscaledDeltaTime, 1f);

        targetMaterial.SetTextureOffset(BaseMap, currentOffset);

        if (targetMaterial.HasProperty(EmissionMap))
        {
            targetMaterial.SetTextureOffset(EmissionMap, currentOffset);
        }
    }
}
