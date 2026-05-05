using UnityEngine;

[CreateAssetMenu(fileName = "Level_", menuName = "TD/关卡配置")]
public class LevelData : ScriptableObject
{
    [Header("关卡名称")]
    public string levelName;

    [Header("所有波次")]
    public WaveData[] waves;
}