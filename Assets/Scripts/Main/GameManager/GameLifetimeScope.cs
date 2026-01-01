
using MessagePipe;
using VContainer;
using VContainer.Unity;
using UnityEngine;

public sealed class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterMessagePipe();

        //非オブジェクトクラス登録
        builder.Register<FadeManager>(Lifetime.Singleton);

        //オブジェクト作成＆DI登録
        CreateGameManager(builder);
        CreateSceneManager(builder);
    }

    void CreateGameManager(IContainerBuilder builder)
    {
        // このLifetimeScopeが付いてるGameObjectの子として生成される
        builder.RegisterComponentOnNewGameObject<GameManager>(Lifetime.Singleton, "GameManager")
            .UnderTransform(transform);
    }

    void CreateSceneManager(IContainerBuilder builder)
    {
        builder.RegisterComponentOnNewGameObject<SceneManager>(
                Lifetime.Singleton,
                "SceneManager")
            .UnderTransform(transform);
    }

}
