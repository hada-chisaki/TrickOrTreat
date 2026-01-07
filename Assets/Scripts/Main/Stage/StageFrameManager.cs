using System.Collections;
using System.Collections.Generic;
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

    [Header("Open Angle (deg) ※左右別")]
    [SerializeField] float targetLeftDeg = 0f;   // 左の開き角（正の値を推奨）
    [SerializeField] float targetRightDeg = 0f;  // 右の開き角（正の値を推奨）

    [Header("Rotate Speed (deg/sec)")]
    [SerializeField] float rotateSpeedDegPerSec = 180f;

    [Header("Editor Preview")]
    [Tooltip("Editor中は targetLeft/RightDeg の状態を表示する")]
    [SerializeField] bool previewInEditMode = true;

    [Header("Play Start")]
    [Tooltip("Play開始時に閉じ(0°)から開く")]
    [SerializeField] bool openFromClosedOnPlayStart = true;

    [Tooltip("Editorプレビューで開いた姿勢のまま再生した場合、Start時のrawBase逆算に targetLeft/RightDeg を使う（全体角度ズレ防止）")]
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

    float _curLeftDeg;
    float _curRightDeg;

    // raw（オフセット/開き角を含まない基準回転）
    Quaternion _rawBaseRight;
    Quaternion _rawBaseLeft;
    bool _hasRawBase;

    Coroutine _rebakeCo;

    void OnEnable()
    {
        if (!leftPlane || !rightPlane) return;

        if (Application.isPlaying)
        {
            float captureLeft =
                (assumeScenePoseIsTargetOnPlayStart && previewInEditMode) ? targetLeftDeg : 0f;
            float captureRight =
                (assumeScenePoseIsTargetOnPlayStart && previewInEditMode) ? targetRightDeg : 0f;

            CaptureRawBaseFromCurrentPose(captureLeft, captureRight);

            if (openFromClosedOnPlayStart)
            {
                _curLeftDeg = 0f;
                _curRightDeg = 0f;
            }
            else
            {
                _curLeftDeg = targetLeftDeg;
                _curRightDeg = targetRightDeg;
            }

            ApplyImmediate();

            if (rebakeOnFirstApply)
                RequestRebake("FirstApply");
        }
        else
        {
            if (!_hasRawBase)
                CaptureRawBaseFromCurrentPose(previewInEditMode ? targetLeftDeg : 0f,
                                              previewInEditMode ? targetRightDeg : 0f);

            if (previewInEditMode)
            {
                _curLeftDeg = targetLeftDeg;
                _curRightDeg = targetRightDeg;
                ApplyImmediate();
            }
        }
    }

    void OnValidate()
    {
        if (!leftPlane || !rightPlane) return;

        if (!Application.isPlaying && previewInEditMode)
        {
            if (!_hasRawBase)
                CaptureRawBaseFromCurrentPose(targetLeftDeg, targetRightDeg);

            _curLeftDeg = targetLeftDeg;
            _curRightDeg = targetRightDeg;
            ApplyImmediate();
        }
    }

    void Update()
    {
        if (!leftPlane || !rightPlane) return;
        if (!Application.isPlaying) return;

        float prevL = _curLeftDeg;
        float prevR = _curRightDeg;

        _curLeftDeg = Mathf.MoveTowards(_curLeftDeg, targetLeftDeg, rotateSpeedDegPerSec * Time.deltaTime);
        _curRightDeg = Mathf.MoveTowards(_curRightDeg, targetRightDeg, rotateSpeedDegPerSec * Time.deltaTime);

        ApplyImmediate();

        if (rebakeOnReachedTarget)
        {
            bool wasAtTarget =
                Mathf.Abs(prevL - targetLeftDeg) <= reachedEpsilonDeg &&
                Mathf.Abs(prevR - targetRightDeg) <= reachedEpsilonDeg;

            bool isAtTarget =
                Mathf.Abs(_curLeftDeg - targetLeftDeg) <= reachedEpsilonDeg &&
                Mathf.Abs(_curRightDeg - targetRightDeg) <= reachedEpsilonDeg;

            if (!wasAtTarget && isAtTarget)
            {
                if (debugLog)
                    Debug.Log($"[StageFrameManager] ReachedTarget (L:{prevL}->{_curLeftDeg}/{targetLeftDeg}, R:{prevR}->{_curRightDeg}/{targetRightDeg})", this);

                RequestRebake("ReachedTarget");
            }
        }
    }

    // 互換：合計角度を渡したい場合は左右に半分ずつ割る
    public void SetOpenAngle(float totalOpenDeg)
    {
        float half = totalOpenDeg * 0.5f;
        targetLeftDeg = half;
        targetRightDeg = half;
    }

    // 新：左右別指定
    public void SetOpenAngles(float leftDeg, float rightDeg)
    {
        targetLeftDeg = leftDeg;
        targetRightDeg = rightDeg;
    }

    /// <summary>
    /// 「今の見た目が left/rightDeg の状態」とみなして、offset + yaw を差し引いて raw を作る
    /// </summary>
    void CaptureRawBaseFromCurrentPose(float leftDeg, float rightDeg)
    {
        // rightPlane は「offset + (+rightDeg)」が乗った姿勢だとみなして raw を逆算
        _rawBaseRight = Quaternion.AngleAxis(-(closedYawOffsetDeg + rightDeg), Vector3.up) * rightPlane.rotation;

        // leftPlane の raw は、ルールに従って決定
        if (flipLeft180OnClosed)
        {
            // 閉じ(0°)で向かい合う、というルールを優先（旧仕様踏襲）
            _rawBaseLeft = Quaternion.AngleAxis(180f, Vector3.up) * _rawBaseRight;
        }
        else
        {
            // 左も現在姿勢から逆算（left は Apply で -leftDeg を渡す前提）
            _rawBaseLeft = Quaternion.AngleAxis(-(closedYawOffsetDeg - leftDeg), Vector3.up) * leftPlane.rotation;
        }

        _hasRawBase = true;
    }

    [ContextMenu("Recapture Raw Base From Current (Use current L/R)")]
    public void RecaptureRawBaseFromCurrent()
    {
        float l = Application.isPlaying ? _curLeftDeg : (previewInEditMode ? targetLeftDeg : 0f);
        float r = Application.isPlaying ? _curRightDeg : (previewInEditMode ? targetRightDeg : 0f);
        CaptureRawBaseFromCurrentPose(l, r);
        ApplyImmediate();
    }

    void ApplyImmediate()
    {
        if (!_hasRawBase)
        {
            float l = Application.isPlaying ? _curLeftDeg : (previewInEditMode ? targetLeftDeg : 0f);
            float r = Application.isPlaying ? _curRightDeg : (previewInEditMode ? targetRightDeg : 0f);
            CaptureRawBaseFromCurrentPose(l, r);
        }

        float zHalf = stackDepthZ * 0.5f;

        // 左はマイナス方向に開く（旧: -half）
        Apply(leftPlane, _rawBaseLeft, -_curLeftDeg, EdgeKind.RightEdgeAtPivot, -zHalf);

        // 右はプラス方向に開く（旧: +half）
        Apply(rightPlane, _rawBaseRight, +_curRightDeg, EdgeKind.LeftEdgeAtPivot, +zHalf);
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
