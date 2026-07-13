/// <summary>
/// 单位行为状态枚举
/// 所有单位通过此状态机统一管理行为切换
/// </summary>
public enum UnitState
{
    Moving,         // 沿路径移动中
    Fighting,       // 与敌方单位交战中
    Garrisoned,     // 驻扎在资源点/驻扎点
    AttackingTower, // 路径走完后攻击敌方塔
    Dead            // 已死亡（终端状态）
}
