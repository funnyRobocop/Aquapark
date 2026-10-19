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
            int index = UnityEngine.Random.Range(0, bgColorList.Length);
            bg.color = bgColorList[index].bgColor;
            gradient.color = bgColorList[index].gradientColor;
        }
    }
}
