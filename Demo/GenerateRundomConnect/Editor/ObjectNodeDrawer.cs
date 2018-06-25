﻿using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using NodeGraph.DataModel;
using NodeGraph;
using NodeGraph.DefultSkin;


[CustomNodeView(typeof(ObjectNode))]
public class ObjectNodeDrawer : DefultSkinNodeView
{
    public override int Style { get { return 1; } }

    public override string Category
    {
        get
        {
            return "panel";
        }
    }
    public override float CustomNodeHeight
    {
        get
        {
            return EditorGUIUtility.singleLineHeight * 1;
        }
    }
    public override void OnNodeGUI(Rect position,NodeData data)
    {
        base.OnNodeGUI(position, data);
        (target as ObjectNode).type =  (PrimitiveType)EditorGUI.EnumPopup(position,(target as ObjectNode).type);
    }
    public override void OnInspectorGUI(NodeGUI gui)
    {
        base.OnInspectorGUI(gui);
        gui.Name = (target as ObjectNode).type.ToString();
    }

}