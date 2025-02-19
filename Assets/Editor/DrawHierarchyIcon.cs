
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using System.Linq;



[InitializeOnLoad]
public static class DrawHierarchyIcon
{
    static Texture2D icon;

    static DrawHierarchyIcon()
    {
        DisableDrawIcon();
        EnableDrawIcon();
    }

    public static void EnableDrawIcon()
    {
#if EDITOR_OPEN_HIERARCHY_DRAW_ICON
        EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
#endif
        EditorApplication.hierarchyWindowItemOnGUI += HierarchyIcon;
    }

    public static void DisableDrawIcon()
    {
    #if EDITOR_OPEN_HIERARCHY_DRAW_ICON
        EditorApplication.hierarchyWindowItemOnGUI -= HierarchyWindowItemOnGUI;
    #endif
        EditorApplication.hierarchyWindowItemOnGUI -= HierarchyIcon;
    }

    private static void HierarchyIcon(int instanceID, Rect selectionRect)
    {
#if DEV_MODE

#else

     if (!Application.isPlaying) return;
#endif
        Rect r = new Rect(selectionRect);

        r.x = 5.0f;

        //调整大小的
        r.width = 20;
        //EditorUtility.InstanceIDToObject是通过一个id来变换成一个GameObj
        GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (go != null )
        {
            if(go.GetComponent<Main>() != null)
            {
                Rect position = new Rect(selectionRect);
                position.x = position.width + (selectionRect.x - 80);
                position.width = 80;
                position.height = 16f;
                GUI.Label(position, "游戏入口");    
            }
        }
        

    }
    private static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (gameObject != null)
        {
            var trans = gameObject.GetComponentsInChildren<Transform>(true);

            Rect position = new Rect(selectionRect);
            position.x = position.width + (selectionRect.x - 40);
            position.width = 40;
            position.height = 16f;
            GUI.Label(position, trans.Length.ToString());
        }
    }
}
#endif