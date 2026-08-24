using UnityEngine;

[Tooltip("Manager that handles updating music and playing various sound effects.")]
public class AudioManager : MonoBehaviour
{
    /// <summary>
    /// Singleton reference for the audio manager.
    /// </summary>
    public static AudioManager Instance { get; private set; }

    [SerializeField, Tooltip("AudioSource that handles playing the active version of the game's music.")]
    AudioSource musicAS;

    [SerializeField, Tooltip("AudioSource that handles playing the low pass filtered version of the game's music.")]
    AudioSource lowPassMusicAS;

    [SerializeField, Tooltip("AudioSource that handles playing the victory SFX.")]
    AudioSource victoryAS;

    [SerializeField, Tooltip("AudioSource that handles playing the loss SFX.")]
    AudioSource lossAS;

    /// <summary>
    /// The current game state detected by the Audio Manager.
    /// </summary>
    InGameManager.GameState gameState = InGameManager.GameState.Waiting;

    /// <summary>
    /// The volume shifting velocity used to adjust volume with SmoothDamp.
    /// </summary>
    float volumeVelocity;

    // Has the local player been assigned.
    bool hasLocalPlayer;

    /// <summary>
    /// Reference to the transform of the local player.
    /// </summary>
    Transform localPlayer;

    /// <summary>
    /// The cached transform reference.
    /// </summary>
    Transform cachedTransform;

    /// <summary>
    /// The rate at which the music fades in seconds.
    /// </summary>
    float musicFadeRate = 0.5f;

    /// <summary>
    /// Sets up the AudioManager on awake.
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        cachedTransform = transform;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Sets the GameState of the AudioManager
    /// </summary>
    /// <param name="gameState"></param>
    public static void SetGameState(InGameManager.GameState gameState)
    {
        Instance.gameState = gameState;
    }
    
    /// <summary>
    /// Sets the Transform reference for the local player.
    /// </summary>
    /// <param name="localPlayer">The transform that the AudioManager will track during gameplay</param>
    public static void AssignLocalPlayer(Transform localPlayer)
    {
        Instance.localPlayer = localPlayer;
        Instance.hasLocalPlayer = localPlayer != null;
        if (!Instance.hasLocalPlayer)
        {
            Instance.cachedTransform.position = Vector3.zero;
        }
    }

    /// <summary>
    /// Updates the volume over time
    /// </summary>
    private void Update()
    {
        // By default, the main music is not playing, so it's volume goal is 0; during gameplay, the goal is 1.
        float volumeGoal = 0f;
        if (gameState == InGameManager.GameState.Game)
            volumeGoal = 1f;

        float previousVolume = musicAS.volume;
        float volume = Mathf.SmoothDamp(previousVolume, volumeGoal, ref volumeVelocity, musicFadeRate);

        // If the previous volume and main volume have changed, we update the music AudioSources' volumes.
        if (!Mathf.Approximately(previousVolume, volume))
        {
            musicAS.volume = volume;

            // The low pass volume is one minus the main volume.
            lowPassMusicAS.volume = 1f - volume;
        }

        // If a local player has been assigned, the AudioManager will move to its position so its spatial sound if the loudest.
        if (hasLocalPlayer)
            cachedTransform.position = localPlayer.position;
    }

    /// <summary>
    /// Play an SFX when the match ends
    /// </summary>
    /// <param name="isWinner">If true, the victory sound players; if false, the lose sound plays.</param>
    public static void PlayerResultSFX(bool isWinner)
    {
        if (isWinner)
            Instance.victoryAS.Play();
        else
            Instance.lossAS.Play();
    }
}