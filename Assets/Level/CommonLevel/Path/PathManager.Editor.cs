using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
/// <summary>
/// PathManager 编辑器扩展（Gizmos 绘制 + ContextMenu 菜单）
/// 作为 partial class 放在与 PathManager.cs 同目录，通过 #if UNITY_EDITOR 在构建时排除编辑器代码
/// </summary>
public partial class PathManager
{
    // ==================== Gizmo 颜色/大小常量 ====================
    private static readonly Color unselectedColor    = new Color(0.3f, 0.5f, 1f, 0.6f);
    private static readonly Color selectedColor      = new Color(1f, 0.85f, 0.2f, 1f);
    private static readonly Color arrowColor         = new Color(1f, 0.6f, 0.1f, 0.9f);
    private static readonly Color arrowColorSelected = new Color(1f, 0.3f, 0.1f, 1f);
    private const float unselectedNodeRadius = 0.08f;
    private const float selectedNodeRadius   = 0.25f;
    private const float unselectedLineWidth  = 2f;
    private const float selectedLineWidth    = 6f;
    private const float arrowSize            = 0.35f;
    private const float arrowSizeSelected    = 0.6f;

    // ==================== ContextMenu 右键菜单 ====================

    [ContextMenu("复制路径（反向 + 切换阵营）")]
    private void DuplicateReversedAndSwapCamp()
    {
        DuplicatePath(reverse: true, swapCamp: true);
    }

    [ContextMenu("复制路径（仅反向）")]
    private void DuplicateReversed()
    {
        DuplicatePath(reverse: true, swapCamp: false);
    }

    [ContextMenu("复制路径（仅切换阵营）")]
    private void DuplicateSwapCamp()
    {
        DuplicatePath(reverse: false, swapCamp: true);
    }

    private void DuplicatePath(bool reverse, bool swapCamp)
    {
        GameObject newGo = UnityEngine.Object.Instantiate(gameObject, transform.parent != null ? transform.parent.parent : null);
        newGo.transform.position = transform.position;
        newGo.transform.rotation = transform.rotation;
        newGo.transform.localScale = transform.localScale;
        newGo.transform.SetSiblingIndex(transform.GetSiblingIndex() + 1);
        newGo.name = gameObject.name + (reverse ? "_Reversed" : "") + (swapCamp ? "_Swapped" : "");

        PathManager newPath = newGo.GetComponent<PathManager>();

        if (swapCamp)
        {
            newPath.camp = camp == CampType.Player ? CampType.Enemy : CampType.Player;
        }

        if (reverse)
        {
            newPath.pathPoints.Reverse();
            for (int i = 0; i < newPath.pathPoints.Count; i++)
            {
                if (newPath.pathPoints[i] != null)
                    newPath.pathPoints[i].SetSiblingIndex(i);
            }
        }

        UnityEditor.Selection.activeGameObject = newGo;
        Debug.Log($"已创建新路径: {newGo.name} | 阵营={newPath.camp} | 方向={(reverse ? "反向" : "正向")}");
    }

    // ==================== Gizmos 绘制 ====================

    private void OnDrawGizmos()
    {
        if (IsChildSelected())
        {
            // 子物体（路径点）被选中时，显示整条路径的高亮
            DrawPathLine(selectedColor, selectedLineWidth);
            DrawPathNodes(selectedColor, selectedNodeRadius);
            DrawDirectionArrows(arrowColorSelected, arrowSizeSelected, filled: true);
            DrawPathLabel();
            DrawStartPointMarker();
        }
        else
        {
            DrawPathLine(unselectedColor, unselectedLineWidth);
            DrawPathNodes(unselectedColor, unselectedNodeRadius);
            DrawDirectionArrows(arrowColor, arrowSize, filled: false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        DrawPathLine(selectedColor, selectedLineWidth);
        DrawPathNodes(selectedColor, selectedNodeRadius);
        DrawDirectionArrows(arrowColorSelected, arrowSizeSelected, filled: true);
        DrawPathLabel();
        DrawStartPointMarker();
    }

    /// <summary>
    /// 判断当前选中的 Transform 是否是本路径的子物体（路径点）
    /// </summary>
    private bool IsChildSelected()
    {
        Transform active = UnityEditor.Selection.activeTransform;
        return active != null && active != transform && active.IsChildOf(transform);
    }

    /// <summary>
    /// 绘制起点标记 — 循环模式下绿色高亮 + "起点" 标签，非循环模式略大圆点
    /// </summary>
    private void DrawStartPointMarker()
    {
        if (pathPoints.Count == 0 || pathPoints[0] == null) return;

        Vector3 startPos = pathPoints[0].position;

        if (isLooping)
        {
            // 循环模式：绿色高亮球 + "起点" 标签
            Color loopStartColor = new Color(0f, 1f, 0.35f, 1f);
            Gizmos.color = loopStartColor;
            Gizmos.DrawSphere(startPos, selectedNodeRadius * 1.5f);

            UnityEditor.Handles.color = loopStartColor;
            UnityEditor.Handles.Label(
                startPos + Vector3.up * 0.85f,
                "起点",
                new GUIStyle()
                {
                    normal = new GUIStyleState { textColor = loopStartColor },
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                }
            );
        }
        else
        {
            // 非循环模式：用选中色略大的球标记起点
            Gizmos.color = selectedColor;
            Gizmos.DrawSphere(startPos, selectedNodeRadius * 1.25f);
        }
    }

    private void DrawPathLine(Color color, float width)
    {
        List<Vector3> displayPoints = GetEditorDisplayPoints();
        if (displayPoints.Count < 2) return;

        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawAAPolyLine(width, displayPoints.ToArray());
    }

    private void DrawPathNodes(Color color, float radius)
    {
        Gizmos.color = color;
        for (int i = 0; i < pathPoints.Count; i++)
        {
            if (pathPoints[i] != null)
                Gizmos.DrawSphere(pathPoints[i].position, radius);
        }
    }

    /// <summary>
    /// 绘制方向箭头 — 曲线模式沿曲线切线放置，直线模式在线段中点
    /// </summary>
    private void DrawDirectionArrows(Color color, float size, bool filled)
    {
        if (pathPoints == null || pathPoints.Count < 2) return;

        UnityEditor.Handles.color = color;

        if (useCurve)
        {
            Vector2[] raw = GetRawControlPoints();
            if (raw.Length < 2) return;

            GetCurveParams(raw, out Vector2[] pts, out float[] nParams, out int segCount);

            for (int seg = 0; seg < segCount; seg++)
            {
                float halfArc = CatmullRomMath.GaussArcLength(pts, nParams, seg, isLooping) * 0.5f;
                float s = 0.5f;

                float lo = 0f, hi = 1f;
                for (int iter = 0; iter < 8; iter++)
                {
                    float mid = (lo + hi) * 0.5f;
                    float arcAtMid = PartialArcLength(pts, nParams, seg, 0f, mid);
                    if (arcAtMid < halfArc) lo = mid;
                    else hi = mid;
                }
                s = (lo + hi) * 0.5f;

                Vector2 pos = CatmullRomMath.CRPosition(pts, nParams, seg, s, isLooping);
                Vector2 dir = CatmullRomMath.CRTangent(pts, nParams, seg, s, isLooping);

                if (dir.magnitude > 0.001f)
                    DrawSingleArrow(pos, dir, size, filled);
            }
        }
        else
        {
            int lineSegs = isLooping ? pathPoints.Count : pathPoints.Count - 1;
            for (int i = 0; i < lineSegs; i++)
            {
                int next = (i + 1) % pathPoints.Count;
                if (pathPoints[i] == null || pathPoints[next] == null) continue;

                Vector3 from = pathPoints[i].position;
                Vector3 to   = pathPoints[next].position;
                Vector3 mid  = (from + to) * 0.5f;
                Vector3 dir  = (to - from).normalized;

                DrawSingleArrow(mid, dir, size, filled);
            }
        }
    }

    /// <summary>
    /// 估算段内局部弧长（采样近似，专供编辑器箭头使用）
    /// </summary>
    private float PartialArcLength(Vector2[] raw, float[] nParams, int seg, float s0, float s1)
    {
        int steps = 8;
        float len = 0f;
        Vector2 prev = CatmullRomMath.CRPosition(raw, nParams, seg, s0, isLooping);
        for (int i = 1; i <= steps; i++)
        {
            float s = Mathf.Lerp(s0, s1, (float)i / steps);
            Vector2 curr = CatmullRomMath.CRPosition(raw, nParams, seg, s, isLooping);
            len += Vector2.Distance(prev, curr);
            prev = curr;
        }
        return len;
    }

    private void DrawSingleArrow(Vector3 pos, Vector3 direction, float size, bool filled)
    {
        Vector3 tip = pos + direction * (size * 0.5f);
        Vector3 baseCenter = pos - direction * (size * 0.5f);
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);
        Vector3 baseLeft  = baseCenter + perpendicular * (size * 0.45f);
        Vector3 baseRight = baseCenter - perpendicular * (size * 0.45f);

        if (filled)
            UnityEditor.Handles.DrawAAConvexPolygon(tip, baseLeft, baseRight);
        else
        {
            UnityEditor.Handles.DrawLine(baseLeft, tip);
            UnityEditor.Handles.DrawLine(baseRight, tip);
            UnityEditor.Handles.DrawLine(baseLeft, baseRight);
        }
    }

    private void DrawPathLabel()
    {
        if (pathPoints.Count == 0 || pathPoints[0] == null) return;

        UnityEditor.Handles.color = selectedColor;
        UnityEditor.Handles.Label(
            pathPoints[0].position + Vector3.up * 0.5f,
            $"[{camp}] {pathId} ({moveType})",
            new GUIStyle()
            {
                normal = new GUIStyleState { textColor = selectedColor },
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            }
        );
    }

    /// <summary>
    /// 获取编辑器曲线参数（始终即时计算，不依赖运行时缓存）
    /// </summary>
    private void GetCurveParams(Vector2[] raw, out Vector2[] pts, out float[] nParams, out int segCount)
    {
        pts = raw;
        if (isLooping)
        {
            nParams = CatmullRomMath.BuildLoopNodeParams(raw, curveAlpha);
            segCount = raw.Length;
        }
        else
        {
            nParams = CatmullRomMath.CentripetalParameterize(raw, curveAlpha);
            segCount = raw.Length - 1;
        }
    }

    /// <summary>
    /// 获取编辑器显示的路径点（曲线=采样点，直线=原始点）
    /// </summary>
    private List<Vector3> GetEditorDisplayPoints()
    {
        var points = new List<Vector3>();
        if (pathPoints == null || pathPoints.Count < 2) return points;

        if (useCurve)
        {
            Vector2[] raw = GetRawControlPoints();
            if (raw.Length < 2)
            {
                for (int i = 0; i < pathPoints.Count; i++)
                    if (pathPoints[i] != null) points.Add(pathPoints[i].position);
                return points;
            }

            GetCurveParams(raw, out Vector2[] pts, out float[] nParams, out int segs);
            Vector2[] sampled = SampleCurveForDisplay(pts, nParams, curveSamples, segs);
            foreach (var p in sampled)
                points.Add(p);

            if (isLooping && pathPoints.Count > 0 && pathPoints[0] != null)
                points.Add(pathPoints[0].position);
        }
        else
        {
            for (int i = 0; i < pathPoints.Count; i++)
                if (pathPoints[i] != null) points.Add(pathPoints[i].position);

            if (isLooping && points.Count > 0)
                points.Add(points[0]);
        }

        return points;
    }
}
#endif
