using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 路径管理器
/// 职责：统一管理一条路径的所有路点
/// Scene视图绘制路径线方便编辑
/// </summary>
public class PathManager : MonoBehaviour
{
    [Header("基础配置")]
    public CampType camp;       // 所属阵营
    public PathID pathId;       // 路线ID
    [Header("路径节点列表")]
    public List<Transform> pathPoints = new List<Transform>();

    /// <summary>
    /// 场景编辑器绘制路径辅助线
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