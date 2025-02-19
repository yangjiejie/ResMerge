using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
// 2. 自定义Editor基类
[CustomEditor(typeof(MonoBehaviour), true)]
public class ShowInInspectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DrawButtons();
    }

    private void DrawButtons()
    {
        var methods = target.GetType().GetMethods(
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        foreach (var method in methods)
        {
            var attributes = method.GetCustomAttributes(typeof(ShowInInspector), true);
            if (attributes.Length == 0) continue;

            var buttonAttribute = attributes[0] as ShowInInspector;
            var buttonName = string.IsNullOrEmpty(buttonAttribute.ButtonName)
                ? method.Name
                : buttonAttribute.ButtonName;

            DrawMethodButton(method, buttonName, buttonAttribute.ButtonHeight);
        }
    }

    private void DrawMethodButton(MethodInfo method, string name, float height)
    {
        GUILayout.Space(5);
        var parameters = method.GetParameters();
        object[] args = new object[parameters.Length];

        // 绘制参数输入
        for (int i = 0; i < parameters.Length; i++)
        {
            args[i] = DrawParameterField(parameters[i]);
        }

        if (GUILayout.Button(name, GUILayout.Height(height)))
        {
            try
            {
                method.Invoke(target, args);
            }
            catch (Exception e)
            {
                Debug.LogError($"执行方法 {name} 失败: {e.InnerException?.Message}");
            }
        }
    }

    private object DrawParameterField(ParameterInfo parameter)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(parameter.Name, GUILayout.Width(120));

        object value = null;
        var paramType = parameter.ParameterType;

        try
        {
            value = paramType switch
            {
                Type t when t == typeof(int) => EditorGUILayout.IntField((int)GetDefaultValue(t)),
                Type t when t == typeof(float) => EditorGUILayout.FloatField((float)GetDefaultValue(t)),
                Type t when t == typeof(string) => EditorGUILayout.TextField((string)GetDefaultValue(t)),
                Type t when t == typeof(bool) => EditorGUILayout.Toggle((bool)GetDefaultValue(t)),
                Type t when t == typeof(Vector2) => EditorGUILayout.Vector2Field("", (Vector2)GetDefaultValue(t)),
                Type t when t == typeof(Vector3) => EditorGUILayout.Vector3Field("", (Vector3)GetDefaultValue(t)),
                Type t when t == typeof(Color) => EditorGUILayout.ColorField((Color)GetDefaultValue(t)),
                Type t when t.IsEnum => EditorGUILayout.EnumPopup((Enum)GetDefaultValue(t)),
                _ => DrawCustomTypeField(paramType)
            };
        }
        catch
        {
            GUILayout.Label($"Unsupported type: {paramType.Name}");
        }

        GUILayout.EndHorizontal();
        return value ?? GetDefaultValue(paramType);
    }

    private object GetDefaultValue(Type t)
    {
        return t.IsValueType ? Activator.CreateInstance(t) : null;
    }

    private object DrawCustomTypeField(Type type)
    {
        // 扩展点：添加自定义类型的绘制逻辑
        return null;
    }
}