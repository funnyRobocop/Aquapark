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
        [SerializeField] private TextMeshProUGUI crownsText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private RewardUI rewardPanel;
        [SerializeField] private Button claimButton;
        [SerializeField] private MainMenuUI mainMenuUI;
        [SerializeField] private Image skinImage;
        [SerializeField] private float maxIconSize = 512f;

        private AsyncOperationHandle<Sprite> _spriteLoadHandle;
        private PlayerSkinData _skinData;

        [Inject] private GameModel _gameModel;
        [Inject] private IPlayerSkinLoader _playerSkinLoader;

        public void Show()
        {
            gameObject.SetActive(true);

            int levelId = 0;

            var crowns = _gameModel.CalculatePoints(_gameModel.SessionFinishCount,
                _gameModel.SessionGoldCount, _gameModel.SessionKickCount, _gameModel.SessionThrowCount);
            crownsText.text = "+" + crowns;

            levelText.text = string.Format(levelText.text, _gameModel.SessionFinishCount,
                _gameModel.SessionGoldCount, _gameModel.SessionKickCount, _gameModel.SessionThrowCount);

            _gameModel.UpdateStatsAndProgress(new GameModel.Stats(levelId, _gameModel.SessionFinishCount,
                _gameModel.SessionGoldCount, _gameModel.SessionKickCount, _gameModel.SessionThrowCount));

            var reward = _gameModel.CalculatePoints(_gameModel.SessionFinishCount,
                _gameModel.SessionGoldCount, _gameModel.SessionKickCount, _gameModel.SessionThrowCount);

            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() =>
            {
                rewardPanel.Show(() => { _gameModel.ClaimBattlePassMoney(reward); Hide(); mainMenuUI.Resume(); },
                    () => { _gameModel.ClaimBattlePassMoney(reward * 2); Hide(); mainMenuUI.Resume(); },
                    reward);
            });

            _gameModel.ClearSessionData();

            _skinData = _playerSkinLoader.PlayerSkinAll[(PlayerSkin)_gameModel.SelectedSkinId];

            skinImage.enabled = false;
            TryLoadSprite();
        }

        private void TryLoadSprite()
        {
            if (_skinData == null || _skinData.SpriteReference == null)
                return;

            // Уже загружен
            if (skinImage.sprite != null)
            {
                skinImage.enabled = true;
                return;
            }

            // Уже идёт загрузка
            if (_spriteLoadHandle.IsValid())
                return;

            _skinData.SpriteReference.LoadAssetAsync().Completed += OnSpriteLoaded;
        }

        private void OnSpriteLoaded(AsyncOperationHandle<Sprite> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _spriteLoadHandle = handle;
                Sprite loadedSprite = handle.Result;
                skinImage.sprite = loadedSprite;

                float originalWidth = loadedSprite.rect.width;
                float originalHeight = loadedSprite.rect.height;

                float scale = Mathf.Min(maxIconSize / originalWidth, maxIconSize / originalHeight);
                scale = Mathf.Min(scale, 1f);

                skinImage.rectTransform.sizeDelta = new Vector2(originalWidth * scale, originalHeight * scale);
                skinImage.enabled = true;
            }
        }

        private void Hide()
        {
            if (skinImage != null)
            {
                skinImage.sprite = null;
                skinImage.enabled = false;
            }

            if (_spriteLoadHandle.IsValid())
            {
                Addressables.Release(_spriteLoadHandle);
                _spriteLoadHandle = default;
            }

            gameObject.SetActive(false);
        }
    }
}