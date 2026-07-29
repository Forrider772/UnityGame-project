using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 战斗核心管理器（单例）
/// 负责关卡初始化、费用系统、胜负判定、游戏结束面板
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("波次生成器")]
    public WaveGenerator waveGenerator;

    [Header("绑定的关卡配置数据")]
    public WaveList waveList;

    [Header("双塔引用")]
    public GameObject playerTower;
    public GameObject enemyTower;

    /// <summary>玩家防御塔是否存活</summary>
    public bool playerTowerAlive = true;

    /// <summary>敌方防御塔是否存活</summary>
    public bool enemyTowerAlive = true;

    [Header("费用")]
    public float nowCost;
    public float maxCost = 10f;
    public float costAddSpeed = 1f;

    [Header("费用UI")]
    public Text costText;

    [Header("胜利面板")]
    public GameObject winPanel;
    public Button winNextLevelButton;
    public Button winMainMenuButton;

    [Header("失败面板")]
    public GameObject losePanel;
    public Button loseRetryButton;
    public Button loseMainMenuButton;

    /// <summary>游戏是否已结束，结束后不再执行 Update 逻辑</summary>
    private bool isGameOver = false;

    void Start()
    {
        // 启动波次出怪
        waveGenerator.StartWave(enemyTower.transform, waveList);

        // 初始化胜利面板按钮及隐藏
        if (winPanel != null)
        {
            winPanel.SetActive(false);
            winNextLevelButton.onClick.AddListener(OnWinNextLevel);
            winMainMenuButton.onClick.AddListener(OnWinMainMenu);
        }

        // 初始化失败面板按钮及隐藏
        if (losePanel != null)
        {
            losePanel.SetActive(false);
            loseRetryButton.onClick.AddListener(OnLoseRetry);
            loseMainMenuButton.onClick.AddListener(OnLoseMainMenu);
        }
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        // 确保 BuffManager 组件存在，供 LevelSetup 在 Awake 阶段注册 buff
        if (GetComponent<BuffManager>() == null)
            gameObject.AddComponent<BuffManager>();
    }

    void Update()
    {
        if (isGameOver) return;
        CostAdd();
        costText.text = Mathf.FloorToInt(nowCost) + "/" + maxCost;
    }

    /// <summary>
    /// 费用自动增长，基础速度 + 资源点占领增益
    /// </summary>
    void CostAdd()
    {
        if (nowCost < maxCost)
        {
            float bonus = ResourcePointManager.Instance != null
                ? ResourcePointManager.Instance.GetTotalBonus(CampType.Player) : 0f;
            nowCost += Time.deltaTime * (costAddSpeed + bonus);
        }
    }

    /// <summary>
    /// 消费费用，成功返回 true，不足返回 false
    /// </summary>
    public bool UseCost(int cost)
    {
        if (nowCost >= cost)
        {
            nowCost -= cost;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 胜负判定入口，由防御塔被摧毁时调用
    /// </summary>
    public void CheckWin()
    {
        if (isGameOver) return;
        if (!enemyTowerAlive)
            GameWin();
        else if (!playerTowerAlive)
            GameLose();
    }

    /// <summary>
    /// 胜利处理：暂停时间 → 标记关卡完成并存档 → 显示胜利面板
    /// </summary>
    void GameWin()
    {
        isGameOver = true;
        Time.timeScale = 0;
        SaveManager.MarkLevelCompleted(SceneManager.GetActiveScene().name, SaveManager.CurrentSlotIndex);
        if (winPanel != null)
            winPanel.SetActive(true);
    }

    /// <summary>
    /// 失败处理：暂停时间 → 显示失败面板
    /// </summary>
    void GameLose()
    {
        isGameOver = true;
        Time.timeScale = 0;
        if (losePanel != null)
            losePanel.SetActive(true);
    }

    /// <summary>
    /// 胜利面板 — "下一关"按钮：恢复时间流速，读取存档进入下一关
    /// </summary>
    void OnWinNextLevel()
    {
        Time.timeScale = 1;
        var save = SaveManager.LoadSave(SaveManager.CurrentSlotIndex);
        string scene = save != null ? save.currentLevelScene : "MenuScene";
        SceneManager.LoadScene(scene);
    }

    /// <summary>
    /// 胜利面板 — "返回主菜单"按钮
    /// </summary>
    void OnWinMainMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MenuScene");
    }

    /// <summary>
    /// 失败面板 — "重试"按钮：重新加载当前关卡
    /// </summary>
    void OnLoseRetry()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 失败面板 — "返回主菜单"按钮
    /// </summary>
    void OnLoseMainMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MenuScene");
    }
}
