using UnityEngine;

[CreateAssetMenu(fileName = "WaveList_", menuName = "Level/Wave List")]
public class WaveList : ScriptableObject
{
    [Header("波次列表名称")]
    public string waveListName;

    [Header("所有波次")]
    public WaveData[] waves;
}