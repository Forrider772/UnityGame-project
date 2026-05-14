using UnityEngine;

[System.Serializable]
public class WaveData
{
    [Header("生成的敌人")]
    public GameObject enemyPrefab;

    [Header("生成数量")]
    public int spawnCount;

    [Header("生成间隔（秒）")]
    public float spawnInterval;

    [Header("行走路径ID")]
    public PathID pathID;
}