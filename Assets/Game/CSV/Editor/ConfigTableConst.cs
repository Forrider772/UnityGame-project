using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 策划配置表：值类型
/// </summary>
public enum ConfigValueKind
{
    Enum,
    Int,
    Float,
    String
}

/// <summary>
/// 策划配置表常量与字段映射表
/// 集中定义：CSV 路径、预制体/资产目录、单位属性列映射
/// 未来新增表（波次/关卡/Buff/塔）只需在这里登记路径与列映射，导入器按同一模式扩展
/// </summary>
public static class ConfigTableConst
{
    // ==================== 目录与文件路径 ====================
    // 目录结构：
    //   Assets/Game/CSV/Core/   通用解析层（CsvReader / CsvTable，所有表共用）
    //   Assets/Game/CSV/Editor/ 编辑器工具（本文件所在目录，导入/导出/菜单）
    //   Assets/Game/CSV/Tables/ 数据表 .csv（每个系统一个表，如 UnitTable / CardTable）
    public const string CsvDir        = "Assets/Game/CSV/Tables";
    public const string UnitCsvPath   = "Assets/Game/CSV/Tables/UnitTable.csv";
    public const string CardCsvPath   = "Assets/Game/CSV/Tables/CardTable.csv";
    public const string UnitPrefabDir = "Assets/Game/Unit/GeneralUnit";
    public const string CardAssetDir  = "Assets/Game/Unit/CardAssets";

    // ==================== 导入安全 ====================
    public const string BackupDir          = "Assets/Game/CSV/Tables/_backup";
    public const bool   BackupBeforeImport = true;

    // ==================== 单位表列映射 ====================
    // (CSV 列名, UnitAttr 序列化字段名, 值类型)
    public static readonly (string col, string prop, ConfigValueKind kind)[] UnitColumns =
    {
        ("camp",            "camp",            ConfigValueKind.Enum),
        ("moveType",        "moveType",        ConfigValueKind.Enum),
        ("attackRangeType", "attackRangeType", ConfigValueKind.Enum),
        ("attackType",      "attackType",      ConfigValueKind.Enum),
        ("maxHp",           "maxHp",           ConfigValueKind.Float),
        ("physicalDefense", "physicalDefense", ConfigValueKind.Float),
        ("magicDefense",    "magicDefense",    ConfigValueKind.Float),
        ("atk",             "atk",             ConfigValueKind.Float),
        ("atkRange",        "atkRange",        ConfigValueKind.Float),
        ("atkCD",           "atkCD",           ConfigValueKind.Float),
        ("moveSpeed",       "moveSpeed",       ConfigValueKind.Float),
        ("detectRange",     "detectRange",     ConfigValueKind.Float),
    };

    // ==================== 卡牌表必填列 ====================
    public static readonly string[] CardColumns = { "cardID", "unitID", "camp", "cardName", "cost", "cooldown" };

    /// <summary>
    /// 导入前备份将改写的资产文件到 _backup/{时间戳}/，供误操作后恢复。
    /// 恢复方法：把备份目录里的文件拷回原路径，再 Unity 里刷新（右键 → Reimport）即可。
    /// </summary>
    public static void BackupFiles(List<string> assetPaths)
    {
        if (!BackupBeforeImport || assetPaths == null || assetPaths.Count == 0) return;

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string dir = $"{BackupDir}/{stamp}";
        Directory.CreateDirectory(dir);

        foreach (var path in assetPaths)
        {
            if (!File.Exists(path)) continue;
            try
            {
                File.Copy(path, $"{dir}/{Path.GetFileName(path)}", true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"配置表备份失败 {path}: {e.Message}");
            }
        }
        Debug.Log($"<color=green>导入前已备份 {assetPaths.Count} 个文件到 {dir}</color>");
    }
}
