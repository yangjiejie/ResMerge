/*
 * http://www.manew.com/thread-102160-1-1.html 游戏蛮牛
 */
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using Spine.Unity;
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
            if(go.GetComponent<UIPanelBase>() != null)
            {
                Rect position = new Rect(selectionRect);
                position.x = position.width + (selectionRect.x - 80);
                position.width = 80;
                position.height = 16f;

                Rect pos2 = new Rect(position);
                pos2.x += 40;
                pos2.width = 60;
                GUI.Label(position, "主ui");
                if(GUI.Button(pos2, "openC#"))
                {
                    var csName = go.GetComponent<UIPanelBase>().GetType().Name;
                    var guids  = AssetDatabase.FindAssets($"t:Script {csName}", new string[] { "Assets/hot_fix" });
                    var listPath = guids.Select((x) => AssetDatabase.GUIDToAssetPath(x)).ToList<string>();
                    string targetCsFile = "";
                    foreach (var item in listPath)
                    {
                        if(!item.Contains("generate"))
                        {
                            targetCsFile = item;
                            break;
                        }
                    }
                    if(!string.IsNullOrEmpty(targetCsFile))
                    {
                        Application.OpenURL(System.Environment.CurrentDirectory + "/" + targetCsFile);
                    }
                }
            }
            else if(go.GetComponent<ManagedBehaviour>() != null)
            {
                Rect position = new Rect(selectionRect);
                position.x = position.width + (selectionRect.x - 80);
                position.width = 80;
                position.height = 16f;
                GUI.Label(position, "子ui");
            }
            else if(go.GetComponent<SkeletonGraphic>() != null || go.GetComponent<SkeletonAnimation>())
            {
                Rect position = new Rect(selectionRect);
                position.x = position.width + (selectionRect.x - 80);
                position.width = 80;
                position.height = 16f;
                GUI.Label(position, "spine");
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