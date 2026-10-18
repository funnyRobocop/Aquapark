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

        public void Hide()
        {
            _currentSpawnedSkinMesh?.SetActive(false);
        }

        public void Show()
        {
            _currentSpawnedSkinMesh.SetActive(true);
        }

        public void SpawnPlayerMesh(PlayerSkinData activeSkinData)
        {
            ClearCurrentSkin();

            activeSkinData.PrefabReference.InstantiateAsync(spawnPoint).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _currentSpawnedSkinMesh = handle.Result; 
                    var skinAnimator = _currentSpawnedSkinMesh.GetComponent<Animator>();
                    _animator.avatar = skinAnimator.avatar;
                    skinAnimator.enabled = false;
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

        private void OnDestroy()
        {
            ClearCurrentSkin();
        }
    }
}
