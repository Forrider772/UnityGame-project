using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

/// <summary>
/// PathVisual 迁移工具 —— 将当前场景中所有路径的 PathVisualManager + LineRenderer
/// 从 PathManager 所在 GameObject 移动到子物体 PathVisual（来自 PathVisual.prefab）
///
/// 使用方法：Unity Editor → Tools → 迁移路径视觉组件到子物体
///
/// 前置条件：必须先手动创建 PathVisual.prefab（见下方说明）
///
/// 迁移前结构:
///   Path_01 (GameObject)
///   ├── PathManager
///   ├── PathVisualManager   ← 要被移走
///   ├── LineRenderer        ← 要被移走
///   └── PathPoint_*
///
/// 迁移后结构:
///   Path_01 (GameObject)
///   ├── PathManager
///   ├── PathVisual (来自 PathVisual.prefab)
///   │   ├── PathVisualManager
///   │   └── LineRenderer
///   └── PathPoint_*
/// </summary>
public class PathVisualMigration : EditorWindow
{
    private const string PrefabPath = "Assets/Level/CommonLevel/Path/PathVisual.prefab";
    private const string MenuItemPath = "Tools/迁移路径视觉组件到子物体";

    [MenuItem(MenuItemPath)]
    public static void MigrateCurrentScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog(
                "缺少预制体",
                $"未找到 PathVisual.prefab，请先在以下路径创建：\n\n{PrefabPath}\n\n创建步骤：\n1. 在场景中创建空 GameObject，命名 PathVisual\n2. 添加 LineRenderer 组件\n3. 添加 PathVisualManager 组件，设置默认视觉参数\n4. 拖入 {PrefabPath} 保存为预制体\n5. 删除场景中的临时 PathVisual",
                "确定");
            return;
        }

        // 检查 PathVisual.prefab 是否有 PathVisualManager 和 LineRenderer
        PathVisualManager prefabVM = prefab.GetComponent<PathVisualManager>();
        LineRenderer prefabLR = prefab.GetComponent<LineRenderer>();
        if (prefabVM == null || prefabLR == null)
        {
            EditorUtility.DisplayDialog(
                "预制体不完整",
                $"PathVisual.prefab 缺少必要组件。请确保预制体包含：\n- PathVisualManager\n- LineRenderer",
                "确定");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            EditorUtility.DisplayDialog("错误", "当前没有打开的场景", "确定");
            return;
        }

        PathManager[] allPaths = FindObjectsOfType<PathManager>();
        int migratedCount = 0;
        int skippedCount = 0;

        foreach (PathManager path in allPaths)
        {
            // 检查是否已经迁移（已有 PathVisual 子物体）
            Transform existingVisual = path.transform.Find("PathVisual");
            if (existingVisual != null)
            {
                skippedCount++;
                continue;
            }

            // 查找同一 GameObject 上的旧组件
            PathVisualManager oldVM = path.GetComponent<PathVisualManager>();
            LineRenderer oldLR = path.GetComponent<LineRenderer>();

            if (oldVM == null)
            {
                // 没有同层级的 PathVisualManager，可能已迁移或从未配置
                skippedCount++;
                continue;
            }

            // 记录撤销
            Undo.RegisterFullObjectHierarchyUndo(path.gameObject, "迁移 PathVisual");

            // 1. 实例化 PathVisual.prefab 为子物体
            GameObject visualChild = (GameObject)PrefabUtility.InstantiatePrefab(prefab, path.transform);
            visualChild.name = "PathVisual";
            visualChild.transform.localPosition = Vector3.zero;
            visualChild.transform.localRotation = Quaternion.identity;
            visualChild.transform.localScale = Vector3.one;
            visualChild.transform.SetAsFirstSibling();

            // 2. 将旧的视觉配置复制到新的 PathVisualManager
            PathVisualManager newVM = visualChild.GetComponent<PathVisualManager>();
            newVM.effect = oldVM.effect;
            newVM.normalColor = oldVM.normalColor;
            newVM.highlightColor = oldVM.highlightColor;
            newVM.normalWidth = oldVM.normalWidth;
            newVM.highlightWidth = oldVM.highlightWidth;
            newVM.pulseSpeed = oldVM.pulseSpeed;

            // 3. 删除旧的 PathVisualManager
            DestroyImmediate(oldVM, true);

            // 4. 删除旧的 LineRenderer
            if (oldLR != null)
                DestroyImmediate(oldLR, true);

            // 标记场景为脏
            EditorUtility.SetDirty(path.gameObject);
            migratedCount++;
        }

        // 显示结果
        string message = $"迁移完成！\n\n已迁移: {migratedCount} 条路径\n已跳过: {skippedCount} 条路径" +
                         (skippedCount > 0 ? "\n\n跳过的路径可能已经迁移过或没有视觉组件。" : "");

        EditorUtility.DisplayDialog("迁移结果", message, "确定");
        Debug.Log($"[PathVisualMigration] {message}");
    }
}
