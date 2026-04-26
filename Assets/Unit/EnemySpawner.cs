using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("生成设置")]
    public GameObject enemyPrefab;       // 敌人预制体
    public Transform moveTarget;        // 敌人移动目标
    public Transform[] spawnPoints;      // 生成点数组（可以多个）
    public float spawnInterval = 2f;    // 生成间隔（秒）
    public int maxEnemyCount = 10;       // 场上最多同时存在的敌人数量

    private int currentEnemyCount = 0;   // 当前场上敌人数量

    void Start()
    {
        // 开始自动生成
        StartCoroutine(SpawnLoop());
    }

    // 生成循环
    IEnumerator SpawnLoop()
    {
        while (true)
        {
            // 如果场上敌人没到上限，就生成
            if (currentEnemyCount < maxEnemyCount)
            {
                SpawnEnemy();
            }

            // 等待指定时间再生成下一个
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // 生成单个敌人
    void SpawnEnemy()
    {
        // 随机选一个生成点
        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randomIndex];

        // 实例化敌人
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

        // 设置目标位置
        newEnemy.GetComponent<Move>().moveTarget = moveTarget;
        
    }

}