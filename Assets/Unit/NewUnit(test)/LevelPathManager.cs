using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡路径管理器
/// 职责：注册当前关卡所有路径，按PathID存取
/// 每一个关卡场景挂一个
/// </summary>
public class LevelPathManager : MonoBehaviour
{
    [Header("当前关卡所有路径配置")]
    public Dictionary<PathID, PathManager> pathDic = new Dictionary<PathID, PathManager>();

    [Header("手动绑定：枚举对应场景路径物体")]
    public List<PathBindItem> pathBindList;

    /// <summary>
    /// 初始化绑定路径字典
    /// </summary>
    void Awake()
    {
        pathDic.Clear();
        foreach (var item in pathBindList)
        {
            if (!pathDic.ContainsKey(item.pathID) && item.pathMgr != null)
            {
                pathDic.Add(item.pathID, item.pathMgr);
            }
        }
    }

    /// <summary>
    /// 根据路径ID获取对应的路径管理器
    /// </summary>
    public PathManager GetPath(PathID id)
    {
        if (pathDic.TryGetValue(id, out var path))
        {
            return path;
        }
        Debug.LogError($"未找到路径：{id}");
        return null;
    }
}

/// <summary>
/// 路径绑定条目：枚举 + 场景路径物体
/// </summary>
[System.Serializable]
public class PathBindItem
{
    public PathID pathID;
    public PathManager pathMgr;
}