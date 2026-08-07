using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 波次表导入器：WaveTable_Level_N.csv → 重建 WaveList_Level_N.asset 的 waves 数组 + LevelConfig.waveList 引用
/// 每关一个 CSV（N=1..6），按文件名解析关卡号。unitID → 单位预制体引用，spawnPoint 不进 CSV（留空用默认）。
/// 重建数组会顺带把旧字段 nextWaveInterval 迁移为新字段 delayBeforeStart。
/// </summary>
public static class WaveTableImporter
{
    [MenuItem("Tools/策划表/导入 波次表")]
    public static void Import()
    {
        var errors = new List<string>();
        var touchedAssets = new List<string>();
        int okLevels = 0;

        string[] files = Directory.GetFiles(ConfigTableConst.CsvDir, "WaveTable_Level_*.csv");
        Array.Sort(files, StringComparer.Ordinal);

        foreach (string file in files)
        {
            string csvPath = file.Replace('\\', '/');
            int level = ParseLevelFromFileName(Path.GetFileNameWithoutExtension(file));
            if (level <= 0)
            {
                errors.Add($"无法从文件名解析关卡号：{csvPath}，已跳过");
                continue;
            }

            List<string[]> raw;
            try
            {
                raw = CsvReader.ReadFile(csvPath);
            }
            catch (IOException e)
            {
                errors.Add($"{csvPath}：{e.Message}");
                continue;
            }

            if (raw.Count < 2)
            {
                errors.Add($"关卡[{level}]：波次 CSV 内容为空，已跳过（文件 {csvPath}）");
                continue;
            }

            var table = new CsvTable(raw[0], raw.GetRange(1, raw.Count - 1));

            var missing = new List<string>();
            foreach (var col in ConfigTableConst.WaveColumns)
            {
                if (!table.HasColumn(col)) missing.Add(col);
            }
            if (missing.Count > 0)
            {
                errors.Add($"关卡[{level}]：CSV 缺少必填列 {string.Join(", ", missing)}，已跳过");
                continue;
            }

            // 解析行（按 waveIndex 排序）
            var rows = new List<(int index, WaveData data)>();
            CampType fileCamp = CampType.Enemy;
            bool hasCamp = false;
            bool levelFailed = false;

            foreach (string[] row in table.Rows)
            {
                if (!int.TryParse(table.Get(row, "waveIndex"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int waveIndex))
                {
                    errors.Add($"关卡[{level}]：waveIndex={table.Get(row, "waveIndex")} 无效，已跳过该行");
                    levelFailed = true;
                    break;
                }

                string unitID = table.Get(row, "unitID");
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ConfigTableConst.UnitPrefabDir}/{unitID}.prefab");
                if (prefab == null)
                {
                    errors.Add($"关卡[{level}] 波[{waveIndex}]：unitID [{unitID}] 无对应预制体");
                    levelFailed = true;
                    break;
                }

                if (!Enum.TryParse(table.Get(row, "camp"), true, out CampType camp))
                {
                    errors.Add($"关卡[{level}] 波[{waveIndex}]：camp={table.Get(row, "camp")} 无效");
                    levelFailed = true;
                    break;
                }
                if (!hasCamp) { fileCamp = camp; hasCamp = true; }
                else if (fileCamp != camp)
                {
                    errors.Add($"关卡[{level}]：camp 不一致（第 {waveIndex} 波为 {camp}，首波为 {fileCamp}），以首波为准");
                }

                if (!Enum.TryParse(table.Get(row, "pathID"), true, out PathID pathID))
                {
                    errors.Add($"关卡[{level}] 波[{waveIndex}]：pathID={table.Get(row, "pathID")} 无效");
                    levelFailed = true;
                    break;
                }
                if (!Enum.TryParse(table.Get(row, "triggerType"), true, out WaveTriggerType trigger))
                {
                    errors.Add($"关卡[{level}] 波[{waveIndex}]：triggerType={table.Get(row, "triggerType")} 无效");
                    levelFailed = true;
                    break;
                }
                if (!int.TryParse(table.Get(row, "spawnCount"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int spawnCount)
                    || !float.TryParse(table.Get(row, "spawnInterval"), NumberStyles.Float, CultureInfo.InvariantCulture, out float spawnInterval))
                {
                    errors.Add($"关卡[{level}] 波[{waveIndex}]：spawnCount/spawnInterval 数值无效");
                    levelFailed = true;
                    break;
                }

                float delay = 2f;   // 默认与 WaveData.delayBeforeStart 一致
                if (table.Get(row, "delayBeforeStart") != null)
                {
                    if (!float.TryParse(table.Get(row, "delayBeforeStart"), NumberStyles.Float, CultureInfo.InvariantCulture, out delay))
                    {
                        errors.Add($"关卡[{level}] 波[{waveIndex}]：delayBeforeStart={table.Get(row, "delayBeforeStart")} 无效");
                        levelFailed = true;
                        break;
                    }
                }

                rows.Add((waveIndex, new WaveData
                {
                    unitPrefab = prefab,
                    pathID = pathID,
                    spawnCount = spawnCount,
                    spawnInterval = spawnInterval,
                    triggerType = trigger,
                    delayBeforeStart = delay,
                }));
            }

            if (levelFailed || rows.Count == 0) continue;

            rows.Sort((a, b) => a.index.CompareTo(b.index));

            // 重建 WaveList 资产
            string waveAssetPath = ConfigTableConst.WaveAssetPath(level);
            if (File.Exists(waveAssetPath)) touchedAssets.Add(waveAssetPath);

            WaveList wl = AssetDatabase.LoadAssetAtPath<WaveList>(waveAssetPath);
            if (wl == null)
            {
                wl = ScriptableObject.CreateInstance<WaveList>();
                AssetDatabase.CreateAsset(wl, waveAssetPath);
            }
            Undo.RegisterCompleteObjectUndo(wl, $"导入波次表 Level_{level}");
            wl.camp = fileCamp;
            wl.waves = new WaveData[rows.Count];
            for (int i = 0; i < rows.Count; i++)
                wl.waves[i] = rows[i].data;
            EditorUtility.SetDirty(wl);

            // 写入 LevelConfig.waveList 引用
            string cfgPath = ConfigTableConst.LevelConfigAssetPath(level);
            if (File.Exists(cfgPath)) touchedAssets.Add(cfgPath);
            LevelConfig cfg = ConfigTableConst.LoadOrCreateLevelConfig(level);
            Undo.RegisterCompleteObjectUndo(cfg, $"导入波次表 Level_{level}");
            cfg.waveList = wl;
            EditorUtility.SetDirty(cfg);

            okLevels++;
        }

        ConfigTableConst.BackupFiles(touchedAssets);
        AssetDatabase.SaveAssets();

        string msg = $"波次表导入完成：成功 {okLevels} 关";
        if (errors.Count > 0)
            msg += $"\n\n失败 {errors.Count} 条：\n" + string.Join("\n", errors);

        Debug.Log($"<color=green>波次表导入完成：成功 {okLevels} 关</color>");
        if (errors.Count > 0) Debug.LogWarning(string.Join("\n", errors));
        EditorUtility.DisplayDialog("波次表导入", msg, "确定");
    }

    /// <summary>从文件名解析关卡号："WaveTable_Level_1" → 1</summary>
    private static int ParseLevelFromFileName(string fileName)
    {
        const string prefix = "WaveTable_Level_";
        if (fileName == null || !fileName.StartsWith(prefix)) return -1;
        string num = fileName.Substring(prefix.Length);
        return int.TryParse(num, out int v) ? v : -1;
    }
}
