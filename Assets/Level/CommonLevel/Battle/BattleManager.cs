using UnityEngine;
using UnityEngine.UI;

// 战斗核心管理器：负责关卡、费用、胜负判断、游戏状态控制
public class BattleManager : MonoBehaviour
{
    // 单例实例，全局唯一，方便其他脚本调用
    public static BattleManager Instance;

    [Header("绑定的关卡配置数据")]
    public WaveList waveList;  // 当前默认的波次、单位配置

    [Header("双塔引用")]
    public GameObject playerTower;   // 玩家防御塔
    public GameObject enemyTower;    // 敌方防御塔

    // 单独标记双塔是否存活（避免直接判空产生报错）
    public bool playerTowerAlive = true;
    public bool enemyTowerAlive = true;

    [Header("费用")]
    public float nowCost;        // 当前拥有的费用
    public float maxCost = 10f;  // 费用上限
    public float costAddSpeed = 1f;  // 费用增长速度

    [Header("费用UI")]
    public Text costText;        // 显示当前费用的UI文本

    private bool isGameOver = false;  // 游戏是否结束的标记

    // 游戏启动时执行：初始化关卡与出怪系统
    void Start()
    {
        // 指定生成点
        // 指定当前使用的关卡数据
        // 启动关卡波次出怪逻辑
        WaveGenerator.Instance.StartWave(enemyTower.transform, waveList);  
    }

    // 脚本实例化时执行：初始化单例
    void Awake()
    {
        if (Instance == null) 
            Instance = this;
    }

    // 每帧执行：更新费用、刷新UI、判断游戏状态
    void Update()
    {
        // 游戏结束后不再执行任何逻辑
        if (isGameOver) return;

        // 执行费用自动增长逻辑
        CostAdd();

        // 刷新UI显示当前费用（取整显示）
        costText.text = Mathf.FloorToInt(nowCost) + "/" + maxCost;
    }

    // 费用自动增加方法
    void CostAdd()
    {
        // 当前费用未达上限时才增长
        if(nowCost < maxCost)
        {
            nowCost += (Time.deltaTime * costAddSpeed);
        }
    }

    // 玩家使用费用（扣费用）
    // 参数：需要消耗的费用
    // 返回值：费用足够返回true，不足返回false
    public bool UseCost(int cost)
    {
        if (nowCost >= cost)
        {
            nowCost -= cost;
            return true;
        }
        return false;
    }

    // 胜负判定核心方法：由塔被摧毁时调用
    public void CheckWin()
    {
        // 游戏已结束，不再重复判断
        if (isGameOver) return;

        // 敌方塔被摧毁 → 玩家胜利
        if (!enemyTowerAlive)
        {
            GameWin();
        }
        // 我方塔被摧毁 → 玩家失败
        else if (!playerTowerAlive)
        {
            GameLose();
        }
    }

    // 胜利执行逻辑
    void GameWin()
    {
        isGameOver = false;
        Debug.Log("===== 游戏胜利 =====");
        
        // 暂停游戏时间，所有物理、动画、协程停止
        Time.timeScale = 0;

        // 后续可扩展：显示胜利弹窗、播放特效、跳转场景
    }

    // 失败执行逻辑
    void GameLose()
    {
        isGameOver = true;
        Debug.Log("===== 游戏失败 =====");
        
        // 暂停游戏时间
        Time.timeScale = 0;

        // 后续可扩展：显示失败界面、重新开始游戏
    }
}