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


        var rect = new Rect(bottomLeftScreen.x, bottomLeftScreen.y - 100, 200, 50);

       
        if (GUI.Button(rect, "资源冗余检查&清理资源"))
        {
            Resolution rs = Screen.currentResolution; //获取当前的分辨率 
            int nWidth = 400;
            int nHeight = 500;
            int x = (rs.width - nWidth) / 2;
            int y = (rs.height - nHeight) / 2;
            Rect rect2 = new Rect(x, y, nWidth, nHeight);
            FindRepeatRes myWindow = (FindRepeatRes)EditorWindow.GetWindowWithRect(typeof(FindRepeatRes), rect2, true,

     "资源查重&合并");
            myWindow.position = rect2;
            myWindow.Show();//展示 

            myWindow.closeAction += EditorLogWindow.CloseWindow;
            EditorLogWindow.OpenWindow(myWindow);

            myWindow.Focus();
        }

        Handles.EndGUI();
    }
}