using UnityEngine;

[System.Serializable]
public class WaveData
{
    [Header("生成的阵营")]
    public CampType camp;
    [Header("生成的单位")]
    public GameObject unitPrefab;
    [Header("生成数量")]
    public int spawnCount;
    [Header("生成间隔（秒）")]
    public float spawnInterval;
    [Header("行走路径ID")]
    public PathID pathID;
}