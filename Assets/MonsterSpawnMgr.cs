using UnityEngine;
using System.Collections;

public class MonsterSpawnMgr : MonoBehaviour
{
    public static MonsterSpawnMgr Instance;

    [Header("出怪点")]
    public Transform spawnPoint;

    [Header("当前关卡配置")]
    public LevelData currentLevel;

    private int curWaveIndex;    // 当前第几波
    private bool isWaveSpawning;

    void Awake()
    {
        Instance = this;
    }

    // 开始整局怪物进攻
    public void StartLevelWave()
    {
        curWaveIndex = 0;
        StartCoroutine(StartNextWave());
    }

    // 开启下一波
    IEnumerator StartNextWave()
    {
        // 所有波次打完 → 关卡胜利（怪物清空完毕再通关）
        if (curWaveIndex >= currentLevel.waves.Length)
        {
            yield break;
        }

        WaveData wave = currentLevel.waves[curWaveIndex];
        isWaveSpawning = true;

        // 循环生成本波所有怪物
        for (int i = 0; i < wave.spawnCount; i++)
        {
            SpawnMonster(wave.monsterPrefab);
            yield return new WaitForSeconds(wave.spawnInterval);
        }

        isWaveSpawning = false;
        curWaveIndex++;

        // 间隔几秒 自动下一波
        yield return new WaitForSeconds(2f);
        StartCoroutine(StartNextWave());
    }

    // 生成单个怪物
    void SpawnMonster(GameObject monster)
    {
        Instantiate(monster, spawnPoint.position, spawnPoint.rotation);
    }

    // 判断：所有波次是否全部结束 + 场上无怪
    public bool IsAllWaveFinishAndNoMonster()
    {
        if (curWaveIndex < currentLevel.waves.Length)
            return false;

        if (isWaveSpawning) return false;

        // 场景没有怪物
        GameObject[] mons = GameObject.FindGameObjectsWithTag("EnemyUnit");
        return mons.Length <= 0;
    }
}