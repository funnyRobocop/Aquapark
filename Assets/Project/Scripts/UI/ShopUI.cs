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
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private GameObject previewPanel;
        [SerializeField] private GameObject itemPrefab;

        private List<ShopItemUI> _shopItems = new List<ShopItemUI>();

        [Inject] private IPlayerSkinLoader _playerSkinLoader;
        [Inject] private GameModel _gameModel;
        [Inject] private SkinPreviewUI _skinPreviewUI;

        private ShopItemUI _clickedItem;
        private HoldButton _buyHoldButton;

        private void Awake()
        {
            selectButton.onClick.AddListener(SelectSkin);

            if (buyButton != null)
            {
                _buyHoldButton = buyButton.GetComponent<HoldButton>();
                if (_buyHoldButton != null)
                {
                    _buyHoldButton.OnHoldComplete += ConfirmPurchase;
                }
            }

            // Создаём айтемы один раз (родитель может быть выключен — это нормально)
            foreach (var kvp in _playerSkinLoader.PlayerSkinAll)
            {
                var itemUI = Instantiate(itemPrefab, itemContainer).GetComponent<ShopItemUI>();
                itemUI.Initialize(kvp.Value);
                itemUI.OnSkinClicked += SelectSkin;
                _shopItems.Add(itemUI);
            }
        }

        private void OnEnable()
        {
            // На всякий случай, если Show вызвали через SetActive
            Load();
        }

        public void Load()
        {
            // Явно грузим все иконки
            foreach (var item in _shopItems)
                item.LoadSprite();

            UpdateButtonsView();

            if (_buyHoldButton != null)
                _buyHoldButton.ResetHold();

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

        public void ClickLeft()
        {
            if (_clickedItem == null || _shopItems.Count == 0) return;

            var leftIndex = _shopItems.IndexOf(_clickedItem) - 1;
            if (leftIndex < 0) leftIndex = _shopItems.Count - 1;

            _shopItems[leftIndex].ClickButton.onClick?.Invoke();
            _skinPreviewUI.SpawnPlayerMesh(_clickedItem.SkinData);
            _skinPreviewUI.ResetRotation();
        }

        public void ClickRight()
        {
            if (_clickedItem == null || _shopItems.Count == 0) return;

            var rightIndex = _shopItems.IndexOf(_clickedItem) + 1;
            if (rightIndex > _shopItems.Count - 1) rightIndex = 0;

            _shopItems[rightIndex].ClickButton.onClick?.Invoke();
            _skinPreviewUI.SpawnPlayerMesh(_clickedItem.SkinData);
            _skinPreviewUI.ResetRotation();
        }

        private void UpdateButtonsView()
        {
            foreach (var item in _shopItems)
                item.SetPurchased(_gameModel.IsSkinPurchased((int)item.SkinData.SkinId));

            selectButton.gameObject.SetActive(false);
            buyButton.gameObject.SetActive(false);
            selectedButton.SetActive(false);
            blockedButton.SetActive(false);

            if (_clickedItem != null)
            {
                if (_gameModel.IsSkinPurchased((int)_clickedItem.SkinData.SkinId))
                {
                    if (_gameModel.SelectedSkinId == (int)_clickedItem.SkinData.SkinId)
                        selectedButton.SetActive(true);
                    else
                        selectButton.gameObject.SetActive(true);
                }
                else
                {
                    buyButton.gameObject.SetActive(true);
                    buyButton.interactable = _gameModel.Money >= _clickedItem.SkinData.Price;
                    if (_gameModel.Money < _clickedItem.SkinData.Price)
                        blockedButton.SetActive(true);
                }
            }
        }

        public void ShowPreview()
        {
            if (_clickedItem == null) return;

            previewPanel.SetActive(true);
            _skinPreviewUI.Show(-450f, 400f);
            _skinPreviewUI.SpawnPlayerMesh(_clickedItem.SkinData);
        }

        public void HidePreview()
        {
            previewPanel.SetActive(false);
            _skinPreviewUI.Hide();
        }

        private void SelectSkin()
        {
            if (_clickedItem == null)
                return;

            _gameModel.SelectSkin((int)_clickedItem.SkinData.SkinId);
            UpdateButtonsView();
        }

        private void ConfirmPurchase()
        {
            if (_clickedItem == null)
                return;

            _gameModel.PurchaseSkin((int)_clickedItem.SkinData.SkinId, _clickedItem.SkinData.Price);
            UpdateButtonsView();

            if (_gameModel.SelectedSkinId < 0)
            {
                SelectSkin();
                // Закрываем через Hide, чтобы корректно выгрузить спрайты
                Hide();

                var menu = FindAnyObjectByType<MainMenuUI>();
                if (menu != null)
                {
                    menu.SpawnPlayerMesh();
                    menu.Resume();
                }
            }
        }

        private void SelectSkin(PlayerSkinData skinData, ShopItemUI shopItem)
        {
            _clickedItem = shopItem;

            foreach (var item in _shopItems)
                item.SetClicked(false);

            if (_clickedItem != null)
                _clickedItem.SetClicked(true);

            int lang = _gameModel.Settings.Language == 0 ? 0 : 1;
            foreach (var item in nameLabel) item.text = skinData.GetNameString(lang);
            foreach (var item in typeLabel) item.text = skinData.GetSkinTypeString(lang);
            priceLabel.text = skinData.Price.ToString() + "$";

            UpdateButtonsView();
            if (_buyHoldButton != null) _buyHoldButton.ResetHold();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            // Явный вызов — не зависим от порядка OnEnable
            Load();
        }

        public void Hide()
        {
            // Явно выгружаем всё
            foreach (var item in _shopItems)
                item.UnloadSprite();

            if (_skinPreviewUI != null && _playerSkinLoader != null && _gameModel != null)
            {
                var selected = (PlayerSkin)_gameModel.SelectedSkinId;
                if (_playerSkinLoader.PlayerSkinAll.TryGetValue(selected, out var data))
                    _skinPreviewUI.SpawnPlayerMesh(data);
            }

            gameObject.SetActive(false);
        }
    }
}