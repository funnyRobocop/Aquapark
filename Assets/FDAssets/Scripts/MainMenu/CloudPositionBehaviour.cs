using UnityEngine;

[Tooltip("Manages the position of the clouds on the main menu and gameplay to prevent the clouds from obscuring main menu UI.")]
public class CloudPositionBehaviour : MonoBehaviour
{
    [SerializeField, Tooltip("The position of the cloud parent on the main menu.")]
    public Vector3 mainMenuPosition;

    [SerializeField, Tooltip("The position of the cloud parent while in the game scene")]
    public Vector3 gameplayPosition;

    /// <summary>
    /// The position goal of the cloud parent
    /// </summary>
    Vector3 positionGoal;

    /// <summary>
    /// The velocity of the cloud parent when using smooth damp.
    /// </summary>
    Vector3 positionVel;

    [SerializeField, Tooltip("The time in seconds to reach the goal")]
    float smoothTime;

    /// <summary>
    /// Cached reference to the transform.
    /// </summary>
    Transform cachedTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cachedTransform = transform;
        positionGoal = mainMenuPosition;
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
    }

    private void SceneManager_activeSceneChanged(UnityEngine.SceneManagement.Scene oldScene, UnityEngine.SceneManagement.Scene newScene)
    {
        enabled = true;
        if (newScene.name == "MainMenu")
            positionGoal = mainMenuPosition;
        else
            positionGoal = gameplayPosition;
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
    }

    private void Update()
    {
        cachedTransform.position = Vector3.SmoothDamp(cachedTransform.position, positionGoal, ref positionVel, smoothTime);

        // Test to try and stop updating this behaviour when the position is close to the goal position.
        if ((positionGoal - cachedTransform.position).sqrMagnitude <= 0.0001f)
            enabled = false;
    }
}
