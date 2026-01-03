using System.Collections;
using System.Reflection;
using UnityEngine;

[ExecuteAlways]
public sealed class StageFrameManager : MonoBehaviour
{
    [Header("Planes")]
    [SerializeField] Transform leftPlane;   // 右端をPivot
    [SerializeField] Transform rightPlane;  // 左端をPivot

    [Header("Closed Direction Offset (deg)")]
    [SerializeField] float closedYawOffsetDeg = 90f; // 0: X方向, 90: Z方向

    [Header("Pivot (null = world zero)")]
    [SerializeField] Transform pivot;

    [Header("Open Angle (deg) ※2枚の合計開き角")]
    [SerializeField] float targetAngleDeg = 0f;

    [Header("Rotate Speed (deg/sec)")]
    [SerializeField] float rotateSpeedDegPerSec = 180f;

    [Header("Closed Pose Rule")]
    [Tooltip("0°(閉じ)で2枚が重なるように、左をYで180°反転させる")]
    [SerializeField] bool flipLeft180OnClosed = true;

    [Header("Stack (Z)")]
    [SerializeField] float stackDepthZ = 0.01f;
    [SerializeField] bool stackInWorldZ = true;

    [Header("NavMesh Rebuild (optional)")]
    [SerializeField] Component[] navMeshSurfaces;
    [SerializeField] bool autoFindNavMeshSurfaces = true;
    [SerializeField] bool rebakeOnReachedTarget = true;
    [SerializeField] bool rebakeOnFirstApply = true;     // ★追加：初回配置でも焼く
    [SerializeField] float reachedEpsilonDeg = 0.01f;

    const float PlaneHalfSize = 5f;

    float _currentOpenDeg;
    float _lastTargetDeg;
    bool _rebakePending;

    // ★ここが重要：オフセットに依存しない「生の閉じ基準回転」
    Quaternion _rawBaseRight;
    Quaternion _rawBaseLeft;
    bool _hasRawBase;

    void OnEnable()
    {
        if (!leftPlane || !rightPlane) return;

        EnsureNavMeshSurfaces();

        // ★基準は一度だけ作る（オフセットを差し引いて raw を作る）
        if (!_hasRawBase)
            CaptureRawClosedBaseFromCurrent();

        _currentOpenDeg = 0f;
        _lastTargetDeg = targetAngleDeg;
        _rebakePending = true; // ★初回も到達も焼けるように

        ApplyImmediate();

        if (Application.isPlaying && rebakeOnFirstApply)
            StartCoroutine(RebakeNextFrame());
    }

    void OnValidate()
    {
        if (!leftPlane || !rightPlane) return;

        EnsureNavMeshSurfaces();

        // ★ここで再キャプチャしない（オフセットが積み上がる原因）
        if (!_hasRawBase)
            CaptureRawClosedBaseFromCurrent();

        _currentOpenDeg = 0f;
        _lastTargetDeg = targetAngleDeg;

        ApplyImmediate();
    }

    void Update()
    {
        if (!leftPlane || !rightPlane) return;

        if (!Mathf.Approximately(_lastTargetDeg, targetAngleDeg))
        {
            _lastTargetDeg = targetAngleDeg;
            _rebakePending = true;
        }

        if (Application.isPlaying)
        {
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

        if (Application.isPlaying && rebakeOnReachedTarget && _rebakePending)
        {
            if (Mathf.Abs(_currentOpenDeg - targetAngleDeg) <= reachedEpsilonDeg)
            {
                RebuildNavMeshOnce();
                _rebakePending = false;
            }
        }
    }

    public void SetOpenAngle(float openAngleDeg)
    {
        targetAngleDeg = openAngleDeg;
        _rebakePending = true;
    }

    // ★手動で基準を取り直したい時用（オフセットを変えた後など）
    [ContextMenu("Recapture Raw Closed Base From Current")]
    public void CaptureRawClosedBaseFromCurrent()
    {
        if (!rightPlane) return;

        // いまの右Plane回転は「(closedYawOffsetDegが乗った見た目)」なので、それを差し引いて raw を作る
        _rawBaseRight = Quaternion.AngleAxis(-closedYawOffsetDeg, Vector3.up) * rightPlane.rotation;

        _rawBaseLeft = flipLeft180OnClosed
            ? (Quaternion.AngleAxis(180f, Vector3.up) * _rawBaseRight)
            : (leftPlane ? Quaternion.AngleAxis(-closedYawOffsetDeg, Vector3.up) * leftPlane.rotation : _rawBaseRight);

        _hasRawBase = true;
    }

    IEnumerator RebakeNextFrame()
    {
        yield return null; // Transform反映後に焼く
        RebuildNavMeshOnce();
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

        // ★rawBase に対して「オフセット + 開き角」を毎回計算で乗せる（積み上がらない）
        Quaternion rot = Quaternion.AngleAxis(closedYawOffsetDeg + yawDeg, Vector3.up) * rawBaseRot;
        plane.rotation = rot;

        float halfWidth = PlaneHalfSize * plane.lossyScale.x;
        Vector3 rightDir = rot * Vector3.right;

        Vector3 center = edge == EdgeKind.RightEdgeAtPivot
            ? (p - rightDir * halfWidth)
            : (p + rightDir * halfWidth);

        Vector3 zDir = stackInWorldZ ? Vector3.forward : (rot * Vector3.forward);
        center += zDir * stackOffsetZ;

        plane.position = center;
    }

    void EnsureNavMeshSurfaces()
    {
        if (!autoFindNavMeshSurfaces) return;
        if (navMeshSurfaces != null && navMeshSurfaces.Length > 0) return;

        // まず子階層
        var monosChild = GetComponentsInChildren<MonoBehaviour>(true);
        navMeshSurfaces = FindNavMeshSurfacesFrom(monosChild);

        // 子に無ければシーン全体（重要）
        if (navMeshSurfaces == null || navMeshSurfaces.Length == 0)
        {
#if UNITY_2023_1_OR_NEWER
            var monosAll = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var monosAll = Object.FindObjectsOfType<MonoBehaviour>(true);
#endif
            navMeshSurfaces = FindNavMeshSurfacesFrom(monosAll);
        }
    }

    static Component[] FindNavMeshSurfacesFrom(MonoBehaviour[] monos)
    {
        int count = 0;
        for (int i = 0; i < monos.Length; i++)
            if (monos[i] && monos[i].GetType().Name == "NavMeshSurface")
                count++;

        if (count == 0) return System.Array.Empty<Component>();

        var arr = new Component[count];
        int idx = 0;
        for (int i = 0; i < monos.Length; i++)
            if (monos[i] && monos[i].GetType().Name == "NavMeshSurface")
                arr[idx++] = monos[i];

        return arr;
    }

    void RebuildNavMeshOnce()
    {
        if (navMeshSurfaces == null || navMeshSurfaces.Length == 0)
        {
            Debug.LogWarning("[StageFrameManager] NavMeshSurface not found. Assign it or enable autoFind.", this);
            return;
        }

        for (int i = 0; i < navMeshSurfaces.Length; i++)
        {
            var c = navMeshSurfaces[i];
            if (!c) continue;

            var m = c.GetType().GetMethod(
                "BuildNavMesh",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (m != null && m.GetParameters().Length == 0)
                m.Invoke(c, null);
            else
                Debug.LogWarning($"[StageFrameManager] BuildNavMesh() not found on {c.GetType().FullName}", c);
        }
    }
}
