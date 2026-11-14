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

        [SerializeField] private int[] levelBuildIndexes = { 1, 2, 3 };

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

            RPC_LoadLevel(CurrentLevelIndex);
        }

        public void RequestSameLevelAgain()
        {
            if (!Object.HasStateAuthority) return;
            if (_isLoading) return;

            _isLoading = true;
            RoundNumber++;
            RPC_LoadLevel(CurrentLevelIndex);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_LoadLevel(int levelIndex)
        {
            int buildIndex = levelBuildIndexes[Mathf.Clamp(levelIndex, 0, levelBuildIndexes.Length - 1)];

            if (_gameModel != null)
                _gameModel.LevelIndex = buildIndex;

            _loadingUI?.Show();
            _loadingUI?.SetProgress(0.15f);

            // Выгружаем ВСЕ игровые уровни (buildIndex > 0), кроме MainMenu
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.IsValid() && s.buildIndex > 0 && s.buildIndex != buildIndex)
                {
                    Runner.UnloadScene(SceneRef.FromIndex(s.buildIndex));
                }
            }

            // Грузим новый
            Runner.LoadScene(
                SceneRef.FromIndex(buildIndex),
                LoadSceneMode.Additive,
                LocalPhysicsMode.None,
                true
            );
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

        public int GetCurrentBuildIndex()
        {
            if (levelBuildIndexes == null || levelBuildIndexes.Length == 0)
                return 1;
            return levelBuildIndexes[Mathf.Clamp(CurrentLevelIndex, 0, levelBuildIndexes.Length - 1)];
        }
    }
}