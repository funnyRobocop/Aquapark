using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NonameGame
{
    public class MainMenuScope : LifetimeScope
    {
        [SerializeField] private LoadingUI loadingUI;
        
        override protected void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(loadingUI).AsImplementedInterfaces().AsSelf();
        }
    }
}
