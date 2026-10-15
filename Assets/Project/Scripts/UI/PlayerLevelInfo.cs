using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace NonameGame
{
    public class PlayerLevelInfo : MonoBehaviour
    {
        public TextMeshProUGUI playerNameLabel;

        public void Show(string message)
        {
            playerNameLabel.text = message;
        }
    }
}
