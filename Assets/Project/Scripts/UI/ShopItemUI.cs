using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace NonameGame
{
    public class ShopItemUI : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private Image skinImage;

        private PlayerSkin _skinType;
        private PlayerSkinData _skinData;

        private IPlayerSkinLoader _playerSkinLoader;

        public event Action<PlayerSkinData> OnSkinSelected;

        public void Initialize(PlayerSkin skinType, IPlayerSkinLoader playerSkinLoader)
        {
            _skinType = skinType;
            _playerSkinLoader = playerSkinLoader;
            _skinData = _playerSkinLoader.PlayerSkinAll[skinType];

            skinImage.sprite = _skinData.Sprite;
            selectButton.onClick.AddListener(OnSelectButtonClicked);
        }

        private void OnSelectButtonClicked()
        {
            LocalSkinSelection.Select((int)_skinType);
            OnSkinSelected?.Invoke(_skinData);
            SetSelected(true);
        }

        public void SetSelected(bool isSelected)
        {
        }

        private void OnDestroy()
        {
            selectButton.onClick.RemoveListener(OnSelectButtonClicked);
        }
    }
}
