using Fusion;
using Fusion.Photon.Realtime;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace NonameGame
{
    public class FusionNetworkManager : MonoBehaviour
    {
        public static NetworkRunner Runner { get; private set; }

        [SerializeField] CrazyManager crazyManager;
        [SerializeField] NetworkRunner networkRunnerPrefab;
        [SerializeField] MainMenuUI mainMenuUI;
        [SerializeField] FinishPlayUI finishPlayUI;
        [SerializeField] string currentRegion = string.Empty;
        [SerializeField] GameObject connectionFailedPanel;
        [SerializeField] TextMeshProUGUI connectionFailText;

        [Inject] private IObjectResolver _resolver;
        [Inject] private LoadingUI _loadingUI;
        [Inject] private GameModel _gameModel;

        string[] availableRegions = { string.Empty, "eu", "us", "usw", "sa", "asia" };

        // Флаг: сцена загрузилась (ставится через SceneManager.sceneLoaded)
        private bool _levelSceneLoaded;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
            SceneManager.sceneLoaded   += SceneManager_sceneLoaded;   // ← хук на загрузку сцены
            connectionFailedPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;
            SceneManager.sceneLoaded   -= SceneManager_sceneLoaded;
        }

        // Когда Fusion загрузил сцену (buildIndex 1) — сцена загружена на 100%
        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex == 1)
                _levelSceneLoaded = true;
        }

        internal void ShowShutdown(ShutdownReason shutdownReason)
        {
            mainMenuUI.Canvas.enabled = true;
            mainMenuUI.Camera.gameObject.SetActive(true);

            if (shutdownReason == ShutdownReason.Ok)
            {
                if (_gameModel.SessionFinishCount > 0) finishPlayUI.Show();
                else { mainMenuUI.Resume(); _gameModel.ClearSessionData(); }
                return;
            }
            connectionFailText.text = shutdownReason.ToString();
            connectionFailedPanel.SetActive(true);
        }

        private void SceneManager_sceneUnloaded(Scene arg0)
        {
            if (mainMenuUI.Canvas != null)
            {
                mainMenuUI.Canvas.enabled = true;
                mainMenuUI.Camera.gameObject.SetActive(true);
            }
            _loadingUI?.Hide();
            Cursor.lockState = CursorLockMode.None;
        }

        public void OnStartNewGamePressed()
        {
            string session = Guid.NewGuid().ToString();
            StartSession(false, false, PhotonAppSettings.Global.AppSettings.AppVersion, currentRegion, session);
        }

        public void OnQuickJoinPressed()
        {
            StartSession(false, false, PhotonAppSettings.Global.AppSettings.AppVersion, currentRegion, string.Empty);
        }

        public void OnChangeRegion(int choice)
        {
            currentRegion = availableRegions[choice];
        }

        public async void StartSession(bool isInstantJoin, bool fromInvite, string appVersion, string region, string session)
        {
            _levelSceneLoaded = false;
            _loadingUI?.Show();
            _loadingUI?.SetProgress(0f);

            EventSystem.current.SetSelectedGameObject(null);

            Runner = Instantiate(networkRunnerPrefab);

            var callbacks = Runner.GetComponent<RunnerCallbacks>();
            if (callbacks != null) _resolver.Inject(callbacks);

            if (region == null) region = string.Empty;
            PhotonAppSettings.Global.AppSettings.FixedRegion = region;
            PhotonAppSettings.Global.AppSettings.AppVersion  = appVersion;

            NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(1), LoadSceneMode.Additive, activeOnLoad: true);

            var objectProvider = new VContainerFusionObjectProvider(() =>
            {
                var gameScope = LifetimeScope.Find<GameScope>();
                if (gameScope == null) { Debug.LogError("GameScope не найден!"); return null; }
                return gameScope.Container;
            });

            // Запускаем анимацию слайдера параллельно с StartGame
            var progressCoroutine = StartCoroutine(AnimateLoadingProgress());

            StartGameArgs startGameArgs = new StartGameArgs()
            {
                SessionName    = session,
                GameMode       = GameMode.Shared,
                PlayerCount    = 8,
                Scene          = sceneInfo,
                ObjectProvider = objectProvider,
            };

            var results = await Runner.StartGame(startGameArgs);

            StopCoroutine(progressCoroutine);
            _loadingUI?.SetProgress(1f);

            if (results.Ok)
            {
                mainMenuUI.Canvas.enabled = false;
                mainMenuUI.Camera.gameObject.SetActive(false);
            }
            else
            {
                ShowShutdown(results.ShutdownReason);
            }

            _loadingUI?.Hide();
        }

        /// <summary>
        /// Анимация слайдера в два этапа:
        /// 1) 0% → 35% — подключение к Photon (медленно, ~3с)
        /// 2) 35% → 95% — ждём срабатывания SceneManager.sceneLoaded (быстро)
        /// 3) после StartGame - ставим 100%
        /// </summary>
        private IEnumerator AnimateLoadingProgress()
        {
            // Этап 1: Photon handshake (0 → 0.35) за ~3 сек
            float t = 0f;
            const float connectDuration = 3f;
            const float connectTarget   = 0.35f;

            while (t < connectDuration && !_levelSceneLoaded)
            {
                t += Time.deltaTime;
                float p = EaseOut(t / connectDuration) * connectTarget;
                _loadingUI?.SetProgress(p);
                yield return null;
            }

            if (_levelSceneLoaded)
            {
                _loadingUI?.SetProgress(0.95f);
                yield break;
            }

            // Этап 2: загрузка сцены (0.35 → 0.95)
            // Прогресс ползёт быстро, пока sceneLoaded не сработает
            float sceneT    = 0f;
            const float sceneDuration  = 5f;   // максимальное время ожидания
            const float sceneStart     = 0.35f;
            const float sceneEnd       = 0.95f;

            while (!_levelSceneLoaded && sceneT < sceneDuration)
            {
                sceneT += Time.deltaTime;
                float ratio = EaseOut(sceneT / sceneDuration);
                float p = sceneStart + ratio * (sceneEnd - sceneStart);
                _loadingUI?.SetProgress(p);
                yield return null;
            }

            _loadingUI?.SetProgress(0.95f);
        }

        // Функция плавности: быстро в начале, медленно в конце
        private static float EaseOut(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 2f);
    }

    public class VContainerFusionObjectProvider : Fusion.NetworkObjectProviderDefault
    {
        private readonly Func<VContainer.IObjectResolver> _containerResolver;

        public VContainerFusionObjectProvider(Func<VContainer.IObjectResolver> containerResolver)
        {
            _containerResolver = containerResolver;
        }

        protected override NetworkObject InstantiatePrefab(NetworkRunner runner, NetworkObject prefab)
        {
            var instance = base.InstantiatePrefab(runner, prefab);
            if (instance != null)
            {
                var container = _containerResolver?.Invoke();
                if (container != null)
                {
                    var components = instance.GetComponentsInChildren<MonoBehaviour>(true);
                    foreach (var mb in components)
                        container.Inject(mb);
                }
            }
            return instance;
        }
    }
}
