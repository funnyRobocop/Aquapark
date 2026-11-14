using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NonameGame
{
    public class MatchManager : NetworkBehaviour
    {
        public static MatchManager Instance { get; private set; }

        [Networked] public int CurrentLevelIndex { get; set; }
        [Networked] public int RoundNumber { get; set; }

        // Build indexes: 1, 2, 3 → циклически
        [SerializeField] private int[] levelBuildIndexes = { 1, 2, 3 };

        public override void Spawned()
        {
            if (Instance != null && Instance != this)
            {
                // Уже есть — этот лишний
                if (Object.HasStateAuthority)
                    Runner.Despawn(Object);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Если это самый первый спавн — можно инициализировать индекс
            // (иначе берём уже зареплицированное значение)
            if (Object.HasStateAuthority && RoundNumber == 0 && CurrentLevelIndex == 0)
            {
                // Синхронизируем индекс с реально загруженной сценой
                int loaded = SceneManager.GetActiveScene().buildIndex;
                for (int i = 0; i < levelBuildIndexes.Length; i++)
                {
                    if (levelBuildIndexes[i] == loaded)
                    {
                        CurrentLevelIndex = i;
                        break;
                    }
                }
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Вызывается из InGameManager после результатов.
        /// </summary>
        public void RequestNextLevel()
        {
            if (!Object.HasStateAuthority) return;

            CurrentLevelIndex = (CurrentLevelIndex + 1) % levelBuildIndexes.Length;
            RoundNumber++;

            RPC_LoadLevel(CurrentLevelIndex);
        }

        public void RequestSameLevelAgain()
        {
            if (!Object.HasStateAuthority) return;
            RoundNumber++;
            RPC_LoadLevel(CurrentLevelIndex);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_LoadLevel(int levelIndex)
        {
            int buildIndex = levelBuildIndexes[Mathf.Clamp(levelIndex, 0, levelBuildIndexes.Length - 1)];

            // Опционально: показать loading UI перед сменой сцены
            // LoadingUI.Instance?.Show();

            Runner.LoadScene(SceneRef.FromIndex(buildIndex), LoadSceneMode.Single);
        }

        /// <summary>
        /// Удобный хелпер: какой build index сейчас активен.
        /// </summary>
        public int GetCurrentBuildIndex()
        {
            if (levelBuildIndexes == null || levelBuildIndexes.Length == 0)
                return 1;
            return levelBuildIndexes[Mathf.Clamp(CurrentLevelIndex, 0, levelBuildIndexes.Length - 1)];
        }
    }
}
