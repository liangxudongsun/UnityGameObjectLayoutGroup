using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameObjectLayoutGroup))]
public class GameObjectLayoutGroupEditor : Editor
{
    private GameObjectLayoutGroup LayoutGroup;
    private SerializedProperty m_padding;
    private SerializedProperty m_cellSize;
    private SerializedProperty m_cellRotation;
    private SerializedProperty m_spacing;
    private SerializedProperty m_includeInactive;
    private SerializedProperty m_autoUpdateLayout;
    private SerializedProperty m_axis;
    private SerializedProperty m_startCorner;
    private SerializedProperty m_childAlignment;
    private SerializedProperty m_controlChildSize;
    private SerializedProperty m_controlChildRotation;
    private SerializedProperty enableSector;
    private SerializedProperty m_sector;
    private SerializedProperty m_enableTweenAnim;
    private SerializedProperty m_tweenAnim;

    // 折叠状态
    private bool m_showLayout = true;
    private bool m_showSector = false;
    private bool m_showTween = false;

    private void OnEnable()
    {
        m_padding = serializedObject.FindProperty("m_padding");
        m_cellSize = serializedObject.FindProperty("m_cellSize");
        m_cellRotation = serializedObject.FindProperty("m_cellRotation");
        m_spacing = serializedObject.FindProperty("m_spacing");
        m_includeInactive = serializedObject.FindProperty("m_includeInactive");
        m_autoUpdateLayout = serializedObject.FindProperty("m_autoUpdateLayout");
        m_axis = serializedObject.FindProperty("m_axis");
        m_startCorner = serializedObject.FindProperty("m_startCorner");
        m_childAlignment = serializedObject.FindProperty("m_childAlignment");
        m_controlChildSize = serializedObject.FindProperty("m_controlChildSize");
        m_controlChildRotation = serializedObject.FindProperty("m_controlChildRotation");
        enableSector = serializedObject.FindProperty("enableSector");
        m_sector = serializedObject.FindProperty("m_sector");
        m_enableTweenAnim = serializedObject.FindProperty("m_enableTweenAnim");
        m_tweenAnim = serializedObject.FindProperty("m_tweenAnim");

        LayoutGroup = (GameObjectLayoutGroup)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawBasicSettings();

        EditorGUILayout.Space(4);
        m_showLayout = EditorGUILayout.Foldout(m_showLayout, "Layout Settings", true, EditorStyles.foldoutHeader);
        if (m_showLayout)
        {
            EditorGUI.indentLevel++;
            DrawLayoutSettings();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);
        m_showSector = EditorGUILayout.Foldout(m_showSector, "Sector Settings", true, EditorStyles.foldoutHeader);
        if (m_showSector)
        {
            EditorGUI.indentLevel++;
            DrawSectorSettings();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);
        m_showTween = EditorGUILayout.Foldout(m_showTween, "Tween Animation", true, EditorStyles.foldoutHeader);
        if (m_showTween)
        {
            EditorGUI.indentLevel++;
            DrawTweenSettings();
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBasicSettings()
    {
        EditorGUILayout.PropertyField(m_padding);
        EditorGUILayout.PropertyField(m_axis);
        EditorGUILayout.PropertyField(m_childAlignment);
        EditorGUILayout.PropertyField(m_autoUpdateLayout);
        EditorGUILayout.PropertyField(m_includeInactive);
    }

    private void DrawLayoutSettings()
    {
        EditorGUILayout.PropertyField(m_startCorner);

        EditorGUILayout.PropertyField(m_spacing);

        EditorGUILayout.Space(2);
        DrawControlSize();
        DrawControlRotation();
    }

    private void DrawSectorSettings()
    {
        EditorGUILayout.PropertyField(enableSector);
        if (enableSector.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_sector, true);
            EditorGUI.indentLevel--;
        }
    }

    private void DrawTweenSettings()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(m_enableTweenAnim, GUIContent.none, GUILayout.Width(20));
        EditorGUILayout.LabelField("Enable Tween Animation", EditorStyles.boldLabel);
        if (m_enableTweenAnim.boolValue && GUILayout.Button("Play", EditorStyles.miniButton, GUILayout.Width(60)))
        {
            LayoutGroup.UpdateLayout(true);
        }
        EditorGUILayout.EndHorizontal();

        if (m_enableTweenAnim.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_tweenAnim, true);
            EditorGUI.indentLevel--;
        }
    }

    private void DrawControlSize()
    {
        var controlX = m_controlChildSize.FindPropertyRelative("ControlX");
        var controlY = m_controlChildSize.FindPropertyRelative("ControlY");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Control Child Size", GUILayout.Width(EditorGUIUtility.labelWidth - 4));

        GUILayout.BeginHorizontal();
        controlX.boolValue = GUILayout.Toggle(controlX.boolValue, "X", GUILayout.ExpandWidth(false));
        GUILayout.Space(10);
        controlY.boolValue = GUILayout.Toggle(controlY.boolValue, "Y", GUILayout.ExpandWidth(false));
        GUILayout.EndHorizontal();

        EditorGUILayout.EndHorizontal();
        if (controlX.boolValue || controlY.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_cellSize);
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUI.indentLevel++;
            GUI.enabled = false;
            EditorGUILayout.PropertyField(m_cellSize);
            GUI.enabled = true;
            EditorGUI.indentLevel--;
        }
    }

    private void DrawControlRotation()
    {
        var controlX = m_controlChildRotation.FindPropertyRelative("ControlX");
        var controlY = m_controlChildRotation.FindPropertyRelative("ControlY");
        var controlZ = m_controlChildRotation.FindPropertyRelative("ControlZ");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Control Child Rotation", GUILayout.Width(EditorGUIUtility.labelWidth - 4));

        GUILayout.BeginHorizontal();
        controlX.boolValue = GUILayout.Toggle(controlX.boolValue, "X", GUILayout.ExpandWidth(false));
        GUILayout.Space(10);
        controlY.boolValue = GUILayout.Toggle(controlY.boolValue, "Y", GUILayout.ExpandWidth(false));
        GUILayout.Space(10);
        controlZ.boolValue = GUILayout.Toggle(controlZ.boolValue, "Z", GUILayout.ExpandWidth(false));
        GUILayout.EndHorizontal();

        EditorGUILayout.EndHorizontal();

        if (controlX.boolValue || controlY.boolValue || controlZ.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_cellRotation);
            EditorGUI.indentLevel--;
        }
    }

    private void OnSceneGUI()
    {
        var serSector = serializedObject.FindProperty("m_sector");
        var enableProp = serializedObject.FindProperty("enableSector");
        if (serSector == null || enableProp == null || !enableProp.boolValue) return;

        var angleProp = serSector.FindPropertyRelative("angle");
        var radiusProp = serSector.FindPropertyRelative("radius");
        var t = LayoutGroup.transform;
        var pos = t.position;

        // 用对象本地方向计算半径末端，使手柄随物体旋转
        Vector3 radiusDir = (t.rotation * Quaternion.Euler(0, 0, -angleProp.floatValue / 2)) * Vector3.right;
        Vector3 radiusEnd = pos + radiusDir * radiusProp.floatValue;

        // 半径滑块手柄（比 PositionHandle 轻量，只沿半径方向拖动）
        EditorGUI.BeginChangeCheck();
        radiusEnd = Handles.Slider(radiusEnd, radiusDir, 0.5f, Handles.SphereHandleCap, 0.1f);
        if (EditorGUI.EndChangeCheck())
        {
            float newRadius = Vector3.Dot(radiusEnd - pos, radiusDir);
            if (newRadius < 0.01f) newRadius = 0.01f;
            Undo.RecordObject(LayoutGroup, "Change Sector Radius");
            radiusProp.floatValue = newRadius;
            serializedObject.ApplyModifiedProperties();
            LayoutGroup.UpdateLayout(serializedObject.FindProperty("m_enableTweenAnim").boolValue);
        }

        // 扇形弧线 + 半径线（在对象本地坐标系中绘制）
        Handles.color = Color.cyan;
        Handles.DrawWireArc(pos, t.forward, t.right, angleProp.floatValue, radiusProp.floatValue);
        Handles.DrawLine(pos, radiusEnd);
        Handles.DrawSolidDisc(radiusEnd, t.forward, 0.15f);

        // 标注
        Handles.Label(pos + radiusDir * (radiusProp.floatValue + 0.5f),
            $"R:{radiusProp.floatValue:F1}  {angleProp.floatValue:F0}°");

        // --- Tween StartPoint 手柄 ---
        var tweenEnable = serializedObject.FindProperty("m_enableTweenAnim");
        var tweenAnim = serializedObject.FindProperty("m_tweenAnim");
        if (tweenEnable != null && tweenEnable.boolValue && tweenAnim != null)
        {
            var startProp = tweenAnim.FindPropertyRelative("startPoint");
            if (startProp != null)
            {
                Vector3 worldStart = t.TransformPoint((Vector3)startProp.vector2Value);
                Handles.color = Color.yellow;
                Handles.DrawSolidDisc(worldStart, t.forward, 0.2f);
                Handles.DrawWireDisc(worldStart, t.forward, 0.3f);
                Handles.Label(worldStart + Vector3.up * 0.4f, "StartPoint");

                EditorGUI.BeginChangeCheck();
                Vector3 newWorldStart = Handles.PositionHandle(worldStart, t.rotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(LayoutGroup, "Move Tween StartPoint");
                    Vector3 localStart = t.InverseTransformPoint(newWorldStart);
                    startProp.vector2Value = new Vector2(localStart.x, localStart.y);
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
