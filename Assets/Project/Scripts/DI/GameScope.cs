using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NonameGame
{
    public class GameScope : LifetimeScope
    {
        [SerializeField] private CameraManager cameraManager;

        override protected void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(cameraManager).AsImplementedInterfaces().AsSelf();
        }
    }
}