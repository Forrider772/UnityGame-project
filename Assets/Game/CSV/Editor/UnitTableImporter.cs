using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 单位表导入器：UnitTable.csv → 更新 Assets/Game/Unit/GeneralUnit/*.prefab 的 UnitAttr 数值
///
/// 关键实现：
///   - 用 SerializedObject + FindProperty + ApplyModifiedProperties 写回序列化字段，
///     直接改 attr.maxHp = x 只改内存实例不会落盘到 .prefab 资产
///   - Undo.RegisterCompleteObjectUndo 保留编辑器 Ctrl+Z
///   - 任一单元格解析失败则整行跳过，坏行不落盘
///   - 结束后统一一次 SaveAssets，配合导入前文件备份，防中途失败刷盘半成品
/// </summary>
public static class UnitTableImporter
{
    [MenuItem("Tools/策划表/导入 单位表")]
    public static void Import()
    {
        string csvPath = ConfigTableConst.UnitCsvPath;
        if (!File.Exists(csvPath))
        {
            EditorUtility.DisplayDialog("导入单位表", $"找不到 CSV 文件：{csvPath}\n请先执行【Tools/策划表/导出 CSV 初版】生成表格。", "确定");
            return;
        }

        List<string[]> raw;
        try
        {
            raw = CsvReader.ReadFile(csvPath);
        }
        catch (IOException e)
        {
            EditorUtility.DisplayDialog("导入单位表", e.Message, "确定");
            return;
        }

        if (raw.Count < 2)
        {
            EditorUtility.DisplayDialog("导入单位表", "CSV 内容为空（至少需要表头 + 一行数据）。", "确定");
            return;
        }

        var table = new CsvTable(raw[0], raw.GetRange(1, raw.Count - 1));

        // 校验必填列：unitID + 全部列映射列，缺列直接中止防止整表静默失效
        var required = new List<string> { "unitID" };
        foreach (var (col, _, _) in ConfigTableConst.UnitColumns)
            required.Add(col);

        var missing = new List<string>();
        foreach (var col in required)
        {
            if (!table.HasColumn(col)) missing.Add(col);
        }
        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog("导入单位表", $"CSV 缺少必填列：{string.Join(", ", missing)}\n表头不可修改，请检查文件。", "确定");
            return;
        }

        var errors = new List<string>();
        var touchedAssets = new List<string>();
        int ok = 0;

        foreach (string[] row in table.Rows)
        {
            string unitID = table.Get(row, "unitID");
            if (string.IsNullOrEmpty(unitID)) continue;   // 空行已由解析器过滤，此处仅防边界

            string prefabPath = $"{ConfigTableConst.UnitPrefabDir}/{unitID}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                errors.Add($"单位 [{unitID}]：预制体不存在 {prefabPath}");
                continue;
            }

            UnitAttr attr = prefab.GetComponent<UnitAttr>();
            if (attr == null)
            {
                errors.Add($"单位 [{unitID}]：预制体缺少 UnitAttr 组件");
                continue;
            }

            // 先整行解析，任一单元格非法则整行跳过不写
            var parsed = ParseRow(table, row, unitID, errors);
            if (parsed == null) continue;

            touchedAssets.Add(prefabPath);
            Undo.RegisterCompleteObjectUndo(prefab, $"导入单位表 {unitID}");

            var so = new SerializedObject(attr);
            foreach (var (col, prop, kind) in ConfigTableConst.UnitColumns)
            {
                SerializedProperty p = so.FindProperty(prop);
                if (p == null)
                {
                    errors.Add($"单位 [{unitID}]：序列化属性 {prop} 未找到");
                    continue;
                }
                switch (kind)
                {
                    case ConfigValueKind.Enum:  p.intValue   = parsed.enums[prop];  break;
                    case ConfigValueKind.Int:   p.intValue   = parsed.ints[prop];   break;
                    case ConfigValueKind.Float: p.floatValue = parsed.floats[prop]; break;
                }
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(prefab);
            ok++;
        }

        ConfigTableConst.BackupFiles(touchedAssets);
        AssetDatabase.SaveAssets();

        string msg = $"单位表导入完成：成功 {ok} 条";
        if (errors.Count > 0)
            msg += $"\n\n失败 {errors.Count} 条：\n" + string.Join("\n", errors);

        Debug.Log($"<color=green>单位表导入完成：成功 {ok} 条</color>");
        if (errors.Count > 0) Debug.LogWarning(string.Join("\n", errors));
        EditorUtility.DisplayDialog("单位表导入", msg, "确定");
    }

    // ==================== 行解析 ====================

    private class ParsedRow
    {
        public Dictionary<string, int> enums = new Dictionary<string, int>();
        public Dictionary<string, int> ints = new Dictionary<string, int>();
        public Dictionary<string, float> floats = new Dictionary<string, float>();
    }

    /// <summary>整行解析，返回 null 表示有非法单元格（错误已写入 errors）</summary>
    private static ParsedRow ParseRow(CsvTable table, string[] row, string unitID, List<string> errors)
    {
        var parsed = new ParsedRow();
        bool failed = false;

        foreach (var (col, prop, kind) in ConfigTableConst.UnitColumns)
        {
            switch (kind)
            {
                case ConfigValueKind.Enum:
                    Type enumType = GetEnumType(prop);
                    string enumStr = table.Get(row, col);
                    if (!TryParseEnum(enumType, enumStr, out int enumVal))
                    {
                        errors.Add($"单位 [{unitID}]：{col}={enumStr ?? "(空)"} 不是有效的 {enumType?.Name ?? "枚举"} 值");
                        failed = true;
                    }
                    else
                    {
                        parsed.enums[prop] = enumVal;
                    }
                    break;

                case ConfigValueKind.Int:
                    if (!int.TryParse(table.Get(row, col), NumberStyles.Integer, CultureInfo.InvariantCulture, out int intVal))
                    {
                        errors.Add($"单位 [{unitID}]：{col}={table.Get(row, col) ?? "(空)"} 不是有效的整数");
                        failed = true;
                    }
                    else
                    {
                        parsed.ints[prop] = intVal;
                    }
                    break;

                case ConfigValueKind.Float:
                    if (!float.TryParse(table.Get(row, col), NumberStyles.Float, CultureInfo.InvariantCulture, out float floatVal))
                    {
                        errors.Add($"单位 [{unitID}]：{col}={table.Get(row, col) ?? "(空)"} 不是有效的数值");
                        failed = true;
                    }
                    else
                    {
                        parsed.floats[prop] = floatVal;
                    }
                    break;
            }
        }

        return failed ? null : parsed;
    }

    /// <summary>通过反射获取 UnitAttr 上序列化字段对应的枚举类型（camp→CampType 等）</summary>
    private static Type GetEnumType(string propName)
    {
        FieldInfo field = typeof(UnitAttr).GetField(propName);
        return field?.FieldType;
    }

    /// <summary>解析枚举：支持枚举名（忽略大小写）与数字两种写法</summary>
    private static bool TryParseEnum(Type type, string value, out int intValue)
    {
        intValue = 0;
        if (type == null || !type.IsEnum || string.IsNullOrEmpty(value)) return false;

        // 优先按名称解析（忽略大小写）；名称解析失败再尝试数字
        if (Enum.TryParse(type, value, true, out object byName) && Enum.IsDefined(type, byName))
        {
            intValue = (int)byName;
            return true;
        }
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int byNum)
            && Enum.IsDefined(type, byNum))
        {
            intValue = byNum;
            return true;
        }
        return false;
    }
}
