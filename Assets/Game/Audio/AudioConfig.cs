using UnityEngine;

/// <summary>
/// 音效槽位枚举
/// 与 AudioConfig 字段一一对应，供 AudioManager.PlaySFX 按槽位取音频
/// </summary>
public enum AudioSlot
{
    UIClick,          // UI 点击
    CardSelect,       // 卡牌选中
    CardDeploy,       // 出牌确认（进入冷却）
    Summon,           // 召唤单位
    AttackMelee,      // 近战攻击
    AttackRanged,     // 远程开火
    BulletHit,        // 子弹命中
    Heal,             // 治疗生效
    Hurt,             // 单位受击
    Die,              // 单位死亡
    TowerAttack,      // 塔攻击
    TowerHurt,        // 塔受击
    TowerDestroy,     // 塔摧毁
    WaveStart,        // 波次开始
    EnemySpawn,       // 出怪
    Win,              // 战斗胜利
    Lose,             // 战斗失败
    BossDefeat,       // Boss 击破
}

/// <summary>
/// 音效配置资源表（ScriptableObject）
/// 在 Unity 里通过 Create → Audio/AudioConfig 创建资产，
/// 把音频文件拖入对应槽位，再经 Inspector 拖到 AudioManager.config 字段。
/// </summary>
[CreateAssetMenu(fileName = "AudioConfig", menuName = "Audio/AudioConfig")]
public class AudioConfig : ScriptableObject
{
    [Header("BGM 背景音乐")]
    public AudioClip bgmMenu;        // 主菜单
    public AudioClip bgmBattle;      // 战斗关卡

    [Header("SFX 音效（与 AudioSlot 一一对应）")]
    public AudioClip sfxUIClick;     // UI 点击
    public AudioClip sfxCardSelect;  // 卡牌选中
    public AudioClip sfxCardDeploy;  // 出牌确认
    public AudioClip sfxSummon;      // 召唤单位
    public AudioClip sfxAttackMelee; // 近战攻击
    public AudioClip sfxAttackRanged;// 远程开火
    public AudioClip sfxBulletHit;   // 子弹命中
    public AudioClip sfxHeal;        // 治疗生效
    public AudioClip sfxHurt;        // 单位受击
    public AudioClip sfxDie;         // 单位死亡
    public AudioClip sfxTowerAttack; // 塔攻击
    public AudioClip sfxTowerHurt;   // 塔受击
    public AudioClip sfxTowerDestroy;// 塔摧毁
    public AudioClip sfxWaveStart;   // 波次开始
    public AudioClip sfxEnemySpawn;  // 出怪
    public AudioClip sfxWin;         // 战斗胜利
    public AudioClip sfxLose;        // 战斗失败
    public AudioClip sfxBossDefeat;  // Boss 击破

    /// <summary>按槽位取音频</summary>
    public AudioClip GetClip(AudioSlot slot)
    {
        switch (slot)
        {
            case AudioSlot.UIClick:      return sfxUIClick;
            case AudioSlot.CardSelect:   return sfxCardSelect;
            case AudioSlot.CardDeploy:   return sfxCardDeploy;
            case AudioSlot.Summon:       return sfxSummon;
            case AudioSlot.AttackMelee:  return sfxAttackMelee;
            case AudioSlot.AttackRanged: return sfxAttackRanged;
            case AudioSlot.BulletHit:    return sfxBulletHit;
            case AudioSlot.Heal:         return sfxHeal;
            case AudioSlot.Hurt:         return sfxHurt;
            case AudioSlot.Die:          return sfxDie;
            case AudioSlot.TowerAttack:  return sfxTowerAttack;
            case AudioSlot.TowerHurt:    return sfxTowerHurt;
            case AudioSlot.TowerDestroy: return sfxTowerDestroy;
            case AudioSlot.WaveStart:    return sfxWaveStart;
            case AudioSlot.EnemySpawn:   return sfxEnemySpawn;
            case AudioSlot.Win:          return sfxWin;
            case AudioSlot.Lose:         return sfxLose;
            case AudioSlot.BossDefeat:   return sfxBossDefeat;
            default:                     return null;
        }
    }
}
