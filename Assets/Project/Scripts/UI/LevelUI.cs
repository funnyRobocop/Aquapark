using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace NonameGame
{
    public class LevelUI : MonoBehaviour
    {
        public GameObject objectiveObj;
        public TextMeshProUGUI playerNameLabel;

        public void ShowObjectiveObj(string playerName)
        {
            objectiveObj.SetActive(true);
            playerNameLabel.text = playerName;
        }
        public void HideObjectiveObj()
        {
            objectiveObj.SetActive(false);
        }
    }
}
