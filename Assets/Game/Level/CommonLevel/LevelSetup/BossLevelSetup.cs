using UnityEngine;

/// <summary>
/// Boss 关独立配置组件（可选挂载，挂到场景才生效）
/// 职责二选一或都选：
///   1. useBossVictory → 写入 BattleManager：敌方塔摧毁不判胜，仅 BossUnit 死亡判胜
///   2. 牵制范围 → 写入己方塔 TowerLeashZone：玩家单位超出半径死亡并返还部分费用
/// 与 LevelSetup 解耦，需要 Boss 机制的关卡手动挂此组件并配置即可。
/// </summary>
public class BossLevelSetup : MonoBehaviour
{
    [Header("━━━ Boss 牵制范围 → 覆盖己方塔 TowerLeashZone ━━━")]
    [Tooltip("开启后本关启用牵制机制：玩家单位超出己方塔牵制半径时死亡并返还部分费用")]
    public bool overrideLeashConfig = false;

    [Tooltip("牵制开关：关闭则本关不启用牵制")]
    public bool enableLeashZone = true;

    [Tooltip("牵制半径（世界单位）")]
    public float leashRange = 10f;

    [Range(0f, 1f)]
    [Tooltip("牵制死亡时返还的部署费用比例（0.5 = 返还一半）")]
    public float leashCostRefundRatio = 0.5f;

    [Header("━━━ Boss 单位胜利（替代敌方塔摧毁判定） ━━━")]
    [Tooltip("启用后：敌方塔被摧毁不再判胜，仅挂有 BossUnit 组件的敌方单位死亡触发胜利；玩家塔被摧毁仍判负")]
    public bool useBossVictory = false;

    void Awake()
    {
        ApplyBattleConfig();
        ApplyLeashConfig();
    }

    /// <summary>
    /// Boss 胜利模式写入 BattleManager（敌方塔摧毁不判胜，仅 BossUnit 死亡判胜）
    /// </summary>
    private void ApplyBattleConfig()
    {
        if (!useBossVictory) return;

        BattleManager bm = FindObjectOfType<BattleManager>();
        if (bm == null)
        {
            Debug.LogWarning("BossLevelSetup: 场景中未找到 BattleManager，跳过 Boss 胜利配置");
            return;
        }

        bm.useBossVictory = true;
    }

    /// <summary>
    /// 将 Boss 牵制范围配置写入己方塔的 TowerLeashZone（若场景中有）
    /// 注意：TowerLeashZone 挂在己方塔上，场景中不存在时此项配置无效果
    /// </summary>
    private void ApplyLeashConfig()
    {
        if (!overrideLeashConfig) return;

        TowerLeashZone zone = FindObjectOfType<TowerLeashZone>();
        if (zone == null)
        {
            Debug.LogWarning("BossLevelSetup: 场景中未找到 TowerLeashZone，跳过牵制范围配置");
            return;
        }

        zone.enabled = enableLeashZone;
        zone.leashRange = leashRange;
        zone.costRefundRatio = leashCostRefundRatio;
    }
}
