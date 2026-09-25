using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NonameGame
{
    public class RootScope : LifetimeScope
    {
        [SerializeField] private PlayerSkinLoader playerSkinLoader;
        
        override protected void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance<IPlayerSkinLoader>(playerSkinLoader);
        }
    }
}
