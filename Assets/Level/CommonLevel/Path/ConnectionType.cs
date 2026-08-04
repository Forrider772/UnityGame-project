/// <summary>
/// 路径段落连接类型枚举
/// 每个路径段（相邻两个 pathPoints 之间）可以配置为行走或传送
/// 传送段：单位到达段起点后等待固定时间，然后瞬间传送到段终点
/// </summary>
public enum ConnectionType
{
    Walk,      // 正常沿路径行走
    Teleport   // 传送：等待固定时间后瞬移
}
