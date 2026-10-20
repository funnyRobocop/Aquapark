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

            // РЕГИСТРАЦИЯ ПРЕФАБА КАК ENTRY POINT И SINGLETON ОДНОВРЕМЕННО:
            // VContainer создаст префаб, вызовет IStartable.Start() 
            // и позволит инжектить SoundManager по типу в другие классы.
            builder.RegisterComponentInNewPrefab(soundManagerPrefab, Lifetime.Singleton)
                   .DontDestroyOnLoad()
                   .AsImplementedInterfaces() // Важно для работы IStartable!
                   .AsSelf();                 // Важно для обычного инжекта в скрипты сцен
        }
    }
}
