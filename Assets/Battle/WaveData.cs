using UnityEngine;

[System.Serializable]
public class WaveData
{
    [Header("本波怪物")]
    public GameObject monsterPrefab;
    public int spawnCount;      // 本波数量
    public float spawnInterval; // 单个怪物间隔
}