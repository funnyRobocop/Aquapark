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
        [SerializeField] private Image iconImage;
        [SerializeField] private Sprite firstSprite;
        [SerializeField] private Sprite finishedSprite;
        [SerializeField] private Sprite readySprite;

        public void ShowReady(string message)
        {
            playerNameLabel.text = message;

            iconImage.sprite = readySprite;
        }

        public void ShowFinish(string message, bool isFirst = false)
        {
            playerNameLabel.text = message;

            if (isFirst)
            {
                iconImage.sprite = firstSprite;
            }
            else
            {
                iconImage.sprite = finishedSprite;
            }

            StartCoroutine(HideCrtn());
        }

        private IEnumerator HideCrtn()
        {
            yield return new WaitForSeconds(1f);
            Destroy(gameObject);
        }
    }
}
