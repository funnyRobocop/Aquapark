using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace NonameGame
{
    [Tooltip("Implements INetworkRunnerCallbacks so various events such as players joining and leaving will trigger different actions.")]
    public class RunnerCallbacks : MonoBehaviour, INetworkRunnerCallbacks
    {
        [Tooltip("The NetworkObject spawned when a player joins the room.")]
        public NetworkObject playerPrefab;

        [Tooltip("MatchManager prefab (must have NetworkObject + MatchManager). Spawned once per session.")]
        public NetworkObject matchManagerPrefab;

        [Inject] private GameModel _gameModel;
        [Inject] private LoadingUI _loadingUI;

        // ─────────────────────────────────────────────────────────────────────
        // Player join / leave
        // ─────────────────────────────────────────────────────────────────────

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            // Спавним только локального игрока
            if (runner.LocalPlayer == player)
            {
                SpawnLocalPlayer(runner);
            }

            // MatchManager спавним один раз — только Shared Mode Master
            TrySpawnMatchManager(runner);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            // Ничего особенного не делаем.
            // MatchManager остаётся жить, StateAuthority мигрирует автоматически.
        }

        // ─────────────────────────────────────────────────────────────────────
        // Scene load
        // ─────────────────────────────────────────────────────────────────────

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            // После LoadScene(Single) все NetworkObject'ы предыдущей сцены despawn'нуты.
            // Локальный игрок тоже — спавним заново, если его объекта нет.
            if (runner.GetPlayerObject(runner.LocalPlayer) == null)
            {
                SpawnLocalPlayer(runner);
            }

            // На всякий случай (если master сменился и MatchManager почему-то отсутствует)
            TrySpawnMatchManager(runner);
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
            // Можно показать loading UI здесь, если нужно
            // _loadingUI?.Show();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Shutdown
        // ─────────────────────────────────────────────────────────────────────

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            _loadingUI?.Show();

            // Выгружаем игровую сцену (не MainMenu)
            if (scene.IsValid() && scene.buildIndex > 0)
            {
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            }
            else
            {
                _loadingUI?.Hide();

                var fm = GameObject.FindFirstObjectByType<FusionNetworkManager>(FindObjectsInactive.Include);
                fm?.ShowShutdown(shutdownReason);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private void SpawnLocalPlayer(NetworkRunner runner)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[RunnerCallbacks] playerPrefab is not assigned!");
                return;
            }

            // Уже есть объект — ничего не делаем
            if (runner.GetPlayerObject(runner.LocalPlayer) != null)
                return;

            int selectedSkinId = _gameModel != null ? _gameModel.SelectedSkinId : 0;

            var newPlayer = runner.Spawn(
                playerPrefab,
                position: Vector3.up,
                inputAuthority: runner.LocalPlayer,
                onBeforeSpawned: (spawnRunner, spawnedObject) =>
                {
                    var skin = spawnedObject.GetComponent<PlayerView>();
                    if (skin != null)
                        skin.InitializeBeforeSpawn(selectedSkinId);
                    else
                        Debug.LogError("[RunnerCallbacks] Add PlayerView to the Player prefab root.");
                });

            runner.SetPlayerObject(runner.LocalPlayer, newPlayer);
        }

        private void TrySpawnMatchManager(NetworkRunner runner)
        {
            if (matchManagerPrefab == null)
            {
                Debug.LogWarning("[RunnerCallbacks] matchManagerPrefab is not assigned!");
                return;
            }

            // Уже есть
            if (MatchManager.Instance != null)
                return;

            // Спавнить может только Shared Mode Master
            if (!runner.IsSharedModeMasterClient)
                return;

            runner.Spawn(matchManagerPrefab);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Unused callbacks (required by interface)
        // ─────────────────────────────────────────────────────────────────────

        public void OnConnectedToServer(NetworkRunner runner) { }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

        public void OnInput(NetworkRunner runner, NetworkInput input) { }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    }
}