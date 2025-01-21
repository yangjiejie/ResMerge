using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class EditorLogWindow : EditorWindow
{
    private static Vector2 logWindowSize = new Vector2(400, 300);
    private static EditorLogWindow instance;

    private Vector2 scrollPosition; // 滚动位置
    private List<string> textList = new List<string>(); // 文本列表

    public static EditorLogWindow GetInstance()
    {
        return instance;
    }
    public static void CloseWindow()
    {
        instance?.Close();
        instance = null;
    }
    public static void OpenWindow(EditorWindow mainWindow)
    {
        instance =  GetWindow<EditorLogWindow>(true,"Log Window",false);

        // 设置日志窗口大小
        instance.minSize = logWindowSize;

        // 计算日志窗口位置
        Rect mainWindowRect = mainWindow.position;
        float logWindowX = mainWindowRect.x + mainWindowRect.width;
        float logWindowY = mainWindowRect.y;

        // 设置日志窗口位置
        instance.position = new Rect(logWindowX, logWindowY, logWindowSize.x, logWindowSize.y);

        for(int i = 0; i < 100; i++)
        {
            instance.textList.Add("测试文本" + i);
        }
       
    }
    
    public void OnGUI()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

        for (int i = 0; i < textList.Count; i++)
        {
            GUILayout.Label(textList[i], EditorStyles.boldLabel);
        }
        GUILayout.EndScrollView();
    }

}
