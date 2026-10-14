using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace NonameGame
{
    public class SkinPreviewUI : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint;

        private GameObject _currentSpawnedSkinMesh;

        public void SpawnPlayerMesh(PlayerSkinData activeSkinData)
        {
            ClearCurrentSkin();

            activeSkinData.PrefabReference.InstantiateAsync(spawnPoint).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _currentSpawnedSkinMesh = handle.Result;

                    _currentSpawnedSkinMesh.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    _currentSpawnedSkinMesh.transform.localScale = new Vector3(500f, 500f, 500f);
                }
            };
        }

        /// <summary>
        /// Метод для правильной очистки и выгрузки ассета из памяти
        /// </summary>
        private void ClearCurrentSkin()
        {
            if (_currentSpawnedSkinMesh != null)
            {
                // Этот метод одновременно уничтожает GameObject на сцене 
                // и полностью выгружает его ассеты (Mesh, текстуры, материалы) из RAM
                Addressables.Release(_currentSpawnedSkinMesh);
                _currentSpawnedSkinMesh = null;
            }
        }

        private void OnDestroy()
        {
            ClearCurrentSkin();
        }
    }
}
