using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class HPBarUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform maskRect; // Mask の RectTransform
    [SerializeField] private RectTransform fillRect; // Fill の RectTransform
    [SerializeField] private Image fillImage;        // 色だけ変える用

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField, Range(0f, 1f)] private float dangerThreshold = 0.25f;

    private int maxHp = 0;

    float maxWidth;

    void Awake()
    {
        // マスク領域の幅を基準値にする
        maxWidth = maskRect.rect.width;
        UpdateBar();
    }

    void Update()
    {

        UpdateBar();
    }

    void UpdateBar()
    {
        float r = OkashiManager.Instance.currentScore / Mathf.Max(1f, OkashiManager.Instance.initialScore);

        // Fill の横幅だけ変更（左起点）
        float w = maxWidth * r;
        fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);

        // 色分岐
        if (fillImage) fillImage.color = (r <= dangerThreshold) ? dangerColor : normalColor;
        if (r <= dangerThreshold)
        {
            RhythmBounceUI ui = GetComponent<RhythmBounceUI>();
            ui.enabled = true;
        }
        else
        {
            RhythmBounceUI ui = GetComponent<RhythmBounceUI>();
            ui.enabled = false;
        }
    }
}
