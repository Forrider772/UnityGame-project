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
                "将用当前预制体 / 卡牌资产的数值生成 UnitTable.csv 和 CardTable.csv，并覆盖已有文件。\n是否继续？",
                "导出", "取消"))
            return;

        string unitLog;
        string cardLog;
        try
        {
            unitLog = ExportUnitTable();
            cardLog = ExportCardTable();
        }
        catch (IOException e)
        {
            Debug.LogError(e.Message);
            EditorUtility.DisplayDialog("导出 CSV 初版", e.Message, "确定");
            return;
        }
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>配置表导出完成</color>\n{unitLog}\n{cardLog}");
        EditorUtility.DisplayDialog("导出 CSV 初版", unitLog + "\n\n" + cardLog + "\n\n可在 Excel 中编辑后执行【导入】。", "确定");
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
