using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace NonameGame
{
    public class MatchManager : NetworkBehaviour
    {
        public static MatchManager Instance { get; private set; }

        [Networked] public int CurrentLevelIndex { get; set; }
        [Networked] public int RoundNumber { get; set; }

        [SerializeField] private int[] levelBuildIndexes = { 3, 2, 1 };

        [Inject] private GameModel _gameModel;
        [Inject] private LoadingUI _loadingUI;
        
        private bool _isLoading;

        public override void Spawned()
        {
            if (Instance != null && Instance != this)
            {
                if (Object.HasStateAuthority)
                    Runner.Despawn(Object);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Синхронизируем индекс с реально загруженной сценой (первый спавн)
            if (Object.HasStateAuthority && RoundNumber == 0)
            {
                SyncIndexWithLoadedScene();
            }

            _isLoading = false;
        }

        public void NotifySceneLoaded()
        {
            _isLoading = false;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this)
                Instance = null;
        }

        public void RequestNextLevel()
        {
            if (!Object.HasStateAuthority) return;
            if (_isLoading) return;

            _isLoading = true;

            CurrentLevelIndex = (CurrentLevelIndex + 1) % levelBuildIndexes.Length;
            RoundNumber++;

            int levelIndex = CurrentLevelIndex;
            int buildIndex = levelBuildIndexes[levelIndex];

            // 1. Сказать всем показать loading + обновить LevelIndex
            RPC_OnLevelLoadStarted(levelIndex, buildIndex);

            // 2. Выгрузить старые уровни (только authority)
            UnloadAllGameLevelsExcept(buildIndex);

            // 3. Загрузить новый уровень (только authority) — Fusion сам синхронизирует
            Runner.LoadScene(
                SceneRef.FromIndex(buildIndex),
                LoadSceneMode.Additive,
                LocalPhysicsMode.None,
                true
            );
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_OnLevelLoadStarted(int levelIndex, int buildIndex)
        {
            if (_gameModel != null)
                _gameModel.LevelIndex = buildIndex;

            _loadingUI?.Show();
            _loadingUI?.SetProgress(0.15f);
        }

        private void UnloadAllGameLevelsExcept(int keepBuildIndex)
        {
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!s.IsValid()) continue;
                if (s.buildIndex <= 0) continue;          // MainMenu не трогаем
                if (s.buildIndex == keepBuildIndex) continue;

                Runner.UnloadScene(SceneRef.FromIndex(s.buildIndex));
            }
        }

        private void SyncIndexWithLoadedScene()
        {
            int loaded = SceneManager.GetActiveScene().buildIndex;
            for (int i = 0; i < levelBuildIndexes.Length; i++)
            {
                if (levelBuildIndexes[i] == loaded)
                {
                    CurrentLevelIndex = i;
                    if (_gameModel != null)
                        _gameModel.LevelIndex = loaded;
                    break;
                }
            }
        }
    }
}