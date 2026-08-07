using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/Dialogue Asset")]
public class DialogueAsset : ScriptableObject
{
    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName;
        public Sprite avatarSprite;
        [TextArea(3, 10)]
        public string dialogueText;
    }

    public DialogueLine[] dialogueLines;
}