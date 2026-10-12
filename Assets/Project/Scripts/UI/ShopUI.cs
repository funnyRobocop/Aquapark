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

        [SerializeField] private Button closeButton;
        [SerializeField] private Button chooseButton;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private TMP_Text purchaseButtonText;
        [SerializeField] private Transform itemContainer;
        [SerializeField] private TMP_Text selectedSkinNameText;
        [SerializeField] private TMP_Text selectedSkinTypeText;

        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private GameObject confirmPanel;

        private List<ShopItemUI> _shopItems = new List<ShopItemUI>();

        [Inject] private IPlayerSkinLoader playerSkinLoader;
        [Inject] private GameModel _gameModel;

        private PlayerSkinData _selectedSkinData;


        private void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            chooseButton.onClick.AddListener(ChooseSkin);
            purchaseButton.onClick.AddListener(PurchaseSkin);

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
            confirmPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(Hide);
            chooseButton.onClick.RemoveListener(ChooseSkin);
            purchaseButton.onClick.RemoveListener(PurchaseSkin);
        }

        private void UpdateButtonsView()
        {
            chooseButton.gameObject.SetActive(false);
            purchaseButton.gameObject.SetActive(false);

            if (_selectedSkinData != null)
            {
                if (_gameModel.IsSkinPurchased((int)_selectedSkinData.SkinId))
                {
                    chooseButton.gameObject.SetActive(true);
                    purchaseButton.gameObject.SetActive(false);
                }
                else
                {
                    chooseButton.gameObject.SetActive(false);
                    purchaseButton.gameObject.SetActive(true);

                    purchaseButtonText.text = _selectedSkinData.Price.ToString();
                    purchaseButton.interactable = _gameModel.Money >= _selectedSkinData.Price;
                }
            }
        }

        private void ChooseSkin()
        {
            if (_selectedSkinData != null)
            {
                _gameModel.SelectSkin((int)_selectedSkinData.SkinId);
            }
        }

        private void PurchaseSkin()
        {
            if (_selectedSkinData != null)
            {
                _gameModel.PurchaseSkin((int)_selectedSkinData.SkinId, _selectedSkinData.Price);
            }
        }

        private void SelectSkin(PlayerSkinData skinData)
        {
            _selectedSkinData = skinData;

            foreach (var item in _shopItems)
                item.SetSelected(false);

            selectedSkinNameText.text = skinData.GetNameString();
            selectedSkinTypeText.text = skinData.GetSkinTypeString();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
