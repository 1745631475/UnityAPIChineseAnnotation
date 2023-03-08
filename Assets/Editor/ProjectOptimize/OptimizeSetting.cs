using Sirenix.OdinInspector;

using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEditor;

using UnityEngine;

public class OptimizeSetting : ScriptableObject
{
    public static string AssetPath = "/Editor/ProjectOptimize/优化设置.asset";

    [LabelText("限制最大GameObject")]
    public int MaxGameObjectNum = 30000;

    [LabelText("限制最大模型数量")]
    public int MaxModelNum = 20000;

    [LabelText("限制单个模型最小顶点数量")]
    public int MinVertsCount = 200;

    [LabelText("限制单个模型最大顶点数量")]
    public int MaxVertsCount = 5000;

    [MenuItem("Tools/项目优化/项目优化设置")]
    private static void CreatOptimizeSetting()
    {
        OptimizeSetting asset = AssetDatabase.LoadAssetAtPath<OptimizeSetting>("Assets" + AssetPath);
        if (!asset)
        {
            asset = CreateInstance<OptimizeSetting>();
            AssetDatabase.CreateAsset(asset, "Assets" + AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        EditorGUIUtility.PingObject(asset);
    }
}
