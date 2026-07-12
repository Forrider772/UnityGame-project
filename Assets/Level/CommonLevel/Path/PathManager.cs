using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 路径数据管理类
/// 维护一条路线的所有路径点、所属阵营、路线ID等数据
/// 集成了 PathVisualManager 用于运行时可视化和交互反馈
/// 支持 Centripetal Catmull-Rom 曲线（可选）
/// </summary>
[SelectionBase]
[RequireComponent(typeof(PathVisualManager))]
public partial class PathManager : MonoBehaviour
{
    [Header("基础配置")]
    [Tooltip("该路线所属的阵营，用于部署时过滤")]
    public CampType camp;
    [Tooltip("路线唯一标识ID，用于区分不同路线")]
    public PathID pathId;
    [Tooltip("路径移动类型，控制哪种单位类型可使用此路径")]
    public PathMoveType moveType = PathMoveType.Ground;

    [Header("路径节点列表")]
    [Tooltip("按顺序排列的路径点Transform（空物体），定义了路线的形状")]
    public List<Transform> pathPoints = new List<Transform>();

    [Header("曲线设置")]
    [Tooltip("启用 Centripetal Catmull-Rom 曲线")]
    public bool useCurve = false;
    [Tooltip("曲线参数化指数：0.5=centripetal（推荐），0=均匀，1=弦长")]
    [Range(0.1f, 0.9f)]
    public float curveAlpha = 0.5f;
    [Tooltip("每段曲线 LineRenderer 采样点数（仅视觉）")]
    [Range(8, 30)]
    public int curveSamples = 15;

    [Header("循环路线")]
    [Tooltip("启用后路线首尾相连形成闭环，单位到达终点后自动回到起点循环前进")]
    public bool isLooping = false;

    private PathVisualManager visualManager;  // 视觉效果管理器引用

    // ==================== 曲线运行时缓存 ====================
    private float[] nodeParams;       // tᵢ — Centripetal 参数化值（循环模式=n+1元素含闭合弦长，普通=n元素）
    private float[] segArcLengths;    // 每段弧长（Gauss 求积）[0..segCount-1]
    private float[][] segArcLUT;      // 每段内弧长→s 映射表 [seg][sample]
    private float totalArcLength;     // 总弧长（循环模式含闭合段）
    private float[] cumArcLengths;    // 累加弧长（二分查找用）
    private const int ArcLUTSamples = 17; // 段内 LUT 采样数

    private int mathSegCount;         // 曲线段数（循环=n, 非循环=n-1）

    private void Awake()
    {
        visualManager = GetComponent<PathVisualManager>();
    }

    private void Start()
    {
        Vector2[] rawPoints = GetRawControlPoints();

        // 曲线模式：预计算参数化和弧长表
        if (useCurve && rawPoints.Length >= 2)
        {
            if (isLooping)
            {
                mathSegCount = rawPoints.Length; // 循环模式有 n 段（含闭合段）
                nodeParams = CatmullRomMath.BuildLoopNodeParams(rawPoints, curveAlpha); // n+1 元素
                BuildArcLengthTable(rawPoints, nodeParams, mathSegCount);
            }
            else
            {
                mathSegCount = rawPoints.Length - 1;
                nodeParams = CatmullRomMath.CentripetalParameterize(rawPoints, curveAlpha);
                BuildArcLengthTable(rawPoints, nodeParams, mathSegCount);
            }
        }
        else
        {
            mathSegCount = 0;
        }

        // 获取显示用路径点（直线=原始点，曲线=采样点）
        Vector2[] waypoints = GetWaypoints2D();

        // 初始化视觉管理器
        visualManager.Initialize(waypoints);

        // 初始时隐藏路线（未进入部署模式）
        visualManager.SetVisible(false);
    }

    /// <summary>
    /// 设置路线是否可见（由 CardDeploy 调用）
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (visualManager != null)
            visualManager.SetVisible(visible);
    }

    /// <summary>
    /// 设置路线高亮状态（由 CardDeploy 调用）
    /// </summary>
    public void SetHighlight(bool highlight)
    {
        if (visualManager != null)
            visualManager.SetHighlight(highlight);
    }

    /// <summary>
    /// 获取路线的起点世界坐标（第一个路径点）
    /// </summary>
    public Vector3 GetStartPoint()
    {
        if (pathPoints.Count > 0 && pathPoints[0] != null)
            return pathPoints[0].position;
        return Vector3.zero;
    }

    /// <summary>
    /// 获取所有路径点的世界坐标数组（Vector2 格式）
    /// 曲线模式下返回密集采样点，直线模式返回原始控制点
    /// </summary>
    public Vector2[] GetWaypoints2D()
    {
        Vector2[] rawPoints = GetRawControlPoints();
        if (rawPoints.Length < 2) return rawPoints;

        if (useCurve)
        {
            int segs = isLooping ? mathSegCount : rawPoints.Length - 1;
            float[] nParams = isLooping ? nodeParams : CatmullRomMath.CentripetalParameterize(rawPoints, curveAlpha);
            Vector2[] sampled = SampleCurveForDisplay(rawPoints, nParams, curveSamples, segs);

            // 循环模式：首尾相接到第一个点
            if (isLooping && sampled.Length > 0)
            {
                var closed = new List<Vector2>(sampled) { rawPoints[0] };
                return closed.ToArray();
            }
            return sampled;
        }

        // 直线模式：循环时首尾相接
        if (isLooping)
        {
            var looped = new List<Vector2>(rawPoints) { rawPoints[0] };
            return looped.ToArray();
        }
        return rawPoints;
    }

    // ==================== 曲线公共 API ====================

    /// <summary>
    /// 获取归一化距离处的曲线位置
    /// </summary>
    /// <summary>
    /// 获取归一化距离处的曲线位置
    /// </summary>
    /// <param name="normalizedDist">归一化距离 [0, 1]</param>
    public Vector2 GetCurvePoint(float normalizedDist)
    {
        if (!TryResolveArcParams(ref normalizedDist, out int seg, out float s, out Vector2[] pts))
        {
            Vector2[] rawPts = GetRawControlPoints();
            return isLooping
                ? PathMath.LoopLinearSample(rawPts, Mathf.Clamp01(normalizedDist))
                : PathMath.LinearSample(rawPts, Mathf.Clamp01(normalizedDist));
        }
        return CatmullRomMath.CRPosition(pts, nodeParams, seg, s, isLooping);
    }

    /// <summary>
    /// 获取归一化距离处的曲线切线方向
    /// </summary>
    public Vector2 GetCurveTangent(float normalizedDist)
    {
        if (!TryResolveArcParams(ref normalizedDist, out int seg, out float s, out Vector2[] pts))
        {
            Vector2[] rawPts = GetRawControlPoints();
            return isLooping
                ? PathMath.LoopLinearTangent(rawPts, Mathf.Clamp01(normalizedDist))
                : PathMath.LinearTangent(rawPts, Mathf.Clamp01(normalizedDist));
        }
        return CatmullRomMath.CRTangent(pts, nodeParams, seg, s, isLooping);
    }

    /// <summary>
    /// 解析弧长参数（GetCurvePoint 和 GetCurveTangent 共用逻辑）
    /// 返回 false 表示应使用线性回退
    /// </summary>
    private bool TryResolveArcParams(ref float dist, out int seg, out float s, out Vector2[] points)
    {
        if (isLooping)
        {
            dist = dist % 1.0f;
            if (dist < 0f) dist += 1.0f;
        }

        if (!useCurve || cumArcLengths == null || cumArcLengths.Length == 0)
        {
            seg = 0; s = 0f; points = null;
            return false;
        }

        dist = Mathf.Clamp01(dist);
        float targetDist = dist * totalArcLength;
        seg = CatmullRomMath.FindSegmentByDistance(cumArcLengths, targetDist);
        float segStartDist = seg > 0 ? cumArcLengths[seg - 1] : 0f;
        float arcFraction = segArcLengths[seg] > 0.0001f
            ? (targetDist - segStartDist) / segArcLengths[seg] : 0f;
        s = CatmullRomMath.ArcFractionToS(segArcLUT, seg, arcFraction);
        points = GetRawControlPoints();
        return true;
    }

    /// <summary>
    /// 获取路径总弧长（曲线模式为弧长，直线模式为折线总长）
    /// </summary>
    public float GetTotalArcLength()
    {
        if (useCurve && totalArcLength > 0f)
            return totalArcLength;

        // 回退：计算折线总长（循环模式含闭合段）
        Vector2[] pts = GetRawControlPoints();
        float len = 0f;
        for (int i = 0; i < pts.Length - 1; i++)
            len += Vector2.Distance(pts[i], pts[i + 1]);
        if (isLooping && pts.Length >= 2)
            len += Vector2.Distance(pts[pts.Length - 1], pts[0]);
        return Mathf.Max(len, 0.001f);
    }

    /// <summary>
    /// 获取路径上距离目标点最近的点（Newton 迭代法，不采样）
    /// </summary>
    /// <returns>(最近点位置, 归一化弧长距离, 最短距离)</returns>
    public (Vector2 point, float curveT, float distance) GetClosestPoint(Vector2 target)
    {
        Vector2[] raw = GetRawControlPoints();
        if (raw.Length < 2)
            return (raw.Length > 0 ? raw[0] : Vector2.zero, 0f,
                raw.Length > 0 ? Vector2.Distance(raw[0], target) : float.MaxValue);

        if (useCurve)
        {
            float[] nParams = isLooping ? nodeParams : CatmullRomMath.CentripetalParameterize(raw, curveAlpha);
            int segCount = isLooping ? mathSegCount : raw.Length - 1;

            int bestSeg = 0;
            float bestS = 0f;
            float bestDist = float.MaxValue;

            for (int seg = 0; seg < segCount; seg++)
            {
                // 粗扫 4 点
                float[] scanT = { 0f, 0.33f, 0.67f, 1f };
                float bestLocalS = 0f;
                float bestLocalDist = float.MaxValue;

                for (int k = 0; k < scanT.Length; k++)
                {
                    Vector2 p = CatmullRomMath.CRPosition(raw, nParams, seg, scanT[k], isLooping);
                    float scanDist = Vector2.Distance(p, target);
                    if (scanDist < bestLocalDist)
                    {
                        bestLocalDist = scanDist;
                        bestLocalS = scanT[k];
                    }
                }

                // Newton 迭代 3 次
                float s = Mathf.Clamp01(bestLocalS);
                for (int iter = 0; iter < 3; iter++)
                {
                    (Vector2 pos, Vector2 vel, Vector2 acc) = CatmullRomMath.CRDerivatives(raw, nParams, seg, s, isLooping);
                    float f = Vector2.Dot(pos - target, vel);
                    float fp = Vector2.Dot(vel, vel) + Vector2.Dot(pos - target, acc);
                    if (Mathf.Abs(fp) < 0.0001f) break;
                    s -= f / fp;
                    s = Mathf.Clamp01(s);
                }

                Vector2 finalPos = CatmullRomMath.CRPosition(raw, nParams, seg, s, isLooping);
                float finalDist = Vector2.Distance(finalPos, target);
                if (finalDist < bestDist)
                {
                    bestDist = finalDist;
                    bestSeg = seg;
                    bestS = s;
                }
            }

            Vector2 bestPoint = CatmullRomMath.CRPosition(raw, nParams, bestSeg, bestS, isLooping);

            // 计算归一化弧长距离
            float segStartArc = bestSeg > 0 ? cumArcLengths[bestSeg - 1] : 0f;
            // 近似：用 s 线性插值弧长
            float approxArcDist = segStartArc + bestS * segArcLengths[bestSeg];
            float curveT = totalArcLength > 0f ? approxArcDist / totalArcLength : 0f;

            return (bestPoint, curveT, bestDist);
        }

        // 直线模式：使用 Math2DHelper 的折线方法（循环模式额外检查闭合段）
        float totalLen = GetTotalArcLength();
        float d, ct;
        Vector2 closest;
        int segIdx;
        float t;

        if (isLooping && raw.Length >= 2)
        {
            // 先检查普通段
            closest = Math2DHelper.ClosestPointOnPolyline(target, raw, out segIdx, out t);
            d = Vector2.Distance(closest, target);

            float accum = 0f;
            for (int i = 0; i < segIdx; i++)
                accum += Vector2.Distance(raw[i], raw[i + 1]);
            accum += t * Vector2.Distance(raw[segIdx], raw[segIdx + 1]);
            ct = totalLen > 0f ? accum / totalLen : 0f;

            // 再检查闭合段
            Vector2 closeOnClosing = PathMath.ClosestPointOnSegment(target, raw[raw.Length - 1], raw[0], out float closingT);
            float closingDist = Vector2.Distance(closeOnClosing, target);
            if (closingDist < d)
            {
                float closingAccum = 0f;
                for (int i = 0; i < raw.Length - 1; i++)
                    closingAccum += Vector2.Distance(raw[i], raw[i + 1]);
                closingAccum += closingT * Vector2.Distance(raw[raw.Length - 1], raw[0]);
                return (closeOnClosing, totalLen > 0f ? closingAccum / totalLen : 0f, closingDist);
            }

            return (closest, ct, d);
        }

        closest = Math2DHelper.ClosestPointOnPolyline(target, raw, out segIdx, out t);
        d = Vector2.Distance(closest, target);

        float accum2 = 0f;
        for (int i = 0; i < segIdx; i++)
            accum2 += Vector2.Distance(raw[i], raw[i + 1]);
        accum2 += t * Vector2.Distance(raw[segIdx], raw[segIdx + 1]);
        ct = totalLen > 0f ? accum2 / totalLen : 0f;

        return (closest, ct, d);
    }

    /// <summary>
    /// 为 LineRenderer 采样曲线显示点
    /// </summary>
    private Vector2[] SampleCurveForDisplay(Vector2[] raw, float[] nParams, int samplesPerSegment, int segCount)
    {
        var result = new List<Vector2>(segCount * samplesPerSegment + 1);
        for (int seg = 0; seg < segCount; seg++)
        {
            int steps = (seg == segCount - 1) ? samplesPerSegment + 1 : samplesPerSegment;
            for (int i = 0; i < steps; i++)
            {
                float s = (float)i / samplesPerSegment;
                if (seg == segCount - 1 && i == steps - 1) s = 1f;
                result.Add(CatmullRomMath.CRPosition(raw, nParams, seg, s, isLooping));
            }
        }
        return result.ToArray();
    }

    /// <summary>
    /// 构建弧长表、段内 LUT 和累加弧长表
    /// </summary>
    private void BuildArcLengthTable(Vector2[] raw, float[] nParams, int segCount)
    {
        segArcLengths = new float[segCount];
        segArcLUT = new float[segCount][];
        cumArcLengths = new float[segCount];
        totalArcLength = 0f;

        for (int i = 0; i < segCount; i++)
        {
            segArcLengths[i] = CatmullRomMath.GaussArcLength(raw, nParams, i, isLooping);
            totalArcLength += segArcLengths[i];
            cumArcLengths[i] = totalArcLength;

            // 构建段内弧长→s 映射表（均匀采样 arc length，存归一化值）
            segArcLUT[i] = new float[ArcLUTSamples];
            float accum = 0f;
            segArcLUT[i][0] = 0f;
            Vector2 prev = CatmullRomMath.CRPosition(raw, nParams, i, 0f, isLooping);
            for (int k = 1; k < ArcLUTSamples; k++)
            {
                float s = (float)k / (ArcLUTSamples - 1);
                Vector2 curr = CatmullRomMath.CRPosition(raw, nParams, i, s, isLooping);
                accum += Vector2.Distance(prev, curr);
                segArcLUT[i][k] = accum;
                prev = curr;
            }
            // 归一化到 [0,1]
            if (accum > 0.0001f)
                for (int k = 0; k < ArcLUTSamples; k++)
                    segArcLUT[i][k] /= accum;
        }
    }

    // ==================== 原始控制点提取 ====================

    private Vector2[] GetRawControlPoints()
    {
        var pts = new List<Vector2>();
        for (int i = 0; i < pathPoints.Count; i++)
        {
            if (pathPoints[i] != null)
                pts.Add(pathPoints[i].position);
        }
        return pts.ToArray();
    }
}

