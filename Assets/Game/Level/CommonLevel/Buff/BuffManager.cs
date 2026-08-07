using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buff 管理器（战斗级单例，挂载在 BattleManager 所在 GameObject）
///
/// 职责：
///   1. 持有当前战斗的生效 buff 列表（由 LevelSetup 在 Awake 阶段注册）
///   2. 单位生成时统一应用 buff：根据阵营匹配，累乘 modifier 到 UnitAttr
///
/// 生命周期：随场景加载创建，随场景卸载销毁
/// </summary>
[DefaultExecutionOrder(-90)]
public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance;

    /// <summary>当前战斗的所有生效 buff</summary>
    private List<BuffData> activeBuffs = new List<BuffData>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    // ==================== 外部 API ====================

    /// <summary>
    /// 注册一个 buff（关卡配置阶段由 LevelSetup 调用）
    /// </summary>
    public void RegisterBuff(BuffData buff)
    {
        if (buff == null) return;
        activeBuffs.Add(buff);
        Debug.Log($"BuffManager: 已注册 Buff [{buff.buffName}] 类型={buff.statType} 值={buff.modifierValue:+0.0%;-0.0%}");
    }

    /// <summary>
    /// 清空所有 buff（关卡重置时可选调用）
    /// </summary>
    public void ClearBuffs()
    {
        activeBuffs.Clear();
    }

    // ==================== Buff 应用 ====================

    /// <summary>
    /// 遍历所有生效 buff，对匹配阵营的目标单位累乘 modifier
    /// 调用时机：单位 Instantiate + UnitHelper.Configure 之后，brain.SetPath 之前
    /// </summary>
    /// <param name="attr">目标单位的 UnitAttr 组件</param>
    /// <param name="camp">目标单位所属阵营</param>
    public void ApplyBuffs(UnitAttr attr, CampType camp)
    {
        if (attr == null) return;

        // 重置所有 multiplier（避免预制体上残留旧值）
        attr.atkMultiplier = 1f;
        attr.maxHpMultiplier = 1f;
        attr.moveSpeedMultiplier = 1f;
        attr.atkSpeedMultiplier = 1f;

        // 遍历生效 buff，累乘 modifier
        foreach (var buff in activeBuffs)
        {
            if (buff == null) continue;
            if (!MatchesTarget(buff.targetCamp, camp)) continue;

            float multiplier = 1f + buff.modifierValue;

            switch (buff.statType)
            {
                case BuffStatType.Attack:
                    attr.atkMultiplier *= multiplier;
                    break;
                case BuffStatType.MaxHp:
                    attr.maxHpMultiplier *= multiplier;
                    break;
                case BuffStatType.MoveSpeed:
                    attr.moveSpeedMultiplier *= multiplier;
                    break;
                case BuffStatType.AttackSpeed:
                    attr.atkSpeedMultiplier *= multiplier;
                    break;
                case BuffStatType.PhysicalDefense:
                    attr.physicalDefenseMultiplier *= multiplier;
                    break;
                case BuffStatType.MagicDefense:
                    attr.magicDefenseMultiplier *= multiplier;
                    break;
            }
        }

        // 修正 currentHp：Awake 时用 base maxHp 初始化，此处按修饰后上限重新设为满血
        // （仅当 multiplier > 0 时处理，防止被意外清零）
        if (attr.maxHpMultiplier > 0f && attr.currentHp > 0f)
            attr.currentHp = attr.ModifiedMaxHp;

        // 同步 Inspector 调试显示
        attr.SyncDebugDisplay();
    }

    // ==================== 内部工具 ====================

    /// <summary>
    /// 判断 buff 目标阵营是否与单位阵营匹配
    /// </summary>
    private bool MatchesTarget(BuffTargetCamp target, CampType camp)
    {
        switch (target)
        {
            case BuffTargetCamp.PlayerOnly:
                return camp == CampType.Player;
            case BuffTargetCamp.EnemyOnly:
                return camp == CampType.Enemy;
            case BuffTargetCamp.All:
                return true;
            default:
                return false;
        }
    }
}
