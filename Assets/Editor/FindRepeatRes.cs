using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Unity.Android.Types;
using UnityEditor;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;


public class FindRepeatRes
{


    
    public class MergedTextureInfo
    {
        public string md5Code;
        public List<SubResInfo> subInfos = new List<SubResInfo>();
        public List<ComboMainResInfo> mainResList =new();
    }

    public class SubResInfo
    {
        public string resName;
        public string resNameLittle;
        public string resPath;
        public string md5Code;
        public string uuid;
        public void Init(string pathName)
        {
            this.resName = Path.GetFileNameWithoutExtension(pathName);
            this.resNameLittle = resName.ToLower();
            this.resPath = pathName;
            this.uuid = AssetDatabase.AssetPathToGUID(pathName);
            this.md5Code = EasyUseEditorFuns.CalculateMD5(pathName);
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

    public static Dictionary<string, List<MainResInfo>> likeSpriteResDepandence = new Dictionary<string, List<MainResInfo>>();

    public static Dictionary<string, List<ComboMainResInfo>> spriteBeDepandence = new Dictionary<string, List<ComboMainResInfo>>();

    public static Dictionary<SubResInfo, SubResInfo> mergeedSpriteBeDepandence = new();

    public static Dictionary<string, List<ComboMainResInfo>> needDelTextureInfos = new Dictionary<string, List<ComboMainResInfo>>();


    public static string CommonImage = "gameCommon/image";



    [MenuItem("Tools/回退本地操作 #&p")]
    public static void ReverseLocalSvn()
    {
        var allFilePath = EasyUseEditorFuns.baseCustomTmpCache;
        var allFiles = Directory.GetFiles(allFilePath,"*.path",SearchOption.AllDirectories);


        

        foreach(var file in allFiles)
        {
            var reallyFilePath =  file.Replace(".path", "");
            var resPath = File.ReadAllText(file);
            var targetFilePath = Path.Combine(System.Environment.CurrentDirectory, resPath);
            EasyUseEditorFuns.UnitySaveCopyFile(reallyFilePath, targetFilePath);
        }
        
    }

    [MenuItem("Tools/测试查找重复资源 #&p")]
    public static void Collect()
    {
        allMainResList?.Clear();
        allSubInfoLists?.Clear();
        spriteBeDepandence?.Clear();
        likeSpriteResDepandence?.Clear();
        mergeedSpriteBeDepandence?.Clear();
        needDelTextureInfos?.Clear();

        var allRes = AssetDatabase.FindAssets("t:prefab t:Material",new string[] {
            "Assets/prefab1" ,
            "Assets/prefab2" ,
        });
        allRes = allRes.Select((xx) => xx = AssetDatabase.GUIDToAssetPath(xx)).ToArray<string>();

        foreach(var pathName in allRes)
        {
            var info = new MainResInfo();
            info.Init(pathName);
            //剔除自己和cs引用 获得纯资源引用 
            var allDps = AssetDatabase.GetDependencies(pathName).Where((xx)=>xx != pathName && !xx.EndsWith(".cs")).ToArray<string>();
            if(allDps.Length > 0 )
            {
                var childsInfo =  info.AddDependency(allDps);

                for(int i = 0; i < childsInfo.Count; i++)
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
        foreach(var mainRes in allMainResList)
        {
            foreach(var subRes in mainRes.childs)
            {
                if(!likeSpriteResDepandence.ContainsKey(subRes.resPath))
                {
                    likeSpriteResDepandence.Add(subRes.resPath, new List<MainResInfo>());
                }
                likeSpriteResDepandence[subRes.resPath].Add(mainRes);
            }            
        }

        //由于 likeSpriteResDepandecen 的list数组中 可能某几个项都是一个功能目录 也就是
        //一个ab包 所以这里还需要再封装依次 

        foreach (var subRes in likeSpriteResDepandence)
        {

            
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
            foreach(var item in map)
            {
                //对于依赖的资源如果他们来自于相同的逻辑目录，也就是相同的ab包 需要合并在一起
                var info = new ComboMainResInfo();
                info.editorResInfos = item.Value;
                spriteBeDepandence[subRes.Key].Add(info);
            }
        }

        // spriteBeDepandence  是子资源 map 一堆主资源的 映射
        // key是assetPath名 
        foreach (var item in spriteBeDepandence)
        {
            
            var findRst = GetTextureInfo(item.Key);
            if (findRst == null) Debug.LogError("运行错误！");

            if(mergeedSpriteBeDepandence.Count == 0)
            {
                mergeedSpriteBeDepandence.Add(findRst, findRst);
            }
            else
            {
                
                var conflictRst = mergeedSpriteBeDepandence.FirstOrDefault((xx) => xx.Key.md5Code == findRst.md5Code);
                if(conflictRst.Key == null || conflictRst.Value == null)
                {
                    mergeedSpriteBeDepandence.Add(findRst, findRst);
                }
                else
                {
                    //如果findRst 包括了common，common是不能被剔除的 .
                    if (findRst.resPath.Contains(CommonImage))
                    {
                        mergeedSpriteBeDepandence.Remove(conflictRst.Key);
                        var unNormalSprite =  spriteBeDepandence.FirstOrDefault((yy) => yy.Key == conflictRst.Value.resPath);
                        needDelTextureInfos.Add(unNormalSprite.Key, unNormalSprite.Value);
                        mergeedSpriteBeDepandence.Add(findRst, findRst);
                    }
                    else
                    {
                        needDelTextureInfos.Add(item.Key, item.Value);
                    }

                }
            }
            
           

        }
        var basePath = System.Environment.CurrentDirectory + "/Assets/GamePlay/Art/Common/_Sprite/";
      //  basePath = CommonUtil.GetLinuxPath(basePath);


        foreach (var item in needDelTextureInfos)
        {
            var needDelTextureRes =  GetTextureInfo(item.Key);
            //需要替换的uuid
            var targetRes =  mergeedSpriteBeDepandence.FirstOrDefault((xx) => xx.Key.md5Code == needDelTextureRes.md5Code);
            var targetUUid = targetRes.Value.uuid;

            
            for(int i = 0; i < item.Value.Count; i++)
            {
                for(int j = 0; j < item.Value[i].editorResInfos.Count;j++)
                {
                    var suorcePath = Path.Combine(System.Environment.CurrentDirectory, item.Value[i].editorResInfos[j].resPath);
                    var targetPath = Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, item.Value[i].editorResInfos[j].resPath);
                    EasyUseEditorFuns.UnitySaveCopyFile(suorcePath, targetPath, true);
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