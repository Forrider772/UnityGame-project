using UnityEngine;

[System.Serializable]
public class WaveData
{
    [Header("生成内容 — 出什么、从哪出、走哪条路")]
    [Tooltip("要生成的单位预制体")]
    public GameObject unitPrefab;

    [Tooltip("本波单位的出生点（为空则使用 WaveGenerator 上的默认生成点）")]
    public Transform spawnPoint;

    [Tooltip("单位行走的路线ID，对应场景中 PathManager 的 pathId")]
    public PathID pathID;

    [Header("生成节奏 — 出多少、多快")]
    [Tooltip("本波总共生成多少个单位")]
    public int spawnCount;

    [Tooltip("每两个单位之间的生成间隔（秒）")]
    public float spawnInterval;

    [Header("触发方式 — 何时开始出怪")]
    [Tooltip("本波的触发方式：\n· AfterPrevious：等上一波生成完毕 + 本波延迟后开始\n· Concurrent：并发模式，遍历到本波时立即开始（可与上波并发出怪）\n· Manual：等待外部调用 Continue() 后开始\n· AllUnitsDead：等场上所有已生成单位全部死亡后开始")]
    public WaveTriggerType triggerType = WaveTriggerType.AfterPrevious;

    [Header("延迟设置")]
    [Tooltip("本波次在满足触发条件后，额外延迟多少秒再开始生成（适用于所有触发类型）")]
    public float delayBeforeStart = 2f;

}
