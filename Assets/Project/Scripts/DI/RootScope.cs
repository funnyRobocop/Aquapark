using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NonameGame
{
    public class RootScope : LifetimeScope
    {
        [SerializeField] private PlayerSkinLoader playerSkinLoader;
        [SerializeField] private AchievementsConfig achievementsConfig;
        [SerializeField] private SoundManager soundManagerPrefab; 
        [SerializeField] private LoadingUI loadingUIPrefab;

        override protected void Configure(IContainerBuilder builder)
        {
            if (playerSkinLoader != null)
                playerSkinLoader.Initialize();

            if (achievementsConfig != null)
                achievementsConfig.Initialize();

            builder.RegisterInstance<IPlayerSkinLoader>(playerSkinLoader);
            builder.RegisterInstance<AchievementsConfig>(achievementsConfig);

            builder.Register<SaveService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameModel>().AsSelf();

            builder.RegisterComponentInNewPrefab(soundManagerPrefab, Lifetime.Singleton)
                   .DontDestroyOnLoad()
                   .AsImplementedInterfaces()
                   .AsSelf();

            builder.RegisterComponentInNewPrefab(loadingUIPrefab, Lifetime.Singleton)
                   .DontDestroyOnLoad()
                   .AsSelf();
        }
    }
}
