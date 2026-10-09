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
        [SerializeField] private Transform itemContainer;
        [SerializeField] private TMP_Text selectedSkinNameText;
        [SerializeField] private TMP_Text selectedSkinTypeText;

        [SerializeField] private GameObject itemPrefab;

        private List<ShopItemUI> _shopItems = new List<ShopItemUI>();

        [Inject] private IPlayerSkinLoader playerSkinLoader;

        private void Awake()
        {
            closeButton.onClick.AddListener(Hide);

            foreach (var item in playerSkinLoader.PlayerSkinAll)
            {
                var skinType = item.Key;

                var itemUI = Instantiate(itemPrefab, itemContainer).GetComponent<ShopItemUI>();
                itemUI.Initialize(skinType, playerSkinLoader);

                itemUI.OnSkinSelected += SelectSkin;
                _shopItems.Add(itemUI);
            }
        }

        private void SelectSkin(PlayerSkinData skinData)
        {
            foreach (var item in _shopItems)
                item.SetSelected(false);

            LocalSkinSelection.Select((int)skinData.SkinId);

            selectedSkinNameText.text = skinData.GetNameString();
            selectedSkinTypeText.text = skinData.GetSkinTypeString();
        }


        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(Hide);
        }

        internal void Show()
        {
            gameObject.SetActive(true);
        }

        internal void Hide()
        {
            gameObject.SetActive(false);
        }

    }
}
