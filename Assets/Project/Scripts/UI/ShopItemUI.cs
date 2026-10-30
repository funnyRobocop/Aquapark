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
        private AsyncOperationHandle<Sprite> _spriteLoadHandle;

        [SerializeField] private Button clickButton;
        [SerializeField] private GameObject clickedIndicator;
        [SerializeField] private GameObject purchasedIndicator;
        [SerializeField] private Image skinImage;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private float maxIconSize = 512f;

        public Button ClickButton => clickButton;
        public PlayerSkinData SkinData { get; private set; }

        public event Action<PlayerSkinData, ShopItemUI> OnSkinClicked;

        public void Initialize(PlayerSkinData skinData)
        {
            SkinData = skinData;
            skinImage.enabled = false;
            skinImage.sprite = null;
            priceLabel.text = SkinData.Price.ToString() + "$";
        }

        private void OnEnable()
        {
            if (clickButton != null)
                clickButton.onClick.AddListener(OnClickButtonClicked);
        }

        public void LoadSprite()
        {
            if (SkinData == null || SkinData.SpriteReference == null)
                return;

            // Уже загружен
            if (skinImage.sprite != null)
                return;

            // Уже идёт загрузка
            if (_spriteLoadHandle.IsValid())
                return;

            // Сохраняем handle СРАЗУ, а не в колбэке
            _spriteLoadHandle = SkinData.SpriteReference.LoadAssetAsync();
            _spriteLoadHandle.Completed += OnSpriteLoaded;
        }

        public void UnloadSprite()
        {
            if (skinImage != null)
            {
                skinImage.sprite = null;
                skinImage.enabled = false;
            }

            if (_spriteLoadHandle.IsValid())
            {
                // Отписываемся, чтобы колбэк не прилетел после выгрузки
                _spriteLoadHandle.Completed -= OnSpriteLoaded;
                Addressables.Release(_spriteLoadHandle);
                _spriteLoadHandle = default;
            }
        }

        private void OnSpriteLoaded(AsyncOperationHandle<Sprite> handle)
        {
            // Защита от устаревших колбэков
            if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded)
                return;

            // Если уже выгрузили — игнорируем
            if (!_spriteLoadHandle.IsValid())
                return;

            Sprite loadedSprite = handle.Result;
            if (loadedSprite == null || skinImage == null)
                return;

            skinImage.sprite = loadedSprite;

            float originalWidth = loadedSprite.rect.width;
            float originalHeight = loadedSprite.rect.height;

            float scale = Mathf.Min(maxIconSize / originalWidth, maxIconSize / originalHeight);
            scale = Mathf.Min(scale, 1f);

            skinImage.rectTransform.sizeDelta = new Vector2(originalWidth * scale, originalHeight * scale);
            skinImage.enabled = true;
        }

        private void OnClickButtonClicked()
        {
            OnSkinClicked?.Invoke(SkinData, this);
        }

        public void SetClicked(bool isClicked)
        {
            if (clickedIndicator != null)
                clickedIndicator.SetActive(isClicked);
        }

        public void SetPurchased(bool isPurchased)
        {
            if (purchasedIndicator != null)
                purchasedIndicator.SetActive(isPurchased);
        }

        private void OnDisable()
        {
            if (clickButton != null)
                clickButton.onClick.RemoveListener(OnClickButtonClicked);

            // На всякий случай чистим при любом выключении
            UnloadSprite();
        }

        private void OnDestroy()
        {
            UnloadSprite();
        }
    }
}