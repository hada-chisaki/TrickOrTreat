// FadeManager.cs
using System;
using Cysharp.Threading.Tasks;
using MessagePipe;
using GameLoopEvents;
using UnityEngine;

public sealed class FadeManager : IDisposable
{
    readonly IPublisher<FadeRequest> _pubReq;
    readonly IDisposable _subFinished;

    int _seq = 0;
    UniTaskCompletionSource _tcs;
    int _waitingId = -1;

    public FadeManager(IPublisher<FadeRequest> pubReq, ISubscriber<FadeFinished> subFinished)
    {
        _pubReq = pubReq;
        _subFinished = subFinished.Subscribe(OnFinished);
    }

    public UniTask FadeOutAsync(float seconds, FadeStyle style = FadeStyle.AlphaBlack)
        => FadeAsync(fadeIn: false, seconds, style);

    public UniTask FadeInAsync(float seconds, FadeStyle style = FadeStyle.AlphaBlack)
        => FadeAsync(fadeIn: true, seconds, style);

    UniTask FadeAsync(bool fadeIn, float seconds, FadeStyle style)
    {
        // 最小：同時フェードは上書き（必要ならキュー化できる）
        _tcs?.TrySetCanceled();
        _tcs = new UniTaskCompletionSource();

        _waitingId = ++_seq;
        _pubReq.Publish(new FadeRequest(_waitingId, fadeIn, seconds, style));

        return _tcs.Task;
    }

    void OnFinished(FadeFinished e)
    {
        if (e.Id != _waitingId) return;
        _tcs?.TrySetResult();
    }

    public void Dispose()
    {
        _subFinished?.Dispose();
        _tcs?.TrySetCanceled();
    }
}
