using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NonameGame
{
    public class ErrorUI : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] TextMeshProUGUI connectionFailText;


        void Awake()
        {
            canvas.enabled = false;
        }

        public void ShowConnectionFailed(string message)
        {
            canvas.enabled = true;
            connectionFailText.text = message;
        }

        public void ReopenMainMenu()
        {
            canvas.enabled = false;

            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.buildIndex > 0)
                SceneManager.UnloadSceneAsync(scene);

            var menuScene = SceneManager.GetSceneByName("MainMenu");
            if (menuScene.isLoaded)
                SceneManager.UnloadSceneAsync(menuScene);

            SceneManager.LoadSceneAsync(0);
        }
    }
}
