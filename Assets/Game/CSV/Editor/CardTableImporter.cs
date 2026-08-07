using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 卡牌表导入器：CardTable.csv → 创建/更新 Assets/Game/Unit/CardAssets/Card_{cardID}.asset
///
/// 关键实现：
///   - unitPrefab 由 unitID 列映射为预制体引用
///   - 永不写 icon 字段，已有图标保留；新建卡 icon 保持 null（图标在 Unity 里配）
///   - 资产存在走 SerializedObject 更新；不存在走 CreateAsset 新建
///   - 卡牌 camp 与单位表 camp 不一致仅告警不阻断（阵营解耦是合法需求）
/// </summary>
public static class CardTableImporter
{
    [MenuItem("Tools/策划表/导入 卡牌表")]
    public static void Import()
    {
        string csvPath = ConfigTableConst.CardCsvPath;
        if (!File.Exists(csvPath))
        {
            EditorUtility.DisplayDialog("导入卡牌表", $"找不到 CSV 文件：{csvPath}\n请先执行【Tools/策划表/导出 CSV 初版】生成表格。", "确定");
            return;
        }

        List<string[]> raw;
        try
        {
            raw = CsvReader.ReadFile(csvPath);
        }
        catch (IOException e)
        {
            EditorUtility.DisplayDialog("导入卡牌表", e.Message, "确定");
            return;
        }

        if (raw.Count < 2)
        {
            EditorUtility.DisplayDialog("导入卡牌表", "CSV 内容为空（至少需要表头 + 一行数据）。", "确定");
            return;
        }

        var table = new CsvTable(raw[0], raw.GetRange(1, raw.Count - 1));

        var missing = new List<string>();
        foreach (var col in ConfigTableConst.CardColumns)
        {
            if (!table.HasColumn(col)) missing.Add(col);
        }
        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog("导入卡牌表", $"CSV 缺少必填列：{string.Join(", ", missing)}\n表头不可修改，请检查文件。", "确定");
            return;
        }

        var errors = new List<string>();
        var touchedAssets = new List<string>();
        int ok = 0;

        foreach (string[] row in table.Rows)
        {
            string cardID = table.Get(row, "cardID");
            if (string.IsNullOrEmpty(cardID)) continue;

            string unitID = table.Get(row, "unitID");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ConfigTableConst.UnitPrefabDir}/{unitID}.prefab");
            if (prefab == null)
            {
                errors.Add($"卡牌 [{cardID}]：unitID [{unitID}] 的预制体不存在 {ConfigTableConst.UnitPrefabDir}/{unitID}.prefab");
                continue;
            }

            if (!Enum.TryParse(table.Get(row, "camp"), true, out CampType camp))
            {
                errors.Add($"卡牌 [{cardID}]：camp={table.Get(row, "camp") ?? "(空)"} 无效");
                continue;
            }
            if (!int.TryParse(table.Get(row, "cost"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int cost))
            {
                errors.Add($"卡牌 [{cardID}]：cost={table.Get(row, "cost") ?? "(空)"} 不是有效的整数");
                continue;
            }
            if (!float.TryParse(table.Get(row, "cooldown"), NumberStyles.Float, CultureInfo.InvariantCulture, out float cooldown))
            {
                errors.Add($"卡牌 [{cardID}]：cooldown={table.Get(row, "cooldown") ?? "(空)"} 不是有效的数值");
                continue;
            }
            string cardName = table.Get(row, "cardName") ?? cardID;

            // 与单位表 camp 一致性校验（仅告警）
            UnitAttr attr = prefab.GetComponent<UnitAttr>();
            if (attr != null && attr.camp != camp)
            {
                Debug.LogWarning($"卡牌 [{cardID}]：camp={camp}，但单位 [{unitID}] 的 camp={attr.camp}，两者不一致（如为有意设计可忽略）");
            }

            string assetPath = $"{ConfigTableConst.CardAssetDir}/Card_{cardID}.asset";
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);

            if (card == null)
            {
                // 新建：参考 CardDataGenerator.CreateCard 的样板
                card = ScriptableObject.CreateInstance<CardData>();
                card.cardID = cardID;
                card.cardName = cardName;
                card.camp = camp;
                card.cost = cost;
                card.cooldown = cooldown;
                card.unitPrefab = prefab;   // 引用在 CreateAsset 前赋值即可序列化
                AssetDatabase.CreateAsset(card, assetPath);
            }
            else
            {
                touchedAssets.Add(assetPath);
                Undo.RegisterCompleteObjectUndo(card, $"导入卡牌表 {cardID}");

                var so = new SerializedObject(card);
                so.FindProperty("cardName").stringValue = cardName;
                so.FindProperty("camp").intValue = (int)camp;
                so.FindProperty("cost").intValue = cost;
                so.FindProperty("cooldown").floatValue = cooldown;
                so.FindProperty("unitPrefab").objectReferenceValue = prefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(card);
            }
            ok++;
        }

        ConfigTableConst.BackupFiles(touchedAssets);
        AssetDatabase.SaveAssets();

        string msg = $"卡牌表导入完成：成功 {ok} 条";
        if (errors.Count > 0)
            msg += $"\n\n失败 {errors.Count} 条：\n" + string.Join("\n", errors);

        Debug.Log($"<color=green>卡牌表导入完成：成功 {ok} 条</color>");
        if (errors.Count > 0) Debug.LogWarning(string.Join("\n", errors));
        EditorUtility.DisplayDialog("卡牌表导入", msg, "确定");
    }
}
