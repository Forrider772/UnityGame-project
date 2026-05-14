// using UnityEngine;
// using UnityEngine.UI;

// // 挂在 卡牌预制体 根物体上
// public class CardItemUI : MonoBehaviour
// {
//     [Header("UI绑定")]
//     public Image icon;         // 卡牌头像
//     public Text costText;      // 卡牌费用文字
//     public Text cdText;        // 冷却倒计时文字
//     public Image cdMask;       // 冷却黑色遮罩
//     public Button clickBtn;    // 卡牌按钮组件

//     // 当前卡牌配置数据
//     private CardData cardData;
//     // 当前剩余冷却时间
//     private float currentCd;
//      // 是否被选中
//     public bool IsSelected { get; private set; }


//     // ========== 初始化卡牌数据（外部管理器调用） ==========
//     public void Init(CardData data)
//     {
//         // 把传进来的卡牌配置存起来
//         cardData = data;

//         // 赋值头像、费用文字
//         icon.sprite = data.icon;
//         costText.text = data.cost.ToString();

//         // 初始状态：无冷却、遮罩全透明
//         cdMask.fillAmount = 0;
//         cdText.text = "";

//         IsSelected = false;

//         // 绑定按钮点击事件
//         clickBtn.onClick.AddListener(OnCardClick);
//     }


//     // ========== 卡牌点击事件 ==========
//     void OnCardClick()
//     {
//         // 如果还在冷却中，直接返回，不能点
//         if (currentCd > 0)
//             return;

//         // 先取消上一张选中
//         if(FindObjectOfType<CardManager>().curSelectedCard != null)
//         {
//             FindObjectOfType<CardManager>().curSelectedCard.IsSelected = false;
//             FindObjectOfType<CardManager>().curSelectedCard.icon.color = Color.white;
//         }

//         // 选中自己
//         IsSelected = true;
//         icon.color = new Color(0.8f,0.8f,0.8f,1f);

//         // 记录到管理器
//         FindObjectOfType<CardManager>().curSelectedCard = this;
       
//         // 执行使用卡牌逻辑
//         UseCard();
//     }


//     // ========== 使用卡牌逻辑 ==========
//     void UseCard()
//     {
//         // 这里你以后写：生成士兵、扣费用等逻辑
//         CardDeployManager.Instance.SelectCard(cardData);
//         Debug.Log("使用卡牌：" + cardData.cardName);
//     }


//     // ========== 开始冷却 ==========
//     void StartCooldown()
//     {
//         // 把冷却时间设为配置表里的总冷却
//         currentCd = cardData.cooldown;
//     }
//         // 真正使用卡牌后，手动调用这个才开始冷却
//     public void StartCardCooldown()
//     {
//         if (cardData == null) return;
//         currentCd = cardData.cooldown;
//         // 用完取消选中
//         IsSelected = false;
//         icon.color = Color.white;
//     }

//     // ========== 每帧更新冷却遮罩和文字 ==========
//     void Update()
//     {
//         // 没有冷却就不执行
//         if (currentCd <= 0)
//             return;

//         // 时间每秒减少
//         currentCd -= Time.deltaTime;

//         // 遮罩比例 = 当前剩余时间 / 总冷却时间
//         cdMask.fillAmount = currentCd / cardData.cooldown;

//         // 显示整数倒计时
//         cdText.text = Mathf.Ceil(currentCd).ToString();

//         // 冷却结束清空文字
//         if (currentCd <= 0)
//         {
//             cdText.text = "";
//         }
//     }

//     // ========== 预留：编辑卡组获取卡牌ID ==========
//     public string GetCardID() => cardData.cardID;
// }