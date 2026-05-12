using UnityEngine;

// 这个脚本不需要修改，直接挂载即可
public class DialogueStarter : MonoBehaviour
{
    [Header("在这里拖入你的对话数据文件")]
    public DialogueAsset dialogueAsset; // 把你的 .asset 文件拖到这里

    void Start()
    {
        // 1. 检查是否拖入了数据
        if (dialogueAsset != null)
        {
            // 2. 检查 DialogueManager 是否存在（确保场景里有挂载该脚本的物体）
            if (DialogueManager.Instance != null)
            {
                // 3. 调用管理器，开始对话！
                DialogueManager.Instance.StartDialogue(dialogueAsset);
            }
            else
            {
                Debug.LogError("场景里找不到 DialogueManager！请确保有一个物体挂载了 DialogueManager 脚本。");
            }
        }
        else
        {
            Debug.LogWarning("DialogueStarter 里没有分配 DialogueAsset！请在 Inspector 里拖入数据。");
        }
    }
}