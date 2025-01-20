//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;
//using UnityEditorInternal;
//using UnityEditor.AnimatedValues;
//using System;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using HotFix;
//using TMPro;

//public class GenerateScriptWindow : EditorWindow
//{
//    AnimBool showInfo = new AnimBool(true);
//    private GUIStyle titleStyle;
//    private int _selectedIndex = 0; // 下拉框选中的索引
//    private string[] _options;
//    private bool _isIncludeListener = true;
//    private bool _isAsync = true;
//    private bool _isMainPage = false;
//    private Dictionary<int, UIType> _enumMap;
//    public bool isCheckPrefab { get; set; } =  true;

//    private void OnDestroy()
//    {
//        isCheckPrefab = true;
//    }
//    private void Awake()
//    {
//        titleStyle = new GUIStyle();
//        titleStyle.fontSize = 20;
//        titleStyle.normal.textColor = Color.yellow;
//        _enumMap = Enum.GetValues(typeof(UIType)).Cast<UIType>().ToDictionary(e => (int)e, e => e);
//        _options = new string[_enumMap.Count];
//        foreach (var type in _enumMap)
//        {
//            string layerName = Enum.GetName(typeof(UIType), type.Value);
//            _options[type.Key - 1] = layerName;
//        }

//        if (IsFormatCorrect() && isCheckPrefab)
//        {
//            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
//            LoadExitProperty(path);
//        }
//    }
    
    

//    private void OnGUI()
//    {
//        if (IsFormatCorrect() == false && isCheckPrefab)
//        {
//            Close();
//            EditorUtility.DisplayDialog("Tips", $"Please select prefab file", "ok");
//        }

//        DrawInfo();
//        GUILayout.Space(100);
//        if (GUILayout.Button("Generator", EditorStyles.miniButtonLeft, GUILayout.MinWidth(90f)))
//        {
//            Close();
//            int layerIndex = _selectedIndex + 1;
//            UIType type = GetEnumByValue(layerIndex);
//            Debug.LogWarning(
//                $"toggleValue:{_isIncludeListener},_isAsync:{_isAsync},_isMainPage:{_isMainPage},type:{type}");
//            ScriptGenerator.AutoGenerateScript(_isAsync, _isMainPage, _isIncludeListener, type);
//        }
//    }

//    void LoadExitProperty(string path)
//    {
//        Debug.Log("  LoadExitProperty  " + path);
//        string filename = Path.GetFileNameWithoutExtension(path);
//        Debug.Log("  LoadExitProperty  " + filename);
//        string typeName = "Assets.Interface." + filename;
//        var _gameAss = AppDomain.CurrentDomain.GetAssemblies()
//            .First(assembly => assembly.GetName().Name == "hot_fix");
//        var type = _gameAss.GetType(typeName);
//        if (type == null)
//        {
//            Debug.LogError($"没有找到类型：{typeName}");
//            return;
//        }

//        var attribute = type.GetCustomAttribute<ViewAttribute>();
//        if (attribute == null)
//        {
//            Debug.LogError($"没有找到ui属性");
//            return;
//        }

//        _isAsync = attribute.isAsync;
//        _isMainPage = attribute.isMainPage;
//        var GetWindowLayer = type.GetMethod("GetWindowLayer");
//        if (GetWindowLayer == null)
//        {
//            Debug.LogError($"没有找到方法:GetWindowLayer");
//            return;
//        }

//        UIType deepth = (UIType)GetWindowLayer.Invoke(Activator.CreateInstance(type), null);
//        string layerName = Enum.GetName(typeof(UIType), deepth);
//        for (int i = 0; i < _options.Length; i++)
//        {
//            if (layerName == _options[i])
//            {
//                _selectedIndex = i;
//                break;
//            }
//        }
//    }

//    private void DrawInfo()
//    {
//        GUILayout.Space(20);
//        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
//        EditorTools.DrawSeparator();
//        GUILayout.Space(5);


//        if (EditorGUILayout.BeginFadeGroup(showInfo.faded))
//        {
//            GUILayout.Space(5);
//            EditorGUILayout.LabelField("Prefab:", $"{Selection.activeObject.name}", GUILayout.ExpandWidth(true));
//            GUILayout.Space(5);
//            EditorGUILayout.LabelField("Path:", $"{path}");
//            GUILayout.Space(5);
//            _isIncludeListener = EditorGUILayout.Toggle("IncludeListener:", _isIncludeListener);
//            GUILayout.Space(5);
//            _isAsync = EditorGUILayout.Toggle("IsAsync:", _isAsync);
//            GUILayout.Space(5);
//            _isMainPage = EditorGUILayout.Toggle("IsMainPage:", _isMainPage);
//            GUILayout.Space(5);
//            _selectedIndex = EditorGUILayout.Popup("Layer:", _selectedIndex, _options);

//            if (GUI.changed)
//            {
//                Debug.Log("Selected option: " + _options[_selectedIndex]);
//            }
//        }

//        EditorGUILayout.EndFadeGroup();
//    }

//    bool IsFormatCorrect()
//    {
//        UnityEngine.Object selectedObject = Selection.activeObject;
//        if (selectedObject != null)
//        {
//            string path = AssetDatabase.GetAssetPath(selectedObject);
//            if (!string.IsNullOrEmpty(path))
//            {
//                string extension = System.IO.Path.GetExtension(path).ToLower();
//                if (extension.Contains(".prefab"))
//                {
//                    return true;
//                }
//            }
//        }

//        return false;
//    }

//    UIType GetEnumByValue(int value)
//    {
//        return _enumMap.TryGetValue(value, out UIType result) ? result : UIType.SubNormal;
//    }

//    public void OnInspectorUpdate()
//    {
//        this.Repaint();
//    }
//}