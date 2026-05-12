using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 平滑路径管理器
/// 职责：统一管理一条路径的所有路点
/// 自动收集子物体作为路径节点，Scene视图绘制路径线方便编辑
/// </summary>
public class PathManager : MonoBehaviour
{
    [Header("路径节点列表")]
    public List<Transform> pathPoints = new List<Transform>();

    /// <summary>
    /// 初始化：自动把所有子物体加入路径节点列表
    /// </summary>
    void Awake()
    {
        pathPoints.Clear();
        // 遍历所有子物体，作为路径行走点
        foreach (Transform child in transform)
        {
            pathPoints.Add(child);
        }
    }

    /// <summary>
    /// 场景编辑器绘制路径辅助线（蓝色连线）
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        // 依次连接相邻路径点
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            if (pathPoints[i] != null && pathPoints[i + 1] != null)
            {
                Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
            }
        }
    }
}