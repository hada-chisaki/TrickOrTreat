using UnityEngine;
using UnityEngine.UI;

public class HPBarUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform maskRect; // Mask の RectTransform
    [SerializeField] private RectTransform fillRect; // Fill の RectTransform
    [SerializeField] private Image fillImage;        // 色だけ変える用
    [SerializeField] public PlayerStatus playerStatus;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField, Range(0f, 1f)] private float dangerThreshold = 0.25f;

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

    public void SetHP(float v)
    {
        playerStatus.currentHP = Mathf.Clamp(v, 0f, playerStatus.maxHP);
        UpdateBar();
    }

    public void SetMaxHP(float v)
    {
        playerStatus.maxHP = Mathf.Max(1f, v);
        playerStatus.currentHP = Mathf.Min(playerStatus.currentHP, playerStatus.maxHP);
        UpdateBar();
    }

    public void Damage(float d) { playerStatus.currentHP = Mathf.Max(0, playerStatus.currentHP - d); UpdateBar(); }
    public void Heal(float a) { playerStatus.currentHP = Mathf.Min(playerStatus.maxHP, playerStatus.currentHP + a); UpdateBar(); }

    void UpdateBar()
    {
        float r = playerStatus.currentHP / Mathf.Max(1f, playerStatus.maxHP);

        // Fill の横幅だけ変更（左起点）
        float w = maxWidth * r;
        fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);

        // 色分岐
        if (fillImage) fillImage.color = (r <= dangerThreshold) ? dangerColor : normalColor;
    }
}
