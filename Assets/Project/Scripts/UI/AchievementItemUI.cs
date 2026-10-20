using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace NonameGame
{
    public class AchievementItemUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Button lockButton;
        [SerializeField] private Button claimButton;
        [SerializeField] private GameObject completedMark; // галочка "награда получена"

        private string _achievementId;
        private System.Action<string> _onClaim;

        public void Setup(GameModel.AchievementData data, AchievementDefinition def, Action<string> onClaim)
        {
            _achievementId = data.Id;
            _onClaim = onClaim;

            iconImage.sprite = def.Sprite;
            titleText.text = def.Title[0];
            descriptionText.text = def.Description[0];
            progressText.text = $"{data.Current} / {data.Goal}";
            progressSlider.value = data.Goal > 0 ? Mathf.Clamp01((float)data.Current / data.Goal) : 0f;

            bool canClaim = data.CanClaim;
            bool taken = data.RewardTaken;

            lockButton.gameObject.SetActive(false);
            claimButton.gameObject.SetActive(false);
            completedMark.SetActive(false);

            if (taken)
            {
                completedMark.SetActive(true);
                return;
            }

            if (canClaim)
            {
                claimButton.gameObject.SetActive(true);
                claimButton.onClick.RemoveAllListeners();
                claimButton.onClick.AddListener(() => _onClaim?.Invoke(_achievementId));
            }

            lockButton.gameObject.SetActive(true);
        }
    }
}