using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 一键生成所有兵种的 CardData 资产
/// 使用方法：Unity Editor → Tools → 生成所有兵种卡牌数据
/// </summary>
public class CardDataGenerator : EditorWindow
{
    const string OutputDir = "Assets/Unit/CardAssets";

    [MenuItem("Tools/生成所有兵种卡牌数据")]
    public static void GenerateAll()
    {
        if (!Directory.Exists(OutputDir))
            Directory.CreateDirectory(OutputDir);

        CreateCard("soldier",   "步兵",       CampType.Player, 3, 3f, "Assets/Unit/GeneralUnit/soldier.prefab");
        CreateCard("hound",     "猎犬",       CampType.Player, 1, 2f, "Assets/Unit/GeneralUnit/hound.prefab");
        CreateCard("Mechs",     "机甲",       CampType.Player, 5, 5f, "Assets/Unit/GeneralUnit/Mechs.prefab");
        CreateCard("Enemy_2",   "敌方步兵",   CampType.Enemy,  0, 0f, "Assets/Unit/GeneralUnit/Enemy 2.prefab");
        CreateCard("FlyingUnit","飞行单位",   CampType.Player, 4, 4f, "Assets/Unit/GeneralUnit/FlyingUnit.prefab");
        CreateCard("Archer",    "射手",       CampType.Player, 4, 4f, "Assets/Unit/GeneralUnit/Archer.prefab");
        CreateCard("Mage",      "法师",       CampType.Player, 5, 5f, "Assets/Unit/GeneralUnit/Mage.prefab");
        CreateCard("Healer",    "治疗师",     CampType.Player, 3, 3f, "Assets/Unit/GeneralUnit/Healer.prefab");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=green>CardData 资产已生成到：{OutputDir}</color>");
    }

    static void CreateCard(string id, string name, CampType camp, int cost, float cooldown, string prefabPath)
    {
        string assetPath = $"{OutputDir}/Card_{id}.asset";

        // 已存在则跳过
        if (File.Exists(assetPath))
        {
            Debug.Log($"跳过（已存在）：{assetPath}");
            return;
        }

        CardData card = ScriptableObject.CreateInstance<CardData>();
        card.cardID = id;
        card.cardName = name;
        card.camp = camp;
        card.cost = cost;
        card.cooldown = cooldown;
        card.unitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        AssetDatabase.CreateAsset(card, assetPath);
        Debug.Log($"创建：{assetPath}");
    }
}
