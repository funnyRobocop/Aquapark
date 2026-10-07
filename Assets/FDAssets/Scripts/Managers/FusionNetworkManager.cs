using Fusion;
using Fusion.Photon.Realtime;
using NonameGame;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

/// <summary>
/// Manager that handles connecting to Photon Fusion
/// </summary>
public class FusionNetworkManager : MonoBehaviour
{
    /// <summary>
    /// Static reference to the runner.
    /// </summary>
    public static NetworkRunner Runner { get; private set; }

    [SerializeField, Tooltip("Reference to the Crazy Manager.")]
    CrazyManager crazyManager;

    [SerializeField, Tooltip("The NetworkRunner prefab that will be instantiated to start a session.")]
    NetworkRunner networkRunnerPrefab;

    [SerializeField, Tooltip("Reference to the main canvas.")]
    Canvas mainMenuCanvas;

    [SerializeField, Tooltip("Reference to the main canvas group.")]
    CanvasGroup mainMenuCanvasGroup;

    [SerializeField, Tooltip("The current region code.  An empty string will direct Fusion to find the best region.")]
    string currentRegion = string.Empty;

    [SerializeField, Tooltip("Panel displayed when the connection fails.")]
    GameObject connectionFailedPanel;

    [SerializeField, Tooltip("Text that displays the connection failure reason.")]
    TextMeshProUGUI connectionFailText;

    [Inject] private LoadingUI _loadingUI;

    /// <summary>
    /// The available regions for this game
    /// </summary>
    string[] availableRegions = new string[]
    {
        string.Empty,
        "eu",
        "us",
        "usw",
        "sa",
        "asia",
    };

    private void Awake()
    {
        Application.targetFrameRate = 60;
        SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        connectionFailedPanel.SetActive(false);
    }

    internal void ShowShutdown(ShutdownReason shutdownReason)
    {
        mainMenuCanvas.enabled = true;
        mainMenuCanvasGroup.alpha = 1f;
        mainMenuCanvasGroup.interactable = true;

        // Displays the reason the connection failed.
        connectionFailText.text = shutdownReason.ToString();
        connectionFailedPanel.SetActive(true);
    }

    /// <summary>
    /// Only the gameplay scene is ever unloaded, so the assumption is that the game is returning to the main menu.
    /// </summary>
    /// <param name="arg0"></param>
    private void SceneManager_sceneUnloaded(Scene arg0)
    {
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.enabled = true;
            mainMenuCanvasGroup.alpha = 1f;
            mainMenuCanvasGroup.interactable = true;
        }

        _loadingUI?.Hide();

        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// When the Start New Game button is pressed, a new session code is made to prevent joining an open room.
    /// </summary>
    public void OnStartNewGamePressed()
    {
        string session = System.Guid.NewGuid().ToString();
        StartSession(false, false, PhotonAppSettings.Global.AppSettings.AppVersion, currentRegion, session);
    }

    /// <summary>
    /// When Quick Join is pressed, the player will join any available room in the requested region.
    /// </summary>
    public void OnQuickJoinPressed()
    {
        StartSession(false, false, PhotonAppSettings.Global.AppSettings.AppVersion, currentRegion, string.Empty);
    }

    /// <summary>
    /// Changes the region based on the main menu's dropdown choice.
    /// </summary>
    /// <param name="choice">The choice</param>
    public void OnChangeRegion(int choice)
    {
        currentRegion = availableRegions[choice];
    }

    /// <summary>
    /// Loads the game
    /// </summary>
    /// <param name="isInstantJoin">If true, this is an instant join.</param>
    /// <param name="appVersion">The version of the game</param>
    /// <param name="region">The region the game is in.  An empty string will make the game try to join its best region.</param>
    /// <param name="session">The session or room name.  An empty string will utilize matchmaking to join any room</param>
    public async void StartSession(bool isInstantJoin, bool fromInvite, string appVersion, string region, string session)
    {
        // The loading screen is shown
        string loadingMessage;
        if (fromInvite)
        {
            loadingMessage = "Joining Room";
        }
        else if (isInstantJoin)
        {
            loadingMessage = "Creating Room";
        }
        else
        {
            loadingMessage = string.Empty;
        }
        _loadingUI?.Show();

        // The main menu is no longer interactable
        mainMenuCanvasGroup.interactable = false;

        // The current menu item is cleared to prevent space from triggering the previosly pressed button.
        EventSystem.current.SetSelectedGameObject(null);

        // The new runner is created
        Runner = Instantiate(networkRunnerPrefab);

        // App settings for region and version are set.
        if (region == null)
            region = string.Empty;
        PhotonAppSettings.Global.AppSettings.FixedRegion = region;
        PhotonAppSettings.Global.AppSettings.AppVersion = appVersion;

        // Scene info that will load scene 1 is setup.
        NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(SceneRef.FromIndex(1), LoadSceneMode.Additive, activeOnLoad: true);

        var objectProvider = new VContainerFusionObjectProvider(() =>
        {
            var gameScope = LifetimeScope.Find<GameScope>();
            if (gameScope == null)
            {
                Debug.LogError("GameScope не был найден на загруженной сцене!");
                return null;
            }
            return gameScope.Container;
        });

        StartGameArgs startGameArgs = new StartGameArgs()
        {
            SessionName = session,
            GameMode = GameMode.Shared,
            PlayerCount = 8,
            Scene = sceneInfo,
            ObjectProvider = objectProvider
        };

        // We wait for the runner to start the game
        var results = await Runner.StartGame(startGameArgs);

        // If the game has loaded properly, main menu canvas will be disabled.
        // Otherwise, we will return to the main menu.
        if (results.Ok)
        {
            mainMenuCanvas.enabled = false;
            mainMenuCanvasGroup.alpha = 0f;
        }
        else
        {
            ShowShutdown(results.ShutdownReason);
        }

        _loadingUI?.Hide();
    }
}

/// <summary>
/// Кастомный провайдер объектов для интеграции Photon Fusion 2 и VContainer.
/// Наследуется от стандартного провайдера Fusion 2.
/// </summary>
public class VContainerFusionObjectProvider : Fusion.NetworkObjectProviderDefault
{
    private readonly Func<VContainer.IObjectResolver> _containerResolver;

    public VContainerFusionObjectProvider(Func<VContainer.IObjectResolver> containerResolver)
    {
        _containerResolver = containerResolver;
    }

    // Перехватываем стандартный спавн префаба во Fusion 2
    protected override NetworkObject InstantiatePrefab(NetworkRunner runner, NetworkObject prefab)
    {
        // 1. Позволяем Fusion 2 штатно инстанцировать префаб
        var instance = base.InstantiatePrefab(runner, prefab);

        if (instance != null)
        {
            // 2. Вызываем нашу ленивую лямбду — на этом этапе аддитивная сцена уже загружена в память
            var container = _containerResolver?.Invoke();

            if (container != null)
            {
                // 3. Автоматически инжектим зависимости во все компоненты на заспавненном объекте игрока
                var components = instance.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var monoBehaviour in components)
                {
                    container.Inject(monoBehaviour);
                }
            }
        }

        return instance;
    }
}
