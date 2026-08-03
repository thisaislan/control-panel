using Thisaislan.ControlPanel.Editor.Data;
using UnityEditor;
using UnityEngine;

namespace Thisaislan.ControlPanel.Editor
{   
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ControlPanelTabData))]
    internal class ControlPanelTabDataInspector : UnityEditor.Editor
    {
        private const string TabNameLabel = "Tab Name";
        private const string DescriptionLabel = "Description";
        private const string ScriptableObjectsLabel = "Scriptable Objects";
        private const string NoItemsMessage = "No Scriptable Objects added to this tab.";
        private const string TotalItemsLabel = "Total items: ";
        private const string SelectButtonLabel = "Select";

        private const string TabNameProperty = "TabName";
        private const string DescriptionProperty = "Description";
        private const string GuidsProperty = "ScriptableObjectGuids";

        private const int SectionSpacing = 8;
        private const int ItemSpacing = 6;
        private const int DescriptionMinHeight = 40;
        private const int TabNameHeight = 18;
        private const int SelectButtonWidth = 50;
        private const int DescriptionWidthOffset = 40;

        private int selectedFoldoutIndex = -1;

        private GUIStyle foldoutStyle;

        private GUIStyle FoldoutStyle
        {
            get
            {
                if (foldoutStyle == null)
                {
                    foldoutStyle = new GUIStyle(EditorStyles.foldout);
                    foldoutStyle.font = EditorStyles.boldLabel.font;
                    foldoutStyle.fontSize = EditorStyles.boldLabel.fontSize;
                    foldoutStyle.fontStyle = EditorStyles.boldLabel.fontStyle;

                }

                return foldoutStyle;
            }
        }

        public override void OnInspectorGUI()
        {
            ValidateFoldoutStyle();

            if (targets.Length > 1)
            {
                DrawMultiObjectInspector();
            }
            else
            {
                DrawSingleInspector(serializedObject);
            }
        }

        private void ValidateFoldoutStyle()
        {
            if (foldoutStyle != null && foldoutStyle.normal.background != EditorStyles.foldout.normal.background)
            {
                foldoutStyle = null;
            }
        }

        private void DrawMultiObjectInspector()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                Object targetObject = targets[i];
                SerializedObject serializedObjectInstance = new SerializedObject(targetObject);
                SerializedProperty tabNameProp = serializedObjectInstance.FindProperty(TabNameProperty);

                bool isOpen = selectedFoldoutIndex == i;

                EditorGUI.BeginChangeCheck();

                isOpen = EditorGUILayout.Foldout(isOpen, tabNameProp.stringValue, true, FoldoutStyle);

                if (EditorGUI.EndChangeCheck())
                {
                    if (isOpen)
                    {
                        selectedFoldoutIndex = i;
                    }
                    else
                    {
                        selectedFoldoutIndex = -1;
                    }
                }

                if (isOpen)
                {
                    EditorGUI.indentLevel++;
                    DrawSingleInspector(serializedObjectInstance);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawSingleInspector(SerializedObject so)
        {
            so.Update();

            SerializedProperty tabNameProp = so.FindProperty(TabNameProperty);
            SerializedProperty descriptionProp = so.FindProperty(DescriptionProperty);
            SerializedProperty scriptableObjectGuidsProp = so.FindProperty(GuidsProperty);

            DrawTabNameSection(tabNameProp);
            DrawDescriptionSection(descriptionProp);
            DrawScriptableObjectsSection(scriptableObjectGuidsProp);
            DrawTotalItemsFooter(scriptableObjectGuidsProp);

            so.ApplyModifiedProperties();
        }

        private void DrawTabNameSection(SerializedProperty tabNameProp)
        {
            EditorGUILayout.LabelField(TabNameLabel, EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(
                tabNameProp.stringValue,
                EditorStyles.wordWrappedLabel,
                GUILayout.Height(TabNameHeight)
            );
        }

        private void DrawDescriptionSection(SerializedProperty descriptionProp)
        {
            EditorGUILayout.Space(SectionSpacing);
            EditorGUILayout.LabelField(DescriptionLabel, EditorStyles.boldLabel);

            string description = descriptionProp.stringValue;
            float availableWidth = EditorGUIUtility.currentViewWidth - DescriptionWidthOffset;

            float descriptionHeight = EditorStyles.wordWrappedLabel.CalcHeight(
                new GUIContent(description),
                availableWidth
            );

            float finalHeight = Mathf.Max(descriptionHeight, DescriptionMinHeight);

            EditorGUILayout.SelectableLabel(
                description,
                EditorStyles.wordWrappedLabel,
                GUILayout.Height(finalHeight)
            );
        }

        private void DrawScriptableObjectsSection(SerializedProperty guidsProp)
        {
            EditorGUILayout.Space(SectionSpacing);
            EditorGUILayout.LabelField(ScriptableObjectsLabel, EditorStyles.boldLabel);

            if (guidsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox(NoItemsMessage, MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(ItemSpacing);

            for (int i = 0; i < guidsProp.arraySize; i++)
            {
                DrawScriptableObjectItem(guidsProp, i);
            }

            EditorGUILayout.Space(ItemSpacing);
            EditorGUILayout.EndVertical();
        }

        private void DrawScriptableObjectItem(SerializedProperty guidsProp, int index)
        {
            string guid = guidsProp.GetArrayElementAtIndex(index).stringValue;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (obj == null)
            {
                return;
            }

            if (index > 0)
            {
                EditorGUILayout.Space(1);
                EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
                EditorGUILayout.Space(1);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(obj.name, EditorStyles.label);

            if (GUILayout.Button(SelectButtonLabel, GUILayout.Width(SelectButtonWidth)))
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTotalItemsFooter(SerializedProperty guidsProp)
        {
            EditorGUILayout.Space(SectionSpacing);
            EditorGUILayout.LabelField(
                TotalItemsLabel + guidsProp.arraySize,
                EditorStyles.miniLabel
            );
        }
    }
}