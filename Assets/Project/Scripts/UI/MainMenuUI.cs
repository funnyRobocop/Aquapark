using UnityEngine;
using UnityEngine.UI;

namespace NonameGame
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button shopButton;
        [SerializeField] private ShopUI shopUI;

        private void Awake()
        {
            shopButton.onClick.AddListener(OnShopButtonClicked);
        }

        private void OnShopButtonClicked()
        {
            shopUI.Show();
        }
    }
}
