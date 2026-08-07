using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡统一配置入口组件
/// 挂在关卡场景中的 LevelSetup GameObject 上，一个场景仅有一个。
/// 在 Awake/Start 中将配置分发到各子系统——所有关卡属性集中在一个 Inspector 面板中。
/// 不影响各子系统独立运行——若场景中无此组件，各管理器使用自身的 Inspector 默认值。
/// </summary>
[DefaultExecutionOrder(-100)]
public class LevelSetup : MonoBehaviour
{
    // ==================== 关卡基本信息 ====================
    [Header("━━━ 关卡基本信息 ━━━")]
    public string levelName;
    public int levelIndex;
    [TextArea(2, 4)]
    public string levelDescription;

    // ==================== 战斗 / 费用 ====================
    [Header("━━━ 战斗 / 费用 → 覆盖 BattleManager ━━━")]
    public bool overrideBattleConfig = true;
    public float maxCost = 10f;
    public float costAddSpeed = 1f;
    public float startingCost = 3f;

    // ==================== 波次 ====================
    [Header("━━━ 波次 ━━━")]
    public bool overrideWaveConfig = true;
    public WaveList waveList;

    // ==================== 防御塔 ====================
    [Header("━━━ 防御塔 (拖入场景中的塔对象) ━━━")]
    public bool overrideTowerConfig = true;
    public GameObject playerTower;
    public GameObject enemyTower;

    [Header("  > 玩家塔属性覆盖")]
    public bool overridePlayerTowerProps = false;
    public TowerOverrideConfig playerTowerOverride = new TowerOverrideConfig();

    [Header("  > 敌方塔属性覆盖")]
    public bool overrideEnemyTowerProps = false;
    public TowerOverrideConfig enemyTowerOverride = new TowerOverrideConfig();

    // ==================== 卡组 ====================
    [Header("━━━ 卡组 (玩家可用卡牌) ━━━")]
    public bool overrideDeckConfig = true;
    public CardData[] availableCards;

    // ==================== 资源点默认设置 ====================
    [Header("━━━ 资源点默认设置 → 覆盖场景中所有 ResourcePoint ━━━")]
    public bool overrideResourcePointConfig = true;
    public int resourcePointMaxGarrison = 3;
    public float resourcePointRange = 1.5f;
    public float resourcePointBonus = 5f;

    // ==================== 驻扎点默认设置 ====================
    [Header("━━━ 驻扎点默认设置 → 写入 GarrisonPointManager ━━━")]
    public bool overrideGarrisonConfig = true;
    public int garrisonPointMaxGarrison = 3;
    public float garrisonPointRange = 1.5f;

    // ==================== 关卡 Buff ====================
    [Header("━━━ 关卡 Buff（对所有匹配阵营的单位生效） ━━━")]
    [Tooltip("在此拖入 BuffData 资产，游戏开始时自动注册到 BuffManager")]
    public BuffData[] levelBuffs;

    // ==================== Awake / Start ====================

    void Awake()
    {
        LoadLevelConfig();
        ApplyBattleConfig();
        ApplyDeckConfig();
        ApplyBuffConfig();
    }

    void Start()
    {
        ApplyTowerConfig();
        ApplyResourcePointConfig();
        ApplyGarrisonConfig();
    }

    /// <summary>
    /// 运行时读取本关 LevelConfig 资产填充自身字段。
    /// 资产由策划表工具从 CSV 导入生成（Resources/Config/LevelConfig_{场景名}.asset）；
    /// 找不到时使用场景默认配置（不影响 Test/BeiFen 等无配置的场景）。
    /// </summary>
    private void LoadLevelConfig()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        LevelConfig cfg = Resources.Load<LevelConfig>($"Config/LevelConfig_{sceneName}");
        if (cfg == null)
        {
            Debug.Log($"LevelSetup: 未找到 {sceneName} 的 LevelConfig 资产，使用场景默认配置");
            return;
        }

        if (cfg.availableCards != null && cfg.availableCards.Length > 0)
        {
            this.availableCards = cfg.availableCards;
            this.overrideDeckConfig = true;
        }

        if (cfg.levelBuffs != null && cfg.levelBuffs.Length > 0)
            this.levelBuffs = cfg.levelBuffs;

        if (cfg.waveList != null)
        {
            this.waveList = cfg.waveList;
            this.overrideWaveConfig = true;
        }

        if (cfg.overridePlayerTowerProps)
        {
            this.overridePlayerTowerProps = true;
            this.playerTowerOverride = cfg.playerTowerOverride;
        }

        if (cfg.overrideEnemyTowerProps)
        {
            this.overrideEnemyTowerProps = true;
            this.enemyTowerOverride = cfg.enemyTowerOverride;
        }
    }

    // ==================== 配置应用方法 ====================

    /// <summary>
    /// 将战斗/费用/波次/双塔引用写入 BattleManager
    /// </summary>
    private void ApplyBattleConfig()
    {
        BattleManager bm = FindObjectOfType<BattleManager>();
        if (bm == null)
        {
            Debug.LogWarning("LevelSetup: 场景中未找到 BattleManager，跳过战斗配置");
            return;
        }

        if (overrideBattleConfig)
        {
            bm.maxCost = maxCost;
            bm.costAddSpeed = costAddSpeed;
            bm.nowCost = startingCost;
        }

        if (overrideWaveConfig && waveList != null)
            bm.waveList = waveList;

        if (overrideTowerConfig)
        {
            if (playerTower != null) bm.playerTower = playerTower;
            if (enemyTower != null)  bm.enemyTower  = enemyTower;
        }
    }

    /// <summary>
    /// 替换 CardManager 的玩家卡组
    /// </summary>
    private void ApplyDeckConfig()
    {
        if (!overrideDeckConfig || availableCards == null || availableCards.Length == 0)
            return;

        CardManager cm = FindObjectOfType<CardManager>();
        if (cm == null)
        {
            Debug.LogWarning("LevelSetup: 场景中未找到 CardManager，跳过卡组配置");
            return;
        }

        if (cm.playerDeck == null)
            cm.playerDeck = new PlayerDeck();

        cm.playerDeck.carryCards.Clear();
        cm.playerDeck.carryCards.AddRange(availableCards);
    }

    /// <summary>
    /// 按开关选择性覆盖双塔的 TowerBase 属性
    /// </summary>
    private void ApplyTowerConfig()
    {
        if (!overrideTowerConfig) return;

        if (overridePlayerTowerProps && playerTower != null)
            playerTowerOverride.ApplyTo(playerTower.GetComponent<TowerBase>());

        if (overrideEnemyTowerProps && enemyTower != null)
            enemyTowerOverride.ApplyTo(enemyTower.GetComponent<TowerBase>());
    }

    /// <summary>
    /// 将资源点默认值应用到场景中所有 ResourcePoint
    /// </summary>
    private void ApplyResourcePointConfig()
    {
        if (!overrideResourcePointConfig) return;

        ResourcePoint[] allPoints = FindObjectsOfType<ResourcePoint>();
        foreach (var rp in allPoints)
        {
            if (rp == null) continue;
            rp.maxGarrison = resourcePointMaxGarrison;
            rp.garrisonRange = resourcePointRange;
            rp.resourceBonus = resourcePointBonus;
        }
    }

    /// <summary>
    /// 将驻扎点默认值写入 GarrisonPointManager，供后续动态创建驻扎点时使用
    /// </summary>
    private void ApplyGarrisonConfig()
    {
        if (!overrideGarrisonConfig) return;

        GarrisonPointManager gpm = GarrisonPointManager.Instance;
        if (gpm == null)
        {
            Debug.LogWarning("LevelSetup: 场景中未找到 GarrisonPointManager，跳过驻扎点配置");
            return;
        }

        gpm.defaultMaxGarrison = garrisonPointMaxGarrison;
        gpm.defaultGarrisonRange = garrisonPointRange;
    }

    /// <summary>
    /// 将关卡 buff 数组注册到 BuffManager，供后续单位生成时应用
    /// </summary>
    private void ApplyBuffConfig()
    {
        if (levelBuffs == null || levelBuffs.Length == 0) return;

        BuffManager buffManager = FindObjectOfType<BuffManager>();
        if (buffManager == null)
        {
            Debug.LogWarning("LevelSetup: 场景中未找到 BuffManager，跳过 Buff 配置");
            return;
        }

        foreach (var buff in levelBuffs)
        {
            if (buff != null)
                buffManager.RegisterBuff(buff);
        }
    }

}
