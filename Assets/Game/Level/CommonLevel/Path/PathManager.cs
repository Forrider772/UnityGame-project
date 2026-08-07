using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 路径数据管理类
/// 维护一条路线的所有路径点、所属阵营、路线ID等数据
/// 通过子物体 PathVisualManager 实现运行时可视化和交互反馈
/// 支持 Centripetal Catmull-Rom 曲线（可选）
/// </summary>
[SelectionBase]
public partial class PathManager : MonoBehaviour
{
    [Header("基础配置")]
    [Tooltip("该路线所属的阵营，用于部署时过滤")]
    public CampType camp;
    [Tooltip("路线唯一标识ID，用于区分不同路线")]
    public PathID pathId;
    [Tooltip("路径移动类型，控制哪种单位类型可使用此路径")]
    public MoveType moveType = MoveType.Ground;

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

    [Header("连接类型配置")]
    [Tooltip("每个路径段的连接类型。n 个路径点有 n-1 个段落（循环模式 n 个）。索引 i 对应 pathPoints[i] → pathPoints[i+1]")]
    public List<ConnectionType> connectionTypes = new List<ConnectionType>();

    [Tooltip("传送段的等待时间（秒），不受单位移动速度影响")]
    [Range(0f, 10f)]
    public float teleportTime = 3.0f;

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
        visualManager = GetComponentInChildren<PathVisualManager>();
        if (visualManager == null)
            Debug.LogWarning($"PathManager [{camp}] {pathId}: 未找到子物体 PathVisualManager，路径将不可见", this);
        BuildCurveData();
        SyncConnectionTypes();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 编辑器中路径点数量变化时自动同步 connectionTypes 列表大小（延迟执行避免序列化冲突）
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            SyncConnectionTypes();
        };
    }
#endif

    /// <summary>
    /// 预计算曲线弧长表（Awake 中执行，确保任何 Start 阶段查询路径时数据已就绪）
    /// </summary>
    private void BuildCurveData()
    {
        Vector2[] rawPoints = GetRawControlPoints();

        if (useCurve && rawPoints.Length >= 2)
        {
            if (isLooping)
            {
                mathSegCount = rawPoints.Length;
                nodeParams = CatmullRomMath.BuildLoopNodeParams(rawPoints, curveAlpha);
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
    }

    private void Start()
    {
        // 获取显示用路径点（直线=原始点，曲线=采样点）
        Vector2[] waypoints = GetWaypoints2D();

        // 初始化视觉管理器
        if (visualManager != null)
        {
            visualManager.Initialize(waypoints);
            // 初始时隐藏路线（未进入部署模式）
            visualManager.SetVisible(false);
        }
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
    /// 设置路线透明度（由 GarrisonPointPlacer 调用）
    /// </summary>
    public void SetAlpha(float alpha)
    {
        if (visualManager != null)
            visualManager.SetAlpha(alpha);
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

    // ==================== 路径段查询 API ====================

    /// <summary>
    /// 确保 connectionTypes 列表大小与路径段数一致（不足补齐 Walk，多余裁剪）
    /// </summary>
    private void SyncConnectionTypes()
    {
        int segCount = GetSegmentCount();
        while (connectionTypes.Count < segCount)
            connectionTypes.Add(ConnectionType.Walk);
        while (connectionTypes.Count > segCount)
            connectionTypes.RemoveAt(connectionTypes.Count - 1);
    }

    /// <summary>
    /// 获取路径段总数
    /// 直线模式 = 有效路径点数 - 1（循环模式 +1），曲线模式使用 mathSegCount
    /// </summary>
    public int GetSegmentCount()
    {
        if (useCurve && mathSegCount > 0)
            return mathSegCount;

        Vector2[] raw = GetRawControlPoints();
        if (raw.Length < 2) return 0;
        return isLooping ? raw.Length : raw.Length - 1;
    }

    /// <summary>
    /// 获取指定路径段的连接类型（索引越界或列表为空时返回 Walk，向后兼容）
    /// </summary>
    public ConnectionType GetConnectionType(int segmentIndex)
    {
        if (connectionTypes == null || connectionTypes.Count == 0) return ConnectionType.Walk;
        if (segmentIndex < 0 || segmentIndex >= connectionTypes.Count) return ConnectionType.Walk;
        return connectionTypes[segmentIndex];
    }

    /// <summary>
    /// 获取指定路径段的归一化起始进度 [0, 1]
    /// </summary>
    public float GetSegmentStartProgress(int segmentIndex)
    {
        int segCount = GetSegmentCount();
        if (segCount <= 0) return 0f;

        float totalLen = GetTotalArcLength();
        if (totalLen <= 0f) return 0f;

        float segStartDist;
        if (useCurve && cumArcLengths != null && cumArcLengths.Length > 0)
        {
            // 曲线模式：使用预计算累加弧长表
            int clamped = Mathf.Clamp(segmentIndex, 0, cumArcLengths.Length);
            segStartDist = clamped > 0 ? cumArcLengths[clamped - 1] : 0f;
        }
        else
        {
            // 直线模式：按折线累加计算
            Vector2[] pts = GetRawControlPoints();
            int limit = Mathf.Min(segmentIndex, segCount);
            segStartDist = 0f;
            for (int i = 0; i < limit; i++)
            {
                int next = isLooping ? (i + 1) % pts.Length : i + 1;
                segStartDist += Vector2.Distance(pts[i], pts[next]);
            }
        }
        return segStartDist / totalLen;
    }

    /// <summary>
    /// 获取指定路径段的归一化结束进度 [0, 1]
    /// </summary>
    public float GetSegmentEndProgress(int segmentIndex)
    {
        int segCount = GetSegmentCount();
        if (segCount <= 0) return 1f;

        // 最后一个段（非循环）结束于路径终点
        if (!isLooping && segmentIndex >= segCount - 1)
            return 1f;
        return GetSegmentStartProgress(segmentIndex + 1);
    }

    /// <summary>
    /// 获取指定路径段起点的世界坐标
    /// </summary>
    public Vector2 GetSegmentStartPoint(int segmentIndex)
    {
        return GetCurvePoint(GetSegmentStartProgress(segmentIndex));
    }

    /// <summary>
    /// 获取指定路径段终点的世界坐标
    /// </summary>
    public Vector2 GetSegmentEndPoint(int segmentIndex)
    {
        return GetCurvePoint(GetSegmentEndProgress(segmentIndex));
    }

    /// <summary>
    /// 根据归一化距离 [0,1] 找到当前所在的路径段索引
    /// 循环模式自动 wrap，非循环 clamp 到 [0, segCount-1]
    /// </summary>
    public int GetSegmentAtProgress(float normalizedProgress)
    {
        int segCount = GetSegmentCount();
        if (segCount <= 0) return 0;

        if (isLooping)
        {
            normalizedProgress = normalizedProgress % 1.0f;
            if (normalizedProgress < 0f) normalizedProgress += 1.0f;
        }
        else
        {
            normalizedProgress = Mathf.Clamp01(normalizedProgress);
        }

        // 曲线模式：用累加弧长表二分查找
        if (useCurve && cumArcLengths != null && cumArcLengths.Length > 0)
        {
            float targetDist = normalizedProgress * totalArcLength;
            return CatmullRomMath.FindSegmentByDistance(cumArcLengths, targetDist);
        }

        // 直线模式：按折线累加长度查找
        // 注意：边界位置归属右侧段（accum + segLen > target 严格大于），
        // 与曲线模式 FindSegmentByDistance 的边界语义保持一致，
        // 避免传送完成后因边界浮点歧义重复触发传送
        Vector2[] pts = GetRawControlPoints();
        float totalLen = GetTotalArcLength();
        float target = normalizedProgress * totalLen;
        float accum = 0f;
        int n = isLooping ? pts.Length : pts.Length - 1;
        for (int i = 0; i < n; i++)
        {
            int next = isLooping ? (i + 1) % pts.Length : i + 1;
            float segLen = Vector2.Distance(pts[i], pts[next]);
            if (accum + segLen > target || i == n - 1)
                return i;
            accum += segLen;
        }
        return n - 1;
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

