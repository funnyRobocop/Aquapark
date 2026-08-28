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
        private bool _wasDashPressedLastFrame = false;

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

                data.MoveDirection = move.normalized;
                data.DashPressed = keyboard[Key.LeftShift].isPressed;

                bool isSpaceCurrentlyPressed = keyboard[Key.Space].isPressed;

                if (isSpaceCurrentlyPressed && !_wasSpacePressedLastFrame)
                {
                    data.JumpPressed = true;
                }
                else
                {
                    data.JumpPressed = false;
                }

                _wasSpacePressedLastFrame = isSpaceCurrentlyPressed;

                bool isDashCurrentlyPressed = keyboard[Key.LeftShift].isPressed;

                if (isDashCurrentlyPressed && !_wasDashPressedLastFrame)
                {
                    data.DashPressed = true;
                }
                else
                {
                    data.DashPressed = false;
                }

                _wasDashPressedLastFrame = isDashCurrentlyPressed;
            }

            if (Camera.main != null)
            {
                data.CameraRotationY = Camera.main.transform.eulerAngles.y;
            }

            input.Set(data);
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            throw new NotImplementedException();
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            throw new NotImplementedException();
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            throw new NotImplementedException();
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            throw new NotImplementedException();
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            throw new NotImplementedException();
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            throw new NotImplementedException();
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            throw new NotImplementedException();
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            throw new NotImplementedException();
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
            throw new NotImplementedException();
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            throw new NotImplementedException();
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
            throw new NotImplementedException();
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            throw new NotImplementedException();
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            throw new NotImplementedException();
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            throw new NotImplementedException();
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
            throw new NotImplementedException();
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            throw new NotImplementedException();
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            throw new NotImplementedException();
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
            throw new NotImplementedException();
        }

        /*public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
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
        public void OnSceneLoadStart(NetworkRunner runner) { }*/
    }

    public struct NetworkInputData : INetworkInput
    {
        public Vector2 MoveDirection;
        public NetworkBool JumpPressed;
        public float CameraRotationY;

        public NetworkBool DashPressed;
    }
}

