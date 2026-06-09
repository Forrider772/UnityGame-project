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
public class PathManager : MonoBehaviour
{
    [Header("基础配置")]
    [Tooltip("该路线所属的阵营，用于部署时过滤")]
    public CampType camp;
    [Tooltip("路线唯一标识ID，用于区分不同路线")]
    public PathID pathId;

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

    private PathVisualManager visualManager;  // 视觉效果管理器引用

    // ==================== 曲线运行时缓存 ====================
    private float[] nodeParams;       // tᵢ — Centripetal 参数化值 [0..n-1]
    private float[] segArcLengths;    // 每段弧长（Gauss 求积）[0..n-2]
    private float[][] segArcLUT;      // 每段内弧长→s 映射表 [seg][sample]
    private float totalArcLength;     // 总弧长
    private float[] cumArcLengths;    // 累加弧长（二分查找用）
    private const int ArcLUTSamples = 17; // 段内 LUT 采样数

    private void Awake()
    {
        visualManager = GetComponent<PathVisualManager>();
    }

    private void Start()
    {
        // 曲线模式：预计算参数化和弧长表
        if (useCurve && pathPoints.Count >= 2)
        {
            Vector2[] rawPoints = GetRawControlPoints();
            if (rawPoints.Length >= 2)
            {
                nodeParams = CentripetalParameterize(rawPoints);
                BuildArcLengthTable(rawPoints, nodeParams);
            }
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

#if UNITY_EDITOR
    /// <summary>
    /// 右键菜单：复制当前路径，反向路径点，切换到对立阵营
    /// </summary>
    [ContextMenu("复制路径（反向 + 切换阵营）")]
    private void DuplicateReversedAndSwapCamp()
    {
        DuplicatePath(reverse: true, swapCamp: true);
    }

    /// <summary>
    /// 右键菜单：复制当前路径并反向路径点（保持阵营不变）
    /// </summary>
    [ContextMenu("复制路径（仅反向）")]
    private void DuplicateReversed()
    {
        DuplicatePath(reverse: true, swapCamp: false);
    }

    /// <summary>
    /// 右键菜单：复制当前路径并切换到对立阵营（保持方向不变）
    /// </summary>
    [ContextMenu("复制路径（仅切换阵营）")]
    private void DuplicateSwapCamp()
    {
        DuplicatePath(reverse: false, swapCamp: true);
    }

    /// <summary>
    /// 复制路径的通用实现
    /// </summary>
    /// <param name="reverse">是否反向路径点顺序</param>
    /// <param name="swapCamp">是否切换到对立阵营</param>
    private void DuplicatePath(bool reverse, bool swapCamp)
    {
        // 1. 复制整个 GameObject，放在父级的上一级，保持完全相同的 transform
        GameObject newGo = Instantiate(gameObject, transform.parent != null ? transform.parent.parent : null);
        newGo.transform.position = transform.position;
        newGo.transform.rotation = transform.rotation;
        newGo.transform.localScale = transform.localScale;
        newGo.transform.SetSiblingIndex(transform.GetSiblingIndex() + 1);
        newGo.name = gameObject.name + (reverse ? "_Reversed" : "") + (swapCamp ? "_Swapped" : "");

        // 2. 获取新对象的 PathManager
        PathManager newPath = newGo.GetComponent<PathManager>();

        // 3. 切换阵营
        if (swapCamp)
        {
            newPath.camp = camp == CampType.Player ? CampType.Enemy : CampType.Player;
        }

        // 4. 反向路径点
        if (reverse)
        {
            // 收集所有子 Transform（路径点），反转在层级中的顺序
            List<Transform> children = new();
            for (int i = 0; i < newGo.transform.childCount; i++)
                children.Add(newGo.transform.GetChild(i));

            children.Reverse();
            for (int i = 0; i < children.Count; i++)
                children[i].SetSiblingIndex(i);

            // 刷新 pathPoints 列表引用
            newPath.pathPoints = new List<Transform>();
            for (int i = 0; i < newGo.transform.childCount; i++)
                newPath.pathPoints.Add(newGo.transform.GetChild(i));
        }

        // 5. 选中新对象
        UnityEditor.Selection.activeGameObject = newGo;

        Debug.Log($"已创建新路径: {newGo.name} | 阵营={newPath.camp} | 方向={(reverse ? "反向" : "正向")}");
    }
#endif

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
            float[] nParams = CentripetalParameterize(rawPoints);
            return SampleCurveForDisplay(rawPoints, nParams, curveSamples);
        }

        return rawPoints;
    }

    // ==================== 曲线公共 API ====================

    /// <summary>
    /// 获取归一化距离处的曲线位置
    /// </summary>
    /// <param name="normalizedDist">归一化距离 [0, 1]</param>
    public Vector2 GetCurvePoint(float normalizedDist)
    {
        if (!useCurve || cumArcLengths == null || cumArcLengths.Length == 0)
        {
            // 回退到原始点线性插值
            Vector2[] pts = GetRawControlPoints();
            return LinearSample(pts, Mathf.Clamp01(normalizedDist));
        }

        normalizedDist = Mathf.Clamp01(normalizedDist);
        float targetDist = normalizedDist * totalArcLength;

        // 二分查找段索引
        int seg = FindSegmentByDistance(targetDist);
        float segStartDist = seg > 0 ? cumArcLengths[seg - 1] : 0f;
        float arcFraction = segArcLengths[seg] > 0.0001f
            ? (targetDist - segStartDist) / segArcLengths[seg]
            : 0f;

        // LUT 转换：弧长比例 → CR 参数 s（保证匀速）
        float s = ArcFractionToS(seg, arcFraction);
        Vector2[] raw = GetRawControlPoints();
        return CRPosition(raw, nodeParams, seg, s);
    }

    /// <summary>
    /// 获取归一化距离处的曲线切线方向
    /// </summary>
    public Vector2 GetCurveTangent(float normalizedDist)
    {
        if (!useCurve || cumArcLengths == null || cumArcLengths.Length == 0)
        {
            Vector2[] pts = GetRawControlPoints();
            return LinearTangent(pts, Mathf.Clamp01(normalizedDist));
        }

        normalizedDist = Mathf.Clamp01(normalizedDist);
        float targetDist = normalizedDist * totalArcLength;

        int seg = FindSegmentByDistance(targetDist);
        float segStartDist = seg > 0 ? cumArcLengths[seg - 1] : 0f;
        float arcFraction = segArcLengths[seg] > 0.0001f
            ? (targetDist - segStartDist) / segArcLengths[seg]
            : 0f;

        float s = ArcFractionToS(seg, arcFraction);
        Vector2[] raw = GetRawControlPoints();
        return CRTangent(raw, nodeParams, seg, s);
    }

    /// <summary>
    /// 获取路径总弧长（曲线模式为弧长，直线模式为折线总长）
    /// </summary>
    public float GetTotalArcLength()
    {
        if (useCurve && totalArcLength > 0f)
            return totalArcLength;

        // 回退：计算折线总长
        Vector2[] pts = GetRawControlPoints();
        float len = 0f;
        for (int i = 0; i < pts.Length - 1; i++)
            len += Vector2.Distance(pts[i], pts[i + 1]);
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
            float[] nParams = CentripetalParameterize(raw);
            int bestSeg = 0;
            float bestS = 0f;
            float bestDist = float.MaxValue;

            for (int seg = 0; seg < raw.Length - 1; seg++)
            {
                // 粗扫 4 点
                float[] scanT = { 0f, 0.33f, 0.67f, 1f };
                float bestLocalS = 0f;
                float bestLocalDist = float.MaxValue;

                for (int k = 0; k < scanT.Length; k++)
                {
                    Vector2 p = CRPosition(raw, nParams, seg, scanT[k]);
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
                    (Vector2 pos, Vector2 vel, Vector2 acc) = CRDerivatives(raw, nParams, seg, s);
                    float f = Vector2.Dot(pos - target, vel);
                    float fp = Vector2.Dot(vel, vel) + Vector2.Dot(pos - target, acc);
                    if (Mathf.Abs(fp) < 0.0001f) break;
                    s -= f / fp;
                    s = Mathf.Clamp01(s);
                }

                Vector2 finalPos = CRPosition(raw, nParams, seg, s);
                float finalDist = Vector2.Distance(finalPos, target);
                if (finalDist < bestDist)
                {
                    bestDist = finalDist;
                    bestSeg = seg;
                    bestS = s;
                }
            }

            Vector2 bestPoint = CRPosition(raw, nParams, bestSeg, bestS);

            // 计算归一化弧长距离
            float segStartArc = bestSeg > 0 ? cumArcLengths[bestSeg - 1] : 0f;
            // 近似：用 s 线性插值弧长
            float approxArcDist = segStartArc + bestS * segArcLengths[bestSeg];
            float curveT = totalArcLength > 0f ? approxArcDist / totalArcLength : 0f;

            return (bestPoint, curveT, bestDist);
        }

        // 直线模式：使用 Math2DHelper 的折线方法
        int segIdx;
        float t;
        Vector2 closest = Math2DHelper.ClosestPointOnPolyline(target, raw, out segIdx, out t);
        float d = Vector2.Distance(closest, target);

        // 计算归一化距离
        float totalLen = GetTotalArcLength();
        float accum = 0f;
        for (int i = 0; i < segIdx; i++)
            accum += Vector2.Distance(raw[i], raw[i + 1]);
        accum += t * Vector2.Distance(raw[segIdx], raw[segIdx + 1]);
        float ct = totalLen > 0f ? accum / totalLen : 0f;

        return (closest, ct, d);
    }

    // ==================== 曲线核心数学 ====================

    /// <summary>
    /// Centripetal 参数化：tᵢ = tᵢ₋₁ + |Pᵢ - Pᵢ₋₁|^alpha
    /// </summary>
    private float[] CentripetalParameterize(Vector2[] points)
    {
        int n = points.Length;
        float[] t = new float[n];
        t[0] = 0f;
        for (int i = 1; i < n; i++)
            t[i] = t[i - 1] + Mathf.Pow(Vector2.Distance(points[i - 1], points[i]), curveAlpha);
        return t;
    }

    /// <summary>
    /// 获取 CR 段所需的 4 个控制点和参数值
    /// 端点使用反射虚拟点（而非重复），保证端点处切线自然不塌缩
    /// </summary>
    private void GetCRQuad(Vector2[] points, float[] nodeParams, int seg,
        out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3,
        out float t0, out float t1, out float t2, out float t3)
    {
        int n = points.Length;

        // P1, P2 — 段两端锚点
        p1 = points[seg];
        p2 = points[Mathf.Min(seg + 1, n - 1)];
        t1 = nodeParams[seg];
        t2 = nodeParams[Mathf.Min(seg + 1, n - 1)];

        // P0 — 首段用反射，否则取前一个锚点
        if (seg == 0)
        {
            p0 = p1 + (p1 - p2);          // 反射 P₂ 过 P₁
            t0 = t1 - (t2 - t1);
        }
        else
        {
            p0 = points[seg - 1];
            t0 = nodeParams[seg - 1];
        }

        // P3 — 末段用反射，否则取后后一个锚点
        if (seg >= n - 2)
        {
            p3 = p2 + (p2 - p1);          // 反射 P₁ 过 P₂
            t3 = t2 + (t2 - t1);
        }
        else
        {
            p3 = points[seg + 2];
            t3 = nodeParams[seg + 2];
        }
    }

    /// <summary>
    /// Centripetal Catmull-Rom 位置插值（反射端点）
    /// </summary>
    private Vector2 CRPosition(Vector2[] points, float[] nodeParams, int seg, float s)
    {
        int n = points.Length;
        if (n < 2) return n > 0 ? points[0] : Vector2.zero;

        GetCRQuad(points, nodeParams, seg, out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3,
            out float t0, out float t1, out float t2, out float t3);

        float t = Mathf.Lerp(t1, t2, s);

        Vector2 A1 = CRLerp(p0, p1, t0, t1, t);
        Vector2 A2 = CRLerp(p1, p2, t1, t2, t);
        Vector2 A3 = CRLerp(p2, p3, t2, t3, t);

        Vector2 B1 = CRLerp(A1, A2, t0, t2, t);
        Vector2 B2 = CRLerp(A2, A3, t1, t3, t);

        return CRLerp(B1, B2, t1, t2, t);
    }

    /// <summary>
    /// Centripetal CR 切线方向
    /// </summary>
    private Vector2 CRTangent(Vector2[] points, float[] nodeParams, int seg, float s)
    {
        (_, Vector2 vel, _) = CRDerivatives(points, nodeParams, seg, s);
        return vel.normalized;
    }

    /// <summary>
    /// Centripetal CR 一阶和二阶导数（位置, 速度, 加速度）
    /// </summary>
    private (Vector2 pos, Vector2 vel, Vector2 acc) CRDerivatives(Vector2[] points, float[] nodeParams, int seg, float s)
    {
        GetCRQuad(points, nodeParams, seg, out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3,
            out float t0, out float t1, out float t2, out float t3);

        float t = Mathf.Lerp(t1, t2, s);
        float dt_ds = t2 - t1; // dt/ds

        // === 位置 (三层递推) ===
        Vector2 A1 = CRLerp(p0, p1, t0, t1, t);
        Vector2 A2 = CRLerp(p1, p2, t1, t2, t);
        Vector2 A3 = CRLerp(p2, p3, t2, t3, t);
        Vector2 B1 = CRLerp(A1, A2, t0, t2, t);
        Vector2 B2 = CRLerp(A2, A3, t1, t3, t);
        Vector2 C = CRLerp(B1, B2, t1, t2, t);

        // === 速度 dC/ds = dC/dt * dt/ds ===
        // dA1/dt
        float d10 = t1 - t0; float d21 = t2 - t1; float d32 = t3 - t2;
        float d20 = t2 - t0; float d31 = t3 - t1;

        Vector2 dA1 = (p1 - p0) / Mathf.Max(d10, 0.0001f);
        Vector2 dA2 = (p2 - p1) / Mathf.Max(d21, 0.0001f);
        Vector2 dA3 = (p3 - p2) / Mathf.Max(d32, 0.0001f);

        // dB1/dt: B1 = A1*(t2-t)/(t2-t0) + A2*(t-t0)/(t2-t0)
        float alpha_t = (t2 - t) / Mathf.Max(d20, 0.0001f);
        float beta_t  = (t - t0) / Mathf.Max(d20, 0.0001f);
        float dalpha = -1f / Mathf.Max(d20, 0.0001f);
        float dbeta  = 1f / Mathf.Max(d20, 0.0001f);
        Vector2 dB1 = dA1 * alpha_t + A1 * dalpha + dA2 * beta_t + A2 * dbeta;

        // dB2/dt
        float gamma_t = (t3 - t) / Mathf.Max(d31, 0.0001f);
        float delta_t = (t - t1) / Mathf.Max(d31, 0.0001f);
        float dgamma = -1f / Mathf.Max(d31, 0.0001f);
        float ddelta = 1f / Mathf.Max(d31, 0.0001f);
        Vector2 dB2 = dA2 * gamma_t + A2 * dgamma + dA3 * delta_t + A3 * ddelta;

        // dC/dt
        float eps_t  = (t2 - t) / Mathf.Max(d21, 0.0001f);
        float zeta_t = (t - t1) / Mathf.Max(d21, 0.0001f);
        float deps  = -1f / Mathf.Max(d21, 0.0001f);
        float dzeta = 1f / Mathf.Max(d21, 0.0001f);
        Vector2 dC_dt = dB1 * eps_t + B1 * deps + dB2 * zeta_t + B2 * dzeta;

        Vector2 vel = dC_dt * dt_ds;

        // === 加速度 (简化：用有限差分近似二阶导) ===
        // 对 vel 采样两个相邻点做差分
        float eps = 0.001f;
        float s2 = Mathf.Min(s + eps, 1f);
        float t2p = Mathf.Lerp(t1, t2, s2);
        // 重新计算 dC/dt 在 s2
        Vector2 A1p = CRLerp(p0, p1, t0, t1, t2p);
        Vector2 A2p = CRLerp(p1, p2, t1, t2, t2p);
        Vector2 A3p = CRLerp(p2, p3, t2, t3, t2p);
        float ap = (t2 - t2p) / Mathf.Max(d20, 0.0001f);
        float bp = (t2p - t0) / Mathf.Max(d20, 0.0001f);
        Vector2 dB1p = dA1 * ap + A1p * dalpha + dA2 * bp + A2p * dbeta;
        float gp = (t3 - t2p) / Mathf.Max(d31, 0.0001f);
        float delp = (t2p - t1) / Mathf.Max(d31, 0.0001f);
        Vector2 B1p = A1p * ap + A2p * bp;
        Vector2 B2p = A2p * gp + A3p * delp;
        Vector2 dB2p = dA2 * gp + A2p * dgamma + dA3 * delp + A3p * ddelta;
        float ep2 = (t2 - t2p) / Mathf.Max(d21, 0.0001f);
        float zp2 = (t2p - t1) / Mathf.Max(d21, 0.0001f);
        Vector2 dC_dt2 = dB1p * ep2 + B1p * deps + dB2p * zp2 + B2p * dzeta;
        Vector2 vel2 = dC_dt2 * dt_ds;

        Vector2 acc = (vel2 - vel) / (eps * dt_ds);

        return (C, vel, acc);
    }

    // 线性插值辅助（防除零）
    private static Vector2 CRLerp(Vector2 a, Vector2 b, float ta, float tb, float t)
    {
        float denom = tb - ta;
        if (Mathf.Abs(denom) < 0.0001f) return a;
        float frac = (t - ta) / denom;
        return Vector2.LerpUnclamped(a, b, frac);
    }

    /// <summary>
    /// 为 LineRenderer 采样曲线显示点
    /// </summary>
    private Vector2[] SampleCurveForDisplay(Vector2[] raw, float[] nParams, int samplesPerSegment)
    {
        int segCount = raw.Length - 1;
        var result = new List<Vector2>(segCount * samplesPerSegment + 1);
        for (int seg = 0; seg < segCount; seg++)
        {
            int steps = (seg == segCount - 1) ? samplesPerSegment + 1 : samplesPerSegment;
            for (int i = 0; i < steps; i++)
            {
                float s = (float)i / samplesPerSegment;
                if (seg == segCount - 1 && i == steps - 1) s = 1f;
                result.Add(CRPosition(raw, nParams, seg, s));
            }
        }
        return result.ToArray();
    }

    // ==================== 弧长计算 ====================

    // Gauss-Legendre 5 点（[-1, 1]）
    private static readonly float[] gaussNodes = { -0.9061798459f, -0.5384693101f, 0f, 0.5384693101f, 0.9061798459f };
    private static readonly float[] gaussWeights = { 0.2369268851f, 0.4786286705f, 0.5688888889f, 0.4786286705f, 0.2369268851f };

    /// <summary>
    /// Gauss-Legendre 求积计算单段弧长（直接在 s∈[0,1] 上积分 |dC/ds|）
    /// </summary>
    private float GaussArcLength(Vector2[] raw, float[] nParams, int seg)
    {
        float length = 0f;
        for (int i = 0; i < 5; i++)
        {
            // Gauss 节点从 [-1,1] 映射到 [0,1]
            float s = (gaussNodes[i] + 1f) * 0.5f;
            (_, Vector2 vel, _) = CRDerivatives(raw, nParams, seg, s);
            length += gaussWeights[i] * vel.magnitude;
        }
        return length * 0.5f; // Jacobian: [-1,1] → [0,1]
    }

    /// <summary>
    /// 构建弧长表、段内 LUT 和累加弧长表
    /// </summary>
    private void BuildArcLengthTable(Vector2[] raw, float[] nParams)
    {
        int segCount = raw.Length - 1;
        segArcLengths = new float[segCount];
        segArcLUT = new float[segCount][];
        cumArcLengths = new float[segCount];
        totalArcLength = 0f;

        for (int i = 0; i < segCount; i++)
        {
            segArcLengths[i] = GaussArcLength(raw, nParams, i);
            totalArcLength += segArcLengths[i];
            cumArcLengths[i] = totalArcLength;

            // 构建段内弧长→s 映射表（均匀采样 arc length，存归一化值）
            segArcLUT[i] = new float[ArcLUTSamples];
            float accum = 0f;
            segArcLUT[i][0] = 0f;
            Vector2 prev = CRPosition(raw, nParams, i, 0f);
            for (int k = 1; k < ArcLUTSamples; k++)
            {
                float s = (float)k / (ArcLUTSamples - 1);
                Vector2 curr = CRPosition(raw, nParams, i, s);
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

    /// <summary>
    /// 段内弧长比例 → CR 参数 s（LUT 二分查找）
    /// </summary>
    private float ArcFractionToS(int seg, float arcFraction)
    {
        if (segArcLUT == null || seg >= segArcLUT.Length) return Mathf.Clamp01(arcFraction);

        float[] lut = segArcLUT[seg];
        arcFraction = Mathf.Clamp01(arcFraction);

        // 二分查找 LUT
        int lo = 0, hi = lut.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (lut[mid] <= arcFraction) lo = mid;
            else hi = mid - 1;
        }

        // 在 lo 和 lo+1 之间线性插值
        if (lo >= lut.Length - 1) return 1f;
        float range = lut[lo + 1] - lut[lo];
        float frac = range > 0.0001f ? (arcFraction - lut[lo]) / range : 0f;
        return ((float)lo + frac) / (lut.Length - 1);
    }

    /// <summary>
    /// 二分查找：按弧长距离定位曲线段
    /// </summary>
    private int FindSegmentByDistance(float dist)
    {
        int lo = 0, hi = cumArcLengths.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (cumArcLengths[mid] < dist)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo;
    }

    // ==================== 直线模式回退 ====================

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

    private Vector2 LinearSample(Vector2[] pts, float t)
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

    private Vector2 LinearTangent(Vector2[] pts, float t)
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

    // ==================== 编辑器 Gizmos ====================
#if UNITY_EDITOR
    private static readonly Color unselectedColor    = new(0.3f, 0.5f, 1f, 0.6f);
    private static readonly Color selectedColor      = new(1f, 0.85f, 0.2f, 1f);
    private static readonly Color arrowColor         = new(1f, 0.6f, 0.1f, 0.9f);
    private static readonly Color arrowColorSelected = new(1f, 0.3f, 0.1f, 1f);
    private const float unselectedNodeRadius = 0.08f;
    private const float selectedNodeRadius   = 0.25f;
    private const float unselectedLineWidth  = 2f;
    private const float selectedLineWidth    = 6f;
    private const float arrowSize            = 0.35f;
    private const float arrowSizeSelected    = 0.6f;

    private void OnDrawGizmos()
    {
        DrawPathLine(unselectedColor, unselectedLineWidth);
        DrawPathNodes(unselectedColor, unselectedNodeRadius);
        DrawDirectionArrows(arrowColor, arrowSize, filled: false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawPathLine(selectedColor, selectedLineWidth);
        DrawPathNodes(selectedColor, selectedNodeRadius);
        DrawDirectionArrows(arrowColorSelected, arrowSizeSelected, filled: true);
        DrawPathLabel();
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
            float[] nParams = CentripetalParameterize(raw);

            // 在每个原始段的中点处画箭头（arc-length 中点）
            for (int seg = 0; seg < raw.Length - 1; seg++)
            {
                // 找弧长中点对应的 s
                float halfArc = GaussArcLength(raw, nParams, seg) * 0.5f;
                float s = 0.5f; // 默认用参数中点

                // 用二分查找弧长中点
                float lo = 0f, hi = 1f;
                for (int iter = 0; iter < 8; iter++)
                {
                    float mid = (lo + hi) * 0.5f;
                    float arcAtMid = PartialArcLength(raw, nParams, seg, 0f, mid);
                    if (arcAtMid < halfArc) lo = mid;
                    else hi = mid;
                }
                s = (lo + hi) * 0.5f;

                Vector2 pos = CRPosition(raw, nParams, seg, s);
                Vector2 dir = CRTangent(raw, nParams, seg, s);

                if (dir.magnitude > 0.001f)
                    DrawSingleArrow(pos, dir, size, filled);
            }
        }
        else
        {
            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                if (pathPoints[i] == null || pathPoints[i + 1] == null) continue;

                Vector3 from = pathPoints[i].position;
                Vector3 to   = pathPoints[i + 1].position;
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
        Vector2 prev = CRPosition(raw, nParams, seg, s0);
        for (int i = 1; i <= steps; i++)
        {
            float s = Mathf.Lerp(s0, s1, (float)i / steps);
            Vector2 curr = CRPosition(raw, nParams, seg, s);
            len += Vector2.Distance(prev, curr);
            prev = curr;
        }
        return len;
    }

    private void DrawSingleArrow(Vector3 pos, Vector3 direction, float size, bool filled)
    {
        Vector3 tip = pos + direction * (size * 0.5f);
        Vector3 baseCenter = pos - direction * (size * 0.5f);
        Vector3 perpendicular = new(-direction.y, direction.x, 0f);
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
            $"[{camp}] {pathId}",
            new GUIStyle()
            {
                normal = new() { textColor = selectedColor },
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            }
        );
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

            float[] nParams = CentripetalParameterize(raw);
            Vector2[] sampled = SampleCurveForDisplay(raw, nParams, curveSamples);
            foreach (var p in sampled)
                points.Add(p);
        }
        else
        {
            for (int i = 0; i < pathPoints.Count; i++)
                if (pathPoints[i] != null) points.Add(pathPoints[i].position);
        }

        return points;
    }
#endif
}
