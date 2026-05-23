using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 存档管理器（静态类）
/// 支持多个存档栏位（0~2），使用 JSON 格式 + persistentDataPath 路径存储
/// </summary>
public static class SaveManager
{
    /// <summary>可用的存档栏位数</summary>
    public const int SlotCount = 3;

    /// <summary>当前正在游玩的存档栏位，由 MenuManager 在加载场景前设置</summary>
    public static int CurrentSlotIndex;

    /// <summary>关卡场景加载顺序，新增关卡只需追加到此数组</summary>
    private static readonly string[] levelSceneOrder = { "Level_1" };

    /// <summary>根据栏位索引生成存档文件路径</summary>
    private static string GetSlotPath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"gamesave_{slotIndex}.json");
    }

    #region 栏位级操作

    /// <summary>
    /// 检查指定栏位是否有存档
    /// </summary>
    public static bool HasSave(int slotIndex)
    {
        return File.Exists(GetSlotPath(slotIndex));
    }

    /// <summary>
    /// 读取指定栏位的存档，文件不存在或损坏时返回 null
    /// </summary>
    public static GameSaveData LoadSave(int slotIndex)
    {
        if (!HasSave(slotIndex)) return null;
        try
        {
            string json = File.ReadAllText(GetSlotPath(slotIndex));
            return JsonUtility.FromJson<GameSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"存档 {slotIndex} 读取失败: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 保存存档到指定栏位，自动记录时间和栏位索引
    /// </summary>
    public static void SaveGame(GameSaveData data, int slotIndex)
    {
        data.slotIndex = slotIndex;
        data.saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetSlotPath(slotIndex), json);
        }
        catch (Exception e)
        {
            Debug.LogError($"存档 {slotIndex} 保存失败: {e.Message}");
        }
    }

    /// <summary>
    /// 删除指定栏位的存档文件
    /// </summary>
    public static void DeleteSave(int slotIndex)
    {
        string path = GetSlotPath(slotIndex);
        if (File.Exists(path))
            File.Delete(path);
    }

    #endregion

    #region 全局操作

    /// <summary>
    /// 创建一份全新的存档数据，初始化所有关卡为未完成，当前关卡设为第一关
    /// </summary>
    public static GameSaveData CreateNewGame()
    {
        var data = new GameSaveData();
        foreach (string scene in levelSceneOrder)
            data.levelRecords.Add(new LevelRecord { levelScene = scene, isCompleted = false });
        data.currentLevelIndex = 0;
        data.currentLevelScene = levelSceneOrder[0];
        return data;
    }

    /// <summary>
    /// 获取最新存档所在的栏位索引，无任何存档时返回 -1
    /// </summary>
    public static int GetLatestSaveSlot()
    {
        int latest = -1;
        string latestTime = "";
        for (int i = 0; i < SlotCount; i++)
        {
            var save = LoadSave(i);
            if (save != null && string.Compare(save.saveTime, latestTime) > 0)
            {
                latestTime = save.saveTime;
                latest = i;
            }
        }
        return latest;
    }

    /// <summary>
    /// 检查是否存在任何存档
    /// </summary>
    public static bool HasAnySave()
    {
        for (int i = 0; i < SlotCount; i++)
            if (HasSave(i)) return true;
        return false;
    }

    /// <summary>
    /// 标记指定关卡为已完成，并自动推进到下一关
    /// 如果已是最后一关，currentLevelScene 设为 "MenuScene"
    /// </summary>
    /// <param name="sceneName">已完成的关卡场景名</param>
    /// <param name="slotIndex">要更新的存档栏位</param>
    public static void MarkLevelCompleted(string sceneName, int slotIndex)
    {
        var data = LoadSave(slotIndex);
        if (data == null) return;

        var record = data.levelRecords.Find(r => r.levelScene == sceneName);
        if (record != null)
            record.isCompleted = true;

        int nextIndex = data.currentLevelIndex + 1;
        if (nextIndex < levelSceneOrder.Length)
        {
            data.currentLevelIndex = nextIndex;
            data.currentLevelScene = levelSceneOrder[nextIndex];
        }
        else
        {
            // 全部通关，返回主菜单
            data.currentLevelScene = "MenuScene";
        }

        SaveGame(data, slotIndex);
    }

    #endregion
}
