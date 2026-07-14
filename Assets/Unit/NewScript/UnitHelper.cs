using UnityEngine;

/// <summary>
/// 单位生成工具
/// 在 Instantiate 后调用 Configure() 注入正确的 camp 和 layer
/// 使得同一预制体可在双方阵营复用，无需为 Enemy 复制预制体
/// </summary>
public static class UnitHelper
{
    /// <summary>
    /// 根据阵营配置单位的 layer 和 UnitAttr.camp
    /// 应在 Instantiate 后、SetPath 前立即调用
    /// </summary>
    public static void Configure(GameObject unit, CampType camp)
    {
        // 1. camp → layer映射
        string layerName = camp == CampType.Player ? "PlayerUnit" : "EnemyUnit";
        int layer = LayerMask.NameToLayer(layerName);
        SetLayerRecursive(unit, layer);

        // 2. 设置 UnitAttr.camp（覆盖预制体默认值）
        var attr = unit.GetComponent<UnitAttr>();
        if (attr != null)
            attr.camp = camp;
    }

    static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
