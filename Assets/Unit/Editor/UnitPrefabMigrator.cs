using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 兵种预制体迁移工具 —— 一键将旧组件体系升级到新的策略组件体系
///
/// 使用方法：Unity Editor → Tools → 迁移兵种预制体
///
/// 每个预制体的迁移策略：
///   soldier/Enemy2/Mechs/hound:  GroundMoveStrategy + MeleeCombatStrategy
///   FlyingUnit:                  FlightMoveStrategy + MeleeCombatStrategy
///   Archer:                      GroundMoveStrategy + RangedCombatStrategy
///   Mage:                        GroundMoveStrategy + RangedCombatStrategy
///   Healer:                      GroundMoveStrategy + HealerCombatStrategy
/// </summary>
public class UnitPrefabMigrator : EditorWindow
{
    [MenuItem("Tools/迁移兵种预制体到新体系")]
    public static void MigrateAllPrefabs()
    {
        string prefabDir = "Assets/Unit/GeneralUnit";

        var prefabConfigs = new Dictionary<string, PrefabConfig>
        {
            ["soldier"] = new PrefabConfig(
                typeof(GroundMoveStrategy), typeof(MeleeCombatStrategy),
                bulletPrefab: null, bulletSpeed: 0, hasHealerConfig: false),

            ["hound"] = new PrefabConfig(
                typeof(GroundMoveStrategy), typeof(MeleeCombatStrategy),
                bulletPrefab: null, bulletSpeed: 0, hasHealerConfig: false),

            ["Mechs"] = new PrefabConfig(
                typeof(GroundMoveStrategy), typeof(MeleeCombatStrategy),
                bulletPrefab: null, bulletSpeed: 0, hasHealerConfig: false),

            ["Enemy 2"] = new PrefabConfig(
                typeof(GroundMoveStrategy), typeof(MeleeCombatStrategy),
                bulletPrefab: null, bulletSpeed: 0, hasHealerConfig: false),

            ["Archer"] = new PrefabConfig(
                typeof(GroundMoveStrategy), typeof(RangedCombatStrategy),
                bulletPrefab: "Assets/Unit/GeneralUnit/Bullet.prefab", bulletSpeed: 8f, hasHealerConfig: false),

            ["Mage"] = new PrefabConfig(
                typeof(GroundMoveStrategy), typeof(RangedCombatStrategy),
                bulletPrefab: "Assets/Unit/GeneralUnit/Fireball.prefab", bulletSpeed: 8f, hasHealerConfig: false),

            ["FlyingUnit"] = new PrefabConfig(
                typeof(FlightMoveStrategy), typeof(MeleeCombatStrategy),
                bulletPrefab: null, bulletSpeed: 0, hasHealerConfig: false),

            ["Healer"] = new PrefabConfig(
                typeof(GroundMoveStrategy), typeof(HealerCombatStrategy),
                bulletPrefab: null, bulletSpeed: 0, hasHealerConfig: true),
        };

        int successCount = 0;
        int failCount = 0;

        foreach (var kv in prefabConfigs)
        {
            string prefabPath = $"{prefabDir}/{kv.Key}.prefab";
            if (MigratePrefab(prefabPath, kv.Value))
                successCount++;
            else
                failCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>兵种预制体迁移完成：成功 {successCount} 个，失败 {failCount} 个</color>");
    }

    static bool MigratePrefab(string prefabPath, PrefabConfig config)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"找不到预制体: {prefabPath}");
            return false;
        }

        // 用 PrefabUtility 编辑预制体
        GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
        if (instance == null)
        {
            Debug.LogWarning($"无法加载预制体: {prefabPath}");
            return false;
        }

        bool changed = false;

        // 1. 添加 UnitBrain（新增，不删除旧组件以避免数据丢失）
        if (instance.GetComponent<UnitBrain>() == null)
        {
            instance.AddComponent<UnitBrain>();
            changed = true;
            Debug.Log($"  ✓ 添加 UnitBrain");
        }

        // 2. 添加移动策略
        if (instance.GetComponent(config.moveStrategyType) == null)
        {
            instance.AddComponent(config.moveStrategyType);
            changed = true;
            Debug.Log($"  ✓ 添加 {config.moveStrategyType.Name}");
        }

        // 3. 添加战斗策略
        if (instance.GetComponent(config.combatStrategyType) == null)
        {
            var combat = instance.AddComponent(config.combatStrategyType);
            changed = true;

            // 设置远程配置
            if (config.combatStrategyType == typeof(RangedCombatStrategy) && config.bulletPrefab != null)
            {
                var ranged = combat as RangedCombatStrategy;
                // 通过 SerializedObject 设置子弹预制体引用
                SerializedObject so = new SerializedObject(ranged);
                var bulletProp = so.FindProperty("bulletPrefab");
                if (bulletProp != null)
                {
                    GameObject bulletAsset = AssetDatabase.LoadAssetAtPath<GameObject>(config.bulletPrefab);
                    bulletProp.objectReferenceValue = bulletAsset;
                    so.ApplyModifiedProperties();
                    Debug.Log($"     bulletPrefab → {config.bulletPrefab}");
                }
                var speedProp = so.FindProperty("bulletSpeed");
                if (speedProp != null)
                {
                    speedProp.floatValue = config.bulletSpeed;
                    so.ApplyModifiedProperties();
                }
                // Mage 的火球发射高度
                if (prefabPath.Contains("Mage"))
                {
                    var heightProp = so.FindProperty("spawnHeightOffset");
                    if (heightProp != null)
                    {
                        heightProp.floatValue = 1.5f;
                        so.ApplyModifiedProperties();
                    }
                }
            }

            // 设置治疗配置
            if (config.hasHealerConfig && config.combatStrategyType == typeof(HealerCombatStrategy))
            {
                var healer = combat as HealerCombatStrategy;
                SerializedObject so = new SerializedObject(healer);
                so.FindProperty("healAmount").floatValue = 25f;
                so.FindProperty("healRange").floatValue = 10f;
                so.FindProperty("healCooldown").floatValue = 2f;
                so.FindProperty("effectHeightOffset").floatValue = 2f;
                so.FindProperty("canHealSelf").boolValue = true;
                so.FindProperty("healEffectPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Unit/GeneralUnit/HealEffect.prefab");
                so.ApplyModifiedProperties();
                Debug.Log($"     治疗配置已应用");
            }

            Debug.Log($"  ✓ 添加 {config.combatStrategyType.Name}");
        }

        // 4. 删除旧组件（现在可以安全删除，新组件已到位）
        System.Type[] oldTypes = {
            typeof(UnitAI), typeof(UnitCombat), typeof(UnitMovement),
            typeof(ArcherCombat), typeof(MageCombat), typeof(HealerCombat)
        };

        foreach (var t in oldTypes)
        {
            var comp = instance.GetComponent(t);
            if (comp != null)
            {
                DestroyImmediate(comp);
                changed = true;
                Debug.Log($"  ✗ 移除旧组件: {t.Name}");
            }
        }

        if (changed)
        {
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        }

        PrefabUtility.UnloadPrefabContents(instance);
        return true;
    }

    struct PrefabConfig
    {
        public System.Type moveStrategyType;
        public System.Type combatStrategyType;
        public string bulletPrefab;
        public float bulletSpeed;
        public bool hasHealerConfig;

        public PrefabConfig(System.Type move, System.Type combat, string bulletPrefab, float bulletSpeed, bool hasHealerConfig)
        {
            moveStrategyType = move;
            combatStrategyType = combat;
            this.bulletPrefab = bulletPrefab;
            this.bulletSpeed = bulletSpeed;
            this.hasHealerConfig = hasHealerConfig;
        }
    }
}
