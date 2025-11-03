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
        var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
        var targetPos = head.position + forward * distance + Vector3.up * heightOffset;
        transform.position = snap ? targetPos : Vector3.Lerp(transform.position, targetPos, 0.25f);

        // 常に顔の方を向く
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }
}
