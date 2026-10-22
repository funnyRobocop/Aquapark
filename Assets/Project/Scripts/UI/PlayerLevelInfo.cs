using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace NonameGame
{
    public class PlayerLevelInfo : MonoBehaviour
    {
        public TextMeshProUGUI playerNameLabel;
        private string readyText;
        private string finishText;

        void Start()
        {
            readyText = LocalizationSettings.StringDatabase.GetLocalizedString("MyStrings", "ready");
            finishText = LocalizationSettings.StringDatabase.GetLocalizedString("MyStrings", "finish");
        }

        public void ShowReady(string message)
        {
            if (readyText!= null)
                playerNameLabel.text = string.Format(readyText, name);
        }
        public void ShowFinish(string message)
        {
            if (finishText != null)
                playerNameLabel.text = string.Format(finishText, name);
        }
    }
}
