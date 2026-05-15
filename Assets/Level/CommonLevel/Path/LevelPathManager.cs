using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景级路径管理中心
/// 功能：收集并管理当前场景内所有路径，提供按阵营+路线ID查询路径的接口
/// 每个场景仅存在一个，独立管理，无全局污染
/// </summary>
public class LevelPathManager : MonoBehaviour
{
    /// <summary>
    /// 路径核心存储结构
    /// 第一层：阵营类型 -> 该阵营所有路线
    /// 第二层：路线ID -> 具体路径对象
    /// </summary>
    private Dictionary<CampType, Dictionary<PathID, PathManager>> campPathDict = new();

    /// <summary>
    /// 场景加载时自动收集所有路径
    /// </summary>
    private void Awake()
    {
        CollectAllPathsInScene();
    }

    /// <summary>
    /// 收集当前场景中所有激活/未激活的路径
    /// 可在编辑器右键脚本手动执行
    /// </summary>
    [ContextMenu("收集场景内所有路径")]
    private void CollectAllPathsInScene()
    {
        // 清空旧数据
        campPathDict.Clear();

        // 获取当前场景所有路径组件
        var allPaths = FindObjectsOfType<PathManager>(includeInactive: true);

        // 遍历所有路径，按【阵营+路线ID】存入字典
        foreach (var path in allPaths)
        {
            // 不存在该阵营则创建新字典
            if (!campPathDict.ContainsKey(path.camp))
                campPathDict.Add(path.camp, new Dictionary<PathID, PathManager>());

            var campMap = campPathDict[path.camp];
            
            // 存入路径，重复ID自动覆盖
            if (!campMap.ContainsKey(path.pathId))
                campMap.Add(path.pathId, path);
            else
                campMap[path.pathId] = path;
        }

        // 打印收集结果
        Debug.Log($"【{gameObject.scene.name}】已收集路径总数：{allPaths.Length} 条");
    }

    #region 外部获取路径接口
    /// <summary>
    /// 获取指定阵营、指定路线ID的单条路径
    /// </summary>
    /// <param name="camp">所属阵营</param>
    /// <param name="pathId">路线ID</param>
    /// <returns>找到的路径对象，找不到返回null</returns>
    public PathManager GetPath(CampType camp, PathID pathId)
    {
        if (campPathDict.TryGetValue(camp, out var campPaths) && campPaths.TryGetValue(pathId, out var path))
            return path;

        Debug.LogWarning($"未找到路径 → 阵营：{camp}，路线ID：{pathId}");
        return null;
    }

    /// <summary>
    /// 获取指定阵营下的所有路径
    /// </summary>
    /// <param name="camp">目标阵营</param>
    /// <returns>路径列表，无路径则返回空列表</returns>
    public List<PathManager> GetAllPathsByCamp(CampType camp)
    {
        List<PathManager> pathList = new List<PathManager>();

        if (campPathDict.TryGetValue(camp, out var campPaths))
            pathList.AddRange(campPaths.Values);

        return pathList;
    }
    #endregion
}