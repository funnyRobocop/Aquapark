using Fusion;
using UnityEngine;

[Tooltip("Behaviour for the Starting Points that player return flags to.")]
public class StartingPointBehaviour : MonoBehaviour
{
    [SerializeField(), Tooltip("The materials of the starting point.")]
    public Material[] materials;

    [SerializeField(), Tooltip("The text that displays the player's score.")]
    TMPro.TextMeshPro scoreDisplay;

    [SerializeField(), Tooltip("The text that displays the player's name.")]
    TMPro.TextMeshPro nameDisplay;

    [SerializeField(), Tooltip("The Arrow GameObject to help the local player now where to return flags more clearly.")]
    GameObject arrow;

    /// <summary>
    /// Validate method to assign materials to various items.
    /// </summary>
    private void OnValidate()
    {
        if (materials == null || materials.Length == 0)
            return;

        Material lastMaterial = materials[materials.Length - 1];

        if (scoreDisplay)
            scoreDisplay.color = lastMaterial.color;

        if (nameDisplay)
            nameDisplay.color = lastMaterial.color;

        if (arrow)
            arrow.GetComponent<Renderer>().sharedMaterial = lastMaterial;
    }

    /// <summary>
    /// Updates the score text with the given value.
    /// </summary>
    public void UpdateScore(int newScore)
    {
        scoreDisplay.text = newScore.ToString();
    }

    /// <summary>
    /// Sets the player's name to this Starting Point.
    /// </summary>
    /// <param name="isLocalPlayer">Is the player local; if so, the arrow will be activated as well.</param>
    /// <param name="playerName">The Networked player name</param>
    public void SetPlayerName(bool isLocalPlayer, NetworkString<_128> playerName)
    {
        // This means it's an empty string
        if (playerName.Length == 0)
        {
            nameDisplay.text = isLocalPlayer ? "You" : string.Empty;
        }
        else
        {
            nameDisplay.text = playerName.ToString();
        }

        SetArrowState(isLocalPlayer);
    }

    /// <summary>
    /// Sets the state of the arrow
    /// </summary>
    public void SetArrowState(bool active)
    {
        arrow.SetActive(active);
    }
}