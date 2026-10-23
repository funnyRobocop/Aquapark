using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace NonameGame
{
    public class LevelUI : MonoBehaviour
    {
        public GameObject objectiveObj;

        public void ShowObjectiveObj(bool value)
        {
            objectiveObj.SetActive(value);
        }
    }
}
