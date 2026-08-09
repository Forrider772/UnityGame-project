/// <summary>
/// 游戏全局事件总线（静态）
/// 系统级游戏事件，供音效/特效/成就等订阅方集中监听。
/// 游戏逻辑只在关键位置发出事件，不直接耦合订阅方实现。
///
/// 注意：这里用 public 静态委托字段而非 C# event 关键字——
/// event 只能在声明类内部 Invoke，而本类是被各处游戏逻辑触发的静态总线，
/// 用普通委托字段允许外部直接 OnX?.Invoke() 触发事件。
/// </summary>
public static class GameEvents
{
    // ===== 卡牌 =====
    public static System.Action OnCardSelected;      // 卡牌选中
    public static System.Action OnCardDeployed;      // 出牌确认（进入冷却）
    public static System.Action OnUnitSummoned;      // 召唤单位

    // ===== 波次 =====
    public static System.Action OnWaveStart;         // 每波开始
    public static System.Action OnEnemySpawned;      // 出怪

    // ===== 塔 =====
    public static System.Action OnTowerAttack;       // 塔攻击
    public static System.Action OnTowerDamaged;      // 塔受击
    public static System.Action OnTowerDestroyed;    // 塔摧毁

    // ===== 战斗 =====
    public static System.Action OnBulletHit;         // 子弹命中

    // ===== 胜负 =====
    public static System.Action OnGameWin;           // 战斗胜利
    public static System.Action OnGameLose;          // 战斗失败
    public static System.Action OnBossDefeated;      // Boss 击破
}
