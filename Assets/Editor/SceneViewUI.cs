using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class SceneViewUI
{
    static SceneViewUI()
    {
        // 订阅 SceneView.duringSceneGui 事件
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        //if(sceneView.camera.orthographic) // 如果是2d模式直接return 
        //{
        //    return;
        //}
        // 在 Scene 视图中绘制文本
        Handles.BeginGUI();
        var oldColor = GUI.color;
        GUI.color = Color.black;

        Vector3 bottomLeftWorld = sceneView.camera.ViewportToWorldPoint(new Vector3(0, 0, sceneView.camera.nearClipPlane));
        Vector2 bottomLeftScreen = HandleUtility.WorldToGUIPoint(bottomLeftWorld);


        var rect = new Rect(bottomLeftScreen.x, bottomLeftScreen.y - 20, 200, 20);

        var size = GameViewTools.GameViewSize();
        string isLand = (size.x > size.y) ? "横屏" : "竖屏";
        GUI.Label(rect, $"屏幕({isLand})分辨率为：({size.x},{size.y})");
        GUI.color = oldColor;
        rect = new Rect(rect.x, rect.y - 20, 200, 20);
        if (GUI.Button(rect, "启动游戏"))
        {
            EasyUseEditorTool.OnSceneOpenOrPlay("Assets/scenes/GameStart.unity");
        }
        rect = new Rect(rect.x, rect.y - 20, 200, 20);
        if ((!EditorPrefs.GetBool("InitializerUiEdit", false) && GUI.Button(rect, "锁定分辨率"))
            || (EditorPrefs.GetBool("InitializerUiEdit", false) && GUI.Button(rect, "解除锁定分辨率")))
        {
            EditorPrefs.SetBool("InitializerUiEdit", !EditorPrefs.GetBool("InitializerUiEdit", false));
        }
        rect = new Rect(rect.x, rect.y - 20, 200, 20);
        if ((!EditorPrefs.GetBool("SelectionTools", true) && GUI.Button(rect, "资源自动展开"))
            || (EditorPrefs.GetBool("SelectionTools", true) && GUI.Button(rect, "解除资源自动展开")))
        {
            EditorPrefs.SetBool("SelectionTools", !EditorPrefs.GetBool("SelectionTools", false));
            AssetDatabase.Refresh();
        }


        rect = new Rect(rect.x, rect.y - 20, 200, 20);
        if (GUI.Button(rect, "ui适配1125,2436"))
        {
            GameViewTools.ChangeSolution(new Vector2(1125, 2436));
        }
        rect = new Rect(rect.x, rect.y - 20, 200, 20);
        if (GUI.Button(rect, "ui适配1080,1920"))
        {
            GameViewTools.ChangeSolution(new Vector2(1080, 1920));
        }
        rect = new Rect(rect.x, rect.y - 20, 200, 20);
        if (GUI.Button(rect, "资源冗余检查&清理资源"))
        {
            Resolution rs = Screen.currentResolution; //获取当前的分辨率 
            int nWidth = 600;
            int nHeight = 500;
            int x = (rs.width - nWidth) / 2;
            int y = (rs.height - nHeight) / 2;
            Rect rect2 = new Rect(x, y, nWidth, nHeight);
            FindRepeatRes myWindow = (FindRepeatRes)EditorWindow.GetWindowWithRect(typeof(FindRepeatRes), rect2, true,

     "资源查重&合并");
            myWindow.position = rect2;
            myWindow.Show();//展示 
            myWindow.closeAction += EditorLogWindow.CloseWindow;
            EditorCoroutine.StartCoroutine(new EditorWaitForSeconds(0.01f, () =>
            {
                EditorLogWindow.OpenWindow(myWindow);
                myWindow.Focus();
            }));


        }

        Handles.EndGUI();
    }
}