/// <summary>
/// 路径移动类型枚举
/// 标记路径可被哪种移动类型的单位使用，与 UnitAttr.unitType 形成映射关系
/// 默认值为 Ground（0），已有路径自动继承此值，无需迁移
/// </summary>
public enum PathMoveType
{
    Ground,  // 地面路径（默认值，已有路径自动继承）
    Flight   // 飞行路径（仅飞行单位可部署）
    // 未来扩展：Water   水路路径（仅水中单位可部署）
}

/// <summary>
/// 路径移动类型与单位类型的兼容性判断辅助类
/// 集中管理 UnitType ↔ PathMoveType 的映射关系，未来扩展只需修改此处
/// </summary>
public static class PathMoveTypeHelper
{
    /// <summary>
    /// 根据单位类型获取其所需的路径移动类型
    /// </summary>
    /// <param name="unitType">单位移动类型</param>
    /// <returns>对应的路径移动类型</returns>
    public static PathMoveType GetRequiredPathType(UnitType unitType)
    {
        switch (unitType)
        {
            case UnitType.Flying:
                return PathMoveType.Flight;
            case UnitType.Ground:
            default:
                return PathMoveType.Ground;
            // 未来扩展：
            // case UnitType.Water:
            //     return PathMoveType.Water;
        }
    }

    /// <summary>
    /// 判断路径移动类型是否兼容给定单位类型
    /// </summary>
    /// <param name="pathType">路径的移动类型</param>
    /// <param name="unitType">单位的移动类型</param>
    /// <returns>true 表示兼容，false 表示不兼容</returns>
    public static bool IsPathCompatible(PathMoveType pathType, UnitType unitType)
    {
        return pathType == GetRequiredPathType(unitType);
    }
}
