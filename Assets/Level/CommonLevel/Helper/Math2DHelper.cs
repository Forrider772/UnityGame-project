using UnityEngine;

/// <summary>
/// 2D 数学辅助类，提供点到线段、点到折线的距离计算
/// 用于鼠标悬停检测
/// </summary>
public static class Math2DHelper
{
    /// <summary>
    /// 计算点 P 到线段 AB 的最短距离的平方
    /// 使用平方距离可避免开根号，提高性能
    /// </summary>
    /// <param name="point">点P</param>
    /// <param name="segmentStart">线段起点A</param>
    /// <param name="segmentEnd">线段终点B</param>
    /// <returns>点到线段的最短距离的平方</returns>
    public static float SqDistPointToSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
    {
        Vector2 ab = segmentEnd - segmentStart;   // 线段向量
        Vector2 ap = point - segmentStart;        // 点P到起点的向量

        // 投影参数 t，表示最近点在线段上的比例位置
        float t = Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab);
        t = Mathf.Clamp01(t);                     // 限制在 [0,1] 之间，确保最近点落在线段上

        // 计算线段上的最近点
        Vector2 closestPoint = segmentStart + t * ab;

        // 返回点到最近点的距离平方
        return (point - closestPoint).sqrMagnitude;
    }

    /// <summary>
    /// 计算点 P 到一条折线（由多个点顺序连接而成）的最短距离
    /// 遍历所有线段，取最小值
    /// </summary>
    /// <param name="point">点P</param>
    /// <param name="polyline">折线的顶点数组（顺序排列）</param>
    /// <returns>点到折线的最短距离（世界单位）</returns>
    public static float MinDistancePointToPolyline(Vector2 point, Vector2[] polyline)
    {
        if (polyline == null || polyline.Length < 2)
            return float.MaxValue;   // 无效折线，返回极大值

        float minSqDist = float.MaxValue;

        // 遍历每一段线段
        for (int i = 0; i < polyline.Length - 1; i++)
        {
            float sqDist = SqDistPointToSegment(point, polyline[i], polyline[i + 1]);
            if (sqDist < minSqDist)
                minSqDist = sqDist;
        }

        // 最后再开方得到实际距离
        return Mathf.Sqrt(minSqDist);
    }
}