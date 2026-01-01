
using MessagePipe;
using VContainer;
using VContainer.Unity;
using UnityEngine;

public sealed class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterMessagePipe();

        //オブジェクト作成＆DI登録
        CreateGameManager(builder);
    }

    void CreateGameManager(IContainerBuilder builder)
    {
        // このLifetimeScopeが付いてるGameObjectの子として生成される
        builder.RegisterComponentOnNewGameObject<GameManager>(Lifetime.Singleton, "GameManager")
            .UnderTransform(transform);
    }

}
