using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI 组件引用")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI contentText;
    public Image avatarImage;
    
    [Header("对话设置")]
    [SerializeField] private DialogueAsset dialogueAsset;

    [Header("设置")]
    public float textSpeed = 0.05f;

    private DialogueAsset currentDialogue;
    private int dialogueIndex = 0;
    private bool isTyping = false;

    // 单例模式
    public static DialogueManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    // 启动对话
    public void StartDialogue(DialogueAsset dialogue)
    {
        if (dialogue == null)
        {
            Debug.LogError("尝试开始对话时，传入的 DialogueAsset 为空！");
            return;
        }

        currentDialogue = dialogue;
        dialogueIndex = 0;
        dialoguePanel.SetActive(true);

        ShowNextLine();
    }

    // 按钮点击调用这个函数
    public void OnContinueClick()
    {
        if (isTyping)
        {
            // 如果正在打字，立即显示全文
            StopAllCoroutines();
            contentText.text = currentDialogue.dialogueLines[dialogueIndex - 1].dialogueText;
            isTyping = false;
        }
        else
        {
            // 如果没在打字，显示下一句
            ShowNextLine();
        }
    }

    private void ShowNextLine()
    {
        // 检查是否还有下一句 (使用 .Length 因为是数组)
        if (dialogueIndex < currentDialogue.dialogueLines.Length)
        {
            DialogueAsset.DialogueLine line = currentDialogue.dialogueLines[dialogueIndex];

            nameText.text = line.speakerName;
            if (avatarImage != null && line.avatarSprite != null)
            {
                avatarImage.sprite = line.avatarSprite;
            }

            contentText.text = "";
            isTyping = true;
            dialogueIndex++;

            StartCoroutine(TypeLine(line.dialogueText));
        }
        else
        {
            EndDialogue();
        }
    }

    private IEnumerator TypeLine(string text)
    {
        foreach (char letter in text.ToCharArray())
        {
            contentText.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }
        isTyping = false;
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        // 这里可以添加对话结束后的逻辑，比如恢复游戏控制
    }
}