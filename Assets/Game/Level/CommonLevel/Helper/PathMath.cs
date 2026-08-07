using UnityEngine;

/// <summary>
/// 路径直线模式纯数学工具类
/// 所有方法均为静态纯函数，无 Unity 组件依赖
/// </summary>
public static class PathMath
{
    /// <summary>
    /// 直线位置采样（开放折线）
    /// </summary>
    public static Vector2 LinearSample(Vector2[] pts, float t)
    {
        if (pts.Length < 2) return pts.Length > 0 ? pts[0] : Vector2.zero;
        float totalLen = 0f;
        for (int i = 0; i < pts.Length - 1; i++)
            totalLen += Vector2.Distance(pts[i], pts[i + 1]);
        float target = t * totalLen;
        float accum = 0f;
        for (int i = 0; i < pts.Length - 1; i++)
        {
            float segLen = Vector2.Distance(pts[i], pts[i + 1]);
            if (accum + segLen >= target || i == pts.Length - 2)
            {
                float localT = segLen > 0.0001f ? (target - accum) / segLen : 0f;
                return Vector2.Lerp(pts[i], pts[i + 1], Mathf.Clamp01(localT));
            }
            accum += segLen;
        }
        return pts[pts.Length - 1];
    }

    /// <summary>
    /// 直线切线方向（开放折线）
    /// </summary>
    public static Vector2 LinearTangent(Vector2[] pts, float t)
    {
        if (pts.Length < 2) return Vector2.right;
        float totalLen = 0f;
        for (int i = 0; i < pts.Length - 1; i++)
            totalLen += Vector2.Distance(pts[i], pts[i + 1]);
        float target = t * totalLen;
        float accum = 0f;
        for (int i = 0; i < pts.Length - 1; i++)
        {
            float segLen = Vector2.Distance(pts[i], pts[i + 1]);
            if (accum + segLen >= target || i == pts.Length - 2)
                return (pts[i + 1] - pts[i]).normalized;
            accum += segLen;
        }
        return (pts[pts.Length - 1] - pts[pts.Length - 2]).normalized;
    }

    /// <summary>
    /// 循环模式直线位置采样（闭合多边形，含 last→first 段）
    /// </summary>
    public static Vector2 LoopLinearSample(Vector2[] pts, float t)
    {
        if (pts.Length < 2) return pts.Length > 0 ? pts[0] : Vector2.zero;
        int n = pts.Length;
        float totalLen = 0f;
        for (int i = 0; i < n; i++)
            totalLen += Vector2.Distance(pts[i], pts[(i + 1) % n]);
        float target = t * totalLen;
        float accum = 0f;
        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;
            float segLen = Vector2.Distance(pts[i], pts[next]);
            if (accum + segLen >= target || i == n - 1)
            {
                float localT = segLen > 0.0001f ? (target - accum) / segLen : 0f;
                return Vector2.Lerp(pts[i], pts[next], Mathf.Clamp01(localT));
            }
            accum += segLen;
        }
        return pts[0];
    }

    /// <summary>
    /// 循环模式直线切线方向（闭合多边形）
    /// </summary>
    public static Vector2 LoopLinearTangent(Vector2[] pts, float t)
    {
        if (pts.Length < 2) return Vector2.right;
        int n = pts.Length;
        float totalLen = 0f;
        for (int i = 0; i < n; i++)
            totalLen += Vector2.Distance(pts[i], pts[(i + 1) % n]);
        float target = t * totalLen;
        float accum = 0f;
        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;
            float segLen = Vector2.Distance(pts[i], pts[next]);
            if (accum + segLen >= target || i == n - 1)
                return (pts[next] - pts[i]).normalized;
            accum += segLen;
        }
        return (pts[1] - pts[0]).normalized;
    }

    /// <summary>
    /// 计算点到线段的最近点
    /// </summary>
    public static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b, out float t)
    {
        Vector2 ab = b - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 0.0001f)
        {
            t = 0f;
            return a;
        }
        t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lenSq);
        return Vector2.LerpUnclamped(a, b, t);
    }
}
