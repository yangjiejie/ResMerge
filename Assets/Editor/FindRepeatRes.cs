using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AdjustSdk;
using Cysharp.Threading.Tasks.Triggers;
using UnityEditor;

using UnityEngine;


public class FindRepeatRes : EditorWindow
{
    private string inputWindowName;
    private int inputWindowName_hashCode;

    private string guidStr;
    private string guidToAssetPath = "路径：";

    private string sourceUUid; 
    private string targetUUid;
    private List<string> beReplaceMainRes = new(); // 被替换的主体资源（prefab mat）
    public class MergedTextureInfo
    {
        public string md5Code;
        public List<SubResInfo> subInfos = new List<SubResInfo>();
        public List<ComboMainResInfo> mainResList = new();
    }

    public class SubResInfo
    {
        public string resName;
        public string resNameLittle;
        public string resPath;
        public string md5Code;
        public string uuid;
        public SubResInfo Init(string pathName)
        {
            this.resName = Path.GetFileNameWithoutExtension(pathName);
            this.resNameLittle = resName.ToLower();
            this.resPath = pathName;
            this.uuid = AssetDatabase.AssetPathToGUID(pathName);
            this.md5Code = EasyUseEditorFuns.CalculateMD5(pathName);
            return this;
        }
        public void DelFromDevice()
        {
            try
            {
                EasyUseEditorFuns.DelEditorResFromDevice(resPath, true);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.ToString());
            }


        }
    }
    /// <summary>
    /// 某些资源同属于一个ab包。比如同在一个功能目录下 比如GameFruitUI/prefab 
    /// </summary>
    public class ComboMainResInfo
    {
        public List<MainResInfo> editorResInfos = null;
    }
    /// <summary>
    /// 主资源 类似prefab Material 这些资源 他们会有很多资源的引用 
    /// </summary>
    public class MainResInfo
    {
        public string resName;
        public string resNameLittle;
        public string resPath;
        public string md5Code;
        public string uuid;
        public List<SubResInfo> childs;
        public void Init(string pathName)
        {
            this.resName = Path.GetFileNameWithoutExtension(pathName);
            this.resNameLittle = resName.ToLower();
            this.resPath = pathName;
            this.uuid = AssetDatabase.AssetPathToGUID(pathName);
            this.md5Code = EasyUseEditorFuns.CalculateMD5(pathName);
        }
        public List<SubResInfo> AddDependency(string[] pathNameArray)
        {
            foreach (string pathName in pathNameArray)
            {
                var info = new SubResInfo();
                info.Init(pathName);
                if (childs == null) childs = new List<SubResInfo>();
                childs.Add(info);
            }
            return childs;
        }
    }

    public static List<MainResInfo> allMainResList = new List<MainResInfo>();
    public static List<SubResInfo> allSubInfoLists = new List<SubResInfo>();
    public static List<SubResInfo> allCommonSubInfoList = new();

    public static Dictionary<string, List<MainResInfo>> likeSpriteResDepandence = new Dictionary<string, List<MainResInfo>>();

    public static Dictionary<string, List<ComboMainResInfo>> spriteBeDepandence = new Dictionary<string, List<ComboMainResInfo>>();

    public static Dictionary<SubResInfo, bool> mergeedSpriteBeDepandence = new();

    public static Dictionary<string, List<ComboMainResInfo>> needDelTextureInfos = new Dictionary<string, List<ComboMainResInfo>>();


    public static string CommonImage = "Assets/Art/gameCommon/image";



    
    public Action closeAction = null;


  
    public void OnDestroy()
    {
       
        closeAction?.Invoke();
        closeAction = null;
    }
    public UnityEngine.Object commonFoloderObj;

    public static UnityEngine.Object checkFolder;
    public static List<UnityEngine.Object> checkFolders =  new List<UnityEngine.Object>();
    public static List<UnityEngine.Object> checkChineseFolders =  new List<UnityEngine.Object>();
    public static List<string> selectFolderPaths = new();
    
    int selectPanel = 0;

    string[] namesPanel = new string[]
    {
        "资源合并",
        "find脚本引用丢失",
        "小工具",
    };

    void DrawResMergeUI()
    {
    
        GUILayout.BeginHorizontal();


        //  EditorGUILayout.LabelField("");
        EditorGUI.BeginChangeCheck();

        GUIStyle customStyle = new GUIStyle(EditorStyles.objectField);
        customStyle.margin = new RectOffset(0, 0, 0, 0); // 调整边距
        GUILayout.Label("common文件夹", GUILayout.Width(90)); // 描述文本
        if (commonFoloderObj == null && !string.IsNullOrEmpty(CommonImage))
        {
            commonFoloderObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(CommonImage);
        }
        commonFoloderObj = EditorGUILayout.ObjectField(commonFoloderObj,
            typeof(DefaultAsset), false, GUILayout.Width(100));
        if (EditorGUI.EndChangeCheck())
        {
            if (commonFoloderObj != null)
            {
                CommonImage = AssetDatabase.GetAssetPath(commonFoloderObj);
            }
        }
        EditorGUI.BeginChangeCheck();
        CommonImage = EditorGUILayout.TextField("", CommonImage);
        if (EditorGUI.EndChangeCheck())
        {
            if (commonFoloderObj == null || AssetDatabase.GetAssetPath(commonFoloderObj) != CommonImage)
            {
                commonFoloderObj = (UnityEngine.Object)AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(CommonImage);
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginVertical();
        GetSelectArtFolders();
        var tmpGuids = AssetDatabase.FindAssets("t:prefab t:Material", selectFolderPaths.ToArray());
        var tmpSubGuids = AssetDatabase.FindAssets("t:Sprite", selectFolderPaths.ToArray());
        GUILayout.Label("检查的目录：主资源" + tmpGuids.Length + "子资源" + tmpSubGuids.Length, GUILayout.Width(340)); // 描述文本

        GUILayout.BeginVertical();
        for (int i = 0; i < checkFolders.Count; i++)
        {
            GUILayout.BeginHorizontal();
            checkFolders[i] = EditorGUILayout.ObjectField(checkFolders[i],
                typeof(DefaultAsset), false, GUILayout.Width(200));
            GUILayout.TextField(selectFolderPaths[i]);
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();


        GUILayout.BeginVertical();
        if (GUILayout.Button("1清理无任何引用关联的资源", GUILayout.Height(50)))
        {
            
            allAssetPaths.Clear();
            dependenciesMap.Clear();
            if (checkFolders == null || checkFolders.Count == 0) return;
            List<string> paths = new();
            foreach (var item in checkFolders)
            {
                var path = AssetDatabase.GetAssetPath(item);
                if ( Regex.IsMatch(path, @"/icon/"))
                {
                    //Debug.LogError("目录不正确不能含有icon");
                    continue;
                }
                if (File.Exists(path))
                {
                    path = System.IO.Path.GetDirectoryName(path);
                }
                var folders = Directory.GetDirectories(path);
                var result = folders.FirstOrDefault((xx) => Regex.IsMatch(xx, "prefab"));
                if (string.IsNullOrEmpty(result))
                {
                    continue;
                }
                if (paths.Count == 0 || !paths.Contains(path))
                    paths.Add(path);
            }

            allAssetPaths = AssetDatabase.FindAssets("t:prefab t:Material", new string[] {"Assets/" }).Select((xx) => AssetDatabase.GUIDToAssetPath(xx)).ToList<string>();

            ClearUnUsedTextures(paths);
        }
        GUILayout.Space(10);
        if (GUILayout.Button("2清理重复资源", GUILayout.Height(50)))
        {
            
            CleanRepeatRes();
        }
        GUILayout.Space(10);
        if (GUILayout.Button("3回滚清理的资源", GUILayout.Height(50)))
        {
            EditorLogWindow.ClearLog();
            ReverseLocalSvn();

        }
        GUILayout.Space(10);
        if (GUILayout.Button("4打开日志", GUILayout.Height(50)))
        {
            if (EditorLogWindow.GetInstance() == null)
            {
                this.closeAction += EditorLogWindow.CloseWindow;
                EditorLogWindow.OpenWindow(this);
                EditorLogWindow.ClearLog();
            }
        }
        GUILayout.Space(10);
        if (GUILayout.Button("5清理日志", GUILayout.Height(50)))
        {
            EditorLogWindow.instance?.Focus();
            EditorLogWindow.ClearLog();
        }
        GUILayout.Space(10);
        if (GUILayout.Button("6清理多余字体", GUILayout.Height(50)))
        {
            
            var selFolders = GetSelectArtFolders();

            allAssetPaths = AssetDatabase.FindAssets("t:prefab t:Material", new string[] { "Assets" }).Select((xx) => AssetDatabase.GUIDToAssetPath(xx)).ToList<string>();

            var allFonts = AssetDatabase.FindAssets("t:font", new string[] { "Assets" }).Select((xx) =>
            AssetDatabase.GUIDToAssetPath(xx)).ToList<string>();

            List<string> hasRefenceFont = new();

            foreach (var item in allAssetPaths)
            {
                if (!item.EndsWith("prefab") && !item.EndsWith(".Material"))
                {
                    continue;
                }
                if (allFonts.Contains(item)) continue;
                var dps = AssetDatabase.GetDependencies(item);
                var intersection = allFonts.Intersect(dps);
                if (allFonts.Intersect(dps).Any())
                {
                    foreach (var it in intersection)
                    {
                        if (hasRefenceFont.Count == 0 || !hasRefenceFont.Contains(it))
                            hasRefenceFont.Add(it);
                    }
                }
            }
            allFonts.RemoveAll(item => hasRefenceFont.Contains(item));

            foreach (var item in allFonts)
            {
                EditorLogWindow.WriteLog(item);
            }
            EditorLogWindow.instance?.Focus();

            // 获取字体的路径

            foreach (var itemFont in allFonts)
            {
                // 获取字体依赖的资源（材质、贴图等）
                string[] dependencies = AssetDatabase.GetDependencies(itemFont, true);

                // 删除字体及其依赖资源
                foreach (string dependency in dependencies)
                {
                    if (!dependency.EndsWith(".prefab"))
                    {
                        var source = Path.Combine(System.Environment.CurrentDirectory, dependency);
                        if (!File.Exists(source))
                        {
                            continue;
                        }
                        EasyUseEditorFuns.UnitySaveCopyFile(
                           source,
                           Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, dependency),
                            true);

                        var metaFilePath = Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, dependency + ".path");
                        // 用额外的txt文件记录该文件的路径 方便回退
                        EasyUseEditorFuns.WriteFileToTargetPath(metaFilePath, dependency);
                        AssetDatabase.DeleteAsset(dependency);
                    }
                }

                var sourceFont = Path.Combine(System.Environment.CurrentDirectory, itemFont);
                if (!File.Exists(sourceFont))
                {
                    continue;
                }
                EasyUseEditorFuns.UnitySaveCopyFile(
                   sourceFont,
                   Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, itemFont),
                    true);

                var metaFilePath2 = Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, itemFont + ".path");
                // 用额外的txt文件记录该文件的路径 方便回退
                EasyUseEditorFuns.WriteFileToTargetPath(metaFilePath2, itemFont);
                AssetDatabase.DeleteAsset(itemFont);

            }
            Debug.Log("处理完毕！");
        }
        GUILayout.EndVertical();
    }
    public List<string> missingPrefab;
    public void OnGUI()
    {
        GUILayout.BeginVertical();
        selectPanel = GUILayout.Toolbar(selectPanel, namesPanel);  //参数1整数 参数2字符串数组
        GUILayout.EndVertical();

        GUILayout.BeginHorizontal();


        EasyUseEditorFuns.baseVersion = EditorGUILayout.TextField("版本号：", EasyUseEditorFuns.baseVersion);

        GUILayout.EndHorizontal();

        if (selectPanel == 0)
        {
            DrawResMergeUI();
        }
        else if(selectPanel == 1)
        {
            if(GUILayout.Button("1查找missing的预设",GUILayout.Height(50)))
            {
                missingPrefab  = FindMissing.FindMissingScriptsInProject();
            }
            GUILayout.Space(10);
            if (GUILayout.Button("2删除missing的预设",GUILayout.Height(50)))
            {
                FindMissing.CleanMissingScriptsInProject(missingPrefab);
            }
            GUILayout.Space(10);
            if (GUILayout.Button("3回滚清理的资源", GUILayout.Height(50)))
            {
                EditorLogWindow.ClearLog();
                ReverseLocalSvn();

            }

        }
        else if(selectPanel == 2)
        {
            
            inputWindowName = EditorGUILayout.TextField("窗口名:", inputWindowName,GUILayout.Width(355));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("hash code =", GUILayout.Width(150));
            inputWindowName_hashCode = Animator.StringToHash(inputWindowName);
            EditorGUI.BeginChangeCheck();
            inputWindowName_hashCode = EditorGUILayout.IntField(inputWindowName_hashCode,GUILayout.Width(200));
            
            if(GUILayout.Button("拷贝hash"))
            {
                GUIUtility.systemCopyBuffer = Animator.StringToHash(inputWindowName).ToString();
            }
            EditorGUILayout.EndHorizontal();
            GetSelectFolders();
            GUILayout.BeginVertical();
            for (int i = 0; i < checkChineseFolders.Count; i++)
            {
                GUILayout.BeginHorizontal();
                checkChineseFolders[i] = EditorGUILayout.ObjectField(checkChineseFolders[i],
                    typeof(DefaultAsset), false, GUILayout.Width(200));
               
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("处理中文字符"))
            {
                List<string> fileList = new();
                foreach(var item  in selectFolderPaths)
                {
                   
                    var allFiles =  System.IO.Directory.GetFiles(CommonUtils.GetLinuxPath(System.Environment.CurrentDirectory + "/" + item), "*.*",SearchOption.AllDirectories);
                    allFiles = allFiles.Where((xx) => !xx.EndsWith(".meta") && (xx.EndsWith(".png") ||   xx.EndsWith(".jpg"))).ToArray();

                    foreach(var li in allFiles)
                    {
                        if (!fileList.Contains(li))
                        {
                            fileList.Add(li);
                        }
                    }
                }
                try
                {
                    foreach (var file in fileList)
                    {
                        string pattern = @"[\u4e00-\u9fff]";
                        if(Regex.IsMatch(file,pattern))
                        {
                            var file1 =  file.Substring(file.IndexOf("Assets/"));
                            var newFile = Regex.Replace(file, pattern, "");
                            var file2 = newFile.Substring(file.IndexOf("Assets/"));
                            var ss = file;
                            var tt = Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, file.Substring(file.IndexOf("Assets/")));


                            EasyUseEditorFuns.UnitySaveCopyFile(file, tt, true);
                            var metaFilePath = Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, file1 + ".path");
                            // 用额外的txt文件记录该文件的路径 方便回退
                            EasyUseEditorFuns.WriteFileToTargetPath(metaFilePath, file1);
                            EditorLogWindow.WriteLog(file);
                        }
                       
                    }
                    AssetDatabase.Refresh();

                    foreach (var file in fileList)
                    {
                        string pattern = @"[\u4e00-\u9fff]";
                        if (Regex.IsMatch(file, pattern))
                        {
                            var file1 = file.Substring(file.IndexOf("Assets/"));
                            var newFile = Regex.Replace(file, pattern, "");
                            var file2 = newFile.Substring(file.IndexOf("Assets/"));
                            if (File.Exists(newFile))
                            {
                                Debug.LogError("需要清理资源" + newFile);
                                AssetDatabase.DeleteAsset(newFile.Substring( newFile.IndexOf("Assets/")));
                            }
                            else
                            {
                                file2 = Path.GetFileName(file2);
                                var value = AssetDatabase.RenameAsset(file1, file2);
                                if (!string.IsNullOrEmpty(value))
                                {
                                    Debug.LogError(value + "重命名错误" + file1);
                                }
                            }


                        }

                    }
                    AssetDatabase.Refresh();
                }
                catch (Exception  e)
                {
                    Debug.LogError(e.ToString());
                }
                
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();

            EditorGUI.BeginChangeCheck();

            guidStr = EditorGUILayout.TextField("输入guid",guidStr);

            EditorGUILayout.LabelField(guidToAssetPath);
            if (EditorGUI.EndChangeCheck())
            {
                guidToAssetPath = AssetDatabase.GUIDToAssetPath(guidStr);
                if (string.IsNullOrEmpty(guidToAssetPath))
                {
                    guidToAssetPath = "路径：no asset!";
                }
                else
                {
                    guidToAssetPath = "路径："+ guidToAssetPath;
                }

            }
            sourceUUid = EditorGUILayout.TextField("被替换的uuid", sourceUUid);
            targetUUid = EditorGUILayout.TextField("替换的uuid", targetUUid);

            if (GUILayout.Button("查找uuid的主体资源"))
            {
                EditorLogWindow.ClearLog();
                if (allAssetPaths.Count == 0)
                {
                    allAssetPaths = AssetDatabase.FindAssets("t:prefab t:Material", new string[] { "Assets/" }).Select((xx) => AssetDatabase.GUIDToAssetPath(xx)).ToList<string>();
                }
                EditorUtility.ClearProgressBar();
                int index = 0; 
                foreach (var item in allAssetPaths)
                {
                    EditorUtility.DisplayProgressBar("遍历所有主体资源", string.Format("{0}/{1}", index, allAssetPaths.Count), 1.0f * index++ / allAssetPaths.Count);
                    var fullPath = CommonUtils.GetLinuxPath(Path.Combine(System.Environment.CurrentDirectory, item));
                    var allContent = File.ReadAllText(fullPath);

                    if(Regex.IsMatch(allContent,sourceUUid))
                    {
                        beReplaceMainRes.Add(fullPath);
                        EditorLogWindow.WriteLog(fullPath);
                    }
                }
                EditorUtility.ClearProgressBar();

            }
            if (GUILayout.Button("替换uuid"))
            {
                foreach(var item in beReplaceMainRes)
                {
                    var allContent = File.ReadAllText(item);
                    var newContent = Regex.Replace(allContent, sourceUUid, targetUUid);
                    File.WriteAllText(item, newContent);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }



            GUILayout.EndVertical();
        }

        if (GUILayout.Button("跳转到版本管理"))
        {
            EditorUtility.RevealInFinder(EasyUseEditorFuns.baseCustomTmpCache);
        }

    }
    private static void ClearUnUsedTextures(List<string> paths)
    {
        // 获取所有资源
        List<string> unusedAssets = new List<string>();
        var allTextures =  AssetDatabase.FindAssets("t:Sprite", paths.ToArray()).Select
            ((xx)=>AssetDatabase.GUIDToAssetPath(xx)).ToList<string>();

        int index = 0;
        foreach (string assetPath in allTextures)
        {
            
            EditorUtility.DisplayProgressBar(string.Format("Processing{0}/{1}", index, allTextures.Count), "", 1.0f* index / allTextures.Count);
            if (!Regex.Match(assetPath,@"/image/").Success)
            {
                index++;
                continue;
            }
            
            // 检查资源是否被引用
            if (!IsAssetUsed(assetPath))
            {
                unusedAssets.Add(assetPath);
            }
            index++;
        }
        EditorUtility.ClearProgressBar();
        // 删除未使用的资源
        if (unusedAssets.Count > 0)
        {
            foreach (string path in unusedAssets)
            {
                Debug.Log("Deleting unused asset: " + path);

                var ss = Path.Combine(System.Environment.CurrentDirectory, path);
                var tt = Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, path);
                EasyUseEditorFuns.UnitySaveCopyFile(ss, tt, true);


                var metaFilePath = Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, path + ".path");
                // 用额外的txt文件记录该文件的路径 方便回退
                EasyUseEditorFuns.WriteFileToTargetPath(metaFilePath, path);

                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.Refresh();
            Debug.Log("Deleted " + unusedAssets.Count + " unused assets.");
        }
        else
        {
            Debug.Log("No unused assets found.");
        }

    }
    public static List<string> allAssetPaths = new();
    public static Dictionary<string, List<string>> dependenciesMap = new();
    private static bool IsAssetUsed(string assetPath)
    {

        foreach(var item in dependenciesMap)
        {
            var path = item.Key;
            var tmpDenpendencies = item.Value;
            if(tmpDenpendencies.Contains(assetPath))
            {
                return true;
            }
        }
        // 获取所有场景和预制件
        foreach (string path in allAssetPaths)
        {
            // 加载资源
            if(dependenciesMap.ContainsKey(path))
            {
                continue;
            }
            var dependencies = AssetDatabase.GetDependencies(path);
            if(!dependenciesMap.ContainsKey(path))
            {
                dependenciesMap.Add(path, new List<string>());
            }

           
            foreach (var obj in dependencies)
            {
                if(obj != null && !dependenciesMap[path].Contains(obj))
                {
                    dependenciesMap[path].Add(obj);
                }
                if (obj != null && obj == assetPath)
                {
                    return true;
                }
            }
        }

        return false;
    }
    public static void ClearUnUsedTexturesImp(string assetPath, List<string> allMainRes)
    {
        EditorSettings.serializationMode = SerializationMode.ForceText;

        string path = assetPath;
        if (!string.IsNullOrEmpty(path))
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            int startIndex = 0;
            if (allMainRes.Count > 0)
            {
                while (startIndex < allMainRes.Count)
                {
                    string file = allMainRes[startIndex];



                    if (Regex.IsMatch(File.ReadAllText(file), guid))
                    {
                        var startCount = file.IndexOf("/Assets");
                        var newFilePath = file.Substring(startCount + 1);
                    }
                    else // 无任何引用 需要清理 
                    {
                        Debug.Log("无任何引用的资源" + path);
                    }

                    startIndex++;
                    if (startIndex >= allMainRes.Count)
                    {
                        EditorUtility.ClearProgressBar();
                        EditorApplication.update = null;
                        startIndex = 0;
                        Debug.Log("<color=#006400>查找结束" + assetPath + "</color>");
                    }


                    else
                        Debug.Log("<color=#006400>查找结束" + assetPath + "</color>");
                }

            }

        }
    }



    public static void ReverseLocalSvn()
    {
        var root = System.Environment.CurrentDirectory + "/../mySvn/" + EasyUseEditorFuns.baseVersion;
        var allFiles = Directory.GetFiles(root, "*.path", SearchOption.AllDirectories);
        foreach (var file in allFiles)
        {
            var reallyFilePath = file.Replace(".path", "");
            var resPath = File.ReadAllText(file);
            var targetFilePath = Path.Combine(System.Environment.CurrentDirectory, resPath);
            if (File.Exists(reallyFilePath))
            {
                EasyUseEditorFuns.UnitySaveCopyFile(reallyFilePath, targetFilePath);
            }
            else
            {
                if (File.Exists(targetFilePath))
                {
                    File.Delete(targetFilePath);
                    File.Delete(targetFilePath + ".meta");
                }


            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AssetDatabase.Refresh();
    }


    public static void CollectAllCommonRes()
    {
        var guids = AssetDatabase.FindAssets("t:Sprite", new string[] { CommonImage });
        var list = guids.Select((xx) => AssetDatabase.GUIDToAssetPath(xx)).ToList<string>();
        for (int i = 0; i < list.Count; i++)
        {
            var info = new SubResInfo();
            info.Init(list[i]);
            allCommonSubInfoList.Add(info);
        }
    }
    private static void UpdateCommonRes(string resPath)
    {
        allCommonSubInfoList.Add(new SubResInfo().Init(resPath));
    }
    public static SubResInfo GetCommonRes(string md5Code)
    {
        if (allCommonSubInfoList.Count == 0) return null;
        var rst = allCommonSubInfoList.Find((xx) => xx.md5Code == md5Code);
        if (rst != null)
        {
            return rst;
        }
        return null;
    }
    static List<string> GetSelectFolders()
    {
        if (Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0)
        {
            checkChineseFolders.Clear();
            selectFolderPaths?.Clear();
            foreach (var guid in Selection.assetGUIDs)
            {
                var tmpPath = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.File.Exists(tmpPath))
                {
                    tmpPath = System.IO.Path.GetDirectoryName(tmpPath);
                }

                if (selectFolderPaths.Count == 0 || !selectFolderPaths.Contains(tmpPath))
                {
                    selectFolderPaths.Add(tmpPath);
                    var assetObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(tmpPath);
                    checkChineseFolders.Add(assetObj);
                }
            }
            return selectFolderPaths;
        }
        else
        {
            checkChineseFolders.Clear();
            selectFolderPaths?.Clear();
            return selectFolderPaths;
        }
    }


    static List<string> GetSelectArtFolders()
    {
        if (Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0)
        {
            checkFolders.Clear();
            selectFolderPaths?.Clear();
            foreach (var guid in Selection.assetGUIDs)
            {
                var tmpPath = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.File.Exists(tmpPath))
                {
                    tmpPath = System.IO.Path.GetDirectoryName(tmpPath);
                }

                if (selectFolderPaths.Count == 0 || !selectFolderPaths.Contains(tmpPath))
                {
                    selectFolderPaths.Add(tmpPath);
                    var assetObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(tmpPath);
                    checkFolders.Add(assetObj);
                }
            }
            return selectFolderPaths;
        }
        else
        {
            checkFolders.Clear();
            selectFolderPaths?.Clear();
            selectFolderPaths.Add("Assets/Art");
            var assetObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(selectFolderPaths[0]);
            checkFolders.Add(assetObj);
            return selectFolderPaths;
        }
       
    }

    public static void CleanRepeatRes()
    {
        allMainResList?.Clear();
        allSubInfoLists?.Clear();
        allCommonSubInfoList?.Clear();
        spriteBeDepandence?.Clear();
        likeSpriteResDepandence?.Clear();
        mergeedSpriteBeDepandence?.Clear();
        needDelTextureInfos?.Clear();

        CollectAllCommonRes();

        var checkFolderPath = GetSelectArtFolders();
        var allRes = AssetDatabase.FindAssets("t:prefab t:Material", checkFolderPath.ToArray());
        allRes = allRes.Select((xx) => xx = AssetDatabase.GUIDToAssetPath(xx)).ToArray<string>();
        int index = 0;
        EditorUtility.ClearProgressBar();
        foreach (var pathName in allRes)
        {
            EditorUtility.DisplayProgressBar("阶段1收集主资源", string.Format("{0}/{1}", index, allRes.Length),1.0f* index++ / allRes.Length);
            var info = new MainResInfo();
            info.Init(pathName);
            //剔除自己和cs引用 获得纯资源引用 
            var allDps = AssetDatabase.GetDependencies(pathName).Where((xx) => xx != pathName && !xx.EndsWith(".cs")).ToArray<string>();
            if (allDps.Length > 0)
            {
                var childsInfo = info.AddDependency(allDps);

                for (int i = 0; i < childsInfo.Count; i++)
                {
                    var findRst = allSubInfoLists.Find((xx) => xx.resPath == childsInfo[i].resPath);
                    if (findRst == null)
                    {
                        allSubInfoLists.Add(childsInfo[i]);
                    }
                }

                allMainResList.Add(info);
            }
        }
        EditorUtility.ClearProgressBar();

        index = 0;
        foreach (var mainRes in allMainResList)
        {
            EditorUtility.DisplayProgressBar("阶段2收集子资源", string.Format("{0}/{1}", index, allMainResList.Count), 1.0f * index++ / allMainResList.Count);
            foreach (var subRes in mainRes.childs)
            {
                if (!likeSpriteResDepandence.ContainsKey(subRes.resPath))
                {
                    if(Regex.IsMatch(subRes.resPath,@"/icon/"))
                    {
                        continue;
                    }
                    likeSpriteResDepandence.Add(subRes.resPath, new List<MainResInfo>());
                }
                likeSpriteResDepandence[subRes.resPath].Add(mainRes);
            }
        }
        EditorUtility.ClearProgressBar();

        index = 0;


        //由于 likeSpriteResDepandecen 的list数组中 可能某几个项都是一个功能目录 也就是
        //一个ab包 所以这里还需要再封装依次 

        foreach (var subRes in likeSpriteResDepandence)
        {
            EditorUtility.DisplayProgressBar("阶段3子资源处理", string.Format("{0}/{1}", index, likeSpriteResDepandence.Count), 1.0f * index++ / likeSpriteResDepandence.Count);
            if (!spriteBeDepandence.ContainsKey(subRes.Key))
            {
                spriteBeDepandence.Add(subRes.Key, new List<ComboMainResInfo>());
            }
            Dictionary<string, List<MainResInfo>> map = new Dictionary<string, List<MainResInfo>>();
            foreach (var mainRes in subRes.Value)
            {
                var folderName = Path.GetDirectoryName(mainRes.resPath);
                if (!map.ContainsKey(folderName))
                {
                    map.Add(folderName, new List<MainResInfo>());
                }
                map[folderName].Add(mainRes);
            }
            foreach (var item in map)
            {
                //对于依赖的资源如果他们来自于相同的逻辑目录，也就是相同的ab包 需要合并在一起
                var info = new ComboMainResInfo();
                info.editorResInfos = item.Value;
                spriteBeDepandence[subRes.Key].Add(info);
            }
        }
        EditorUtility.ClearProgressBar();

        index = 0;
        // spriteBeDepandence  是子资源 map 一堆主资源的 映射
        // key是assetPath名 
        foreach (var item in spriteBeDepandence)
        {

            var findRst = GetTextureInfo(item.Key);
            if (findRst == null) Debug.LogError("运行错误！");

            var listCombo = item.Value;
            if (mergeedSpriteBeDepandence.Count == 0)
            {
                mergeedSpriteBeDepandence.Add(findRst, true);
            }
            else
            {
                //已合并的资源中是否有md5相同的资源 有则冲突 
                var mergedObj = mergeedSpriteBeDepandence.FirstOrDefault((xx) => xx.Key.md5Code == findRst.md5Code);
                if (mergedObj.Key == null)
                {
                    mergeedSpriteBeDepandence.Add(findRst, true);
                }
                else
                {
                    //如果findRst 包括了common，common是不能被剔除的 .
                    if (findRst.resPath.Contains(CommonImage))
                    {
                        mergeedSpriteBeDepandence.Remove(mergedObj.Key);
                        var unNormalSprite = spriteBeDepandence.FirstOrDefault((yy) => yy.Key == mergedObj.Key.resPath);
                        needDelTextureInfos.Add(unNormalSprite.Key, unNormalSprite.Value);
                        mergeedSpriteBeDepandence.Add(findRst, true);
                    }
                    else
                    {
                        needDelTextureInfos.Add(item.Key, item.Value);

                    }
                }
            }
        }
        DoReplace();
    }
    /// <summary>
    /// 执行资源的清理工作 
    /// </summary>
    static void DoReplace()
    {
        //先copy到local版本
        int index = 0; 
        foreach (var item in needDelTextureInfos)
        {
            EditorUtility.DisplayProgressBar("阶段4存档", string.Format("{0}/{1}", index, likeSpriteResDepandence.Count), 1.0f * index++ / needDelTextureInfos.Count);
            // 先存档 本地版本管理 
            for (int i = 0; i < item.Value.Count; i++)
            {
                for (int j = 0; j < item.Value[i].editorResInfos.Count; j++)
                {
                    var suorcePath = Path.Combine(System.Environment.CurrentDirectory, item.Value[i].editorResInfos[j].resPath);
                    var targetPath = Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, item.Value[i].editorResInfos[j].resPath);
                    EasyUseEditorFuns.UnitySaveCopyFile(suorcePath, targetPath, true);


                    var metaFilePath = Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, item.Value[i].editorResInfos[j].resPath + ".path");
                    // 用额外的txt文件记录该文件的路径 方便回退
                    EasyUseEditorFuns.WriteFileToTargetPath(metaFilePath, item.Value[i].editorResInfos[j].resPath);
                }
            }
        }
        index = 0;
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh(); // 刷新unity DB
        foreach (var item in needDelTextureInfos)
        {
            EditorUtility.DisplayProgressBar("阶段5替换资源", string.Format("{0}/{1}", index, likeSpriteResDepandence.Count), 1.0f * index++ / needDelTextureInfos.Count);
            var needDelTextureRes = GetTextureInfo(item.Key);

            var targetRes = mergeedSpriteBeDepandence.FirstOrDefault((xx) => xx.Key.md5Code == needDelTextureRes.md5Code);

            if (needDelTextureRes.resPath == targetRes.Key.resPath)
            {
                Debug.LogError("逻辑错误");
                continue;
            }

            var targetUUid = targetRes.Key.uuid;

            var commonRes = GetCommonRes(needDelTextureRes.md5Code);

            if (commonRes == null)
            {
                //将已经被设为合并的资源放到common中 ，这个资源被依赖的主资源不用更新 
                var source = Path.Combine(System.Environment.CurrentDirectory, targetRes.Key.resPath);
                var fileName = Path.GetFileName(targetRes.Key.resPath);
                var target = Path.Combine(System.Environment.CurrentDirectory, CommonImage, fileName);
                EasyUseEditorFuns.UnitySaveCopyFile(source, target, true);

                UpdateCommonRes(target);
                //这里我们只有.path文件过去 没有实际的文件拷贝过去,目的就是为了做回滚
                var metaFilePath = Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, targetRes.Key.resPath + ".path");
                // 用额外的txt文件记录该文件的路径 方便回退
                EasyUseEditorFuns.WriteFileToTargetPath(metaFilePath, targetRes.Key.resPath);
                // end 
                targetRes.Key.DelFromDevice();

            }
            else
            {
                var commonBeDependance = spriteBeDepandence.FirstOrDefault((xx) => xx.Key == commonRes.resPath);
                for (int i = 0; commonBeDependance.Key != null && i < commonBeDependance.Value.Count; i++)
                {
                    for (int j = 0; j < commonBeDependance.Value[i].editorResInfos.Count; j++)
                    {
                        EditorResReplaceByUuid.ReplaceUUID(commonBeDependance.Value[i].editorResInfos[j].resPath, commonRes.uuid, targetUUid);

                    }
                }
            }




            for (int i = 0; i < item.Value.Count; i++)
            {
                for (int j = 0; j < item.Value[i].editorResInfos.Count; j++)
                {
                    EditorResReplaceByUuid.ReplaceUUID(item.Value[i].editorResInfos[j].resPath, needDelTextureRes.uuid, targetUUid);

                }
            }

            needDelTextureRes.DelFromDevice(); // 从磁盘上删除 

            Debug.Log("需要删除" + item.Key);
        }

        AssetDatabase.Refresh(); // 刷新unity DB
    }

    static SubResInfo GetTextureInfo(string assetPath)
    {
        return allSubInfoLists.Find((xx) => assetPath == xx.resPath);
    }
}