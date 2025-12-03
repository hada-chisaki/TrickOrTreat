using UnityEngine;
using DG.Tweening;

public class UIEasing : MonoBehaviour
{
    [Header("イージング設定")]
    [SerializeField] private EasingType easingType = EasingType.UpDown;
    [SerializeField] private float startDelay = 0f;
    [SerializeField] private bool loop = false;

    [Header("上下に揺らす設定")]
    [SerializeField] private float moveDistance = 20f;
    [SerializeField] private float moveDuration = 1f;

    [Header("拡縮設定")]
    [SerializeField] private Vector3 targetScale = Vector3.one;
    [SerializeField] private float scaleDuration = 0.5f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    private RectTransform rectTransform;
    private Vector3 initialPosition;

    public enum EasingType
    {
        UpDown = 0,
        Scale = 1
    }

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        initialPosition = rectTransform.anchoredPosition;

        Invoke(nameof(StartEasing), startDelay);
    }

    private void StartEasing()
    {
        switch (easingType)
        {
            case EasingType.UpDown:
                StartUpDownEasing();
                break;
            case EasingType.Scale:
                StartScaleEasing();
                break;
        }
    }

    private void StartUpDownEasing()
    {
        int loopCount = loop ? -1 : 0;
        rectTransform.DOAnchorPosY(initialPosition.y + moveDistance, moveDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(loopCount, LoopType.Yoyo);
    }

    private void StartScaleEasing()
    {
        rectTransform.localScale = Vector3.zero;
        int loopCount = loop ? -1 : 0;
        rectTransform.DOScale(targetScale, scaleDuration)
            .SetEase(scaleEase)
            .SetLoops(loopCount, LoopType.Restart);
    }
}