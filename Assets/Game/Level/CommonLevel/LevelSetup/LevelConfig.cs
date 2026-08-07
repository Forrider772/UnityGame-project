using UnityEngine;

/// <summary>
/// 关卡配置资产：由策划表工具（Tools/策划表/导入 xxx 表）从 CSV 导入生成，每关一个。
/// LevelSetup 在 Awake 时通过 Resources.Load 读取本资产并填充自身字段。
/// 引用字段（availableCards / levelBuffs / waveList）在导入时已用 AssetDatabase 解析为资产引用，
/// 因此运行时无需再按 ID 查找。
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Level/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("=== 关卡标识 ===")]
    public int levelIndex;
    public string levelName;

    [Header("=== 初始牌组（按 cardID 解析引用） ===")]
    [Tooltip("由 DeckTable.csv 导入生成；LevelSetup 读到非空时启用 overrideDeckConfig")]
    public CardData[] availableCards;

    [Header("=== 关卡 Buff（按 buffID 解析引用） ===")]
    [Tooltip("由 BuffTable.csv 导入生成；LevelSetup 读到非空时注册到 BuffManager")]
    public BuffData[] levelBuffs;

    [Header("=== 波次（引用 WaveList 资产） ===")]
    [Tooltip("由 WaveTable_Level_N.csv 导入生成；LevelSetup 读到非空时启用 overrideWaveConfig")]
    public WaveList waveList;

    [Header("=== 玩家塔覆盖 ===")]
    [Tooltip("由 TowerTable.csv 导入生成；LevelSetup 读到开关时应用 TowerOverrideConfig")]
    public bool overridePlayerTowerProps;
    public TowerOverrideConfig playerTowerOverride = new TowerOverrideConfig();

    [Header("=== 敌方塔覆盖 ===")]
    public bool overrideEnemyTowerProps;
    public TowerOverrideConfig enemyTowerOverride = new TowerOverrideConfig();
}
