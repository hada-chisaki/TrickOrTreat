using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class NavMeshCheckpointWalker : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Collider[] checkpoints; // ← 各チェックポイントにTrigger付きColliderを置く

    [Header("設定")]
    [SerializeField] private float waitAfterArrival = 1f; // 少し待ってから次へ

    [Header("イベント")]
    public UnityEvent onReachPoint1;
    public UnityEvent onReachPoint2;
    public UnityEvent onReachPoint3;
    public UnityEvent onAllPointsReached;

    private int currentIndex = 0;
    private bool isWaiting = false;

    [Header("デバッグ用")]
    private float currentSpeed = 1.0f;
    private float currentSAccele = 1.0f;

    void Start()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (checkpoints.Length > 0)
        {
            MoveToCheckpoint(checkpoints[currentIndex].transform.position);
        }

        currentSpeed = agent.speed;
        currentSAccele = agent.acceleration;
    }

    private void MoveToCheckpoint(Vector3 pos)
    {
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(pos, out var hit, 5f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        agent.SetDestination(pos);
        agent.isStopped = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // もし現在の目的地のチェックポイントに触れたら
        if (isWaiting) return; // 二重呼び出し防止

        if (checkpoints.Length == 0) return;
        if (currentIndex >= checkpoints.Length) return;

        if (other == checkpoints[currentIndex])
        {
            // 到達イベントを発火
            switch (currentIndex)
            {
                case 0: onReachPoint1?.Invoke(); break;
                case 1: onReachPoint2?.Invoke(); break;
                case 2: onReachPoint3?.Invoke(); break;
            }

            // 次のポイントへ
            StartCoroutine(WaitAndGoNext());
        }
    }

    private System.Collections.IEnumerator WaitAndGoNext()
    {
        isWaiting = true;
        agent.isStopped = true;

        yield return new WaitForSeconds(waitAfterArrival);

        currentIndex++;

        if (currentIndex < checkpoints.Length)
        {
            MoveToCheckpoint(checkpoints[currentIndex].transform.position);
        }
        else
        {
            onAllPointsReached?.Invoke();
        }

        isWaiting = false;
    }
}
