using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 音效管理器（单例，Inspector 拖入 AudioConfig）
/// 双通道：BGM 循环源 + SFX 一次性源，音量独立调节并持久化（PlayerPrefs）。
/// 集中订阅 GameEvents 系统事件与 UnitBrain 单位事件，驱动全游戏音效。
///
/// 使用方式：把 AudioManager.prefab 放入 MenuScene 与 CommonLevel.prefab 各一个实例。
/// Awake 单例去重 + DontDestroyOnLoad，跨场景保活 BGM；后加载场景的实例自动销毁。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("音频配置（Inspector 拖入）")]
    public AudioConfig config;

    const string KEY_BGM = "AudioBGMVolume";
    const string KEY_SFX = "AudioSFXVolume";
    const string KEY_LEGACY = "Volume";

    AudioSource bgmSource;     // BGM 循环源
    AudioSource sfxSource;     // SFX 一次性源
    float bgmVolume = 1f;
    float sfxVolume = 1f;
    AudioClip currentBgmClip;

    // 已注册单位（避免重复订阅）
    readonly HashSet<UnitBrain> registeredUnits = new HashSet<UnitBrain>();
    float lastHurtTime = -1f;  // 受击音节流时间戳（unscaled，暂停期间也生效）

    void Awake()
    {
        // 单例去重：已有常驻实例（来自先加载的场景）则销毁自己
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 创建两个音频源
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        // 全局音量交给 per-source 控制，避免 AudioListener 二次缩放
        AudioListener.volume = 1f;

        LoadVolumes();
        SubscribeGameEvents();
    }

    /// <summary>读取双通道音量；旧版单一 Volume 键一次性迁移</summary>
    void LoadVolumes()
    {
        if (!PlayerPrefs.HasKey(KEY_BGM) && !PlayerPrefs.HasKey(KEY_SFX))
        {
            float legacy = PlayerPrefs.GetFloat(KEY_LEGACY, 1f);
            bgmVolume = legacy;
            sfxVolume = legacy;
            PlayerPrefs.SetFloat(KEY_BGM, bgmVolume);
            PlayerPrefs.SetFloat(KEY_SFX, sfxVolume);
            PlayerPrefs.DeleteKey(KEY_LEGACY);
            PlayerPrefs.Save();
        }
        else
        {
            bgmVolume = PlayerPrefs.GetFloat(KEY_BGM, 1f);
            sfxVolume = PlayerPrefs.GetFloat(KEY_SFX, 1f);
        }

        if (bgmSource != null)
            bgmSource.volume = bgmVolume;
    }

    /// <summary>订阅全部系统级游戏事件（常驻实例只需订阅一次）</summary>
    void SubscribeGameEvents()
    {
        GameEvents.OnCardSelected   += () => PlaySFX(AudioSlot.CardSelect);
        GameEvents.OnCardDeployed   += () => PlaySFX(AudioSlot.CardDeploy);
        GameEvents.OnUnitSummoned   += () => PlaySFX(AudioSlot.Summon);
        GameEvents.OnWaveStart      += () => PlaySFX(AudioSlot.WaveStart);
        GameEvents.OnEnemySpawned   += () => PlaySFX(AudioSlot.EnemySpawn);
        GameEvents.OnTowerAttack    += () => PlaySFX(AudioSlot.TowerAttack);
        GameEvents.OnTowerDamaged   += () => PlaySFX(AudioSlot.TowerHurt);
        GameEvents.OnTowerDestroyed += () => PlaySFX(AudioSlot.TowerDestroy);
        GameEvents.OnBulletHit      += () => PlaySFX(AudioSlot.BulletHit);
        GameEvents.OnGameWin        += () => PlaySFX(AudioSlot.Win);
        GameEvents.OnGameLose       += () => PlaySFX(AudioSlot.Lose);
        GameEvents.OnBossDefeated   += () => PlaySFX(AudioSlot.BossDefeat);
    }

    // ==================== 播放 API ====================

    /// <summary>播放 BGM（循环）。同一 clip 正在播放时不重启，保证跨场景连续。</summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (clip == currentBgmClip && bgmSource.isPlaying) return;

        currentBgmClip = clip;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    /// <summary>按槽位播发音效。config 或对应 clip 未配置时静默 no-op。</summary>
    public void PlaySFX(AudioSlot slot)
    {
        if (config == null) return;
        AudioClip clip = config.GetClip(slot);
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// <summary>给按钮绑定 UI 点击音效（编辑态 Instance 为 null 时安全跳过）</summary>
    public static void BindClick(Button button)
    {
        if (button == null) return;
        button.onClick.AddListener(() =>
        {
            if (Instance != null) Instance.PlaySFX(AudioSlot.UIClick);
        });
    }

    // ==================== 音量 ====================

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
            bgmSource.volume = bgmVolume;
        PlayerPrefs.SetFloat(KEY_BGM, bgmVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(KEY_SFX, sfxVolume);
        PlayerPrefs.Save();
    }

    public float GetBGMVolume() => bgmVolume;
    public float GetSFXVolume() => sfxVolume;

    /// <summary>读取持久化的 BGM/SFX 音量（供设置面板在实例缺失或初始化前安全取初值）</summary>
    public static float GetPersistedBGMVolume() => PlayerPrefs.GetFloat(KEY_BGM, 1f);
    public static float GetPersistedSFXVolume() => PlayerPrefs.GetFloat(KEY_SFX, 1f);

    // ==================== 单位事件注册 ====================

    /// <summary>单位 Awake 时注册（所有单位经 Instantiate 生成），订阅攻击/受击/死亡音</summary>
    public void RegisterUnit(UnitBrain brain)
    {
        if (brain == null || !registeredUnits.Add(brain)) return;

        brain.OnAttackHit   += OnUnitAttackHit;
        brain.OnRangedFire  += OnUnitRangedFire;
        brain.OnHeal        += OnUnitHeal;
        brain.OnDamageTaken += OnUnitDamageTaken;
        brain.OnDeath       += OnUnitDeath;
    }

    public void UnregisterUnit(UnitBrain brain)
    {
        if (brain == null || !registeredUnits.Remove(brain)) return;

        brain.OnAttackHit   -= OnUnitAttackHit;
        brain.OnRangedFire  -= OnUnitRangedFire;
        brain.OnHeal        -= OnUnitHeal;
        brain.OnDamageTaken -= OnUnitDamageTaken;
        brain.OnDeath       -= OnUnitDeath;
    }

    // —— 单位事件回调 ——
    // 说明：NotifyAttackHit 仅近战（DealDamage）触发；远程走 OnRangedFire、治疗走 OnHeal，
    //      所以无需按策略类型区分，直接分发到对应槽位即可。

    void OnUnitAttackHit()     { PlaySFX(AudioSlot.AttackMelee); }
    void OnUnitRangedFire()    { PlaySFX(AudioSlot.AttackRanged); }
    void OnUnitHeal()          { PlaySFX(AudioSlot.Heal); }

    void OnUnitDamageTaken(float damage, AttackType type)
    {
        // 节流：避免高频伤害叠音；用 unscaledTime 使暂停期间也生效
        if (Time.unscaledTime - lastHurtTime < 0.15f) return;
        lastHurtTime = Time.unscaledTime;
        PlaySFX(AudioSlot.Hurt);
    }

    void OnUnitDeath(GameObject unit)
    {
        // Boss 死亡音由 GameEvents.OnBossDefeated 统一播放，这里跳过避免双响
        if (unit != null && unit.GetComponent<BossUnit>() != null) return;
        PlaySFX(AudioSlot.Die);
    }
}
