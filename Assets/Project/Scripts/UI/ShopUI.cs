using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VContainer;

namespace NonameGame
{
    // MainMenu only. Wire each skin button to SelectSkin(int) in the Inspector.
    public class ShopUI : MonoBehaviour
    {

        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Button chooseButton;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button confirmPurchaseButton;
        [SerializeField] private TMP_Text purchaseButtonText;
        [SerializeField] private TMP_Text confirmPurchaseButtonText;
        [SerializeField] private Transform itemContainer;
        [SerializeField] private TMP_Text selectedSkinNameText;
        [SerializeField] private TMP_Text selectedSkinTypeText;

        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private GameObject confirmPanel;

        private List<ShopItemUI> _shopItems = new List<ShopItemUI>();

        [Inject] private IPlayerSkinLoader playerSkinLoader;
        [Inject] private GameModel _gameModel;

        private PlayerSkinData _selectedSkinData;
        private ShopItemUI _selectedItem;



        private void Awake()
        {
            chooseButton.onClick.AddListener(ChooseSkin);
            purchaseButton.onClick.AddListener(PurchaseSkin);
            confirmPurchaseButton.onClick.AddListener(ConfirmPurchase);

            foreach (var item in playerSkinLoader.PlayerSkinAll)
            {
                var skinType = item.Key;

                var itemUI = Instantiate(itemPrefab, itemContainer).GetComponent<ShopItemUI>();
                itemUI.Initialize(skinType, playerSkinLoader);

                itemUI.OnSkinSelected += SelectSkin;
                _shopItems.Add(itemUI);
            }

        }
        
        private void OnEnable()
        {
            UpdateButtonsView();
            HideConfirmPanel();

            _shopItems[0].SelectButton.onClick?.Invoke();
            scroll.verticalNormalizedPosition = 1f; // Scroll to top

            foreach (var item in _shopItems)
                item.SetChoosed(_gameModel.SelectedSkinId == (int)item.PlayerSkin);
        }

        private void OnDestroy()
        {
            chooseButton.onClick.RemoveListener(ChooseSkin);
            purchaseButton.onClick.RemoveListener(PurchaseSkin);
            confirmPurchaseButton.onClick.RemoveListener(ConfirmPurchase);
        }


        private void UpdateButtonsView()
        {
            chooseButton.gameObject.SetActive(false);
            purchaseButton.gameObject.SetActive(false);

            if (_selectedSkinData != null)
            {
                if (_gameModel.IsSkinPurchased((int)_selectedSkinData.SkinId))
                {
                    chooseButton.gameObject.SetActive(_gameModel.SelectedSkinId != (int)_selectedSkinData.SkinId);
                    purchaseButton.gameObject.SetActive(false);
                }
                else
                {
                    chooseButton.gameObject.SetActive(false);
                    purchaseButton.gameObject.SetActive(true);

                    purchaseButtonText.text = _selectedSkinData.Price.ToString();
                    confirmPurchaseButtonText.text = _selectedSkinData.Price.ToString();
                    purchaseButton.interactable = _gameModel.Money >= _selectedSkinData.Price;
                }
            }
        }

        private void ChooseSkin()
        {
            if (_selectedSkinData == null)
                return;

            _gameModel.SelectSkin((int)_selectedSkinData.SkinId);
            UpdateButtonsView();

            foreach (var item in _shopItems)
                item.SetChoosed(false);

            _selectedItem?.SetChoosed(true);
        }

        private void PurchaseSkin()
        {
            if (_selectedSkinData == null)
                return;

            ShowConfirmPanel();
        }

        private void ConfirmPurchase()
        {
            if (_selectedSkinData == null)
                return;

            _gameModel.PurchaseSkin((int)_selectedSkinData.SkinId, _selectedSkinData.Price);            

            HideConfirmPanel();
            UpdateButtonsView();
        }

        private void SelectSkin(PlayerSkinData skinData, ShopItemUI shopItem)
        {
            _selectedSkinData = skinData;
            _selectedItem = shopItem;

            foreach (var item in _shopItems)
                item.SetSelected(false);

            if (_selectedItem != null)
                _selectedItem.SetSelected(true);

            selectedSkinNameText.text = skinData.GetNameString();
            selectedSkinTypeText.text = skinData.GetSkinTypeString();

            UpdateButtonsView();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void ShowConfirmPanel()
        {
            confirmPanel.SetActive(true);
        }

        public void HideConfirmPanel()
        {
            confirmPanel.SetActive(false);
        }
    }
}
