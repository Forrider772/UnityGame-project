/// <summary>
/// 移动类型枚举（统一）
/// 用于标记单位移动类型和路径允许的移动类型
/// 默认值为 Ground（0），已有资产自动继承此值，无需迁移
/// </summary>
public enum MoveType
{
    Ground,  // 地面（默认值）
    Flying   // 飞行
    // 未来扩展：Water   水中
}

/// <summary>
/// 移动类型兼容性判断辅助类
/// 规则（以单位能力为驱动，而非路径）：
/// - 地面单位：仅可走 Ground 路径
/// - 飞行单位（更强）：可走 Ground + Flying 路径
/// - 水中单位（未来，更强）：可走 Ground + Flying + Water 路径
/// </summary>
public static class MoveTypeHelper
{
    /// <summary>
    /// 判断路径移动类型是否兼容给定单位移动类型
    /// 飞行单位能力强，可兼容地面路径；地面单位只能走地面路径
    /// </summary>
    /// <param name="pathType">路径的移动类型</param>
    /// <param name="unitType">单位的移动类型</param>
    /// <returns>true 表示兼容，false 表示不兼容</returns>
    public static bool IsPathCompatible(MoveType pathType, MoveType unitType)
    {
        switch (unitType)
        {
            case MoveType.Flying:
                // 飞行单位能力强，可走地面和飞行路径
                return pathType == MoveType.Ground || pathType == MoveType.Flying;

            case MoveType.Ground:
            default:
                // 地面单位只能走地面路径
                return pathType == MoveType.Ground;

            // 未来扩展：
            // case MoveType.Water:
            //     // 水中单位更强，可走所有类型路径
            //     return true;
        }
    }
}
