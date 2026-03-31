using Thisaislan.ControlPanel.Editor.Data;
using UnityEditor;
using UnityEngine;

namespace Thisaislan.ControlPanel.Editor
{
    /// <summary>
    /// Custom Inspector for ControlPanelTabData ScriptableObject assets.
    /// Displays read-only preview of tab contents in the Project window.
    /// </summary>
    [CustomEditor(typeof(ControlPanelTabData))]
    internal class ControlPanelTabDataInspector : UnityEditor.Editor
    {
        private const string TabNameLabel = "Tab Name";
        private const string TabNamePropertyName = "TabName";
        private const string DescriptionPropertyName = "Description";
        private const string ScriptableObjectGuidsPropertyName = "ScriptableObjectGuids";
        
        private const string DescriptionLabel = "Description";
        private const string ScriptableObjectsLabel = "Scriptable Objects";
        private const string NoItemsMessage = "No Scriptable Objects added to this tab.";
        private const string TotalItemsLabel = "Total items: ";
        private const string SelectButtonLabel = "Select";
        
        private const int SectionSpacing = 8;
        private const int ItemSpacing = 6;
        private const int DescriptionMinHeight = 40;
        private const int TabNameHeight = 18;
        private const int SelectButtonWidth = 50;
        private const int DescriptionWidthOffset = 40;
        
        private SerializedProperty tabNameProp;
        private SerializedProperty descriptionProp;
        private SerializedProperty scriptableObjectGuidsProp;
        
        private void OnEnable()
        {
            if (target == null)
            {
                DestroyImmediate(this);
                return;
            }
            
            if (serializedObject == null || serializedObject.targetObject == null)
            {
                return;
            }

            tabNameProp = serializedObject.FindProperty(TabNamePropertyName);
            descriptionProp = serializedObject.FindProperty(DescriptionPropertyName);
            scriptableObjectGuidsProp = serializedObject.FindProperty(ScriptableObjectGuidsPropertyName);

            CleanupInvalidEntries();
        }

        private void CleanupInvalidEntries()
        {
            bool changed = false;
            
            for (int i = scriptableObjectGuidsProp.arraySize - 1; i >= 0; i--)
            {
                string guid = scriptableObjectGuidsProp.GetArrayElementAtIndex(i).stringValue;
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                
                if (obj != null)
                {
                    continue;
                }
                
                scriptableObjectGuidsProp.DeleteArrayElementAtIndex(i);
                changed = true;
            }
            
            if (changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawTabNameSection();
            DrawDescriptionSection();
            DrawScriptableObjectsSection();
            DrawTotalItemsFooter();
            
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTabNameSection()
        {
            EditorGUILayout.LabelField(TabNameLabel, EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(
                tabNameProp.stringValue, 
                EditorStyles.wordWrappedLabel, 
                GUILayout.Height(TabNameHeight)
            );
        }

        private void DrawDescriptionSection()
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

        private void DrawScriptableObjectsSection()
        {
            EditorGUILayout.Space(SectionSpacing);
            EditorGUILayout.LabelField(ScriptableObjectsLabel, EditorStyles.boldLabel);
            
            if (scriptableObjectGuidsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox(NoItemsMessage, MessageType.Info);
                return;
            }
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(ItemSpacing);
            
            for (int i = 0; i < scriptableObjectGuidsProp.arraySize; i++)
            {
                DrawScriptableObjectItem(i);
            }
            
            EditorGUILayout.Space(ItemSpacing);
            EditorGUILayout.EndVertical();
        }

        private void DrawScriptableObjectItem(int index)
        {
            string guid = scriptableObjectGuidsProp.GetArrayElementAtIndex(index).stringValue;
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

        private void DrawTotalItemsFooter()
        {
            EditorGUILayout.Space(SectionSpacing);
            EditorGUILayout.LabelField(
                $"{TotalItemsLabel}{scriptableObjectGuidsProp.arraySize}", 
                EditorStyles.miniLabel
            );
        }
    }
}