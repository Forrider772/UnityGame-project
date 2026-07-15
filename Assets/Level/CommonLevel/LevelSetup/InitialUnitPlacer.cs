using UnityEngine;

/// <summary>
/// 初始布阵 — 单个单位的配置条目
/// 每个条目独立指定预制体、阵营、路线、生成位置
/// </summary>
[System.Serializable]
public class InitialUnitEntry
{
    [Header("单位")]
    [Tooltip("要生成的单位预制体")]
    public GameObject unitPrefab;

    [Header("阵营与路线")]
    [Tooltip("单位所属阵营（Player 或 Enemy）")]
    public CampType camp;

    [Tooltip("单位行走的路线ID，对应场景中 PathManager 的 pathId")]
    public PathID pathID;

    [Header("生成位置")]
    [Tooltip("（可选）引用场景中的 Transform，优先使用其位置")]
    public Transform spawnPoint;

    [Tooltip("世界坐标位置（spawnPoint 为空时使用）")]
    public Vector2 spawnPosition;
}

/// <summary>
/// 初始布阵 — 游戏开始时在场上预置单位
///
/// 使用方式：
///   1. 在场景中任意 GameObject 上挂载此组件
///   2. 在 Inspector 中配置 initialUnits 数组
///   3. 游戏启动时自动生成所有单位
///
/// 生成流程（与 WaveGenerator 一致）：
///   Instantiate → UnitHelper.Configure(camp) → brain.SetPath(path)
///
/// 额外：生成的单位会注册到 WaveGenerator，参与 AllUnitsDead 判定
/// </summary>
[DefaultExecutionOrder(-50)]
public class InitialUnitPlacer : MonoBehaviour
{
    [Header("初始布阵单位列表")]
    [Tooltip("每个条目对应一个场上预置单位，可分别设置阵营、路线、位置")]
    public InitialUnitEntry[] initialUnits;

    void Start()
    {
        if (initialUnits == null || initialUnits.Length == 0)
            return;

        WaveGenerator waveGen = BattleManager.Instance != null
            ? BattleManager.Instance.waveGenerator
            : null;

        int spawnedCount = 0;

        foreach (var entry in initialUnits)
        {
            if (entry.unitPrefab == null)
            {
                Debug.LogWarning("InitialUnitPlacer: unitPrefab 为空，跳过该条目");
                continue;
            }

            // 确定生成位置：优先使用 spawnPoint，否则用 spawnPosition
            Vector3 spawnPos = entry.spawnPoint != null
                ? entry.spawnPoint.position
                : (Vector3)entry.spawnPosition;

            // 1. 生成单位
            GameObject unit = Instantiate(entry.unitPrefab, spawnPos, Quaternion.identity);

            // 2. 注入阵营和层级
            UnitHelper.Configure(unit, entry.camp);

            // 3. 分配行走路径（从生成位置就近走入路线，不传送回起点）
            PathManager path = LevelPathManager.Instance != null
                ? LevelPathManager.Instance.GetPath(entry.camp, entry.pathID)
                : null;

            UnitBrain brain = unit.GetComponent<UnitBrain>();
            if (brain != null && path != null)
            {
                brain.SetPathFromPosition(path, spawnPos);
            }
            else if (path == null)
            {
                Debug.LogWarning(
                    $"InitialUnitPlacer: 未找到路径 [阵营:{entry.camp}, ID:{entry.pathID}]，" +
                    $"单位 {entry.unitPrefab.name} 将没有移动路径");
            }

            // 4. 敌方单位注册到 WaveGenerator，参与 AllUnitsDead 判定
            if (waveGen != null && entry.camp == CampType.Enemy)
                waveGen.RegisterExternalUnit(unit);

            spawnedCount++;
        }

        Debug.Log($"InitialUnitPlacer: 已生成 {spawnedCount} 个初始单位");
    }
}
