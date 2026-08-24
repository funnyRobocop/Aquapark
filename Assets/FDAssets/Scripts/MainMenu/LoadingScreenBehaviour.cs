using TMPro;
using UnityEngine;

[Tooltip("Singleton Behaviour that displays the loading screen.")]
public class LoadingScreenBehaviour : MonoBehaviour
{
    public static LoadingScreenBehaviour Instance { get; private set; }


    [SerializeField, Tooltip("References to the Canvas component.")]
    Canvas canvas;

    [SerializeField, Tooltip("References to the CanvasGroup component.")]
    CanvasGroup group;

    [SerializeField, Tooltip("The rate at which the loading screen fades in / out.")]
    public float rate;

    /// <summary>
    /// Is the loading screen currenlty being shown.
    /// </summary>
    bool fadeIn;

    [SerializeField, Tooltip("Text that displays if the game is loading due to an instant join.")]
    TextMeshProUGUI loadingMessageDisplay;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        loadingMessageDisplay.enabled = true;
        loadingMessageDisplay.text = "";
    }

    /// <summary>
    /// Fades in the loading screen
    /// </summary>
    /// <param name="isInstantJoin">Is the loading screen part of an instant join</param>
    public void Show(string loadingMessage)
    {
        enabled = true;
        fadeIn = true;
        canvas.enabled = true;


        loadingMessageDisplay.text = loadingMessage;
    }

    /// <summary>
    /// Fades out the loading screen.
    /// </summary>
    public void Hide(string loadingMessage)
    {
        enabled = true;
        fadeIn = false;

        loadingMessageDisplay.text = loadingMessage;
    }

    private void Update()
    {
        group.alpha += Time.deltaTime * (fadeIn ? rate : -rate);

        if (group.alpha <= 0f || group.alpha >= 1f)
        {
            canvas.enabled = group.alpha > 0f;
            enabled = false;
        }
    }
}