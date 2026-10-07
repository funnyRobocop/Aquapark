using UnityEngine;


namespace NonameGame
{
    public class LoadingUI : MonoBehaviour
    {
        public void Show()
        {
            if (gameObject == null) return;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (gameObject == null) return;
            gameObject.SetActive(false);
        }
    }
}
