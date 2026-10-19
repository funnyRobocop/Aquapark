using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VContainer;

namespace NonameGame
{
    public class ShopUI : MonoBehaviour
    {
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Button buyButton;
        [SerializeField] private GameObject blockedButton;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject selectedButton;
        [SerializeField] private Transform itemContainer;
        [SerializeField] private TMP_Text[] nameLabel;
        [SerializeField] private TMP_Text[] typeLabel;
        [SerializeField] private GameObject previewPanel;
        [SerializeField] private GameObject itemPrefab;

        private List<ShopItemUI> _shopItems = new List<ShopItemUI>();

        [Inject] private IPlayerSkinLoader _playerSkinLoader;
        [Inject] private GameModel _gameModel;
        [Inject] private SkinPreviewUI _skinPreviewUI;

        private ShopItemUI _klickedItem;
        private HoldButton _buyHoldButton; // Ссылка на наш новый компонент

        private void Awake()
        {
            selectButton.onClick.AddListener(SelectSkin);

            // Находим компонент HoldButton на кнопке покупки и подписываемся
            if (buyButton != null)
            {
                _buyHoldButton = buyButton.GetComponent<HoldButton>();
                if (_buyHoldButton != null)
                {
                    _buyHoldButton.OnHoldComplete += ConfirmPurchase;
                }
            }

            foreach (var item in _playerSkinLoader.PlayerSkinAll)
            {
                var skinType = item.Key;
                var itemUI = Instantiate(itemPrefab, itemContainer).GetComponent<ShopItemUI>();
                itemUI.Initialize(skinType, _playerSkinLoader);
                itemUI.OnSkinClicked += SelectSkin;
                _shopItems.Add(itemUI);
            }
        }

        private void OnEnable()
        {
            UpdateButtonsView();
            if (_buyHoldButton != null) _buyHoldButton.ResetHold();

            if (_shopItems.Count > 0 && _shopItems[0].ClickButton != null)
                _shopItems[0].ClickButton.onClick?.Invoke();

            scroll.verticalNormalizedPosition = 1f;
        }

        private void OnDestroy()
        {
            selectButton.onClick.RemoveListener(SelectSkin);
            if (_buyHoldButton != null)
            {
                _buyHoldButton.OnHoldComplete -= ConfirmPurchase;
            }
        }

        private void UpdateButtonsView()
        {
            foreach (var item in _shopItems)
                item.SetPurchased(_gameModel.IsSkinPurchased((int)item.SkinData.SkinId));

            selectButton.gameObject.SetActive(false);
            buyButton.gameObject.SetActive(false);
            selectedButton.SetActive(false);
            blockedButton.SetActive(false);

            if (_klickedItem != null)
            {
                if (_gameModel.IsSkinPurchased((int)_klickedItem.SkinData.SkinId))
                {
                    if (_gameModel.SelectedSkinId == (int)_klickedItem.SkinData.SkinId)
                        selectedButton.SetActive(true);
                    else
                        selectButton.gameObject.SetActive(true);
                }
                else
                {
                    buyButton.gameObject.SetActive(true);
                    buyButton.interactable = _gameModel.Money >= _klickedItem.SkinData.Price;
                    if (_gameModel.Money < _klickedItem.SkinData.Price)
                        blockedButton.SetActive(true);
                }
            }
        }

        public void ShowPreview()
        {
            previewPanel.SetActive(true);
            _skinPreviewUI.Show(-450f, 400f);
            _skinPreviewUI.SpawnPlayerMesh(_klickedItem.SkinData);
        }

        public void HidePreview()
        {
            previewPanel.SetActive(false);
            _skinPreviewUI.Hide();
        }

        private void SelectSkin()
        {
            if (_klickedItem == null)
                return;

            _gameModel.SelectSkin((int)_klickedItem.SkinData.SkinId);
            UpdateButtonsView();
        }

        private void ConfirmPurchase()
        {
            if (_klickedItem == null)
                return;

            _gameModel.PurchaseSkin((int)_klickedItem.SkinData.SkinId, _klickedItem.SkinData.Price);
            UpdateButtonsView();
        }

        private void SelectSkin(PlayerSkinData skinData, ShopItemUI shopItem)
        {
            _klickedItem = shopItem;

            foreach (var item in _shopItems)
                item.SetClicked(false);

            if (_klickedItem != null)
                _klickedItem.SetClicked(true);

            foreach (var item in nameLabel) item.text = skinData.GetNameString();
            foreach (var item in typeLabel) item.text = skinData.GetSkinTypeString();

            UpdateButtonsView();
            if (_buyHoldButton != null) _buyHoldButton.ResetHold();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _skinPreviewUI.SpawnPlayerMesh(_playerSkinLoader.PlayerSkinAll[(PlayerSkin)_gameModel.SelectedSkinId]);
            gameObject.SetActive(false);
        }
    }
}
