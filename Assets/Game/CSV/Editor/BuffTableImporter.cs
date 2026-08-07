using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Buff 表导入器：BuffTable.csv → LevelConfig_Level_N.levelBuffs
/// 每行 = 某关应用的一个 Buff（buffID 引用已有 Buff_{buffID}.asset，不生成 Buff 资产）。
/// 策划需先在 Unity 建好 BuffData 资产，再在 CSV 里引用。
/// </summary>
public static class BuffTableImporter
{
    [MenuItem("Tools/策划表/导入 Buff 表")]
    public static void Import()
    {
        string csvPath = ConfigTableConst.BuffCsvPath;
        if (!File.Exists(csvPath))
        {
            EditorUtility.DisplayDialog("导入 Buff 表", $"找不到 CSV 文件：{csvPath}\n请先执行【Tools/策划表/导出 CSV 初版】。", "确定");
            return;
        }

        List<string[]> raw;
        try
        {
            raw = CsvReader.ReadFile(csvPath);
        }
        catch (IOException e)
        {
            EditorUtility.DisplayDialog("导入 Buff 表", e.Message, "确定");
            return;
        }

        if (raw.Count < 2)
        {
            EditorUtility.DisplayDialog("导入 Buff 表", "CSV 内容为空（至少需要表头 + 一行数据）。", "确定");
            return;
        }

        var table = new CsvTable(raw[0], raw.GetRange(1, raw.Count - 1));

        var missing = new List<string>();
        foreach (var col in ConfigTableConst.BuffColumns)
        {
            if (!table.HasColumn(col)) missing.Add(col);
        }
        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog("导入 Buff 表", $"CSV 缺少必填列：{string.Join(", ", missing)}\n表头不可修改，请检查文件。", "确定");
            return;
        }

        var errors = new List<string>();
        var touchedAssets = new List<string>();

        // 按 levelIndex 分组收集 buffID（按行序保持顺序）
        var buffByLevel = new SortedDictionary<int, List<string>>();
        foreach (string[] row in table.Rows)
        {
            if (!int.TryParse(table.Get(row, "levelIndex"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int level))
            {
                errors.Add($"levelIndex={table.Get(row, "levelIndex")} 无效，已跳过该行");
                continue;
            }
            string buffID = table.Get(row, "buffID");
            if (string.IsNullOrEmpty(buffID)) continue;

            if (!buffByLevel.TryGetValue(level, out var list))
            {
                list = new List<string>();
                buffByLevel[level] = list;
            }
            list.Add(buffID);
        }

        int ok = 0;
        foreach (var kv in buffByLevel)
        {
            int level = kv.Key;
            var buffs = new List<BuffData>();
            bool levelFailed = false;

            foreach (var buffID in kv.Value)
            {
                BuffData buff = ConfigTableConst.FindAssetByFileName<BuffData>($"Buff_{buffID}");
                if (buff == null)
                {
                    errors.Add($"关卡[{level}]：buffID [{buffID}] 未找到 Buff_{buffID}.asset（请先在 Unity 建好 Buff 资产）");
                    levelFailed = true;
                    continue;
                }
                buffs.Add(buff);
            }
            if (levelFailed) continue;

            string cfgPath = ConfigTableConst.LevelConfigAssetPath(level);
            if (File.Exists(cfgPath)) touchedAssets.Add(cfgPath);

            LevelConfig cfg = ConfigTableConst.LoadOrCreateLevelConfig(level);
            Undo.RegisterCompleteObjectUndo(cfg, $"导入 Buff 表 Level_{level}");
            cfg.levelBuffs = buffs.ToArray();
            EditorUtility.SetDirty(cfg);
            ok++;
        }

        ConfigTableConst.BackupFiles(touchedAssets);
        AssetDatabase.SaveAssets();

        string msg = $"Buff 表导入完成：成功 {ok} 关";
        if (errors.Count > 0)
            msg += $"\n\n失败 {errors.Count} 条：\n" + string.Join("\n", errors);

        Debug.Log($"<color=green>Buff 表导入完成：成功 {ok} 关</color>");
        if (errors.Count > 0) Debug.LogWarning(string.Join("\n", errors));
        EditorUtility.DisplayDialog("Buff 表导入", msg, "确定");
    }
}
