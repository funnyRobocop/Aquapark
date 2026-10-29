using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace NonameGame
{
    public class LevelResultsItem : MonoBehaviour
    {
        public TextMeshProUGUI playerNameLabel;
        [SerializeField] private Image bgImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Sprite firstSprite;
        [SerializeField] private Sprite finishedSprite;
        [SerializeField] private Color normalColor;
        [SerializeField] private Color highlightColor;

        public void Load(PlayerRaceData data, int place, int playerCount)
        {
            var isLocal = PlayerRaceData.LocalInstance == data;
            if (isLocal)
            {
                bgImage.color = highlightColor;
            }
            else
            {
                bgImage.color = normalColor;
            }

            iconImage.enabled = false;

            if (playerCount > 1)
            {
                if (place == 1)
                {
                    playerNameLabel.text = $"{place}. {data.DisplayName}";
                    iconImage.enabled = true;
                    iconImage.sprite = firstSprite;
                }
                else if (place > 0)
                {
                    playerNameLabel.text = $"{place}. {data.DisplayName}";
                    iconImage.enabled = true;
                    iconImage.sprite = finishedSprite;
                }
                else
                {
                    playerNameLabel.text = string.Format(data.DisplayName);
                }
            }
            else
            {
                if (place > 0)
                {
                    playerNameLabel.text = $"{place}. {data.DisplayName}";
                    iconImage.enabled = true;
                    iconImage.sprite = finishedSprite;
                }
                else
                {
                    playerNameLabel.text = string.Format(data.DisplayName);
                }
            }
        }
    }
}
