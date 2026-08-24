using TMPro;
using UnityEngine;

[Tooltip("Behaviour that handles toggling the AudioListener's volume.")]
public class AudioToggleBehaviour : MonoBehaviour
{
    [SerializeField, Tooltip("Reference to the TextMeshProUGUI Component to display the sound state.")]
    TextMeshProUGUI audioButtonText;

    [SerializeField, Tooltip("Audio displayed when the audio is on.")]
    string audioOnText = "AUDIO: ON";

    [SerializeField, Tooltip("Audio displayed when the audio is off.")]
    string audioOffText = "AUDIO: OFF";

    [SerializeField, Tooltip("The maximum audio volume of the game."), Range(0f,1f)]
    float audioMax = 0.75f;

    /// <summary>
    /// Is the audio for the game onor off?
    /// </summary>
    bool audioOn = true;

    private void Awake()
    {
        AudioListener.volume = audioMax;
        SetAudioText();
    }

    /// <summary>
    /// Called through a Button on the main menu.
    /// </summary>
    public void ToggleAudio()
    {
        audioOn = !audioOn;
        AudioListener.volume = audioOn ? audioMax : 0f;
        SetAudioText();
    }
    private void SetAudioText()
    {
        if (audioButtonText == null)
            return;
        audioButtonText.text = audioOn ? audioOnText : audioOffText;
    }
}