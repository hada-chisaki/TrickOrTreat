using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class NormalEnemy : Enemy
{
    [Header("NavMesh Agent")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Start Delay")]
    [SerializeField] private float startDelaySeconds = 1.0f; // 開始何秒後に動き出すか
    [SerializeField] private bool disableAgentDuringDelay = true; // trueなら enabled=false にして「切る」

    [Header("Checkpoint1 (Find by Name)")]
    [SerializeField] private string checkpoint1Name = "CheckPoint1";

    [Header("Checkpoint2 (Random child BoxCollider under parent by name)")]
    [SerializeField] private string checkpoint2ParentName = "CheckPoint2"; // ←親の名前（ここを検索）
    [Tooltip("親の直下の子にBoxColliderが無い場合、孫以降も含めて探す")]
    [SerializeField] private bool checkpoint2SearchInChildren = true;

    [Header("Move Settings")]
    [SerializeField] private float waitAfterArrival = 1f;

    [Header("Events")]
    public UnityEvent onReachPoint1;
    public UnityEvent onReachPoint2;
    public UnityEvent onAllPointsReached;

    private Collider[] _checkpoints;
    private int _currentIndex = 0;
    private bool _isWaiting = false;
    private bool _navActive = false;

    protected override void Awake()
    {
        base.Awake();

        if (!agent) agent = GetComponent<NavMeshAgent>();

        // チェックポイントをシーンから名前検索で取得（2つだけ）
        _checkpoints = new Collider[2];

        // CheckPoint1：従来通り、名前でCollider取得
        _checkpoints[0] = FindCheckpointCollider(checkpoint1Name);

        // CheckPoint2：親を名前で検索 → 子からランダムに1つ選ぶ → その子のBoxColliderを使用
        _checkpoints[1] = FindRandomChildBoxColliderFromParent(checkpoint2ParentName, checkpoint2SearchInChildren);
    }

    protected override void Start()
    {
        base.Start(); // 親のStart（StartSE再生など）も実行
        StartCoroutine(BeginAfterDelay());
    }

    private IEnumerator BeginAfterDelay()
    {
        // 開始から指定秒数は NavMeshAgent を切っておく
        if (agent && disableAgentDuringDelay)
            agent.enabled = false;

        if (startDelaySeconds > 0f)
            yield return new WaitForSeconds(startDelaySeconds);

        if (!agent)
        {
            Debug.LogWarning($"{name}: NavMeshAgent がありません。");
            yield break;
        }

        if (disableAgentDuringDelay)
            agent.enabled = true;

        // チェックポイントが見つからない場合は止める
        if (_checkpoints == null || _checkpoints.Length < 2 || !_checkpoints[0] || !_checkpoints[1])
        {
            Debug.LogWarning($"{name}: CheckPoint が見つかりません。Name={checkpoint1Name}, Parent={checkpoint2ParentName}");
            yield break;
        }

        _currentIndex = 0;
        _navActive = true;

        MoveToCheckpoint(_checkpoints[_currentIndex].transform.position);
    }

    private Collider FindCheckpointCollider(string objName)
    {
        if (string.IsNullOrEmpty(objName)) return null;

        var go = GameObject.Find(objName);
        if (!go)
        {
            Debug.LogWarning($"{name}: CheckPoint '{objName}' がシーンに存在しません。");
            return null;
        }

        var col = go.GetComponent<Collider>();
        if (!col)
        {
            Debug.LogWarning($"{name}: CheckPoint '{objName}' に Collider がありません。");
            return null;
        }

        if (!col.isTrigger)
            Debug.LogWarning($"{name}: CheckPoint '{objName}' の Collider は Trigger 推奨です（OnTriggerEnter用）。");

        return col;
    }

    private Collider FindRandomChildBoxColliderFromParent(string parentName, bool searchInChildren)
    {
        if (string.IsNullOrEmpty(parentName)) return null;

        var parent = GameObject.Find(parentName);
        if (!parent)
        {
            Debug.LogWarning($"{name}: CheckPoint2 Parent '{parentName}' がシーンに存在しません。");
            return null;
        }

        // まず「親の直下の子」から BoxCollider を集める（inactive は除外）
        var candidates = new List<BoxCollider>();
        foreach (Transform child in parent.transform)
        {
            if (!child.gameObject.activeInHierarchy) continue; // ★追加：falseならスキップ

            var box = child.GetComponent<BoxCollider>();
            if (box) candidates.Add(box);
        }

        // 直下に無いなら、孫以降も含めて探す（オプション）（inactive は除外）
        if (candidates.Count == 0 && searchInChildren)
        {
            var all = parent.GetComponentsInChildren<BoxCollider>(true); // true=inactiveも拾う
            foreach (var box in all)
            {
                if (box.transform == parent.transform) continue;
                if (!box.gameObject.activeInHierarchy) continue; // ★追加：falseならスキップ

                candidates.Add(box);
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"{name}: Parent '{parentName}' の子(または子孫)に有効な BoxCollider が見つかりません。");
            return null;
        }

        var picked = candidates[Random.Range(0, candidates.Count)];

        if (!picked.isTrigger)
            Debug.LogWarning($"{name}: CheckPoint2 '{picked.name}' の BoxCollider は Trigger 推奨です（OnTriggerEnter用）。");

        return picked;
    }


    private void MoveToCheckpoint(Vector3 pos)
    {
        if (!agent.enabled) agent.enabled = true;

        // agent が NavMesh 上にいない時の保険
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 5f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        agent.isStopped = false;
        agent.SetDestination(pos);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        // 親（ハンマーHitなど）の判定も生かす
        base.OnTriggerEnter(other);

        if (!_navActive) return;
        if (_isWaiting) return;
        if (_checkpoints == null || _checkpoints.Length == 0) return;
        if (_currentIndex >= _checkpoints.Length) return;

        // 現在の目的地のチェックポイントに触れたら次へ
        if (other == _checkpoints[_currentIndex])
        {
            switch (_currentIndex)
            {
                case 0: onReachPoint1?.Invoke(); break;
                case 1: onReachPoint2?.Invoke(); break;
            }

            StartCoroutine(WaitAndGoNext());
        }
    }

    private IEnumerator WaitAndGoNext()
    {
        _isWaiting = true;

        if (agent && agent.enabled)
            agent.isStopped = true;

        if (waitAfterArrival > 0f)
            yield return new WaitForSeconds(waitAfterArrival);

        _currentIndex++;

        if (_currentIndex < _checkpoints.Length)
        {
            MoveToCheckpoint(_checkpoints[_currentIndex].transform.position);
        }
        else
        {
            _navActive = false;
            onAllPointsReached?.Invoke();
        }

        _isWaiting = false;
    }

    // Enemy側の死亡シーケンスに合わせて、ナビを止めたい場合
    protected override IEnumerator DieSequence()
    {
        _navActive = false;

        if (agent)
        {
            if (agent.enabled) agent.isStopped = true;
        }

        yield return base.DieSequence();
    }

    public void GoalCheckPoint2()
    {

        Destroy(this.gameObject);
    }
}
