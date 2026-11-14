using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NonameGame
{
    public class MainMenuScope : LifetimeScope
    {
        [SerializeField] private SkinPreviewUI skinPreviewUI;

        override protected void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(skinPreviewUI).AsImplementedInterfaces().AsSelf();
        }
    }
}
