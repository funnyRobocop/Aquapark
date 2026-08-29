using Unity.Cinemachine;
using UnityEngine;

namespace NonameGame
{
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _playerCamera;
        [SerializeField] private CinemachineCamera[] _allRagdollCameras;

        public void InitForPlayer(Transform playerTransform)
        {
            _playerCamera.Target.TrackingTarget = playerTransform;
            _playerCamera.Priority = 10;
        }

        public void InitForRagdoll(Transform ragdollTransform)
        {
            foreach (var cam in _allRagdollCameras)
            {
                //cam.Target.TrackingTarget = ragdollTransform;
                cam.Target.LookAtTarget = ragdollTransform;
            }
        }

        public void SwitchOnRagdollCamera(CinemachineCamera _activeCamera)
        {
            SwitchOffAllRagdollCameras();

            if (_activeCamera != null) _activeCamera.Priority = 20;
        }

        public void SwitchOnRagdollCamera(int cameraIndex)
        {
            SwitchOffAllRagdollCameras();

            if (cameraIndex >= 0 && cameraIndex < _allRagdollCameras.Length)
            {
                _allRagdollCameras[cameraIndex].Priority = 20;
            }
        }

        public void SwitchOffAllRagdollCameras()
        {
            foreach (var cam in _allRagdollCameras)
            {
                if (cam != null) cam.Priority = 0;
            }
        }
    }
}
