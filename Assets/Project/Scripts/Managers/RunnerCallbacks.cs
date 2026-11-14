using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace NonameGame
{
    [Tooltip("Implements INetworkRunnerCallbacks so various events such as playings joining and leaving will trigger different actions.")]
    public class RunnerCallbacks : MonoBehaviour, INetworkRunnerCallbacks
    {

        [Tooltip("The Spawned on the Network when a player joins the room.")]
        public NetworkObject playerPrefab; 
        
        [Tooltip("MatchManager prefab (NetworkObject)")]
        public NetworkObject matchManagerPrefab;

        [Inject] private GameModel _gameModel;
        [Inject] private LoadingUI _loadingUI;

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.LocalPlayer != player)
                return;

            int selectedSkinId = _gameModel.SelectedSkinId;
            var newPlayer = runner.Spawn(
                playerPrefab,
                position: Vector3.up,
                inputAuthority: player,
                onBeforeSpawned: (spawnRunner, spawnedObject) =>
                {
                    var skin = spawnedObject.GetComponent<PlayerView>();
                    if (skin != null)
                        skin.InitializeBeforeSpawn(selectedSkinId);
                    else
                        Debug.LogError("Add PlayerView to the Player prefab root.");
                });

            runner.SetPlayerObject(player, newPlayer);

            // Спавним MatchManager только один раз, только master'ом
            if (runner.IsSharedModeMasterClient && MatchManager.Instance == null)
            {
                runner.Spawn(matchManagerPrefab);
            }
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            // Если локальный игрок ещё не имеет объекта — спавним
            if (runner.GetPlayerObject(runner.LocalPlayer) == null)
            {
                int selectedSkinId = _gameModel.SelectedSkinId;
                var newPlayer = runner.Spawn(
                    playerPrefab,
                    position: Vector3.up,
                    onBeforeSpawned: (spawnRunner, spawnedObject) =>
                    {
                        var skin = spawnedObject.GetComponent<PlayerView>();
                        if (skin != null)
                            skin.InitializeBeforeSpawn(selectedSkinId);
                        else
                            Debug.LogError("Add PlayerView to the Player prefab root.");
                    });
                runner.SetPlayerObject(runner.LocalPlayer, newPlayer);
            }
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            // Attempts to unload the gameplay scene.
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("GameplayScene");
            _loadingUI?.Show();
            if (scene.IsValid())
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

        #region Unused Callbacks

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
        }
        
        public void OnConnectedToServer(NetworkRunner runner)
        {

        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {

        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {

        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {

        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {

        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {

        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {

        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {

        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {

        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {

        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {

        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {

        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {

        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {

        }



        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {

        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
        {

        }

        #endregion
    }
}
