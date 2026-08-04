using UnityEngine;

/// <summary>
/// Boss 单位标记组件 — 挂到任意单位 prefab 上即可将其标记为 Boss。
/// 当该单位死亡（UnitBrain 触发 OnDeath）且关卡启用了 Boss 胜利模式时，
/// 通知 BattleManager 触发 GameWin。
///
/// 使用方式：
///   1. 在单位预制体上挂载此组件
///   2. 在 LevelSetup 中勾选 useBossVictory = true
///   3. Boss 死亡即胜利，敌方塔被摧毁不再触发胜利
/// </summary>
public class BossUnit : MonoBehaviour
{
    void Awake()
    {
        // 订阅单位死亡事件（仅在挂载于有 UnitBrain 的单位上时生效）
        var brain = GetComponent<UnitBrain>();
        if (brain != null)
            brain.OnDeath += OnDeath;
    }

    void OnDestroy()
    {
        // 解绑，避免悬挂引用
        var brain = GetComponent<UnitBrain>();
        if (brain != null)
            brain.OnDeath -= OnDeath;
    }

    /// <summary>单位死亡回调：通知 BattleManager 触发 Boss 胜利</summary>
    void OnDeath(GameObject dyingUnit)
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnBossDefeated();
    }
}
