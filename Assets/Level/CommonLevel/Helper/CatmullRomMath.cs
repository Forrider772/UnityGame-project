using UnityEngine;

/// <summary>
/// Centripetal Catmull-Rom 曲线纯数学工具类
/// 所有方法均为静态纯函数，无 Unity 组件依赖
/// isLooping=true 时，直接对原始点数组取模索引，不再需要扩展数组
/// </summary>
public static class CatmullRomMath
{
    // ==================== Gauss-Legendre 求积 ====================

    /// <summary>Gauss-Legendre 5 点节点（[-1, 1]）</summary>
    public static readonly float[] GaussNodes = { -0.9061798459f, -0.5384693101f, 0f, 0.5384693101f, 0.9061798459f };
    /// <summary>Gauss-Legendre 5 点权重</summary>
    public static readonly float[] GaussWeights = { 0.2369268851f, 0.4786286705f, 0.5688888889f, 0.4786286705f, 0.2369268851f };

    // ==================== Centripetal Catmull-Rom 核心 ====================

    /// <summary>
    /// Centripetal 参数化：tᵢ = tᵢ₋₁ + |Pᵢ - Pᵢ₋₁|^alpha（开放路径，n 个元素）
    /// </summary>
    public static float[] CentripetalParameterize(Vector2[] points, float alpha)
    {
        int n = points.Length;
        float[] t = new float[n];
        t[0] = 0f;
        for (int i = 1; i < n; i++)
            t[i] = t[i - 1] + Mathf.Pow(Vector2.Distance(points[i - 1], points[i]), alpha);
        return t;
    }

    /// <summary>
    /// 循环模式 Centripetal 参数化：n+1 个元素，末元素为总周长（含闭合弦长）
    /// </summary>
    public static float[] BuildLoopNodeParams(Vector2[] points, float alpha)
    {
        int n = points.Length;
        float[] t = new float[n + 1];
        t[0] = 0f;
        for (int i = 1; i < n; i++)
            t[i] = t[i - 1] + Mathf.Pow(Vector2.Distance(points[i - 1], points[i]), alpha);
        t[n] = t[n - 1] + Mathf.Pow(Vector2.Distance(points[n - 1], points[0]), alpha);
        return t;
    }

    /// <summary>
    /// 获取 CR 段所需的 4 个控制点和参数值
    /// 普通模式：端点使用反射虚拟点保证切线自然不塌缩
    /// 循环模式：对原始点做 % n 取模，nodeParams 需要 n+1 个元素（含闭合弦长）
    /// </summary>
    public static void GetCRQuad(Vector2[] points, float[] nodeParams, int seg,
        out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3,
        out float t0, out float t1, out float t2, out float t3, bool isLooping = false)
    {
        if (isLooping)
        {
            int n = points.Length; // 原始点数量（非扩展数组）
            // 段 k: P_k → P_{(k+1)%n}
            p1 = points[seg];
            p2 = points[(seg + 1) % n];
            t1 = nodeParams[seg];
            t2 = seg < n - 1 ? nodeParams[seg + 1] : nodeParams[n]; // nodeParams[n] = 总周长

            // P0 (前驱): P_{(k-1+n)%n}
            p0 = points[(seg - 1 + n) % n];
            t0 = seg > 0 ? nodeParams[seg - 1] : nodeParams[n - 1] - nodeParams[n];

            // P3 (后继的后继): P_{(k+2)%n}
            int idx3 = (seg + 2) % n;
            p3 = points[idx3];
            t3 = seg < n - 2 ? nodeParams[seg + 2] : nodeParams[idx3] + nodeParams[n];
            return;
        }

        int N = points.Length;

        // P1, P2 — 段两端锚点
        p1 = points[seg];
        p2 = points[Mathf.Min(seg + 1, N - 1)];
        t1 = nodeParams[seg];
        t2 = nodeParams[Mathf.Min(seg + 1, N - 1)];

        // P0 — 首段用反射，否则取前一个锚点
        if (seg == 0)
        {
            p0 = p1 + (p1 - p2);
            t0 = t1 - (t2 - t1);
        }
        else
        {
            p0 = points[seg - 1];
            t0 = nodeParams[seg - 1];
        }

        // P3 — 末段用反射，否则取后后一个锚点
        if (seg >= N - 2)
        {
            p3 = p2 + (p2 - p1);
            t3 = t2 + (t2 - t1);
        }
        else
        {
            p3 = points[seg + 2];
            t3 = nodeParams[seg + 2];
        }
    }

    /// <summary>
    /// Centripetal Catmull-Rom 位置插值
    /// </summary>
    public static Vector2 CRPosition(Vector2[] points, float[] nodeParams, int seg, float s, bool isLooping = false)
    {
        int n = points.Length;
        if (n < 2) return n > 0 ? points[0] : Vector2.zero;

        GetCRQuad(points, nodeParams, seg, out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3,
            out float t0, out float t1, out float t2, out float t3, isLooping);

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
    public static Vector2 CRTangent(Vector2[] points, float[] nodeParams, int seg, float s, bool isLooping = false)
    {
        (_, Vector2 vel, _) = CRDerivatives(points, nodeParams, seg, s, isLooping);
        return vel.normalized;
    }

    /// <summary>
    /// Centripetal CR 一阶和二阶导数（位置, 速度, 加速度）
    /// </summary>
    public static (Vector2 pos, Vector2 vel, Vector2 acc) CRDerivatives(
        Vector2[] points, float[] nodeParams, int seg, float s, bool isLooping = false)
    {
        GetCRQuad(points, nodeParams, seg, out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3,
            out float t0, out float t1, out float t2, out float t3, isLooping);

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
        float d10 = t1 - t0; float d21 = t2 - t1; float d32 = t3 - t2;
        float d20 = t2 - t0; float d31 = t3 - t1;

        Vector2 dA1 = (p1 - p0) / Mathf.Max(d10, 0.0001f);
        Vector2 dA2 = (p2 - p1) / Mathf.Max(d21, 0.0001f);
        Vector2 dA3 = (p3 - p2) / Mathf.Max(d32, 0.0001f);

        float alpha_t = (t2 - t) / Mathf.Max(d20, 0.0001f);
        float beta_t  = (t - t0) / Mathf.Max(d20, 0.0001f);
        float dalpha = -1f / Mathf.Max(d20, 0.0001f);
        float dbeta  = 1f / Mathf.Max(d20, 0.0001f);
        Vector2 dB1 = dA1 * alpha_t + A1 * dalpha + dA2 * beta_t + A2 * dbeta;

        float gamma_t = (t3 - t) / Mathf.Max(d31, 0.0001f);
        float delta_t = (t - t1) / Mathf.Max(d31, 0.0001f);
        float dgamma = -1f / Mathf.Max(d31, 0.0001f);
        float ddelta = 1f / Mathf.Max(d31, 0.0001f);
        Vector2 dB2 = dA2 * gamma_t + A2 * dgamma + dA3 * delta_t + A3 * ddelta;

        float eps_t  = (t2 - t) / Mathf.Max(d21, 0.0001f);
        float zeta_t = (t - t1) / Mathf.Max(d21, 0.0001f);
        float deps  = -1f / Mathf.Max(d21, 0.0001f);
        float dzeta = 1f / Mathf.Max(d21, 0.0001f);
        Vector2 dC_dt = dB1 * eps_t + B1 * deps + dB2 * zeta_t + B2 * dzeta;

        Vector2 vel = dC_dt * dt_ds;

        // === 加速度 (有限差分近似) ===
        float eps = 0.001f;
        float s2 = Mathf.Min(s + eps, 1f);
        float t2p = Mathf.Lerp(t1, t2, s2);
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

    /// <summary>线性插值辅助（防除零）</summary>
    public static Vector2 CRLerp(Vector2 a, Vector2 b, float ta, float tb, float t)
    {
        float denom = tb - ta;
        if (Mathf.Abs(denom) < 0.0001f) return a;
        float frac = (t - ta) / denom;
        return Vector2.LerpUnclamped(a, b, frac);
    }

    // ==================== 弧长计算 ====================

    /// <summary>
    /// Gauss-Legendre 求积计算单段弧长（直接在 s∈[0,1] 上积分 |dC/ds|）
    /// </summary>
    public static float GaussArcLength(Vector2[] raw, float[] nParams, int seg, bool isLooping = false)
    {
        float length = 0f;
        for (int i = 0; i < 5; i++)
        {
            float s = (GaussNodes[i] + 1f) * 0.5f;
            (_, Vector2 vel, _) = CRDerivatives(raw, nParams, seg, s, isLooping);
            length += GaussWeights[i] * vel.magnitude;
        }
        return length * 0.5f; // Jacobian: [-1,1] → [0,1]
    }

    /// <summary>
    /// 段内弧长比例 → CR 参数 s（LUT 二分查找）
    /// </summary>
    public static float ArcFractionToS(float[][] segArcLUT, int seg, float arcFraction)
    {
        if (segArcLUT == null || seg >= segArcLUT.Length) return Mathf.Clamp01(arcFraction);

        float[] lut = segArcLUT[seg];
        arcFraction = Mathf.Clamp01(arcFraction);

        int lo = 0, hi = lut.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (lut[mid] <= arcFraction) lo = mid;
            else hi = mid - 1;
        }

        if (lo >= lut.Length - 1) return 1f;
        float range = lut[lo + 1] - lut[lo];
        float frac = range > 0.0001f ? (arcFraction - lut[lo]) / range : 0f;
        return ((float)lo + frac) / (lut.Length - 1);
    }

    /// <summary>
    /// 二分查找：按弧长距离定位曲线段
    /// </summary>
    public static int FindSegmentByDistance(float[] cumArcLengths, float dist)
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
}
