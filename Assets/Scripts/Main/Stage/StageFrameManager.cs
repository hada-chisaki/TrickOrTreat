using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

[ExecuteAlways]
public sealed class StageFrameManager : MonoBehaviour
{
    [Header("Planes")]
    [SerializeField] Transform leftPlane;   // 右端をPivot
    [SerializeField] Transform rightPlane;  // 左端をPivot

    [Header("Pivot (null = world zero)")]
    [SerializeField] Transform pivot; // nullなら(0,0,0)

    [Header("Closed Direction Offset (deg)")]
    [SerializeField] float closedYawOffsetDeg = 90f;

    [Header("Open Angle (deg) ※2枚の合計開き角")]
    [SerializeField] float targetAngleDeg = 0f;

    [Header("Rotate Speed (deg/sec)")]
    [SerializeField] float rotateSpeedDegPerSec = 180f;

    [Header("Start Behavior")]
    [Tooltip("Play開始時はゆっくりではなく、最初から目標角度にする")]
    [SerializeField] bool snapToTargetOnPlayStart = true;

    [Header("Closed Pose Rule")]
    [SerializeField] bool flipLeft180OnClosed = true;

    [Header("Optional Z-Stack (avoid z-fighting)")]
    [SerializeField] float stackDepthZ = 0.01f;
    [SerializeField] bool stackInWorldZ = true;

    [Header("NavMesh Rebuild")]
    [SerializeField] NavMeshSurface[] navMeshSurfaces; // Transform入らない
    [SerializeField] bool rebakeOnFirstApply = true;
    [SerializeField] bool rebakeOnReachedTarget = true;
    [SerializeField] float reachedEpsilonDeg = 0.01f;

    [Header("Debug")]
    [SerializeField] bool debugLog = true;

    const float PlaneHalfSize = 5f;

    float _currentOpenDeg;
    float _lastTargetDeg;

    // オフセットに依存しない「生の閉じ基準回転」
    Quaternion _rawBaseRight;
    Quaternion _rawBaseLeft;
    bool _hasRawBase;

    Coroutine _rebakeCo;

    void OnEnable()
    {
        if (!leftPlane || !rightPlane) return;

        if (!_hasRawBase)
            CaptureRawClosedBaseFromCurrent();

        if (Application.isPlaying && snapToTargetOnPlayStart)
            _currentOpenDeg = targetAngleDeg;   // ★開始時は目標角度にスナップ
        else
            _currentOpenDeg = 0f;

        _lastTargetDeg = targetAngleDeg;

        ApplyImmediate();

        if (Application.isPlaying && rebakeOnFirstApply)
            RequestRebake("FirstApply");
    }

    void OnValidate()
    {
        if (!leftPlane || !rightPlane) return;

        if (!_hasRawBase)
            CaptureRawClosedBaseFromCurrent();

        _currentOpenDeg = 0f;
        _lastTargetDeg = targetAngleDeg;

        ApplyImmediate();
    }

    void Update()
    {
        if (!leftPlane || !rightPlane) return;

        // 目標変更
        if (!Mathf.Approximately(_lastTargetDeg, targetAngleDeg))
        {
            _lastTargetDeg = targetAngleDeg;
        }

        float prev = _currentOpenDeg;

        if (Application.isPlaying)
        {
            // ★開始時スナップ後は、通常はMoveTowardsで追従（速度0なら動かない＝到達しない）
            _currentOpenDeg = Mathf.MoveTowards(
                _currentOpenDeg,
                targetAngleDeg,
                rotateSpeedDegPerSec * Time.deltaTime
            );
        }
        else
        {
            _currentOpenDeg = 0f;
        }

        float half = _currentOpenDeg * 0.5f;
        float zHalf = stackDepthZ * 0.5f;

        Apply(leftPlane, _rawBaseLeft, -half, EdgeKind.RightEdgeAtPivot, -zHalf);
        Apply(rightPlane, _rawBaseRight, +half, EdgeKind.LeftEdgeAtPivot, +zHalf);

        // ---- 到達判定（取りこぼさない版）----
        if (Application.isPlaying && rebakeOnReachedTarget)
        {
            bool wasAtTarget = Mathf.Abs(prev - targetAngleDeg) <= reachedEpsilonDeg;
            bool isAtTarget = Mathf.Abs(_currentOpenDeg - targetAngleDeg) <= reachedEpsilonDeg;

            // 「未到達 → 到達」に変わった瞬間だけベイク
            if (!wasAtTarget && isAtTarget)
            {
                // ここは debugLog に関係なくログ出す（出ない問題の切り分け用）
                Debug.Log($"[StageFrameManager] ReachedTarget (prev={prev}, cur={_currentOpenDeg}, target={targetAngleDeg})", this);
                RequestRebake("ReachedTarget");
            }

            // 追加の切り分け（ログが出ない時）
            if (debugLog)
            {
                // 回転がそもそも進んでいるかチェック
                if (Mathf.Approximately(prev, _currentOpenDeg) && !Mathf.Approximately(_currentOpenDeg, targetAngleDeg))
                {
                    Debug.LogWarning(
                        $"[StageFrameManager] Not moving toward target. speed={rotateSpeedDegPerSec}, prev={prev}, cur={_currentOpenDeg}, target={targetAngleDeg}",
                        this
                    );
                }
            }
        }
    }

    public void SetOpenAngle(float openAngleDeg)
    {
        targetAngleDeg = openAngleDeg;
    }

    [ContextMenu("Recapture Raw Closed Base From Current")]
    public void CaptureRawClosedBaseFromCurrent()
    {
        if (!rightPlane) return;

        _rawBaseRight = Quaternion.AngleAxis(-closedYawOffsetDeg, Vector3.up) * rightPlane.rotation;

        _rawBaseLeft = flipLeft180OnClosed
            ? (Quaternion.AngleAxis(180f, Vector3.up) * _rawBaseRight)
            : (leftPlane
                ? Quaternion.AngleAxis(-closedYawOffsetDeg, Vector3.up) * leftPlane.rotation
                : _rawBaseRight);

        _hasRawBase = true;
    }

    void ApplyImmediate()
    {
        float half = _currentOpenDeg * 0.5f;
        float zHalf = stackDepthZ * 0.5f;

        Apply(leftPlane, _rawBaseLeft, -half, EdgeKind.RightEdgeAtPivot, -zHalf);
        Apply(rightPlane, _rawBaseRight, +half, EdgeKind.LeftEdgeAtPivot, +zHalf);
    }

    enum EdgeKind { RightEdgeAtPivot, LeftEdgeAtPivot }

    void Apply(Transform plane, Quaternion rawBaseRot, float yawDeg, EdgeKind edge, float stackOffsetZ)
    {
        Vector3 p = pivot ? pivot.position : Vector3.zero;

        Quaternion rot = Quaternion.AngleAxis(closedYawOffsetDeg + yawDeg, Vector3.up) * rawBaseRot;
        plane.rotation = rot;

        float halfWidth = PlaneHalfSize * plane.lossyScale.x;
        Vector3 rightDir = rot * Vector3.right;

        Vector3 center = (edge == EdgeKind.RightEdgeAtPivot)
            ? (p - rightDir * halfWidth)
            : (p + rightDir * halfWidth);

        if (stackDepthZ != 0f)
        {
            Vector3 zDir = stackInWorldZ ? Vector3.forward : (rot * Vector3.forward);
            center += zDir * stackOffsetZ;
        }

        plane.position = center;
    }

    void RequestRebake(string reason)
    {
        if (navMeshSurfaces == null || navMeshSurfaces.Length == 0)
        {
            Debug.LogWarning($"[StageFrameManager] NavMeshSurface is empty. reason={reason}", this);
            return;
        }

        if (_rebakeCo != null) StopCoroutine(_rebakeCo);
        _rebakeCo = StartCoroutine(RebakeEndOfFrame(reason));
    }

    IEnumerator RebakeEndOfFrame(string reason)
    {
        yield return new WaitForEndOfFrame();

        int ok = 0;
        foreach (var s in navMeshSurfaces)
        {
            if (!s) continue;
            s.RemoveData();
            s.BuildNavMesh();
            ok++;
        }

        var tri = NavMesh.CalculateTriangulation();
        Debug.Log($"[StageFrameManager] Rebake done. reason={reason}, surfacesBuilt={ok}, verts={tri.vertices.Length}, tris={tri.indices.Length / 3}", this);

        _rebakeCo = null;
    }
}
