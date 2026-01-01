// SceneManager.cs
using System;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using VContainer;
using GameLoopEvents;
using System.Threading.Tasks;

public sealed class SceneManager : MonoBehaviour
{
    [SerializeField] float fadeOutSeconds = 1.0f;
    [SerializeField] float fadeInSeconds = 1.0f;
    [SerializeField] FadeStyle fadeStyle = FadeStyle.AlphaBlack;

    IDisposable _sub;
    bool _busy;

    FadeManager _fade;
    IPublisher<SceneTransition> _pubTransition;

    void Awake() => DontDestroyOnLoad(gameObject);

    [Inject]
    void Construct(
        ISubscriber<SceneTransitionRequest> subRequest,
        IPublisher<SceneTransition> pubTransition,
        FadeManager fade)
    {
        _fade = fade;
        _pubTransition = pubTransition;
        _sub = subRequest.Subscribe(req => TransitionAsync(req).Forget());
    }

    void OnDestroy() => _sub?.Dispose();

    void Start()
    {
        _fade.FadeInAsync(fadeInSeconds, FadeStyle.AlphaBlack);
    }

    async UniTaskVoid TransitionAsync(SceneTransitionRequest req)
    {
        if (_busy) return;
        _busy = true;

        // Fadeアウト完了を待つ（方式が増えてもズレない）
        await _fade.FadeOutAsync(fadeOutSeconds, fadeStyle);

        if (req.DelaySeconds > 0f)
            await UniTask.Delay(TimeSpan.FromSeconds(req.DelaySeconds));

        _pubTransition.Publish(new SceneTransition(req.SceneName));
        UnityEngine.SceneManagement.SceneManager.LoadScene(req.SceneName);

        await UniTask.Yield();

        await _fade.FadeInAsync(fadeInSeconds, fadeStyle);

        _busy = false;
    }
}
