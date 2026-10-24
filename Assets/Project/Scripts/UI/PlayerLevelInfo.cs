using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace NonameGame
{
    public class PlayerLevelInfo : MonoBehaviour
    {
        public TextMeshProUGUI playerNameLabel;
        private string playerReadyText;
        private string playerFinishText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Sprite firstSprite;
        [SerializeField] private Sprite finishedSprite;
        [SerializeField] private Sprite readySprite;

        void Start()
        {
            playerReadyText = LocalizationSettings.StringDatabase.GetLocalizedString("MyStrings", "playerReady");
            playerFinishText = LocalizationSettings.StringDatabase.GetLocalizedString("MyStrings", "playerFinish");

            iconImage.gameObject.SetActive(false);
        }

        public void ShowReady(string message)
        {
            if (playerReadyText != null)
                playerNameLabel.text = string.Format(playerReadyText, message);

            iconImage.gameObject.SetActive(true);
            iconImage.sprite = readySprite;
        }
        
        public void ShowFinish(string message, bool isFirst = false)
        {
            if (playerFinishText != null)
                playerNameLabel.text = string.Format(playerFinishText, message);

            iconImage.gameObject.SetActive(true);
            if (isFirst)
            {
                iconImage.sprite = firstSprite;
            }
            else
            {
                iconImage.sprite = finishedSprite;
            }
        }
    }
}
