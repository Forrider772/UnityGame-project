using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 资源点管理器（场景级单例）
/// 汇总所有资源点，提供查询和增益计算
/// </summary>
public class ResourcePointManager : MonoBehaviour
{
    public static ResourcePointManager Instance;

    private List<ResourcePoint> _allResourcePoints = new List<ResourcePoint>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void Register(ResourcePoint rp)
    {
        if (!_allResourcePoints.Contains(rp))
            _allResourcePoints.Add(rp);
    }

    /// <summary>
    /// 根据路径ID查询资源点（同一条路径双方共用）
    /// </summary>
    public ResourcePoint GetResourcePointOnPath(PathID pathId)
    {
        return _allResourcePoints.Find(rp => rp.pathID == pathId);
    }

    /// <summary>
    /// 获取某阵营所有活跃资源点的增益总和
    /// </summary>
    public float GetTotalBonus(CampType camp)
    {
        float total = 0f;
        foreach (var rp in _allResourcePoints)
        {
            if (rp != null && rp.isActive && rp.occupyingCamp == camp)
                total += rp.resourceBonus;
        }
        return total;
    }

    /// <summary>
    /// 获取某阵营占领的所有资源点
    /// </summary>
    public List<ResourcePoint> GetOccupiedBy(CampType camp)
    {
        List<ResourcePoint> result = new List<ResourcePoint>();
        foreach (var rp in _allResourcePoints)
        {
            if (rp != null && rp.occupyingCamp == camp)
                result.Add(rp);
        }
        return result;
    }
}
