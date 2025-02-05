using System;
using Launcher;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Application;

[CustomEditor(typeof(Main))]
public class ClientProxyEditor : Editor
{
    bool SymbleDefine = true;

    string userToken;

    string[] defineArray = new string[]
    {
        "",
        "ENABLE_ODIN_PLUGIN",
        "DEV_MODE",
        "FRUIT_DEBUG",
        "EDITOR_OPEN_HIERARCHY_DRAW_ICON",
        "DEV_DEBUG_NET", // 网络调试   
        
    };

    private void OnEnable()
    {
        int nBit = 0;
        if(defineArray.Length >= 32)
        {
            Debug.LogError("警告位已经不够用了");
           
        }
        for(int i = 0; i < defineArray.Length; i++)
        {
            if (string.IsNullOrEmpty(defineArray[i])) continue;
            if(EasyUseEditorFuns.HasDebugSymble(defineArray[i]))
            {
                nBit |=  GetBit(nBit, i);
            }
        }
        (target as Main).DefineBit = nBit;
        
        //c# read json 

    }

    static int GetBit(int number, int bitPosition)
    {
        // 检查位是否有效
        if (bitPosition < 0 || bitPosition >= sizeof(int) * 8)
        {
            throw new ArgumentOutOfRangeException(nameof(bitPosition), "位位置无效");
        }

        // 生成掩码：将 1 左移 bitPosition 位，然后取反
        int mask = (1 << bitPosition);

        // 使用按位与操作干掉指定位
        return number | mask;
    }
    static int ClearBit(int number, int bitPosition)
    {
        // 检查位是否有效
        if (bitPosition < 0 || bitPosition >= sizeof(int) * 8)
        {
            throw new ArgumentOutOfRangeException(nameof(bitPosition), "位位置无效");
        }

        // 生成掩码：将 1 左移 bitPosition 位，然后取反
        int mask = ~(1 << bitPosition);

        // 使用按位与操作干掉指定位
        return number & mask;
    }

    static bool HasBit(int number, int bitPosition)
    {
        // 检查位是否有效
        if (bitPosition < 0 || bitPosition >= sizeof(int) * 8)
        {
            throw new ArgumentOutOfRangeException(nameof(bitPosition), "位位置无效");
        }

        // 判断指定位是否为 1
        return (number & (1 << bitPosition)) != 0;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GUILayout.BeginHorizontal();
        if(!Application.isPlaying &&  GUILayout.Button("启动", GUILayout.Height(50)))
        {
            EditorBuildSettingsScene[] tempScenes = new UnityEditor.EditorBuildSettingsScene[1];
            tempScenes[0] = new EditorBuildSettingsScene("Assets/scenes/GameStart.unity", true);
            EditorBuildSettings.scenes = tempScenes;
            EditorApplication.isPlaying = true;
        }
        if(GUILayout.Button("暂停", GUILayout.Height(50)))
        {
            EditorApplication.ExecuteMenuItem("Edit/Pause");
        }

        if (GUILayout.Button("退出",GUILayout.Height(50)))
        {
            if(Application.isPlaying)
            {
                EditorApplication.ExecuteMenuItem("Edit/Play");
            }
        }
        GUILayout.EndHorizontal();

        if(GUILayout.Button("打开c#"))
        {
           
            Application.OpenURL(System.Environment.CurrentDirectory +  "/Assets/Launcher/Main.cs");   
        }

        Main global = target as Main;
        SymbleDefine = EditorGUILayout.Foldout(SymbleDefine, new GUIContent("宏定义"));
        if (SymbleDefine)
        {
            for(int i = 1; i < defineArray.Length; i++)
            {
                if (global && HasBit(global.DefineBit, i) && GUILayout.Button($"关闭宏{defineArray[i]}"))
                {
                    global.DefineBit = ClearBit(global.DefineBit, i);
                    EasyUseEditorFuns.ChangeToXXDefine(false, defineArray[i]);
                }
                if (global && !HasBit(global.DefineBit, i) && GUILayout.Button($"开启宏{defineArray[i]}"))
                {
                    global.DefineBit = GetBit(global.DefineBit, i);
                    EasyUseEditorFuns.ChangeToXXDefine(true, defineArray[i]);
                }
            }
            
            
        }
    }
}