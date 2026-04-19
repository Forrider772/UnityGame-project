using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnOnClick : MonoBehaviour
{
    public GameObject prefabToSpawn;

    public Transform spawnPoint;

    // 随机偏移的范围，x和y分别对应水平和垂直的随机范围
    public Vector2 randomRange = new Vector2(2f, 2f);

    public void SpawnPrefab()
    {
        // 生成随机偏移量
        float randomX = Random.Range(-randomRange.x, randomRange.x);
        float randomY = Random.Range(-randomRange.y, randomRange.y);
        // 计算本次生成的位置：初始位置 + 随机偏移
        Vector3 spawnPos = spawnPoint.position + new Vector3(randomX, randomY, 0);
        // 生成物体
        if (prefabToSpawn != null)
        {
            Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        }
    }
}
