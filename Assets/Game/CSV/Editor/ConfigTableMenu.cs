using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 策划配置表工具菜单：一键导入全部 + 打开 CSV 文件夹
/// 菜单结构：
///   Tools/策划表/导入 单位表         → UnitTableImporter
///   Tools/策划表/导入 卡牌表         → CardTableImporter
///   Tools/策划表/导入 初始牌组表     → DeckTableImporter
///   Tools/策划表/导入 Buff 表        → BuffTableImporter
///   Tools/策划表/导入 塔属性表       → TowerTableImporter
///   Tools/策划表/导入 波次表         → WaveTableImporter
///   Tools/策划表/一键导入全部         → 六表顺序执行
///   Tools/策划表/导出 CSV 初版        → ConfigTableExporter
///   Tools/策划表/打开 CSV 文件夹      → 打开 Assets/Game/CSV/Tables
/// </summary>
public static class ConfigTableMenu
{
    [MenuItem("Tools/策划表/一键导入全部")]
    public static void ImportAll()
    {
        UnitTableImporter.Import();
        CardTableImporter.Import();
        DeckTableImporter.Import();
        BuffTableImporter.Import();
        TowerTableImporter.Import();
        WaveTableImporter.Import();
    }

    [MenuItem("Tools/策划表/打开 CSV 文件夹")]
    public static void OpenCsvFolder()
    {
        if (!Directory.Exists(ConfigTableConst.CsvDir))
        {
            EditorUtility.DisplayDialog("策划表", "CSV 文件夹不存在，请先执行【Tools/策划表/导出 CSV 初版】。", "确定");
            return;
        }
        EditorUtility.RevealInFinder(ConfigTableConst.CsvDir);
    }
}
