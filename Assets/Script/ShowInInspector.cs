// 1. 创建按钮特性
using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Method)]
public class ShowInInspector : PropertyAttribute
{
    public string ButtonName { get; }
    public float ButtonHeight { get; }

    public ShowInInspector(string name = "", float height = 20f)
    {
        ButtonName = name;
        ButtonHeight = height;
    }
}