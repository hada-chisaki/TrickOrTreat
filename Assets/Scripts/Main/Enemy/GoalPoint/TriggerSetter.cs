using UnityEngine;

public class TriggerSetter : MonoBehaviour
{
    [SerializeField] private string targetTag = "Ghost";

    private void OnCollisionEnter(Collision collision)
    {
        // タグ確認
        if (!collision.gameObject.CompareTag(targetTag)) return;

        // Collider取得
        Collider col = collision.collider;
        if (col == null)
        {
            Debug.LogWarning("対象にColliderが見つかりませんでした");
            return;
        }

        // isTriggerに変更
        col.isTrigger = true;
        Debug.Log($"{collision.gameObject.name} のColliderをTrigger化しました");
    }
}
