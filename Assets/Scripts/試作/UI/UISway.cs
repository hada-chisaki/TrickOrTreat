using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UISway : MonoBehaviour
{
    [SerializeField] float swayAmplitude = 50f; // 振れ幅(px)
    [SerializeField] float swaySpeed = 3f;      // 揺れスピード
    [SerializeField] bool useSmooth = true;     // 動きを滑らかに

    RectTransform rect;
    Vector3 basePos;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        basePos = rect.anchoredPosition;
    }

    void Update()
    {
        float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmplitude;
        Vector3 target = new Vector3(basePos.x + swayX, basePos.y, basePos.z);

        if (useSmooth)
            rect.anchoredPosition = Vector3.Lerp(rect.anchoredPosition, target, 0.2f);
        else
            rect.anchoredPosition = target;
    }
}
