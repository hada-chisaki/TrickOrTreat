using UnityEngine;

public class CircleWallBuilder : MonoBehaviour
{
    [Header("ターゲットステージ（円の中心）")]
    [SerializeField] private Transform center;          // Plane など、中心となる Transform

    [Header("円の設定")]
    [SerializeField, Min(0.01f)]
    private float radius = 5f;                          // 壁の半径

    [SerializeField, Min(3)]
    private int segmentCount = 32;                      // 分割数（多いほど丸く見える）

    [SerializeField]
    private float yOffset = 0f;                         // 壁の高さオフセット（床から少し浮かせたい時用）

    [Header("壁パーツ")]
    [SerializeField] private GameObject wallSegmentPrefab;

    [Header("向き設定")]
    [SerializeField] private bool faceCenter = true;    // true: 内側（中心）を向く / false: 外側を向く
    [SerializeField] private float segmentYRotationOffset = 0f; // プレハブの向き調整用（度数）

    private void Start()
    {
        BuildWall();
    }

    [ContextMenu("Rebuild Wall")]
    public void BuildWall()
    {
        if (center == null)
        {
            Debug.LogError("[CircleWallBuilder] center が設定されていません。", this);
            return;
        }

        if (wallSegmentPrefab == null)
        {
            Debug.LogError("[CircleWallBuilder] wallSegmentPrefab が設定されていません。", this);
            return;
        }

        if (segmentCount < 3) segmentCount = 3;
        if (radius <= 0f) radius = 0.01f;

        // このオブジェクト自身を「中心」と同じ位置・回転・単位スケールにそろえる
        transform.position = center.position;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        // 既存の子オブジェクト（前に作った壁）を削除
        ClearChildren();

        float angleStepDeg = 360f / segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            float angleDeg = angleStepDeg * i;
            float angleRad = Mathf.Deg2Rad * angleDeg;

            // ローカル空間での円周方向ベクトル（XZ平面）
            Vector3 dir = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad));

            // ローカル位置：中心から radius 分だけ外側
            Vector3 localPos = dir * radius;
            localPos.y = yOffset;

            // 向き：内側を向かせるなら -dir、外側なら dir
            Vector3 lookDir = faceCenter ? -dir : dir;
            Quaternion rot = Quaternion.LookRotation(lookDir, Vector3.up);

            // プレハブのローカル向きが合わない場合用の調整（Y軸回転を足す）
            rot *= Quaternion.Euler(0f, segmentYRotationOffset, 0f);

            // 子オブジェクトとして生成し、ローカル座標で配置
            GameObject seg = Instantiate(wallSegmentPrefab, transform);
            seg.transform.localPosition = localPos;
            seg.transform.localRotation = rot;
        }
    }

    /// <summary>
    /// 既存の子オブジェクトを全削除
    /// </summary>
    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child.gameObject);
            else
                Destroy(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
    }
}
