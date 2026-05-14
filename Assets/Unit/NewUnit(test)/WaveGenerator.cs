using System.Collections;
using UnityEngine;

/// <summary>
/// 波次生成器
/// 流程：
/// 1. 读取本关波次配置
/// 2. 根据路径ID从LevelPathManager拿到真实路径
/// 3. 生成怪物 → 给UnitMovement赋值路径
/// </summary>
public class WaveGenerator : MonoBehaviour
{
    // 单例实例，全局唯一，方便外部调用
    public static WaveGenerator Instance;

    [Header("引用")]
    private Transform spawnPoint;  // 生成点
    private LevelData levelData;   // 整关所有波次配置容器
    private LevelPathManager levelPathManager;   // 本关路径管理器

    // 脚本初始化时设置单例
    void Awake()
    {
        Instance = this;
        levelPathManager = GetComponent<LevelPathManager>();
    }

    /// <summary>
    /// 外部调用：开始所有波次
    /// </summary>
    public void StartWave(Transform spawnPoint, LevelData levelData)
    {
        this.spawnPoint = spawnPoint;
        this.levelData = levelData;
        StartCoroutine(WaveLoop());
    }

    /// <summary>
    /// 遍历所有波次，逐个执行
    /// </summary>
    IEnumerator WaveLoop()
    {
        // 遍历配置里每一个波次
        foreach (var wave in levelData.waves)
        {
            // 等待当前整波生成完毕，再进行下一波
            yield return StartCoroutine(SpawnOneWave(wave));
            // 波次之间固定间隔2秒
            yield return new WaitForSeconds(2f);
        }
    }

    /// <summary>
    /// 生成单个波次的所有敌人
    /// </summary>
    IEnumerator SpawnOneWave(WaveData wave)
    {
        // 1. 根据波次配置的路径ID，从路径管理器拿到真实路径对象
        PathManager targetPath = levelPathManager.GetPath(wave.pathID);
        if (targetPath == null) yield break;  // 路径找不到，直接终止本波

        // 2. 循环生成当前波次指定数量的敌人
        for (int i = 0; i < wave.spawnCount; i++)
        {
            // 在生成点位置生成敌人
            GameObject enemy = Instantiate(wave.enemyPrefab, spawnPoint.position, Quaternion.identity);

            // 3. 给敌人移动脚本赋值行走路径
            UnitMovement move = enemy.GetComponent<UnitMovement>();
            if (move != null)
            {
                move.SetPath(targetPath);
            }

            // 每只敌人生成间隔
            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }
}