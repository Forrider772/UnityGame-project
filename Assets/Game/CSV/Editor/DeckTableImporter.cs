using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 初始牌组表导入器：DeckTable.csv → LevelConfig_Level_N.availableCards
/// 每行 = 某关卡组里的一张卡（cardID 引用已有 Card_{cardID}.asset，不生成卡牌资产）。
/// 按 levelIndex 分组填充，LevelSetup 读到非空时自动启用 overrideDeckConfig。
/// </summary>
public static class DeckTableImporter
{
    [MenuItem("Tools/策划表/导入 初始牌组表")]
    public static void Import()
    {
        string csvPath = ConfigTableConst.DeckCsvPath;
        if (!File.Exists(csvPath))
        {
            EditorUtility.DisplayDialog("导入初始牌组表", $"找不到 CSV 文件：{csvPath}\n请先执行【Tools/策划表/导出 CSV 初版】。", "确定");
            return;
        }

        List<string[]> raw;
        try
        {
            raw = CsvReader.ReadFile(csvPath);
        }
        catch (IOException e)
        {
            EditorUtility.DisplayDialog("导入初始牌组表", e.Message, "确定");
            return;
        }

        if (raw.Count < 2)
        {
            EditorUtility.DisplayDialog("导入初始牌组表", "CSV 内容为空（至少需要表头 + 一行数据）。", "确定");
            return;
        }

        var table = new CsvTable(raw[0], raw.GetRange(1, raw.Count - 1));

        var missing = new List<string>();
        foreach (var col in ConfigTableConst.DeckColumns)
        {
            if (!table.HasColumn(col)) missing.Add(col);
        }
        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog("导入初始牌组表", $"CSV 缺少必填列：{string.Join(", ", missing)}\n表头不可修改，请检查文件。", "确定");
            return;
        }

        var errors = new List<string>();
        var touchedAssets = new List<string>();

        // 按 levelIndex 分组收集 cardID（按行序保持卡组顺序）
        var deckByLevel = new SortedDictionary<int, List<string>>();
        foreach (string[] row in table.Rows)
        {
            if (!int.TryParse(table.Get(row, "levelIndex"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int level))
            {
                errors.Add($"levelIndex={table.Get(row, "levelIndex")} 无效，已跳过该行");
                continue;
            }
            string cardID = table.Get(row, "cardID");
            if (string.IsNullOrEmpty(cardID)) continue;

            if (!deckByLevel.TryGetValue(level, out var list))
            {
                list = new List<string>();
                deckByLevel[level] = list;
            }
            list.Add(cardID);
        }

        int ok = 0;
        foreach (var kv in deckByLevel)
        {
            int level = kv.Key;
            var cards = new List<CardData>();
            bool levelFailed = false;

            foreach (var cardID in kv.Value)
            {
                CardData card = ConfigTableConst.FindAssetByFileName<CardData>($"Card_{cardID}");
                if (card == null)
                {
                    errors.Add($"关卡[{level}]：cardID [{cardID}] 未找到 Card_{cardID}.asset");
                    levelFailed = true;
                    continue;
                }
                cards.Add(card);
            }
            if (levelFailed) continue;   // 该关有缺失引用则整关跳过，避免写入不完整卡组

            string cfgPath = ConfigTableConst.LevelConfigAssetPath(level);
            if (File.Exists(cfgPath)) touchedAssets.Add(cfgPath);

            LevelConfig cfg = ConfigTableConst.LoadOrCreateLevelConfig(level);
            Undo.RegisterCompleteObjectUndo(cfg, $"导入初始牌组表 Level_{level}");
            cfg.availableCards = cards.ToArray();
            EditorUtility.SetDirty(cfg);
            ok++;
        }

        ConfigTableConst.BackupFiles(touchedAssets);
        AssetDatabase.SaveAssets();

        string msg = $"初始牌组表导入完成：成功 {ok} 关";
        if (errors.Count > 0)
            msg += $"\n\n失败 {errors.Count} 条：\n" + string.Join("\n", errors);

        Debug.Log($"<color=green>初始牌组表导入完成：成功 {ok} 关</color>");
        if (errors.Count > 0) Debug.LogWarning(string.Join("\n", errors));
        EditorUtility.DisplayDialog("初始牌组表导入", msg, "确定");
    }
}
