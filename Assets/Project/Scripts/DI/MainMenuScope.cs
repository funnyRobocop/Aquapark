using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NonameGame
{
    public class MainMenuScope : LifetimeScope
    {
        [SerializeField] private LoadingUI loadingUI;
        [SerializeField] private SkinPreviewUI skinPreviewUI;

        override protected void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(loadingUI).AsImplementedInterfaces().AsSelf();
            builder.RegisterComponent(skinPreviewUI).AsImplementedInterfaces().AsSelf();
        }
    }
}
