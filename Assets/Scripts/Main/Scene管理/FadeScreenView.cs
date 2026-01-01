// FadeScreenView.cs
using System;
using DG.Tweening;
using MessagePipe;
using UnityEngine;
using VContainer;
using GameLoopEvents;

public sealed class FadeScreenView : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] RectTransform panel; // Slide用（同一でもOK）

    IDisposable _sub;
    IPublisher<FadeFinished> _pubFinished;

    static FadeScreenView _instance;

    void Awake()
    {
        // シーン跨ぎ常駐＋重複防止
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    [Inject]
    void Construct(ISubscriber<FadeRequest> sub, IPublisher<FadeFinished> pubFinished)
    {
        _pubFinished = pubFinished;
        _sub = sub.Subscribe(Play);
    }

    void OnDestroy() => _sub?.Dispose();

    void Play(FadeRequest req)
    {
        var kind = req.FadeIn ? FadeKind.FadeIn : FadeKind.FadeOut;

        if (canvasGroup == null) { _pubFinished?.Publish(new FadeFinished(req.Id, kind)); return; }

        DOTween.Kill(this);
        canvasGroup.blocksRaycasts = !req.FadeIn;

        switch (req.Style)
        {
            default:
            case FadeStyle.AlphaBlack:
                canvasGroup.DOFade(req.FadeIn ? 0f : 1f, req.Seconds)
                    .SetTarget(this)
                    .OnComplete(() => _pubFinished.Publish(new FadeFinished(req.Id, kind)));
                break;

            case FadeStyle.SlideLeft:
                canvasGroup.alpha = 1f;

                if (panel == null)
                {
                    canvasGroup.DOFade(req.FadeIn ? 0f : 1f, req.Seconds)
                        .SetTarget(this)
                        .OnComplete(() => _pubFinished.Publish(new FadeFinished(req.Id, kind)));
                    return;
                }

                float w = panel.rect.width;
                if (w <= 0f) w = Screen.width;

                float fromX = req.FadeIn ? 0f : w;
                float toX = req.FadeIn ? -w : 0f;

                panel.anchoredPosition = new Vector2(fromX, panel.anchoredPosition.y);

                panel.DOAnchorPosX(toX, req.Seconds)
                    .SetTarget(this)
                    .OnComplete(() =>
                    {
                        if (req.FadeIn) canvasGroup.alpha = 0f;
                        _pubFinished.Publish(new FadeFinished(req.Id, kind));
                    });
                break;
        }
    }

}
