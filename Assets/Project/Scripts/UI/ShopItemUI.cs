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
            //TryLoadSprite();
        }

        private void OnEnable()
        {
            if (clickButton != null)
                clickButton.onClick.AddListener(OnClickButtonClicked);

            skinImage.enabled = false;
            //TryLoadSprite();
        }

        private void TryLoadSprite()
        {
            if (SkinData == null || SkinData.SpriteReference == null)
                return;

            // Если спрайт уже стоит — ничего не делаем
            if (skinImage.sprite != null)
                return;

            // Если handle уже в процессе загрузки — тоже выходим
            if (_spriteLoadHandle.IsValid())
                return;

            priceLabel.text = SkinData.Price.ToString() + "$";
            SkinData.SpriteReference.LoadAssetAsync().Completed += OnSpriteLoaded;
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
                skinImage.enabled = true;
            }
        }

        private void OnClickButtonClicked()
        {
            OnSkinClicked?.Invoke(SkinData, this);
        }

        public void SetClicked(bool isClicked)
        {
            clickedIndicator.SetActive(isClicked);
        }

        public void SetPurchased(bool isPurchased)
        {
            purchasedIndicator.SetActive(isPurchased);
        }

        private void OnDisable()
        {
            if (clickButton != null)
                clickButton.onClick.RemoveListener(OnClickButtonClicked);

            if (skinImage != null)
                skinImage.sprite = null;

            if (_spriteLoadHandle.IsValid())
            {
                Addressables.Release(_spriteLoadHandle);
                _spriteLoadHandle = default;
            }
        }
    }
}
