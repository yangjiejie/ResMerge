using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using System.Reflection;
using UnityEditor.Build;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
public class EasyUseEditorFuns
{
    //  public static string baseCustomTmpCache =  System.Environment.CurrentDirectory + "/../mySvn/" //+ //DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");


    public static string _baseVersion;
    public static string baseVersion
    {
        get
        {
            _baseVersion = EditorPrefs.GetString(nameof(baseVersion), "1");
            return _baseVersion;
        }
        set
        {
            if (_baseVersion != value)
            {
                _baseVersion = value;
                EditorPrefs.SetString(nameof(baseVersion), value);
            }
        }
    }


    public static string baseCustomTmpCache
    {
        get
        {
            return System.Environment.CurrentDirectory + "/../mySvn/" + baseVersion;
        }
    }



    public static string GetLinuxPath(string s)
    {
        return s.Replace("\\", "/");
    }
    /// <summary>
    /// 拷贝unity的文件从source到taget 并且也拷贝meta文件
    /// </summary>
    /// <param name="source 需要全路径"></param>
    /// <param name="target 需要全路径"></param>
    /// <param name="overrite"></param>
    public static void UnitySaveCopyFile(string source, string target, bool overrite = true)
    {
        source = GetLinuxPath(source);
        target = GetLinuxPath(target);
        var sourceFolder = System.IO.Path.GetDirectoryName(source);
        var targetFolder = System.IO.Path.GetDirectoryName(target);
        sourceFolder = Path.GetFullPath(sourceFolder);
        targetFolder = Path.GetFullPath(targetFolder);
        CreateDir(sourceFolder);
        CreateDir(targetFolder);

        var sourceName = System.IO.Path.GetFileName(source);
        var targetName = System.IO.Path.GetFileName(target);
        //拷贝源文件  
        System.IO.File.Copy(source, target, overrite);
        Regex.Match(source, @"\.([a-zA-Z0-9]+)$");
        //拷贝meta文件 
        var metaSourceFile = Path.Combine(sourceFolder, sourceName + ".meta");
        var metaTargeFile = Path.Combine(targetFolder, targetName + ".meta");
        System.IO.File.Copy(metaSourceFile, metaTargeFile, overrite);
    }
    /// <summary>
    /// 参数2 是否存档 
    /// </summary>
    /// <param name="resPath"></param>
    /// <param name="isSaveToLocal"></param>
    public static void DelEditorResFromDevice(string resPath, bool isSaveToLocal = true)
    {
        try
        {
            if (!isSaveToLocal)
                Debug.Log($"{resPath}已删除且不存档");
            else
            {
                //拷贝到相关目录 会包含meta 
                var source = Path.Combine(System.Environment.CurrentDirectory, resPath);
                var target = Path.Combine(baseCustomTmpCache, resPath);
                UnitySaveCopyFile(source, target, true);

                var metaFilePath = Path.Combine(baseCustomTmpCache, resPath + ".path");
                // 用额外的txt文件记录该文件的路径 方便回退
                WriteFileToTargetPath(metaFilePath, resPath);
            }
            AssetDatabase.DeleteAsset(resPath);
            AssetDatabase.DeleteAsset(resPath + ".meta");
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.ToString());
        }


    }
    public static string CalculateMD5(string filePath)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                // 计算文件的 MD5 哈希值
                byte[] hashBytes = md5.ComputeHash(stream);

                // 将字节数组转换为十六进制字符串
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2")); // "x2" 表示两位小写十六进制
                }

                return sb.ToString();
            }
        }
    }

    public static string GetScriptSymble(out NamedBuildTarget nameBt)
    {
        BuildTarget target = BuildTarget.iOS;
#if UNITY_ANDROID
		target = BuildTarget.Android;

#elif UNITY_WEBGL
		target = BuildTarget.WebGL;

#elif UNITY_STANDALONE
        target = BuildTarget.StandaloneWindows;
#endif
        string symbles = "";
        NamedBuildTarget nameBuildTarget;
        if (target == BuildTarget.iOS)
        {
            nameBuildTarget = NamedBuildTarget.iOS;
        }
        else if (target == BuildTarget.Android)
        {
            nameBuildTarget = NamedBuildTarget.Android;
        }
        else if (target == BuildTarget.WebGL)
        {
            nameBuildTarget = NamedBuildTarget.WebGL;
        }
        else
        {
            nameBuildTarget = NamedBuildTarget.Standalone;
        }
        symbles = PlayerSettings.GetScriptingDefineSymbols(nameBuildTarget);
        nameBt = nameBuildTarget;
        return symbles;
    }
    /// <summary>
    /// 是否具有预编译宏 xx
    /// </summary>
    /// <param name="symble"></param>
    /// <returns></returns>
    public static bool HasDebugSymble(string symble)
    {
        NamedBuildTarget nameBt;
        var symbles = GetScriptSymble(out nameBt);
        if (!string.IsNullOrEmpty(symbles))
        {
            var symble_arr = symbles.Split(";");
            var index = Array.IndexOf(symble_arr, symble);
            if (index >= 0)
            {
                return true;
            }
        }
        return false;
    }
    public static void ChangeToXXDefine(bool isDebug, string symble)
    {
        NamedBuildTarget nameBt;
        var symbles = GetScriptSymble(out nameBt);
        if (isDebug)
        {
            if (!string.IsNullOrEmpty(symbles))
            {
                var symble_arr = symbles.Split(";");
                if (Array.IndexOf(symble_arr, symble) < 0)
                {
                    symbles += $";{symble}";
                }
            }
            else
            {
                symbles = symble;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(symbles))
            {
                var symble_arr = symbles.Split(";");
                var index = Array.IndexOf(symble_arr, symble);
                if (index >= 0)
                {
                    if (index == symble_arr.Length - 1)
                    {
                        symbles = index > 0 ? symbles.Replace($";{symble}", "") : symbles.Replace(symble, "");
                    }
                    else
                    {
                        symbles = symbles.Replace($"{symble};", "");
                    }
                }

            }
        }
        PlayerSettings.SetScriptingDefineSymbols(nameBt, symbles);

        AssetDatabase.Refresh();

    }
    /// <summary>
    /// 任何情况下都需要能写入一个文件
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="contents"></param>
    public static void WriteFileToTargetPath(string filePath, string contents)
    {
        filePath = Path.GetFullPath(filePath);
        var folderName = System.IO.Path.GetDirectoryName(filePath);
        if (!Directory.Exists(folderName))
        {
            CreateDir(folderName);
        }
        File.WriteAllText(filePath, contents);
        var writeFilePath = CommonUtils.GetLinuxPath(baseCustomTmpCache);
        filePath = filePath.Replace(baseCustomTmpCache + "/", "");


        EditorLogWindow.WriteLog(filePath.Replace(".path", ""));
    }

    public static int CreateDir(string path)
    {

        if (Directory.Exists(path))
        {
            return 1;
        }
        path = GetLinuxPath(path);
        string tmp = path.Substring(0, path.LastIndexOf("/"));
        if (1 == CreateDir(tmp))
        {
            if (!(path.LastIndexOf(".") > 0))
            {
                Directory.CreateDirectory(path);
                return 1;
            }
        }
        return 0;
    }
    public static GameObject GetSelectObject()
    {
        return Selection.activeGameObject;
    }

    public static string GetNodePath(GameObject go)
    {
        var parent = go.transform.parent;
        return parent == null ? go.name : GetNodePath(parent.gameObject) + "/" + go.name;
    }
    public static List<GameObject> GetAllPrefabFormFolder(string curPath)
    {
        List<GameObject> goes = new List<GameObject>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { curPath });
        for (int i = 0; i < guids.Length; i++)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject go = (GameObject)AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));
            goes.Add(go);
        }
        return goes;
    }


    public static List<string> GetSelFoloderPath()
    {
        UnityEngine.Object[] selObjs = Selection.GetFiltered(
            typeof(UnityEngine.Object),
            SelectionMode.DeepAssets);
        return null;
    }
    public static List<GameObject> GetSelectPrefabs() // 也可以用于选中单个prefab
    {

        List<GameObject> tmp = new List<GameObject>();
        var gos = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
        foreach (var go in gos)
        {
            if (go as GameObject)
            {
                tmp.Add(go as GameObject);
            }
        }
        return tmp;
    }

    public static AudioClip GetSelectAudio()
    {
        var go = Selection.activeObject as AudioClip;
        return go;

    }

    public static List<UnityEngine.Material> GetSelectMaterials()
    {
        List<UnityEngine.Material> tmp = new List<UnityEngine.Material>();
        var gos = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
        foreach (var go in gos)
        {
            if (go as UnityEngine.Material)
            {
                tmp.Add(go as UnityEngine.Material);
            }
        }
        return tmp;
    }


    public static GameObject GetSelGameObjectInHierarchy()
    {
        return Selection.activeGameObject;
    }
    //获取选中对象路径 在展示面板上 
    public static string GetSelObjPathInHierarchy(GameObject go)
    {

        List<string> pathStack = new List<string>();
        while (go != null)
        {
            pathStack.Add(go.name);
            if (go.transform.parent != null)
            {
                go = go.transform.parent.gameObject;
            }
            else
            {
                break;
            }
        }
        string str = "";
        for (int i = pathStack.Count - 1; i >= 0; i--)
        {
            if (i == pathStack.Count - 1)
            {
                str += pathStack[i];
            }
            else
            {
                str += "/" + pathStack[i];
            }

        }
        return str;
    }

    /// <summary>
    /// 拷贝目录
    /// </summary>
    /// <param name="oldpath">源目录</param>
    /// <param name="newpath">新目录</param>
    public static void CopyDirectory(string oldpath, string newpath)
    {
        oldpath = oldpath.Replace("\\", "/");
        newpath = newpath.Replace("\\", "/");
        if (string.IsNullOrWhiteSpace(newpath)) return;
        var folderName = oldpath.Substring(oldpath.LastIndexOf("/", StringComparison.Ordinal) + 1);
        var desfolderdir = newpath + "/" + folderName;
        if (newpath.LastIndexOf("/", StringComparison.Ordinal) == (newpath.Length - 1))
        {
            desfolderdir = newpath + folderName;

        }
        var filenames = Directory.GetFileSystemEntries(oldpath);
        foreach (string file in filenames)
        {
            var file2 = file.Replace("\\", "/");
            if (Directory.Exists(file))
            {
                var currentdir = desfolderdir + "/" + file2.Substring(file2.LastIndexOf("/", StringComparison.Ordinal) + 1);
                if (!Directory.Exists(currentdir))
                {
                    Directory.CreateDirectory(currentdir);

                }
                CopyDirectory(file2, desfolderdir);
            }
            else
            {
                var srcfileName = file2.Substring(file2.LastIndexOf("/", StringComparison.Ordinal) + 1);
                srcfileName = desfolderdir + "/" + srcfileName;
                if (!Directory.Exists(desfolderdir))
                {
                    Directory.CreateDirectory(desfolderdir);
                }
                File.Copy(file, srcfileName, true);
            }
        }
    }

    /// <summary>
    /// 调用公开的静态方法
    /// </summary>
    /// <param name="type">类的类型</param>
    /// <param name="method">类里要调用的方法名</param>
    /// <param name="parameters">调用方法传入的参数</param>

    public static object InvokePublicStaticMethod(System.Type type, string method, params object[] parameters)
    {
        var methodInfo = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
        if (methodInfo == null)
        {
            UnityEngine.Debug.LogError($"{type.FullName} not found method : {method}");
            return null;
        }
        return methodInfo.Invoke(null, parameters);
    }

    /// <summary>
    /// 调用私有的静态方法
    /// </summary>
    /// <param name="type">类的类型</param>
    /// <param name="method">类里要调用的方法名</param>
    /// <param name="parameters">调用方法传入的参数</param>
    public static object InvokeNonPublicStaticMethod(System.Type type, string method, params object[] parameters)
    {
        var methodInfo = type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static);
        if (methodInfo == null)
        {
            UnityEngine.Debug.LogError($"{type.FullName} not found method : {method}");
            return null;
        }
        return methodInfo.Invoke(null, parameters);
    }

#if UNITY_EDITOR
    [MenuItem("Tools/EasyUseEditorTool/find脚本引用丢失")]
    static void FindMissing()
    {
        // 获取所有预设的 GUID
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        // 遍历所有预设
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                Debug.Log($"无法加载预设: {path}");
                continue;
            }

            // 检查预设中是否有丢失的脚本
            List<Component> missingComponents = new List<Component>();
            CheckForMissingScripts(prefab, missingComponents);

            if (missingComponents.Count > 0)
            {
                Debug.Log($"预设 {path} 中有 {missingComponents.Count} 个丢失的脚本:");
                foreach (var component in missingComponents)
                {
                    if (component != null)
                        Debug.Log($"- 丢失的脚本: {component.name}");
                }
            }
        }

        Debug.Log("查找完成！");
    }
    private static void CheckForMissingScripts(GameObject gameObject, List<Component> missingComponents)
    {
        // 检查当前 GameObject 的所有组件
        Component[] components = gameObject.GetComponents<Component>();
        foreach (var component in components)
        {
            if (component == null)
            {
                missingComponents.Add(component);
            }
        }

        // 递归检查子对象
        foreach (Transform child in gameObject.transform)
        {
            CheckForMissingScripts(child.gameObject, missingComponents);
        }
    }
#endif

}


