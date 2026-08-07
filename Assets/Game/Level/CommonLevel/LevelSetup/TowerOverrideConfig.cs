using System;
using UnityEngine;

/// <summary>
/// 塔属性选择性覆盖配置
/// 每个属性配独立的 override 开关，仅开关开启时才覆盖，其余保留 TowerBase 原值
/// </summary>
[Serializable]
public class TowerOverrideConfig
{
    [Header("生命值")]
    public bool overrideHp = false;
    public float hp = 800f;

    [Header("攻击力")]
    public bool overrideAtk = false;
    public float atk = 12f;

    [Header("攻击范围")]
    public bool overrideAtkRange = false;
    public float atkRange = 3.5f;

    [Header("攻击冷却")]
    public bool overrideAtkCD = false;
    public float atkCD = 1.2f;

    [Header("物理防御")]
    public bool overridePhysicalDefense = false;
    public float physicalDefense = 0f;

    [Header("法术防御")]
    public bool overrideMagicDefense = false;
    public float magicDefense = 0f;

    /// <summary>将本配置覆盖到目标 TowerBase，仅覆盖开关开启的字段</summary>
    public void ApplyTo(TowerBase tower)
    {
        if (tower == null) return;
        if (overrideHp)              tower.hp              = hp;
        if (overrideAtk)             tower.atk             = atk;
        if (overrideAtkRange)        tower.atkRange        = atkRange;
        if (overrideAtkCD)           tower.atkCD           = atkCD;
        if (overridePhysicalDefense) tower.physicalDefense = physicalDefense;
        if (overrideMagicDefense)    tower.magicDefense    = magicDefense;
    }
}
