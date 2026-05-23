using System;
using System.Collections.Generic;

/// <summary>
/// 关卡进度记录
/// 存储单个关卡的通关状态
/// </summary>
[System.Serializable]
public class LevelRecord
{
    /// <summary>关卡场景名称，如 "Level_1"</summary>
    public string levelScene;

    /// <summary>是否已完成</summary>
    public bool isCompleted;
}

/// <summary>
/// 游戏存档数据
/// 包含所属栏位、关卡进度和当前所在关卡信息
/// </summary>
[System.Serializable]
public class GameSaveData
{
    /// <summary>存档版本号，用于后续兼容升级</summary>
    public int saveVersion = 1;

    /// <summary>存档栏位索引（0~2）</summary>
    public int slotIndex;

    /// <summary>存档时间，保存时自动更新</summary>
    public string saveTime;

    /// <summary>当前关卡在 levelRecords 中的索引</summary>
    public int currentLevelIndex;

    /// <summary>当前关卡场景名称，继续游戏时直接读取</summary>
    public string currentLevelScene;

    /// <summary>所有关卡的完成记录列表</summary>
    public List<LevelRecord> levelRecords = new List<LevelRecord>();
}
