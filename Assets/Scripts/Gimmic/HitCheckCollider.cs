using UnityEngine;
using UnityEngine.Events;

public class HitCheckCollider : MonoBehaviour
{
    [Header("当たり判定監視対象")]
    [SerializeField] private string tagName = "Enemy";

    [Header("衝突時に呼ばれるイベント")]
    public UnityEvent onHitTriggerEnter;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagName))
        {
            onHitTriggerEnter.Invoke();
        }
    }
}
