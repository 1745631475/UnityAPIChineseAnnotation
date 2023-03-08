using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

public class OptimizeWindow : OdinEditorWindow
{
    #region 打开窗口
    [MenuItem("Tools/项目优化/项目优化窗口")]
    private static void OpenWindow()
    {
        var window = GetWindow<OptimizeWindow>("项目优化窗口");
        window.position = GUIHelper.GetEditorWindowRect().AlignCenter(700, 700);
    }
    #endregion

    #region 字段
    private OptimizeSetting optimizeSetting;

    [EnumToggleButtons]
    [HideLabel]
    public OptimizeWindowTable optimizeWindowTable;

    [DisplayAsString]
    [Title("游戏对象", horizontalLine: false)]
    [LabelText("游戏对象数量")]
    [ShowIf("optimizeWindowTable", OptimizeWindowTable.A)]
    public int gameObjectSum;

    [DisplayAsString]
    [LabelText("游戏对象数量(激活)")]
    [ShowIf("optimizeWindowTable", OptimizeWindowTable.A)]
    public int gameObjectSumActive;

    [DisplayAsString]
    [LabelText("游戏对象数量(未激活)")]
    [ShowIf("optimizeWindowTable", OptimizeWindowTable.A)]
    public int gameObjectSumNotActive;

    [DisplayAsString]
    [Title("模型", horizontalLine: false)]
    [LabelText("模型数量")]
    [ShowIf("optimizeWindowTable", OptimizeWindowTable.A)]
    public int modelSum;

    [DisplayAsString]
    [LabelText("模型占用内存大小")]
    [ShowIf("optimizeWindowTable", OptimizeWindowTable.A)]
    public string modelMemorySize;

    [DisplayAsString]
    [LabelText("模型顶点数量")]
    [ShowIf("optimizeWindowTable", OptimizeWindowTable.A)]
    public int vertsNum;

    [DisplayAsString]
    [LabelText("模型三角面数量")]
    [ShowIf("optimizeWindowTable", OptimizeWindowTable.A)]
    public int trisNum;

    [Title("网格合并", horizontalLine: false)]
    [LabelText("网格合并后是否移除原组件")]
    [ShowIf("optimizeWindowTable", OptimizeWindowTable.B)]
    public bool isRemovePrimaryComponent;

    [LabelText("网格合并是否包含隐藏对象")]
    [ShowIf("optimizeWindowTable", OptimizeWindowTable.B)]
    public bool isIncludeInactive;

    [LabelText("将网格合并到新创建的对象")]
    [ShowIf("optimizeWindowTable", OptimizeWindowTable.B)]
    public bool isNewCreate;

    [LabelText("开启网格合并预览")]
    [ShowIf("optimizeWindowTable", OptimizeWindowTable.B)]
    public bool OpenPreview;

    [DisplayAsString]
    [LabelText("当前选中网格数量")]
    [InfoBox("当前未选择任何网格对象", "isCurrentSelectionMeshCount")]
    [ShowIf("optimizeWindowTable", OptimizeWindowTable.B)]
    public int CurrentSelectionMeshCount;

    [DisplayAsString]
    [LabelText("当前选中网格顶点数量")]
    [InfoBox("当前未选择任何网格对象", "isCurrentSelectionMeshCount")]
    [ShowIf("optimizeWindowTable", OptimizeWindowTable.B)]
    public int CurrentSelectionMeshVertsCount;

    [DisplayAsString]
    [LabelText("当前选中网格及其子对象网格数量")]
    [InfoBox("当前未选择任何网格对象", "isCurrentSelectionMeshAndSubobjectMeshCount")]
    [ShowIf("optimizeWindowTable", OptimizeWindowTable.B)]
    public int CurrentSelectionMeshAndSubobjectMeshCount;

    [DisplayAsString]
    [LabelText("当前选中网格顶点及其子对象网格顶点数量")]
    [InfoBox("当前未选择任何网格对象", "isCurrentSelectionMeshAndSubobjectMeshCount")]
    [ShowIf("optimizeWindowTable", OptimizeWindowTable.B)]
    public int CurrentSelectionMeshVertsAndSubobjectMeshVertsCount;

    [LabelText("网格合并预览")]
    [PropertyOrder(1)]
    [ReadOnly]
    [PreviewField(300, Sirenix.OdinInspector.ObjectFieldAlignment.Left)]
    [ShowIf("@this.optimizeWindowTable==OptimizeWindowTable.B&&this.OpenPreview")]
    [LabelWidth(100)]
    public GameObject preview;
    #endregion

    #region 属性
    public OptimizeSetting GetOptimizeSetting
    {
        get
        {
            if (!optimizeSetting)
            {
                optimizeSetting = AssetDatabase.LoadAssetAtPath<OptimizeSetting>("Assets" + OptimizeSetting.AssetPath);
                if (!optimizeSetting)
                {
                    optimizeSetting = CreateInstance<OptimizeSetting>();
                    AssetDatabase.CreateAsset(optimizeSetting, "Assets" + OptimizeSetting.AssetPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
            return optimizeSetting;
        }
    }
    #endregion

    #region 主要方法
    [ShowIf("optimizeWindowTable", OptimizeWindowTable.B)]
    [Button("合并选中所有对象网格")]
    public void ButtonCombinedMesh_All()
    {

    }

    [ShowIf("optimizeWindowTable", OptimizeWindowTable.B)]
    [Button("合并选中单个对象及其子对象网格")]
    public void ButtonCombinedMesh_ThisAndSubobject()
    {

    }

    [ShowIf("optimizeWindowTable", OptimizeWindowTable.B)]
    [Button("将每一个选中对象及其子对象合并成一个网格")]

    public void ButtonCombinedMesh_Allbatching()
    {

    }
    #endregion

    #region 辅助方法
    public void GetViewInfo()
    {
        //获取场景GmaeObject信息
        GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(obj => obj.scene.isLoaded && obj.name != "SceneLight").Select(obj => obj).ToArray();
        gameObjectSum = gameObjects.Length;
        gameObjectSumActive = gameObjects.Where(obj => obj.activeSelf == true).Count();
        gameObjectSumNotActive = gameObjects.Where(obj => obj.activeSelf == false).Count();
        //获取模型信息
        MeshFilter[] meshFilters = gameObjects
            .Where(obj => obj.GetComponent<MeshFilter>()).Select(obj => obj.GetComponent<MeshFilter>()).ToArray();
        modelSum = meshFilters.Count();
        long memorySize = 0;
        int verts = 0;
        int tris = 0;
        foreach (MeshFilter item in meshFilters)
        {
            if (item.sharedMesh)
            {
                memorySize += Profiler.GetRuntimeMemorySizeLong(item.sharedMesh);
                verts += item.sharedMesh.vertices.Length;
                tris += item.sharedMesh.triangles.Length;

                long sinze = item.sharedMesh.vertices.Length * sizeof(float) * 3;
            }
        }
        modelMemorySize = EditorUtility.FormatBytes(memorySize);
        vertsNum = verts;
        trisNum = tris / 3;
    }

    public bool isCurrentSelectionMeshCount()
    {
        List<Mesh> meshes = Selection.gameObjects
            .Where(obj => obj.scene.isLoaded && obj.GetComponent<MeshFilter>() && obj.GetComponent<MeshFilter>().sharedMesh)
            .Select(obj => obj.GetComponent<MeshFilter>().sharedMesh).ToList();
        CurrentSelectionMeshCount = meshes.Count;
        int count = 0;
        foreach (Mesh item in meshes)
            count += item.vertexCount;
        CurrentSelectionMeshVertsCount = count;
        if (meshes == null || meshes.Count == 0)
            return true;
        return false;
    }

    public bool isCurrentSelectionMeshAndSubobjectMeshCount()
    {
        List<Mesh> meshes = new List<Mesh>();
        MeshFilter[] memberFilter;
        foreach (GameObject item in Selection.gameObjects)
        {
            if (item.scene.isLoaded)
            {
                memberFilter = item.GetComponentsInChildren<MeshFilter>();
                meshes.AddRange(memberFilter.Where(value => value.sharedMesh).Select(value => value.sharedMesh));
            }
        }
        CurrentSelectionMeshAndSubobjectMeshCount = meshes.Count;
        int count = 0;
        foreach (Mesh item in meshes)
            count += item.vertexCount;
        CurrentSelectionMeshVertsAndSubobjectMeshVertsCount = count;
        if (meshes == null || meshes.Count == 0)
            return true;
        return false;
    }

    private void EditorSceneManager_sceneSaved(Scene scene)
    {
        GetViewInfo();
    }

    private void EditorSceneManager_activeSceneChanged(Scene arg0, Scene arg1)
    {
        GetViewInfo();
    }
    #endregion

    #region 消息
    private void Awake()
    {
        GetViewInfo();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EditorSceneManager.activeSceneChangedInEditMode += EditorSceneManager_activeSceneChanged;
        EditorSceneManager.sceneSaved += EditorSceneManager_sceneSaved;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EditorSceneManager.activeSceneChangedInEditMode -= EditorSceneManager_activeSceneChanged;
        EditorSceneManager.sceneSaved -= EditorSceneManager_sceneSaved;
    }

    private void OnInspectorUpdate()
    {

    }
    #endregion

    public enum OptimizeWindowTable
    {
        [LabelText("资源信息")]
        A,
        [LabelText("优化处理")]
        B,
    }
}