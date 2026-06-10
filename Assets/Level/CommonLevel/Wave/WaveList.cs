using UnityEngine;

[CreateAssetMenu(fileName = "WaveList_", menuName = "Level/Wave List")]
public class WaveList : ScriptableObject
{
    [Header("波次列表名称")]
    public string waveListName;

    [Header("阵营")]
    [Tooltip("本波次列表所有波次的出怪阵营")]
    public CampType camp = CampType.Enemy;

    [Header("所有波次")]
    public WaveData[] waves;
}