using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NonameGame
{
    public class RootScope : LifetimeScope
    {
        [SerializeField] private PlayerSkinLoader playerSkinLoader;
        [SerializeField] private AchievementsConfig achievementsConfig;

        override protected void Configure(IContainerBuilder builder)
        {
            if (playerSkinLoader != null)
            {
                playerSkinLoader.Initialize();
            }
            if (achievementsConfig != null)
            {
                achievementsConfig.Initialize();
            }

            builder.RegisterInstance<IPlayerSkinLoader>(playerSkinLoader);
            builder.RegisterInstance<AchievementsConfig>(achievementsConfig);

            // Регистрируем сервис сохранения как обычный Singleton
            builder.Register<SaveService>(Lifetime.Singleton);

            // Регистрируем GameModel как EntryPoint (чтобы сработал IInitializable) 
            // и как AsSelf() (чтобы другие классы могли запрашивать её напрямую по типу GameModel)
            builder.RegisterEntryPoint<GameModel>().AsSelf();
        }
    }
}
