using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NonameGame
{
    public class InputHandler : NetworkBehaviour, INetworkRunnerCallbacks
    {

        private bool _wasSpacePressedLastFrame = false;

        public override void Spawned()
        {
            if (Object.HasInputAuthority)
            {
                Runner.AddCallbacks(this);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (runner != null)
                runner.RemoveCallbacks(this);
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            if (!Object.HasInputAuthority) return;

            var data = new NetworkInputData();

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Vector2 move = Vector2.zero;

                if (keyboard[Key.W].isPressed) move.y += 1f;
                if (keyboard[Key.S].isPressed) move.y -= 1f;
                if (keyboard[Key.A].isPressed) move.x -= 1f;
                if (keyboard[Key.D].isPressed) move.x += 1f;

                data.Move = move.normalized;

                bool isSpaceCurrentlyPressed = keyboard[Key.Space].isPressed;

                if (isSpaceCurrentlyPressed && !_wasSpacePressedLastFrame)
                {
                    data.SpacePressed = true;
                }
                else
                {
                    data.SpacePressed = false;
                }

                _wasSpacePressedLastFrame = isSpaceCurrentlyPressed;
            }

            if (Camera.main != null)
            {
                Debug.Log(Camera.main.transform.eulerAngles.y);
                data.CameraRotationY = Camera.main.transform.eulerAngles.y;
            }

            input.Set(data);
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
    }

    public struct NetworkInputData : INetworkInput
    {
        public Vector2 Move;
        public float CameraRotationY;

        public NetworkBool SpacePressed;
    }
}