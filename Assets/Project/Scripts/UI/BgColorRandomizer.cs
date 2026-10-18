using UnityEngine;
using UnityEngine.UI;


namespace NonameGame
{
    [System.Serializable]
    public class ColorData
    {
        public Color bgColor;
        public Color gradientColor;
    }

    public class BgColorRandomizer : MonoBehaviour
    {
        [SerializeField] private ColorData[] bgColorList;
        [SerializeField] private Image bg;
        [SerializeField] private Image gradient;

        void OnEnable()
        {
            bg.color = bgColorList[UnityEngine.Random.Range(0, bgColorList.Length - 1)].bgColor;
            gradient.color = bgColorList[UnityEngine.Random.Range(0, bgColorList.Length - 1)].gradientColor;
        }
    }
}
