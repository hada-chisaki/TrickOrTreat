using UnityEngine;
public class VRBillboard : MonoBehaviour
{
    [Header("VR Rig")]
    [SerializeField] Transform head;           // CenterEyeAnchor を割り当て
    [Header("Placement")]
    [SerializeField] float distance = 1.2f;    // 1.0〜1.5m が見やすい
    [SerializeField] float heightOffset = -0.05f; // 少し下げて視界中央へ
    [SerializeField] bool followWhileOpen = false; // trueなら常に追従

    void OnEnable() { Place(true); }
    void Update() { if (followWhileOpen && head) Place(false); }

    void Place(bool snap)
    {
        if (!head) return;

        // 頭の向きをそのまま使う（上下も含める）
        var forward = head.forward.normalized;

        // 頭の向いている方向に distance だけ前進
        // heightOffset は頭の「上方向」基準でオフセット
        var targetPos = head.position
                        + forward * distance
                        + head.up * heightOffset;

        if (snap)
        {
            transform.position = targetPos;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, 0.25f);
        }

        // 常に顔の方を向く（上下も含めた向き）
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        // もし頭の傾きも完全に合わせたいなら ↓ に変えてもOK
        // transform.rotation = Quaternion.LookRotation(forward, head.up);
    }


    public void StopFllowCamera()
    {
        followWhileOpen = false;
    }

    public void StartFllowCamera()
    {
        followWhileOpen = true;
    }
}
