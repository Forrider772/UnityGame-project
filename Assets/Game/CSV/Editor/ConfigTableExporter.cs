using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 配置表导出器：从现有 prefab / 卡牌资产 反向生成 CSV 初版（UTF-8 BOM）
/// 用途：策划拿到的 CSV 即当前真实数值，直接在表格上调整后点导入即可生效。
/// </summary>
public static class ConfigTableExporter
{
    [MenuItem("Tools/策划表/导出 CSV 初版")]
    public static void ExportAll()
    {
        if (!EditorUtility.DisplayDialog("导出 CSV 初版",
                "将用当前预制体 / 卡牌资产 / LevelConfig 资产生成六张 CSV，并覆盖已有文件。\n是否继续？",
                "导出", "取消"))
            return;

        var logs = new List<string>();
        try
        {
            logs.Add(ExportUnitTable());
            logs.Add(ExportCardTable());
            logs.Add(ExportDeckTable());
            logs.Add(ExportBuffTable());
            logs.Add(ExportTowerTable());
            logs.Add(ExportWaveTable());
        }
        catch (IOException e)
        {
            Debug.LogError(e.Message);
            EditorUtility.DisplayDialog("导出 CSV 初版", e.Message, "确定");
            return;
        }
        AssetDatabase.Refresh();

        string all = string.Join("\n", logs);
        Debug.Log($"<color=green>配置表导出完成</color>\n{all}");
        EditorUtility.DisplayDialog("导出 CSV 初版", all + "\n\n可在 Excel 中编辑后执行【导入】。", "确定");
    }

    /// <summary>遍历 GeneralUnit 预制体，读取 UnitAttr 当前数值生成单位表</summary>
    public static string ExportUnitTable()
    {
        Directory.CreateDirectory(ConfigTableConst.CsvDir);

        var headers = new List<string> { "unitID" };
        foreach (var (col, _, _) in ConfigTableConst.UnitColumns)
            headers.Add(col);

        var rows = new List<string[]>();
        foreach (string file in Directory.GetFiles(ConfigTableConst.UnitPrefabDir, "*.prefab"))
        {
            string prefabPath = file.Replace('\\', '/');
            string unitID = Path.GetFileNameWithoutExtension(file);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            UnitAttr attr = prefab.GetComponent<UnitAttr>();
            if (attr == null) continue;

            var row = new List<string> { unitID };
            foreach (var (col, prop, kind) in ConfigTableConst.UnitColumns)
                row.Add(FormatValue(attr, prop, kind));
            rows.Add(row.ToArray());
        }

        CsvWriterHelper.WriteFile(ConfigTableConst.UnitCsvPath, headers.ToArray(), rows);
        return $"单位表：{ConfigTableConst.UnitCsvPath}（{rows.Count} 行）";
    }

    /// <summary>遍历 CardAssets 卡牌资产生成卡牌表</summary>
    public static string ExportCardTable()
    {
        Directory.CreateDirectory(ConfigTableConst.CsvDir);

        string[] headers = { "cardID", "unitID", "camp", "cardName", "cost", "cooldown" };
        var rows = new List<string[]>();

        string[] assets = AssetDatabase.FindAssets("t:CardData", new[] { ConfigTableConst.CardAssetDir });
        foreach (var guid in assets)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
            if (card == null) continue;

            // unitID 由 unitPrefab 的文件名推导；unitPrefab 为空时回退用 cardID
            string unitID = card.unitPrefab != null
                ? Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(card.unitPrefab))
                : card.cardID;

            rows.Add(new[]
            {
                card.cardID,
                unitID,
                card.camp.ToString(),
                card.cardName,
                card.cost.ToString(CultureInfo.InvariantCulture),
                card.cooldown.ToString(CultureInfo.InvariantCulture)
            });
        }

        CsvWriterHelper.WriteFile(ConfigTableConst.CardCsvPath, headers, rows);
        return $"卡牌表：{ConfigTableConst.CardCsvPath}（{rows.Count} 行）";
    }

    /// <summary>导出初始牌组表：遍历各关 LevelConfig.availableCards → levelIndex, cardID</summary>
    public static string ExportDeckTable()
    {
        Directory.CreateDirectory(ConfigTableConst.CsvDir);

        string[] headers = { "levelIndex", "cardID" };
        var rows = new List<string[]>();
        for (int level = 1; level <= ConfigTableConst.WaveLevelCount; level++)
        {
            LevelConfig cfg = AssetDatabase.LoadAssetAtPath<LevelConfig>(ConfigTableConst.LevelConfigAssetPath(level));
            if (cfg == null || cfg.availableCards == null) continue;
            foreach (var card in cfg.availableCards)
            {
                if (card == null) continue;
                rows.Add(new[] { level.ToString(CultureInfo.InvariantCulture), card.cardID });
            }
        }
        CsvWriterHelper.WriteFile(ConfigTableConst.DeckCsvPath, headers, rows);
        return $"初始牌组表：{ConfigTableConst.DeckCsvPath}（{rows.Count} 行）";
    }

    /// <summary>导出 Buff 表：遍历各关 LevelConfig.levelBuffs → levelIndex, buffID</summary>
    public static string ExportBuffTable()
    {
        Directory.CreateDirectory(ConfigTableConst.CsvDir);

        string[] headers = { "levelIndex", "buffID" };
        var rows = new List<string[]>();
        for (int level = 1; level <= ConfigTableConst.WaveLevelCount; level++)
        {
            LevelConfig cfg = AssetDatabase.LoadAssetAtPath<LevelConfig>(ConfigTableConst.LevelConfigAssetPath(level));
            if (cfg == null || cfg.levelBuffs == null) continue;
            foreach (var buff in cfg.levelBuffs)
            {
                if (buff == null) continue;
                rows.Add(new[] { level.ToString(CultureInfo.InvariantCulture), buff.name.StartsWith("Buff_") ? buff.name.Substring(5) : buff.name });
            }
        }
        CsvWriterHelper.WriteFile(ConfigTableConst.BuffCsvPath, headers, rows);
        return $"Buff 表：{ConfigTableConst.BuffCsvPath}（{rows.Count} 行）";
    }

    /// <summary>导出塔属性表：遍历各关 LevelConfig 塔覆盖 → 每关每阵营一行</summary>
    public static string ExportTowerTable()
    {
        Directory.CreateDirectory(ConfigTableConst.CsvDir);

        var rows = new List<string[]>();
        for (int level = 1; level <= ConfigTableConst.WaveLevelCount; level++)
        {
            LevelConfig cfg = AssetDatabase.LoadAssetAtPath<LevelConfig>(ConfigTableConst.LevelConfigAssetPath(level));
            if (cfg == null) continue;
            rows.Add(FormatTowerRow(level, CampType.Player, cfg.playerTowerOverride));
            rows.Add(FormatTowerRow(level, CampType.Enemy, cfg.enemyTowerOverride));
        }
        CsvWriterHelper.WriteFile(ConfigTableConst.TowerCsvPath, ConfigTableConst.TowerColumns, rows);
        return $"塔属性表：{ConfigTableConst.TowerCsvPath}（{rows.Count} 行）";
    }

    /// <summary>导出波次表：遍历各关 WaveList 资产 → WaveTable_Level_N.csv（每关一个文件）</summary>
    public static string ExportWaveTable()
    {
        Directory.CreateDirectory(ConfigTableConst.CsvDir);

        int total = 0;
        for (int level = 1; level <= ConfigTableConst.WaveLevelCount; level++)
        {
            var rows = new List<string[]>();
            WaveList wl = AssetDatabase.LoadAssetAtPath<WaveList>(ConfigTableConst.WaveAssetPath(level));
            if (wl != null && wl.waves != null)
            {
                for (int i = 0; i < wl.waves.Length; i++)
                {
                    var w = wl.waves[i];
                    string unitID = w.unitPrefab != null
                        ? Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(w.unitPrefab))
                        : "";
                    rows.Add(new[]
                    {
                        (i + 1).ToString(CultureInfo.InvariantCulture),
                        wl.camp.ToString(),
                        unitID,
                        w.pathID.ToString(),
                        w.spawnCount.ToString(CultureInfo.InvariantCulture),
                        w.spawnInterval.ToString(CultureInfo.InvariantCulture),
                        w.triggerType.ToString(),
                        w.delayBeforeStart.ToString(CultureInfo.InvariantCulture),
                    });
                }
            }
            CsvWriterHelper.WriteFile(ConfigTableConst.WaveCsvPath(level), ConfigTableConst.WaveColumns, rows);
            total += rows.Count;
        }
        return $"波次表：WaveTable_Level_1..6.csv（共 {total} 行）";
    }

    /// <summary>格式化塔覆盖为一行（levelIndex, towerCamp, 6 数值）</summary>
    private static string[] FormatTowerRow(int level, CampType camp, TowerOverrideConfig t)
    {
        return new[]
        {
            level.ToString(CultureInfo.InvariantCulture),
            camp.ToString(),
            t.hp.ToString(CultureInfo.InvariantCulture),
            t.atk.ToString(CultureInfo.InvariantCulture),
            t.atkRange.ToString(CultureInfo.InvariantCulture),
            t.atkCD.ToString(CultureInfo.InvariantCulture),
            t.physicalDefense.ToString(CultureInfo.InvariantCulture),
            t.magicDefense.ToString(CultureInfo.InvariantCulture),
        };
    }

    /// <summary>按值类型格式化 UnitAttr 字段：枚举转名称、数值用 InvariantCulture</summary>
    private static string FormatValue(UnitAttr attr, string propName, ConfigValueKind kind)
    {
        FieldInfo field = typeof(UnitAttr).GetField(propName);
        if (field == null) return "";

        object val = field.GetValue(attr);
        switch (kind)
        {
            case ConfigValueKind.Enum:
                return val?.ToString() ?? "";
            case ConfigValueKind.Int:
                return val == null ? "" : ((int)val).ToString(CultureInfo.InvariantCulture);
            case ConfigValueKind.Float:
                return val == null ? "" : ((float)val).ToString(CultureInfo.InvariantCulture);
            case ConfigValueKind.String:
                return val?.ToString() ?? "";
            default:
                return "";
        }
    }
}
