using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 路径数据管理类
/// 维护一条路线的所有路径点、所属阵营、路线ID等数据
/// 集成了 PathVisualManager 用于运行时可视化和交互反馈
/// </summary>
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

    private PathVisualManager visualManager;  // 视觉效果管理器引用

    private void Awake()
    {
        // 获取 PathVisualManager 组件（由 RequireComponent 保证存在）
        visualManager = GetComponent<PathVisualManager>();

        // 将 Transform 列表转换为 Vector2 数组供 LineRenderer 使用
        Vector2[] waypoints = new Vector2[pathPoints.Count];
        for (int i = 0; i < pathPoints.Count; i++)
        {
            if (pathPoints[i] != null)
                waypoints[i] = pathPoints[i].position;
        }

        // 初始化视觉管理器
        visualManager.Initialize(waypoints);

        // 初始时隐藏路线（未进入部署模式）
        visualManager.SetVisible(false);
    }

    /// <summary>
    /// 设置路线是否可见（由 CardDeploy 调用）
    /// </summary>
    /// <param name="visible">true 显示，false 隐藏</param>
    public void SetVisible(bool visible)
    {
        if (visualManager != null)
            visualManager.SetVisible(visible);
    }

    /// <summary>
    /// 设置路线高亮状态（由 CardDeploy 调用）
    /// </summary>
    /// <param name="highlight">true 高亮，false 恢复普通</param>
    public void SetHighlight(bool highlight)
    {
        if (visualManager != null)
            visualManager.SetHighlight(highlight);
    }

    /// <summary>
    /// 获取路线的起点世界坐标（第一个路径点）
    /// 部署单位时使用
    /// </summary>
    /// <returns>起点坐标 Vector3，若无路径点则返回 (0,0,0)</returns>
    public Vector3 GetStartPoint()
    {
        if (pathPoints.Count > 0 && pathPoints[0] != null)
            return pathPoints[0].position;
        return Vector3.zero;
    }

    /// <summary>
    /// 获取所有路径点的世界坐标数组（Vector2 格式）
    /// 用于鼠标悬停时的距离计算
    /// </summary>
    /// <returns>Vector2 数组</returns>
    public Vector2[] GetWaypoints2D()
    {
        Vector2[] points = new Vector2[pathPoints.Count];
        for (int i = 0; i < pathPoints.Count; i++)
            points[i] = pathPoints[i].position;
        return points;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器下绘制路径辅助线（蓝色线条），方便可视化编辑
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            if (pathPoints[i] != null && pathPoints[i + 1] != null)
                Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
        }
    }
#endif
}