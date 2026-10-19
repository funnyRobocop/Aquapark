using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NonameGame
{
    public class RewardUI : MonoBehaviour
    {

        public Action OnClaim;
        public Action OnAdClaim;

        [SerializeField] private Button claimBtn;
        [SerializeField] private Button adClaimBtn;
        [SerializeField] private TextMeshProUGUI rewadLabel;


        void Awake()
        {
            claimBtn.onClick.AddListener(OnClaimClick);
            adClaimBtn.onClick.AddListener(OnAdClaimClick);
        }

        public void Show(Action claim, Action adClaim, int value)
        {
            rewadLabel.text = value.ToString() + "$";
            gameObject.SetActive(true);
            OnClaim += claim;
            OnAdClaim += adClaim;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            OnClaim = null;
            OnAdClaim = null;
        }

        private void OnClaimClick()
        {
            OnClaim?.Invoke();
            Hide();
        }

        private void OnAdClaimClick()
        {
            OnAdClaim?.Invoke();
            Hide();
        }
    }
}
