using System.Collections.Generic;
using UnityEngine;

namespace NonameGame
{
    public class HintsUI : MonoBehaviour
    {
        [SerializeField] private Canvas hintCanvas;
        [SerializeField] private GameObject hintsObject;
        [SerializeField] private List<GameObject> hintsList;

        public void ShowRandomHint()
        {
            if (hintsList == null || hintsList.Count == 0)
            {
                Debug.LogWarning("No hints available.");
                return;
            }

            int randomIndex = Random.Range(0, hintsList.Count);
            ShowHint(randomIndex);
        }

        public void ShowHint(int index)
        {

            if (index < 0 || index >= hintsList.Count)
            {
                Debug.LogWarning("Invalid hint index: " + index);
                return;
            }

            hintCanvas.enabled = true; 
            hintsObject.SetActive(true);

            foreach (var hint in hintsList)
            {
                hint.SetActive(false);
            }

            hintsList[index].SetActive(true);
        }

        public void HideHint()
        {
            hintCanvas.enabled = false;
            hintsObject.SetActive(false);

            foreach (var hint in hintsList)
            {
                hint.SetActive(false);
            }
        }
    }
}
