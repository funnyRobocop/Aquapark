using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

[Tooltip("Implements INetworkRunnerCallbacks so various events such as playings joining and leaving will trigger different actions.")]
public class RunnerCallbacks : MonoBehaviour, INetworkRunnerCallbacks
{
    [Tooltip("The Spawned on the Network when a player joins the room.")]
    public NetworkObject playerPrefab;

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.LocalPlayer != player)
            return;

        var newPlayer = runner.Spawn(playerPrefab, position: Vector3.up, inputAuthority: player);

        AudioManager.AssignLocalPlayer(newPlayer.transform);

        runner.SetPlayerObject(player, newPlayer);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.LocalPlayer == player)
            AudioManager.AssignLocalPlayer(null);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        AudioManager.AssignLocalPlayer(null);

        // Attempts to unload the gameplay scene.
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("GameplayScene");
        LoadingScreenBehaviour.Instance.Show("Runner Shuttong Down");
        if (scene.IsValid())
        {
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
        }
        else
        {
            LoadingScreenBehaviour.Instance.Hide("Returning To Main Menu");

            var fm = GameObject.FindFirstObjectByType<FusionNetworkManager>(FindObjectsInactive.Include);
            fm?.ShowShutdown(shutdownReason);
        }
    }

    #region Unused Callbacks
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

    public void OnSceneLoadDone(NetworkRunner runner)
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

#endregion
}
