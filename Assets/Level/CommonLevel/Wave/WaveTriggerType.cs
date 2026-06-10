/// <summary>
/// 波次触发方式
/// 决定波次何时开始生成单位
/// </summary>
public enum WaveTriggerType
{
    /// <summary>
    /// 等上一波完成后 + nextWaveInterval 秒再开始（默认，兼容旧配置）
    /// </summary>
    AfterPrevious,

    /// <summary>
    /// 遍历到本波时立即开始，不等待上一波，可与上波并发出怪
    /// </summary>
    Immediate,

    /// <summary>
    /// 挂起等待，外部调用 WaveGenerator.Continue() 后立即开始
    /// </summary>
    Manual,

    /// <summary>
    /// 等场上所有已生成单位全部死亡后才开始
    /// </summary>
    AllUnitsDead
}
