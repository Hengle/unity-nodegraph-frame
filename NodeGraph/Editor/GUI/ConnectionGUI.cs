using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using NodeGraph.DataModel;

namespace NodeGraph
{
    [Serializable]
    public class ConnectionGUI
    {
        [SerializeField]
        private ConnectionData m_data;

        [SerializeField]
        private ConnectionPointData m_outputPoint;
        [SerializeField]
        private ConnectionPointData m_inputPoint;
        [SerializeField]
        private ConnectionGUIInspectorHelper m_inspector;
        [SerializeField]
        private NodeGraphController m_controller;
        [SerializeField]
        private string connectionButtonStyle;
        private ConnectionDrawer connectionDrawer;
        public string Label
        {
            get
            {
                return m_data.Label;
            }
            set
            {
                m_data.Label = value;
            }
        }

        public string Id
        {
            get
            {
                return m_data.Id;
            }
        }

        public ConfigGraph ParentGraph
        {
            get
            {
                return m_controller.TargetGraph;
            }
        }
        public NodeGraphController Controller
        {
            get
            {
                return m_controller;
            }
        }

        public ConnectionData Data
        {
            get
            {
                return m_data;
            }
        }
        public string OutputNodeId
        {
            get
            {
                return m_outputPoint.NodeId;
            }
        }

        public string InputNodeId
        {
            get
            {
                return m_inputPoint.NodeId;
            }
        }

        public ConnectionPointData OutputPoint
        {
            get
            {
                return m_outputPoint;
            }
        }

        public ConnectionPointData InputPoint
        {
            get
            {
                return m_inputPoint;
            }
        }


        public ConnectionGUIInspectorHelper Inspector
        {
            get
            {
                if (m_inspector == null)
                {
                    m_inspector = ScriptableObject.CreateInstance<ConnectionGUIInspectorHelper>();
                    m_inspector.hideFlags = HideFlags.DontSave;
                }
                return m_inspector;
            }
        }

        public bool IsSelected
        {
            get
            {
                return (m_inspector != null && Selection.activeObject == m_inspector && m_inspector.connectionGUI == this);
            }
        }

        private Rect m_buttonRect;

        public static ConnectionGUI LoadConnection(ConnectionData data, ConnectionPointData output, ConnectionPointData input, NodeGraphController controller)
        {
            return new ConnectionGUI(
                data,
                output,
                input,
                controller
            );
        }

        public static ConnectionGUI CreateConnection(string label, string type, ConnectionPointData output, ConnectionPointData input, NodeGraphController controller)
        {
            var connection = NodeConnectionUtility.CustomConnectionTypes.Find(x => x.connection.Name == type).CreateInstance();

            return new ConnectionGUI(
                new ConnectionData(label, connection, output, input),
                output,
                input,
                controller
            );
        }

        private ConnectionGUI(ConnectionData data, ConnectionPointData output, ConnectionPointData input, NodeGraphController controller)
        {

            UnityEngine.Assertions.Assert.IsTrue(output.IsOutput, "Given Output point is not output.");
            UnityEngine.Assertions.Assert.IsTrue(input.IsInput, "Given Input point is not input.");
            m_controller = controller;
            m_inspector = ScriptableObject.CreateInstance<ConnectionGUIInspectorHelper>();
            m_inspector.hideFlags = HideFlags.DontSave;

            this.m_data = data;
            this.m_outputPoint = output;
            this.m_inputPoint = input;
            connectionDrawer = UserDefineUtility.GetUserDrawer(data.Operation.Object.GetType()) as ConnectionDrawer;
            if (connectionDrawer != null) connectionDrawer.target = data.Operation.Object;
            connectionButtonStyle = "sv_label_0";
        }

        public Rect GetRect()
        {
            return m_buttonRect;
        }

        public void DrawConnection(List<NodeGUI> nodes)
        {

            var startNode = nodes.Find(node => node.Id == OutputNodeId);
            if (startNode == null)
            {
                return;
            }

            var endNode = nodes.Find(node => node.Id == InputNodeId);
            if (endNode == null)
            {
                return;
            }
            var startPoint = m_outputPoint.GetGlobalPosition(startNode.Region);
            var startV3 = new Vector3(startPoint.x, startPoint.y, 0f);

            var endPoint = m_inputPoint.GetGlobalPosition(endNode.Region);
            var endV3 = new Vector3(endPoint.x, endPoint.y, 0f);

            var centerPoint = startPoint + ((endPoint - startPoint) / 2);
            var centerPointV3 = new Vector3(centerPoint.x, centerPoint.y, 0f);

            var pointDistanceX = NGEditorSettings.GUI.CONNECTION_CURVE_LENGTH;

            var startTan = new Vector3(startPoint.x + pointDistanceX, centerPoint.y, 0f);
            var endTan = new Vector3(endPoint.x - pointDistanceX, centerPoint.y, 0f);

            Color lineColor;
            var lineWidth = connectionDrawer == null ? 3 : connectionDrawer.LineWidth;// (totalAssets > 0) ? 3f : 2f;

            if (IsSelected)
            {
                lineColor = NGEditorSettings.GUI.COLOR_ENABLED;
            }
            else
            {
                lineColor = connectionDrawer == null ? Color.gray : connectionDrawer.LineColor;
            }

            ConnectionGUIUtility.HandleMaterial.SetPass(0);
            Handles.DrawBezier(startV3, endV3, startTan, endTan, lineColor, null, lineWidth);

            // draw connection label if connection's label is not normal.
            GUIStyle labelStyle = new GUIStyle("WhiteMiniLabel");
            labelStyle.alignment = TextAnchor.MiddleLeft;

            switch (Label)
            {
                case NGSettings.DEFAULT_OUTPUTPOINT_LABEL:
                    {
                        // show nothing
                        break;
                    }

                case NGSettings.BUNDLECONFIG_BUNDLE_OUTPUTPOINT_LABEL:
                    {
                        var labelWidth = labelStyle.CalcSize(new GUIContent(NGSettings.BUNDLECONFIG_BUNDLE_OUTPUTPOINT_LABEL));
                        var labelPointV3 = new Vector3(centerPointV3.x - (labelWidth.x / 2), centerPointV3.y - 24f, 0f);
                        Handles.Label(labelPointV3, NGSettings.BUNDLECONFIG_BUNDLE_OUTPUTPOINT_LABEL, labelStyle);
                        break;
                    }

                default:
                    {
                        var labelWidth = labelStyle.CalcSize(new GUIContent(Label));
                        var labelPointV3 = new Vector3(centerPointV3.x - (labelWidth.x / 2), centerPointV3.y - 24f, 0f);
                        Handles.Label(labelPointV3, Label, labelStyle);
                        break;
                    }
            }

            string connectionLabel = connectionDrawer == null ? "" : connectionDrawer.Label;

            var style = new GUIStyle(connectionButtonStyle);

            var labelSize = style.CalcSize(new GUIContent(connectionLabel));
            m_buttonRect = new Rect(centerPointV3.x - labelSize.x / 2f, centerPointV3.y - labelSize.y / 2f, labelSize.x, 30f);

            if (
                Event.current.type == EventType.ContextClick
                || (Event.current.type == EventType.MouseUp && Event.current.button == 1)
            )
            {
                var rightClickPos = Event.current.mousePosition;
                if (m_buttonRect.Contains(rightClickPos))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(
                        new GUIContent("Delete"),
                        false,
                        () =>
                        {
                            Delete();
                        }
                    );
                    menu.ShowAsContext();
                    Event.current.Use();
                }
            }

            if (GUI.Button(m_buttonRect, connectionLabel, style))
            {
                Inspector.UpdateInspector(this);
                ConnectionGUIUtility.ConnectionEventHandler(new ConnectionEvent(ConnectionEvent.EventType.EVENT_CONNECTION_TAPPED, this));
            }
        }


        public bool IsEqual(ConnectionPointData from, ConnectionPointData to)
        {
            return (m_outputPoint == from && m_inputPoint == to);
        }


        public void SetActive(bool active)
        {
            if (active)
            {
                Selection.activeObject = Inspector;
                connectionButtonStyle = "sv_label_1";
            }
            else
            {
                connectionButtonStyle = "sv_label_0";
            }
        }
        public void DrawObject(ConnectionGUIEditor editor)
        {
            if(connectionDrawer == null)
            {

            }
            else
            {
                connectionDrawer.OnInspectorGUI(this, editor, () =>
                {
                    Controller.Perform();
                    Data.Operation.Save();
                    ParentGraph.SetGraphDirty();
                });
            }
            
        }
        public void Delete()
        {
            ConnectionGUIUtility.ConnectionEventHandler(new ConnectionEvent(ConnectionEvent.EventType.EVENT_CONNECTION_DELETED, this));
        }
    }

    public static class NodeEditor_ConnectionListExtension
    {
        public static bool ContainsConnection(this List<ConnectionGUI> connections, ConnectionPointData output, ConnectionPointData input)
        {
            foreach (var con in connections)
            {
                if (con.IsEqual(output, input))
                {
                    return true;
                }
            }
            return false;
        }
    }
}