using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss 关牵制范围组件 — 挂在己方塔上。
///
/// 机制：己方单位从卡牌部署（路径起点）出发沿路径前进，但离开己方塔
/// 一定圆形范围（牵制半径）后立即死亡，并返还部分部署费用。
/// 创造一个"有限后勤"的战术约束——玩家需要规划单位在范围内的活动。
///
/// 实现：
///   - 单例模式（场景中不存在此组件时，CardDeploy 的注册为 no-op）
///   - CardDeploy 在生成单位后调用 RegisterUnit 注册
///   - 周期性（checkInterval）检测已注册单位与塔的距离
///   - 超出半径 → 返还费用 → 调用 UnitBrain.Die() 走正常死亡流程
///   - 用 LeashCircleDisplay（世界坐标 LineRenderer）绘制空心圆范围圈
/// </summary>
public class TowerLeashZone : MonoBehaviour
{
    // ==================== 单例 ====================
    public static TowerLeashZone Instance { get; private set; }

    // ==================== 配置 ====================
    [Header("牵制范围")]
    [Tooltip("牵制半径（世界单位），玩家单位距离己方塔超过此半径时死亡并返还费用")]
    public float leashRange = 10f;

    [Header("费用返还")]
    [Range(0f, 1f)]
    [Tooltip("牵制死亡时返还的部署费用比例（0.5 = 返还一半）")]
    public float costRefundRatio = 0.5f;

    [Header("性能")]
    [Tooltip("检测间隔（秒），降低每帧计算开销")]
    public float checkInterval = 0.25f;

    /// <summary>范围圈颜色（玩家阵营绿色）</summary>
    public static readonly Color CircleColor = new Color(0.2f, 0.9f, 0.2f, 0.8f);

    // ==================== 运行时状态 ====================
    private readonly HashSet<UnitBrain> trackedUnits = new HashSet<UnitBrain>();
    private LeashCircleDisplay leashCircle;
    private float checkTimer;

    // ==================== 生命周期 ====================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // 自动添加空心圆范围圈显示组件
        leashCircle = GetComponent<LeashCircleDisplay>();
        if (leashCircle == null)
            leashCircle = gameObject.AddComponent<LeashCircleDisplay>();
    }

    void Start()
    {
        // 初始显示范围圈（半径可能被 LevelSetup 在 Awake 阶段覆盖）
        leashCircle?.UpdateDisplay(leashRange, CircleColor);
    }

    void Update()
    {
        // 每帧同步范围圈（半径/中心变化时自动重算，且不受父级缩放影响）
        leashCircle?.UpdateDisplay(leashRange, CircleColor);

        // 周期性检测
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            CheckLeash();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ==================== 单位注册 / 注销 ====================

    /// <summary>
    /// 注册玩家单位（CardDeploy 生成单位后调用）。
    /// 注册时挂接 OnDeath 事件，单位死亡后自动取消注册。
    /// </summary>
    public void RegisterUnit(UnitBrain brain)
    {
        if (brain == null) return;
        if (trackedUnits.Add(brain))
        {
            brain.OnDeath -= OnUnitDeath;
            brain.OnDeath += OnUnitDeath;
        }
    }

    /// <summary>单位死亡回调：从追踪列表移除并解绑事件</summary>
    void OnUnitDeath(GameObject dyingUnit)
    {
        if (dyingUnit == null) return;
        UnitBrain brain = dyingUnit.GetComponent<UnitBrain>();
        if (brain != null)
        {
            trackedUnits.Remove(brain);
            brain.OnDeath -= OnUnitDeath;
        }
    }

    // ==================== 牵制检测 ====================

    /// <summary>
    /// 遍历所有已注册单位，找出超出牵制半径的，统一执行牵制死亡。
    /// 延迟收集（toKill 列表）避免在 foreach 中修改 HashSet 导致异常。
    /// </summary>
    void CheckLeash()
    {
        // 清理已销毁/已死亡的悬挂引用
        trackedUnits.RemoveWhere(u => u == null || u.state == UnitState.Dead);

        if (trackedUnits.Count == 0) return;

        Vector2 towerPos = transform.position;
        List<UnitBrain> toKill = null;

        foreach (var brain in trackedUnits)
        {
            if (brain == null || brain.state == UnitState.Dead) continue;

            // 传送中单位完全无敌，跳过检测（与 UnitBrain 索敌逻辑一致）
            if (brain.IsTeleporting()) continue;

            float dist = Vector2.Distance(towerPos, brain.transform.position);
            if (dist > leashRange)
            {
                if (toKill == null)
                    toKill = new List<UnitBrain>();
                toKill.Add(brain);
            }
        }

        if (toKill == null) return;

        foreach (var brain in toKill)
        {
            LeashKill(brain);
        }
    }

    /// <summary>
    /// 牵制死亡：返还费用 → 触发正常死亡流程。
    /// </summary>
    void LeashKill(UnitBrain brain)
    {
        if (brain == null || brain.state == UnitState.Dead) return;

        UnitAttr attr = brain.GetComponent<UnitAttr>();

        // 1. 计算并返还费用
        if (attr != null && attr.deployCost > 0 && BattleManager.Instance != null)
        {
            float refund = attr.deployCost * costRefundRatio;
            if (refund > 0f)
                BattleManager.Instance.AddCost(refund);
        }

        // 2. 触发死亡（内部触发 OnDeath → 淡出 → 销毁）
        //    先移除追踪（OnDeath 也会移除，此处幂等安全）
        if (trackedUnits.Remove(brain))
        {
            brain.OnDeath -= OnUnitDeath;
        }
        brain.Die();
    }

    // ==================== 编辑器可视化 ====================

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, leashRange);
    }
}
