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
    [SerializeField] Transform pivot;

    [Header("Closed Direction Offset (deg)")]
    [Tooltip("閉じ(0°)の向きオフセット。0: X方向 / 90: Z方向")]
    [SerializeField] float closedYawOffsetDeg = 90f;

    [Header("Open Angle (deg) ※2枚の合計開き角")]
    [SerializeField] float targetAngleDeg = 0f;

    [Header("Rotate Speed (deg/sec)")]
    [SerializeField] float rotateSpeedDegPerSec = 180f;

    [Header("Editor Preview")]
    [Tooltip("Editor中は targetAngleDeg の状態を表示する")]
    [SerializeField] bool previewInEditMode = true;

    [Header("Play Start")]
    [Tooltip("Play開始時に閉じ(0°)から開く")]
    [SerializeField] bool openFromClosedOnPlayStart = true;

    [Tooltip("Editorプレビューで開いた姿勢のまま再生した場合、Start時のrawBase逆算に targetAngleDeg を使う（全体角度ズレ防止）")]
    [SerializeField] bool assumeScenePoseIsTargetOnPlayStart = true;

    [Header("Closed Pose Rule")]
    [Tooltip("0°(閉じ)で2枚が向かい合うように、左をYで180°反転させる")]
    [SerializeField] bool flipLeft180OnClosed = true;

    [Header("Optional Z-Stack (avoid z-fighting)")]
    [SerializeField] float stackDepthZ = 0.01f;
    [SerializeField] bool stackInWorldZ = true;

    [Header("NavMesh Rebuild")]
    [SerializeField] NavMeshSurface[] navMeshSurfaces;
    [SerializeField] bool rebakeOnFirstApply = true;
    [SerializeField] bool rebakeOnReachedTarget = true;
    [SerializeField] float reachedEpsilonDeg = 0.01f;

    [Header("Debug")]
    [SerializeField] bool debugLog = true;

    // Unity Primitive Plane は 10x10（中心から端まで 5）
    const float PlaneHalfSize = 5f;

    float _currentOpenDeg;

    // raw（オフセット/開き角を含まない基準回転）
    Quaternion _rawBaseRight;
    Quaternion _rawBaseLeft;
    bool _hasRawBase;

    Coroutine _rebakeCo;

    void OnEnable()
    {
        if (!leftPlane || !rightPlane) return;

        // --- ここが原因修正の本体 ---
        // Play開始時点のシーン姿勢が「targetAngleで開いてるプレビュー姿勢」なら、
        // その前提で rawBase を逆算してから、閉じ→開きにする。
        if (Application.isPlaying)
        {
            float captureOpen =
                (assumeScenePoseIsTargetOnPlayStart && previewInEditMode)
                ? targetAngleDeg
                : 0f;

            CaptureRawBaseFromCurrentPose(captureOpen);

            if (openFromClosedOnPlayStart)
                _currentOpenDeg = 0f;           // ★閉じから開始
            else
                _currentOpenDeg = targetAngleDeg; // ★最初から開き

            ApplyImmediate();

            if (rebakeOnFirstApply)
                RequestRebake("FirstApply");
        }
        else
        {
            // Editor：初回だけraw作って、プレビュー反映
            if (!_hasRawBase)
                CaptureRawBaseFromCurrentPose(previewInEditMode ? targetAngleDeg : 0f);

            if (previewInEditMode)
            {
                _currentOpenDeg = targetAngleDeg;
                ApplyImmediate();
            }
        }
    }

    void OnValidate()
    {
        if (!leftPlane || !rightPlane) return;

        // Editor中：数値変更で即プレビュー（rawBaseは勝手に作り直さない＝offset相殺しない）
        if (!Application.isPlaying && previewInEditMode)
        {
            if (!_hasRawBase)
                CaptureRawBaseFromCurrentPose(targetAngleDeg);

            _currentOpenDeg = targetAngleDeg;
            ApplyImmediate();
        }
    }

    void Update()
    {
        if (!leftPlane || !rightPlane) return;
        if (!Application.isPlaying) return;

        float prev = _currentOpenDeg;

        // 実行中は目標へ開く（要望：Start時も開く → openFromClosedOnPlayStart=true で閉じから開く）
        _currentOpenDeg = Mathf.MoveTowards(
            _currentOpenDeg,
            targetAngleDeg,
            rotateSpeedDegPerSec * Time.deltaTime
        );

        ApplyImmediate();

        // 到達した瞬間だけベイク
        if (rebakeOnReachedTarget)
        {
            bool wasAtTarget = Mathf.Abs(prev - targetAngleDeg) <= reachedEpsilonDeg;
            bool isAtTarget = Mathf.Abs(_currentOpenDeg - targetAngleDeg) <= reachedEpsilonDeg;

            if (!wasAtTarget && isAtTarget)
            {
                if (debugLog)
                    Debug.Log($"[StageFrameManager] ReachedTarget (prev={prev}, cur={_currentOpenDeg}, target={targetAngleDeg})", this);

                RequestRebake("ReachedTarget");
            }
        }
    }

    public void SetOpenAngle(float openAngleDeg)
    {
        targetAngleDeg = openAngleDeg;
    }

    /// <summary>
    /// 「今の見た目が openAngleDeg の状態」とみなして、offset + (±open/2) を差し引いて raw を作る
    /// </summary>
    void CaptureRawBaseFromCurrentPose(float openAngleDeg)
    {
        float half = openAngleDeg * 0.5f;

        // rightPlane は「offset + (+half)」が乗った姿勢だとみなして raw を逆算
        _rawBaseRight = Quaternion.AngleAxis(-(closedYawOffsetDeg + half), Vector3.up) * rightPlane.rotation;

        // left は right を閉じで向かい合わせにする
        _rawBaseLeft = flipLeft180OnClosed
            ? (Quaternion.AngleAxis(180f, Vector3.up) * _rawBaseRight)
            : (Quaternion.AngleAxis(-(closedYawOffsetDeg - half), Vector3.up) * leftPlane.rotation);

        _hasRawBase = true;
    }

    [ContextMenu("Recapture Raw Base From Current (Use current open)")]
    public void RecaptureRawBaseFromCurrent()
    {
        float open = Application.isPlaying ? _currentOpenDeg : (previewInEditMode ? targetAngleDeg : 0f);
        CaptureRawBaseFromCurrentPose(open);
        ApplyImmediate();
    }

    void ApplyImmediate()
    {
        if (!_hasRawBase)
            CaptureRawBaseFromCurrentPose(Application.isPlaying ? _currentOpenDeg : (previewInEditMode ? targetAngleDeg : 0f));

        float half = _currentOpenDeg * 0.5f;
        float zHalf = stackDepthZ * 0.5f;

        Apply(leftPlane, _rawBaseLeft, -half, EdgeKind.RightEdgeAtPivot, -zHalf);
        Apply(rightPlane, _rawBaseRight, +half, EdgeKind.LeftEdgeAtPivot, +zHalf);
    }

    enum EdgeKind { RightEdgeAtPivot, LeftEdgeAtPivot }

    void Apply(Transform plane, Quaternion rawBaseRot, float yawDeg, EdgeKind edge, float stackOffsetZ)
    {
        Vector3 p = pivot ? pivot.position : Vector3.zero;

        // ★ここでoffsetが効く（rawBaseを毎回作り直さないので相殺されない）
        Quaternion rot = Quaternion.AngleAxis(closedYawOffsetDeg + yawDeg, Vector3.up) * rawBaseRot;
        plane.rotation = rot;

        float halfWidth = PlaneHalfSize * plane.lossyScale.x;
        Vector3 rightDir = rot * Vector3.right;

        Vector3 center = (edge == EdgeKind.RightEdgeAtPivot)
            ? (p - rightDir * halfWidth)  // 左: 右端をPivotへ
            : (p + rightDir * halfWidth); // 右: 左端をPivotへ

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

        if (debugLog)
        {
            var tri = NavMesh.CalculateTriangulation();
            Debug.Log($"[StageFrameManager] Rebake done. reason={reason}, surfacesBuilt={ok}, verts={tri.vertices.Length}, tris={tri.indices.Length / 3}", this);
        }

        _rebakeCo = null;
    }
}
