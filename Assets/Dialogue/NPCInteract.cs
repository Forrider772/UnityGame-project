using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    [Header("NPC 设置")]
    public DialogueAsset npcDialogue; // 分配该 NPC 的对话数据
    private DialogueManager dialogueManager;

    private void Start()
    {
        // 自动获取场景中的对话管理器
        dialogueManager = FindObjectOfType<DialogueManager>();
    }

    // 当玩家进入触发范围
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // 确保碰撞体是玩家
        {
            // 这里可以显示一个“按R对话”的提示UI
            if (Input.GetKeyDown(KeyCode.R))
            {
                dialogueManager.StartDialogue(npcDialogue);
            }
        }
    }
}