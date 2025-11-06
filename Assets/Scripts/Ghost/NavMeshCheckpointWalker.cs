using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class NavMeshCheckpointWalker : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform[] checkpoints;

    [Header("設定")]
    [SerializeField] private float arriveThreshold = 0.5f; // 目標到達判定距離
    [SerializeField] private float waitAfterArrival = 1f;  // 次のポイントに進む前の待機秒

    [Header("イベント")]
    public UnityEvent onReachPoint1;
    public UnityEvent onReachPoint2;
    public UnityEvent onReachPoint3;
    public UnityEvent onAllPointsReached;

    private int currentIndex = 0;
    private bool isWalking = false;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (checkpoints.Length > 0)
        {
            MoveToNextCheckpoint();
        }
    }

    void Update()
    {
        if (!isWalking || agent.pathPending) return;

        if (agent.remainingDistance <= arriveThreshold)
        {
            StartCoroutine(HandleArrival());
        }
    }

    private IEnumerator HandleArrival()
    {
        isWalking = false;
        agent.isStopped = true;

        // イベント発火
        switch (currentIndex)
        {
            case 0: onReachPoint1?.Invoke(); break;
            case 1: onReachPoint2?.Invoke(); break;
            case 2: onReachPoint3?.Invoke(); break;
        }

        yield return new WaitForSeconds(waitAfterArrival);

        currentIndex++;

        if (currentIndex < checkpoints.Length)
        {
            MoveToNextCheckpoint();
        }
        else
        {
            onAllPointsReached?.Invoke();
        }
    }

    private void MoveToNextCheckpoint()
    {
        if (checkpoints == null || checkpoints.Length == 0) return;

        agent.SetDestination(checkpoints[currentIndex].position);
        agent.isStopped = false;
        isWalking = true;
    }
}
