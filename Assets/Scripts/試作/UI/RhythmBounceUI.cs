using UnityEngine;
using DG.Tweening;

public class RhythmBounceUI : MonoBehaviour
{
    [SerializeField] float scaleUp = 1.2f;   // どこまで大きくするか
    [SerializeField] float duration = 0.15f; // 行って戻る片道時間

    void Start()
    {
        // 元のスケールを基準にする
        var baseScale = transform.localScale;

        // 無限に「大きく → 戻る」を繰り返す
        transform.DOScale(baseScale * scaleUp, duration)
                 .SetEase(Ease.OutBack)
                 .SetLoops(-1, LoopType.Yoyo);
    }
}
