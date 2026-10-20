using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace NonameGame
{
    public class SkinPreviewUI : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Animator _animator;

        private GameObject _currentSpawnedSkinMesh;
        private PlayerSkin playerSkin;
        private Quaternion defaultRotate;

        void Awake()
        {
            defaultRotate = spawnPoint.localRotation;
        }

        public void Hide()
        {
            _currentSpawnedSkinMesh?.SetActive(false);
        }

        public void Show(float yPos, float scale)
        {
            _currentSpawnedSkinMesh?.SetActive(true);
            spawnPoint.localPosition = new Vector3(0f, yPos, -100f);
            spawnPoint.localScale = Vector3.one * scale;
            spawnPoint.localRotation = defaultRotate;
        }

        public void ResetRotation()
        {
            spawnPoint.localRotation = defaultRotate;
        }

        public void SpawnPlayerMesh(PlayerSkinData activeSkinData)
        {
            if (activeSkinData.SkinId == playerSkin)
                return;

            ClearCurrentSkin();

            activeSkinData.PrefabReference.InstantiateAsync(spawnPoint).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _currentSpawnedSkinMesh = handle.Result; 
                    var skinAnimator = _currentSpawnedSkinMesh.GetComponent<Animator>();
                    _animator.avatar = skinAnimator.avatar;
                    //_animator.SetTrigger("Preview");
                    skinAnimator.enabled = false;
                    playerSkin = activeSkinData.SkinId;
                }
            };
        }

        /// <summary>
        /// Метод для очистки и выгрузки ассета из памяти
        /// </summary>
        public void ClearCurrentSkin()
        {
            if (_currentSpawnedSkinMesh != null)
            {
                Addressables.ReleaseInstance(_currentSpawnedSkinMesh);
                _currentSpawnedSkinMesh = null;
            }
        }

        private void OnDisable()
        {
            ClearCurrentSkin();
        }
    }
}
