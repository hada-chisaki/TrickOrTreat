using UnityEngine;
using DG.Tweening;

public class GhostAnim : MonoBehaviour
{
    [SerializeField] private Transform target;
    private Tween tween;
    private int currentAnimIndex = -1;

    private Vector3 basePos;
    private Vector3 baseScale;
    private Quaternion baseRot;

    void Awake()
    {
        if (target == null) target = transform;
        basePos = target.position;
        baseScale = target.localScale;
        baseRot = target.rotation;
    }

    void Update()
    {
        // Idleなどで手動波動ループさせたい場合はここを使う（現在はTweenで制御）
    }

    public void SetAnim(int animIndex)
    {
        if (animIndex == currentAnimIndex) return;
        currentAnimIndex = animIndex;

        tween?.Kill(false);

        Vector3 startPos = target.position;
        Vector3 startScale = target.localScale;
        Quaternion startRot = target.rotation;

        switch (animIndex)
        {
            case 0: // Idle: 途切れない上下移動
                // MoveをTweenでやるのではなく、LocalYを時間経過で正弦波的に動かす
                tween = DOTween.To(
                    () => 0f,
                    x =>
                    {
                        float offset = Mathf.Sin(x) * 0.2f; // 正弦波運動
                        target.position = new Vector3(startPos.x, basePos.y + offset, startPos.z);
                    },
                    Mathf.PI * 2f, // 一周期
                    1f // 秒数
                )
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental); // 無限ループ・途切れなし
                break;

            case 1: // 回転: シームレス360度ループ
                tween = target
                    .DORotate(startRot.eulerAngles + new Vector3(0, 360, 0), 1.5f, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Incremental);
                break;

            case 2: // 拡大縮小: 正弦波スケール
                tween = DOTween.To(
                    () => 0f,
                    x =>
                    {
                        float s = 1f + Mathf.Sin(x) * 0.2f;
                        target.localScale = baseScale * s;
                    },
                    Mathf.PI * 2f,
                    1.2f
                )
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
                break;
        }
    }

    public void StopNow()
    {
        tween?.Kill(false);
        target.position = basePos;
        target.localScale = baseScale;
        target.rotation = baseRot;
    }
}
