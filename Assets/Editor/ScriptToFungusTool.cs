using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Fungus;
using Fungus.EditorUtils;

/// <summary>
/// 正式剧本 txt → Fungus 对话 编辑器工具
/// 菜单: Tools → Fungus → 导入剧本文本…
///
/// 解析「正式剧本」规范格式（Assets/JuBen/*_正式剧本.txt）：
///   ## 幕标题            → 新建一个 Block
///   【场景】/【BGM】/【SE】/【画面】/【提示框】/（闪回） → Comment 命令
///   @说话人             → 设置当前说话人，后续行合并为该角色台词（可多行）
///   @说话人：xxx        → 同行写法
///   @旁白               → 无角色
///   @A（内心）          → 角色 A（括号是备注，解析时去掉）
///   其他行              → 追加到当前说话人的文本
///
/// 立绘匹配：自动查找 {portraitRoot}/Portrait_{角色名}.png 并挂到角色上。
/// 幕 Block 自动用 Call 命令串联；触发方式由你在场景中自行设置（如 GameStarted 事件）。
/// </summary>
public class ScriptToFungusTool : EditorWindow
{
    private Flowchart flowchart;
    private TextAsset scriptAsset;
    private string portraitRoot = "Assets/ArtResources/Portraits";
    private bool splitByAct = true;
    private bool showPortraits = true;

    [MenuItem("Tools/Fungus/导入剧本文本…")]
    public static void ShowWindow()
    {
        var w = GetWindow<ScriptToFungusTool>("剧本导入");
        w.minSize = new Vector2(420, 320);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("正式剧本 → Fungus 对话", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        flowchart = (Flowchart)EditorGUILayout.ObjectField("目标 Flowchart", flowchart, typeof(Flowchart), true);
        scriptAsset = (TextAsset)EditorGUILayout.ObjectField("正式剧本文本 (.txt)", scriptAsset, typeof(TextAsset), false);
        portraitRoot = EditorGUILayout.TextField("立绘目录", portraitRoot);
        splitByAct = EditorGUILayout.Toggle("按幕拆分 Block", splitByAct);
        showPortraits = EditorGUILayout.Toggle("生成立绘（Stage + Portrait）", showPortraits);

        EditorGUILayout.Space();
        if (GUILayout.Button("导入", GUILayout.Height(30)))
        {
            Import();
        }

        EditorGUILayout.HelpBox(
            "规范格式：\n## 幕标题 → 新 Block\n@旁白 / @A / @A（内心）→ 说话人\n@说话人：xxx → 同行写法\n【】与（闪回）→ Comment",
            MessageType.Info);
    }

    // ================= 导入主流程 =================

    private void Import()
    {
        if (flowchart == null) { EditorUtility.DisplayDialog("错误", "请先在场景中选择目标 Flowchart", "确定"); return; }
        if (scriptAsset == null) { EditorUtility.DisplayDialog("错误", "请选择正式剧本文本（.txt 资产）", "确定"); return; }

        var entries = ParseScript(scriptAsset.text);
        if (entries.Count == 0) { EditorUtility.DisplayDialog("提示", "未解析到任何内容", "确定"); return; }

        // 立绘舞台（没有则自动创建）
        Stage stage = null;
        if (showPortraits) stage = EnsureStage();

        var chars = new Dictionary<string, Character>();
        Block currentBlock = null;
        var actBlocks = new List<Block>(); // 按导入顺序记录幕 Block

        foreach (var e in entries)
        {
            if (e.isActTitle)
            {
                string blockName = splitByAct ? e.actTitle : "对话";
                currentBlock = GetOrCreateBlock(blockName);
                if (!actBlocks.Contains(currentBlock)) actBlocks.Add(currentBlock);
                continue;
            }

            if (currentBlock == null)
            {
                currentBlock = GetOrCreateBlock("对话");
            }

            if (e.isDirection)
            {
                AddComment(currentBlock, flowchart, e.text);
                continue;
            }

            Character ch = null;
            if (!string.IsNullOrEmpty(e.speaker))
            {
                if (!chars.TryGetValue(e.speaker, out ch))
                {
                    ch = GetOrCreateCharacter(e.speaker);
                    chars[e.speaker] = ch;
                }
                if (stage != null)
                {
                    AddPortrait(currentBlock, stage, ch);
                }
            }

            AddSay(currentBlock, flowchart, e.text, ch);
        }

        // 幕 Block 自动串联：上一幕结尾 Call 下一幕，整章连续播放
        if (splitByAct)
        {
            for (int i = 0; i < actBlocks.Count - 1; i++)
            {
                AddCall(actBlocks[i], actBlocks[i + 1]);
            }
        }

        EditorUtility.SetDirty(flowchart);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", $"已导入 {entries.Count} 条内容到 Flowchart「{flowchart.name}」", "确定");
    }

    // ================= 正式剧本解析 =================

    private class Entry
    {
        public bool isActTitle;
        public string actTitle;
        public bool isDirection;
        public string speaker; // 空字符串 = 旁白
        public string text;
    }

    private List<Entry> ParseScript(string content)
    {
        var entries = new List<Entry>();
        string[] lines = content.Split('\n');

        string curSpeaker = null; // null = 当前没有说话人
        var curLines = new List<string>();

        System.Action flush = () =>
        {
            if (curLines.Count == 0) return;
            entries.Add(new Entry
            {
                speaker = curSpeaker ?? "",
                text = string.Join("\n", curLines)
            });
            curLines.Clear();
            curSpeaker = null;
        };

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // ## 幕标题
            if (line.StartsWith("##"))
            {
                flush();
                string title = line.TrimStart('#', ' ', '\t');
                entries.Add(new Entry { isActTitle = true, actTitle = title });
                continue;
            }

            // 演出提示：【…】、…、（闪回…）、(…)
            if (line.StartsWith("【") || line.StartsWith("（") || line.StartsWith("("))
            {
                flush();
                entries.Add(new Entry { isDirection = true, text = line });
                continue;
            }

            // @说话人 或 @说话人：xxx
            if (line.StartsWith("@"))
            {
                flush();
                string rest = line.Substring(1).Trim();
                int idx = rest.IndexOf('：');
                if (idx < 0) idx = rest.IndexOf(':');
                if (idx > 0)
                {
                    string speaker = NormalizeSpeaker(rest.Substring(0, idx).Trim());
                    string text = rest.Substring(idx + 1).Trim();
                    curSpeaker = speaker;
                    if (text.Length > 0) curLines.Add(text);
                }
                else if (idx == 0)
                {
                    curSpeaker = ""; // @：xxx 异常行，按旁白处理
                }
                else
                {
                    curSpeaker = NormalizeSpeaker(rest);
                }
                continue;
            }

            // 正文行：合并到当前说话人
            if (curSpeaker != null || curLines.Count > 0)
                curLines.Add(line);
        }

        flush();
        return entries;
    }

    /// <summary>
    /// 说话人规范化：去掉括号备注并处理旁白。
    /// A（内心）→ A，旁白 → 空串。
    /// </summary>
    private string NormalizeSpeaker(string speaker)
    {
        if (speaker == "旁白") return "";
        int pi = speaker.IndexOf('（');
        if (pi > 0) speaker = speaker.Substring(0, pi).Trim();
        pi = speaker.IndexOf('(');
        if (pi > 0) speaker = speaker.Substring(0, pi).Trim();
        return speaker;
    }

    // ================= Fungus 生成 =================

    private Block GetOrCreateBlock(string blockName)
    {
        var b = flowchart.FindBlock(blockName);
        if (b != null) return b;
        b = flowchart.CreateBlock(Vector2.zero);
        b.BlockName = blockName;
        return b;
    }

    private Character GetOrCreateCharacter(string name)
    {
        foreach (var c in FindObjectsOfType<Character>())
        {
            if (c.NameText == name) return c;
        }

        var go = new GameObject("Character_" + name);
        Undo.RegisterCreatedObjectUndo(go, "Create Character");
        var ch = go.AddComponent<Character>();

        var so = new SerializedObject(ch);
        so.FindProperty("nameText").stringValue = name;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{portraitRoot}/Portrait_{name}.png");
        if (sprite != null)
        {
            var portraits = so.FindProperty("portraits");
            portraits.arraySize = 1;
            portraits.GetArrayElementAtIndex(0).objectReferenceValue = sprite;
        }
        so.ApplyModifiedProperties();
        return ch;
    }

    private void AddSay(Block block, Flowchart fc, string text, Character ch)
    {
        var cmd = Undo.AddComponent<Say>(block.gameObject);
        fc.AddSelectedCommand(cmd);
        cmd.ParentBlock = block;
        cmd.ItemId = fc.NextItemId();
        cmd.OnCommandAdded(block);

        var so = new SerializedObject(cmd);
        so.FindProperty("storyText").stringValue = text;
        if (ch != null) so.FindProperty("character").objectReferenceValue = ch;
        so.ApplyModifiedProperties();

        block.CommandList.Add(cmd);
    }

    private Stage EnsureStage()
    {
        var existing = FindObjectsOfType<Stage>();
        if (existing.Length > 0) return existing[0];

        // 用 Fungus 官方菜单的方式创建（等价 Tools → Fungus → Create → Stage）
        var go = FlowchartMenuItems.SpawnPrefab("Stage");
        if (go == null)
        {
            Debug.LogWarning("[剧本导入] 创建 Stage 失败，请手动 Tools → Fungus → Create → Stage");
            return null;
        }
        return go.GetComponentInChildren<Stage>();
    }

    private void AddPortrait(Block block, Stage stage, Character ch)
    {
        Sprite sprite = (ch.Portraits != null && ch.Portraits.Count > 0) ? ch.Portraits[0] : null;
        if (sprite == null) return; // 角色没配立绘就不加 Portrait 命令

        var cmd = Undo.AddComponent<Portrait>(block.gameObject);
        flowchart.AddSelectedCommand(cmd);
        cmd.ParentBlock = block;
        cmd.ItemId = flowchart.NextItemId();
        cmd.OnCommandAdded(block);

        var so = new SerializedObject(cmd);
        var displayProp = so.FindProperty("display");
        if (displayProp != null) displayProp.enumValueIndex = 1; // DisplayType.Show
        so.FindProperty("stage").objectReferenceValue = stage;
        so.FindProperty("character").objectReferenceValue = ch;
        so.FindProperty("portrait").objectReferenceValue = sprite;
        so.ApplyModifiedProperties();

        block.CommandList.Add(cmd);
    }

    private void AddCall(Block block, Block target)
    {
        var cmd = Undo.AddComponent<Call>(block.gameObject);
        flowchart.AddSelectedCommand(cmd);
        cmd.ParentBlock = block;
        cmd.ItemId = flowchart.NextItemId();
        cmd.OnCommandAdded(block);

        var so = new SerializedObject(cmd);
        var prop = so.FindProperty("targetBlock");
        if (prop != null) prop.objectReferenceValue = target;
        so.ApplyModifiedProperties();

        block.CommandList.Add(cmd);
    }

    private void AddComment(Block block, Flowchart fc, string text)
    {
        var cmd = Undo.AddComponent<Comment>(block.gameObject);
        fc.AddSelectedCommand(cmd);
        cmd.ParentBlock = block;
        cmd.ItemId = fc.NextItemId();
        cmd.OnCommandAdded(block);

        var so = new SerializedObject(cmd);
        var prop = so.FindProperty("commentText");
        if (prop != null) prop.stringValue = text;
        so.ApplyModifiedProperties();

        block.CommandList.Add(cmd);
    }
}
