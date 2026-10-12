using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

namespace NonameGame
{
    public class ShopItemUI : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject selectedIndicator;
        [SerializeField] private GameObject choosedIndicator;
        [SerializeField] private Image skinImage;
        [SerializeField] private float maxIconSize = 512f;

        public Button SelectButton => selectButton;

        private PlayerSkin _playerSkin;
        private PlayerSkinData _skinData;

        private IPlayerSkinLoader _playerSkinLoader;

        // Храним хэндл загрузки, чтобы выгрузить из памяти при удалении объекта
        private AsyncOperationHandle<Sprite> _spriteLoadHandle;

        public event Action<PlayerSkinData, ShopItemUI> OnSkinSelected;

        public PlayerSkin PlayerSkin => _playerSkin;

        public void Initialize(PlayerSkin skinType, IPlayerSkinLoader playerSkinLoader)
        {
            _playerSkin = skinType;
            _playerSkinLoader = playerSkinLoader;
            _skinData = _playerSkinLoader.PlayerSkinAll[skinType];

            selectButton.onClick.AddListener(OnSelectButtonClicked);

            if (_skinData.SpriteReference != null)
            {
                _skinData.SpriteReference.LoadAssetAsync().Completed += OnSpriteLoaded;
            }
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

                // Вычисляем коэф. масштабирования, чтобы картинка пропорционально вписалась в рамки магазина (maxIconSize)
                float scale = Mathf.Min(maxIconSize / originalWidth, maxIconSize / originalHeight);
                scale = Mathf.Min(scale, 1f); // Не растягиваем мелкие картинки, если они меньше лимита

                // Применяем сжатый пропорциональный размер на UI
                skinImage.rectTransform.sizeDelta = new Vector2(originalWidth * scale, originalHeight * scale);
            }
        }

        private void OnSelectButtonClicked()
        {
            OnSkinSelected?.Invoke(_skinData, this);
        }

        public void SetSelected(bool isSelected)
        {
            selectedIndicator.SetActive(isSelected);
        }

        public void SetChoosed(bool isChoosed)
        {
            choosedIndicator.SetActive(isChoosed);
        }

        private void OnDestroy()
        {
            selectButton.onClick.RemoveListener(OnSelectButtonClicked);

            if (_spriteLoadHandle.IsValid())
            {
                Addressables.Release(_spriteLoadHandle);
            }
        }
    }
}
