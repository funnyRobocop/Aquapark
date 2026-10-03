using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NonameGame
{
    public class GameScope : LifetimeScope
    {
        [SerializeField] private CameraManager cameraManager;
        [SerializeField] private VFXManager vfxManager;

        override protected void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(cameraManager).AsImplementedInterfaces().AsSelf();
            builder.RegisterComponent(vfxManager).AsImplementedInterfaces().AsSelf();
        }
    }
}