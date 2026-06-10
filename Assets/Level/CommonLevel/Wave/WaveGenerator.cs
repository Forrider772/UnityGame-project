using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 波次生成器（实例组件，场景中可挂多个同时运行）
///
/// 流程：
/// 1. 读取本关波次列表配置
/// 2. 根据每波的触发方式决定何时开始生成
/// 3. 根据阵营和路径ID从LevelPathManager拿到真实路径
/// 4. 生成单位 → 给UnitMovement赋值路径
/// </summary>
public class WaveGenerator : MonoBehaviour
{
    [Header("默认生成点（波次未单独指定时使用）")]
    [SerializeField] private Transform defaultSpawnPoint;

    private WaveList waveList;                          // 波次列表配置
    private List<GameObject>[] waveSpawnedUnits;        // 每波追踪生成的单位，用于 AllUnitsDead 判定
    private bool[] waveSpawningDone;                    // 每波生成是否完毕
    private bool isWaitingForContinue;                  // Manual 模式挂起标记
    private Coroutine waveLoopCoroutine;

    /// <summary>是否正在运行波次</summary>
    public bool IsRunning { get; private set; }

    /// <summary>当前处理到的波次索引（-1 表示未开始）</summary>
    public int CurrentWaveIndex { get; private set; } = -1;

    /// <summary>全部波次是否已完成</summary>
    public bool IsAllWavesComplete => !IsRunning && CurrentWaveIndex >= 0;

    /// <summary>
    /// 外部调用：开始所有波次
    /// </summary>
    /// <param name="defaultSpawn">默认生成点（波次未单独指定时使用）</param>
    /// <param name="list">波次列表配置</param>
    public void StartWave(Transform defaultSpawn, WaveList list)
    {
        defaultSpawnPoint = defaultSpawn;
        waveList = list;
        IsRunning = true;
        CurrentWaveIndex = -1;
        waveLoopCoroutine = StartCoroutine(WaveLoop());
    }

    /// <summary>
    /// 停止波次生成（会中断所有未开始的波次，已生成的单位不受影响）
    /// </summary>
    public void StopWave()
    {
        if (waveLoopCoroutine != null)
            StopCoroutine(waveLoopCoroutine);
        IsRunning = false;
    }

    /// <summary>
    /// 继续下一个 Manual 波次
    /// </summary>
    public void Continue()
    {
        isWaitingForContinue = false;
    }

    /// <summary>
    /// 遍历波次列表，根据触发方式决定开始时机
    /// </summary>
    IEnumerator WaveLoop()
    {
        int waveCount = waveList.waves.Length;
        waveSpawnedUnits = new List<GameObject>[waveCount];
        waveSpawningDone = new bool[waveCount];
        for (int i = 0; i < waveCount; i++)
            waveSpawnedUnits[i] = new List<GameObject>();

        int index = 0;
        while (index < waveCount)
        {
            WaveData wave = waveList.waves[index];
            CurrentWaveIndex = index;

            // 并发波次由前一个非并发波次连带启动；此处只处理列表开头就是并发的情况
            if (wave.triggerType == WaveTriggerType.Concurrent)
            {
                StartCoroutine(SpawnOneWave(wave, index));
                index++;
                continue;
            }

            // 等待触发条件
            switch (wave.triggerType)
            {
                case WaveTriggerType.Manual:
                    isWaitingForContinue = true;
                    yield return new WaitWhile(() => isWaitingForContinue);
                    break;
                case WaveTriggerType.AfterPrevious:
                    yield return new WaitWhile(() => AnyPreviousWaveStillSpawning(index));
                    yield return new WaitForSeconds(wave.delayBeforeStart);
                    break;
                case WaveTriggerType.AllUnitsDead:
                    yield return new WaitWhile(() => AnyTrackedUnitStillAlive());
                    break;
            }

            // 启动当前波次
            int anchor = index;
            StartCoroutine(SpawnOneWave(wave, index));
            index++;

            // 连带启动紧随的连续并发波次
            while (index < waveCount && waveList.waves[index].triggerType == WaveTriggerType.Concurrent)
            {
                CurrentWaveIndex = index;
                StartCoroutine(SpawnOneWave(waveList.waves[index], index));
                index++;
            }

            // 等当前非并发波次出完，再处理下一组
            yield return new WaitWhile(() => !waveSpawningDone[anchor]);
        }

        yield return new WaitWhile(() => AnyPreviousWaveStillSpawning(waveCount));
        IsRunning = false;
    }

    /// <summary>
    /// 生成单个波次的所有单位
    /// </summary>
    /// <param name="wave">波次配置</param>
    /// <param name="waveIndex">波次索引，用于追踪生成单位</param>
    IEnumerator SpawnOneWave(WaveData wave, int waveIndex)
    {
        // 确定生成点：波次自己的 > 默认的
        Transform spawnAt = wave.spawnPoint != null ? wave.spawnPoint : defaultSpawnPoint;

        // 根据配置的路径ID + 阵营 拿到真实路径对象
        PathManager targetPath = LevelPathManager.Instance.GetPath(waveList.camp, wave.pathID);
        if (targetPath == null)
        {
            waveSpawningDone[waveIndex] = true;
            yield break;
        }

        var units = waveSpawnedUnits[waveIndex];

        for (int i = 0; i < wave.spawnCount; i++)
        {
            // 在生成点位置生成单位
            GameObject unit = Instantiate(wave.unitPrefab, spawnAt.position, Quaternion.identity);

            // 给单位移动脚本赋值行走路径
            UnitMovement move = unit.GetComponent<UnitMovement>();
            if (move != null)
                move.SetPath(targetPath);

            // 追踪本波生成的单位（用于 AllUnitsDead 判定）
            units.Add(unit);

            // 生成间隔
            yield return new WaitForSeconds(wave.spawnInterval);
        }

        waveSpawningDone[waveIndex] = true;
    }

    /// <summary>
    /// 检查索引 i 之前是否有波次仍在生成中
    /// </summary>
    bool AnyPreviousWaveStillSpawning(int upToIndex)
    {
        for (int i = 0; i < upToIndex; i++)
        {
            if (!waveSpawningDone[i])
                return true;
        }
        return false;
    }

    /// <summary>
    /// 检查场上是否还有存活的已生成单位
    /// （遍历所有已生成完的波次，清理 null 的同时判断是否全死光）
    /// </summary>
    bool AnyTrackedUnitStillAlive()
    {
        for (int i = 0; i < waveSpawnedUnits.Length; i++)
        {
            var units = waveSpawnedUnits[i];
            // 从后往前遍历，安全清理已销毁的单位
            for (int j = units.Count - 1; j >= 0; j--)
            {
                if (units[j] == null)
                    units.RemoveAt(j);
            }
            // 还有存活单位 → 条件未满足
            if (units.Count > 0)
                return true;
        }
        return false;
    }
}
