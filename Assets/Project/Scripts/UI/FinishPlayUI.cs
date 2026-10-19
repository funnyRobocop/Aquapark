using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;

namespace NonameGame
{
    public class FinishPlayUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private RewardPanel rewardPanel;
        [SerializeField] private Button claimButton;
        [SerializeField] private MainMenuUI mainMenuUI;
        [SerializeField] private Image skinImage;
        [SerializeField] private float maxIconSize = 512f;
        private AsyncOperationHandle<Sprite> _spriteLoadHandle;

        [Inject] private GameModel _gameModel;
        [Inject] private IPlayerSkinLoader _playerSkinLoader;

        
        private void OnSpriteLoaded(AsyncOperationHandle<Sprite> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _spriteLoadHandle = handle;
                Sprite loadedSprite = handle.Result;
                skinImage.sprite = loadedSprite;

                float originalWidth = loadedSprite.rect.width;
                float originalHeight = loadedSprite.rect.height;

                // Вычисляем коэф. масштабирования, чтобы картинка пропорционально вписалась в рамки магазина (maxIconSize)
                float scale = Mathf.Min(maxIconSize / originalWidth, maxIconSize / originalHeight);
                scale = Mathf.Min(scale, 1f); // Не растягиваем мелкие картинки, если они меньше лимита

                // Применяем сжатый пропорциональный размер на UI
                skinImage.rectTransform.sizeDelta = new Vector2(originalWidth * scale, originalHeight * scale);
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);

            int levelId = 0;

            levelText.text = string.Format(levelText.text, _gameModel.SessionFinishCount,
                _gameModel.SessionGoldCount, _gameModel.SessionKickCount, _gameModel.SessionThrowCount);

            _gameModel.UpdateStatsAndProgress(new GameModel.Stats(levelId, _gameModel.SessionFinishCount,
                _gameModel.SessionGoldCount, _gameModel.SessionKickCount, _gameModel.SessionThrowCount));

            var reward = _gameModel.CalculatePoints(_gameModel.SessionFinishCount,
                _gameModel.SessionGoldCount, _gameModel.SessionKickCount, _gameModel.SessionThrowCount);

            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() =>
            {
                rewardPanel.Show(() => { _gameModel.ClaimBattlePassMoney(reward); Hide();  mainMenuUI.SpawnPlayerMesh(); mainMenuUI.Resume();},
                    () => { _gameModel.ClaimBattlePassMoney(reward * 2); Hide();  mainMenuUI.SpawnPlayerMesh(); mainMenuUI.Resume();},
                    reward);
            });

            _gameModel.ClearSessionData();

            var _skinData = _playerSkinLoader.PlayerSkinAll[(PlayerSkin)_gameModel.SelectedSkinId];

            if (_skinData.SpriteReference != null)
            {
                _skinData.SpriteReference.LoadAssetAsync().Completed += OnSpriteLoaded;
            }
        }

        private void Hide()
        {
            gameObject.SetActive(false);

            if (_spriteLoadHandle.IsValid())
            {
                Addressables.Release(_spriteLoadHandle);
            }
        }
    }
}
