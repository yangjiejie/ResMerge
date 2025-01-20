using UnityEditor;
using UnityEngine;
using System.IO;

public class EditorResReplaceByUuid 
{
   

    public static void ReplaceUUID(string resPath, string uuidA,string uuidB)
    {
        if (string.IsNullOrEmpty(uuidA) || string.IsNullOrEmpty(uuidB))
        {
            Debug.LogError("请指定 UUID A 和 UUID B！");
            return;
        }
        string[] allAssetPaths = null;
        if (string.IsNullOrEmpty(resPath))
        {
            allAssetPaths = AssetDatabase.GetAllAssetPaths();

            RepaceAll();
        }
        else
        {
            if (resPath.EndsWith(".prefab") || resPath.EndsWith(".mat"))
            {
                // 读取文件内容
                string fileContent = File.ReadAllText(resPath);

                // 检查是否包含 UUID A
                if (fileContent.Contains(uuidA))
                {
                    // 替换 UUID A 为 UUID B
                    fileContent = fileContent.Replace(uuidA, uuidB);

                    //写回文件之前先备份

                    // 写回文件
                    File.WriteAllText(resPath, fileContent);

                    Debug.Log($"替换成功：{resPath}");
                    
                }
            }
            else
            {
                return;
            }
           
        }

        

        void RepaceAll()
        {
            int replacedCount = 0;

            foreach (string assetPath in allAssetPaths)
            {
                if (assetPath.EndsWith(".prefab") || assetPath.EndsWith(".mat"))
                {
                    // 读取文件内容
                    string fileContent = File.ReadAllText(assetPath);

                    // 检查是否包含 UUID A
                    if (fileContent.Contains(uuidA))
                    {
                        // 替换 UUID A 为 UUID B
                        fileContent = fileContent.Replace(uuidA, uuidB);

                        // 写回文件
                        File.WriteAllText(assetPath, fileContent);

                        Debug.Log($"替换成功：{assetPath}");
                        replacedCount++;
                    }
                }
            }

            Debug.Log($"替换完成！共替换了 {replacedCount} 处引用。");

        }
        // 刷新 Unity 项目
        AssetDatabase.Refresh();
    }
}