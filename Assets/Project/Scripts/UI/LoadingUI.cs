using UnityEngine.UI;
using UnityEngine;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private Slider _slider;

    public void Show()
    {
        _root.SetActive(true);
        _slider.value = 0f;
    }

    public void Hide()
    {
        _root.SetActive(false);
    }

    public void SetProgress(float value) // 0..1
    {
        _slider.value = value;
    }
}