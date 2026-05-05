using UnityEngine;
using System.Collections;

// 怪物生成管理器：负责按关卡波次配置，自动刷新敌方怪物
public class EnemySpawnMgr : MonoBehaviour
{
    // 单例实例，全局唯一，方便外部调用
    public static EnemySpawnMgr Instance;

    [Header("出怪点")]
    public Transform spawnPoint;          // 怪物出生点

    [Header("当前关卡配置")]
    public LevelData currentLevel;        // 当前使用的关卡数据（波次、怪物配置）

    private int curWaveIndex;             // 当前正在刷的波次索引
    private bool isWaveSpawning;          // 是否正在刷怪中（防止重复刷怪）

    // 脚本初始化时设置单例
    void Awake()
    {
        Instance = this;
    }

    // 开始整局怪物进攻：由战斗管理器调用，启动第一波
    public void StartLevelWave()
    {
        curWaveIndex = 0;                 // 重置波次为第一波
        StartCoroutine(StartNextWave());  // 开启协程执行刷波次
    }

    // 开启下一波怪物（协程）
    IEnumerator StartNextWave()
    {
        // 如果当前波次 >= 总波次，说明全部刷完，直接退出
        if (curWaveIndex >= currentLevel.waves.Length)
        {
            yield break;
        }

        // 获取当前要刷的波次数据
        WaveData wave = currentLevel.waves[curWaveIndex];
        isWaveSpawning = true;            // 标记正在刷怪

        // 循环生成本波所有怪物
        for (int i = 0; i < wave.spawnCount; i++)
        {
            SpawnMonster(wave.monsterPrefab);
            // 等待波次配置的间隔时间，再刷下一只
            yield return new WaitForSeconds(wave.spawnInterval);
        }

        isWaveSpawning = false;           // 当前波刷怪结束
        curWaveIndex++;                   // 波次+1，准备下一波

        // 波次间隔2秒，自动开启下一波
        yield return new WaitForSeconds(2f);
        StartCoroutine(StartNextWave());
    }

    // 生成单个怪物
    void SpawnMonster(GameObject monster)
    {
        // 在出怪点生成怪物
        Instantiate(monster, spawnPoint.position, spawnPoint.rotation);
    }

    // 判断：所有波次是否全部结束 + 场上没有任何怪物
    public bool IsAllWaveFinishAndNoMonster()
    {
        // 波次没刷完 → 未完成
        if (curWaveIndex < currentLevel.waves.Length)
            return false;

        // 正在刷怪 → 未完成
        if (isWaveSpawning) return false;

        // 查找场景中所有敌方单位，判断数量是否为0
        GameObject[] mons = GameObject.FindGameObjectsWithTag("EnemyUnit");
        return mons.Length <= 0;
    }
}