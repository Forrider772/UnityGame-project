using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 塔属性表导入器：TowerTable.csv → LevelConfig_Level_N 的塔覆盖数值 + 开关
/// 每行 = 某关某阵营塔的数值（主键 levelIndex + towerCamp，6 关 × 2 = 12 行）。
/// 导入时启用 overridePlayerTowerProps/overrideEnemyTowerProps 并把各属性 override 开关置 true。
/// 注意：塔覆盖要运行时生效，还需在场景 LevelSetup 手动接好 playerTower/enemyTower 引用。
/// </summary>
public static class TowerTableImporter
{
    // (CSV 列名, TowerOverrideConfig 属性开关, TowerOverrideConfig 数值字段)
    private static readonly (string col, string toggle, string val)[] TowerProps =
    {
        ("hp", "overrideHp", "hp"),
        ("atk", "overrideAtk", "atk"),
        ("atkRange", "overrideAtkRange", "atkRange"),
        ("atkCD", "overrideAtkCD", "atkCD"),
        ("physicalDefense", "overridePhysicalDefense", "physicalDefense"),
        ("magicDefense", "overrideMagicDefense", "magicDefense"),
    };

    [MenuItem("Tools/策划表/导入 塔属性表")]
    public static void Import()
    {
        string csvPath = ConfigTableConst.TowerCsvPath;
        if (!File.Exists(csvPath))
        {
            EditorUtility.DisplayDialog("导入塔属性表", $"找不到 CSV 文件：{csvPath}\n请先执行【Tools/策划表/导出 CSV 初版】。", "确定");
            return;
        }

        List<string[]> raw;
        try
        {
            raw = CsvReader.ReadFile(csvPath);
        }
        catch (IOException e)
        {
            EditorUtility.DisplayDialog("导入塔属性表", e.Message, "确定");
            return;
        }

        if (raw.Count < 2)
        {
            EditorUtility.DisplayDialog("导入塔属性表", "CSV 内容为空（至少需要表头 + 一行数据）。", "确定");
            return;
        }

        var table = new CsvTable(raw[0], raw.GetRange(1, raw.Count - 1));

        var missing = new List<string>();
        foreach (var col in ConfigTableConst.TowerColumns)
        {
            if (!table.HasColumn(col)) missing.Add(col);
        }
        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog("导入塔属性表", $"CSV 缺少必填列：{string.Join(", ", missing)}\n表头不可修改，请检查文件。", "确定");
            return;
        }

        var errors = new List<string>();
        var touchedAssets = new List<string>();
        var updatedLevels = new HashSet<int>();

        foreach (string[] row in table.Rows)
        {
            if (!int.TryParse(table.Get(row, "levelIndex"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int level))
            {
                errors.Add($"levelIndex={table.Get(row, "levelIndex")} 无效，已跳过该行");
                continue;
            }
            if (!Enum.TryParse(table.Get(row, "towerCamp"), true, out CampType camp))
            {
                errors.Add($"关卡[{level}]：towerCamp={table.Get(row, "towerCamp")} 无效（应为 Player/Enemy）");
                continue;
            }

            // 解析本行全部数值，任一非法则整行跳过
            var values = new Dictionary<string, float>();
            bool rowFailed = false;
            foreach (var (col, _, _) in TowerProps)
            {
                if (!float.TryParse(table.Get(row, col), NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                {
                    errors.Add($"关卡[{level}] {camp}：{col}={table.Get(row, col)} 不是有效的数值");
                    rowFailed = true;
                    break;
                }
                values[col] = v;
            }
            if (rowFailed) continue;

            string cfgPath = ConfigTableConst.LevelConfigAssetPath(level);
            if (File.Exists(cfgPath)) touchedAssets.Add(cfgPath);

            LevelConfig cfg = ConfigTableConst.LoadOrCreateLevelConfig(level);
            TowerOverrideConfig target = camp == CampType.Player ? cfg.playerTowerOverride : cfg.enemyTowerOverride;

            // 写入数值 + 启用对应属性开关 + 启用整组覆盖开关
            foreach (var (col, toggle, val) in TowerProps)
            {
                var toggleField = typeof(TowerOverrideConfig).GetField(toggle);
                var valField = typeof(TowerOverrideConfig).GetField(val);
                if (toggleField == null || valField == null) continue;
                toggleField.SetValue(target, true);
                valField.SetValue(target, values[col]);
            }
            if (camp == CampType.Player) cfg.overridePlayerTowerProps = true;
            else                        cfg.overrideEnemyTowerProps = true;

            EditorUtility.SetDirty(cfg);
            updatedLevels.Add(level);
        }

        ConfigTableConst.BackupFiles(touchedAssets);
        AssetDatabase.SaveAssets();

        string msg = $"塔属性表导入完成：更新 {updatedLevels.Count} 关";
        if (errors.Count > 0)
            msg += $"\n\n失败 {errors.Count} 条：\n" + string.Join("\n", errors);

        Debug.Log($"<color=green>塔属性表导入完成：更新 {updatedLevels.Count} 关</color>");
        if (errors.Count > 0) Debug.LogWarning(string.Join("\n", errors));
        EditorUtility.DisplayDialog("塔属性表导入", msg, "确定");
    }
}
