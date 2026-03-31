using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Thisaislan.ControlPanel.Editor.Data;

namespace Thisaislan.ControlPanel.Editor
{
    /// <summary>
    /// Main Control Panel window for managing tabs and ScriptableObject collections.
    /// Provides a visual interface for organizing and inspecting ScriptableObjects.
    /// </summary>
    internal class ControlPanelWindow : EditorWindow
    {
        //  Constants 
        private const int DefaultTabSelectedValue = -1;
        private const string MenuItemName = "Tools/Control Panel/Open";
        private const string WindowTitle = "Control Panel";
        private const string WelcomeMessage = "Start by creating a tab\nJust tap the + button at the top right ↗";
        private const string IconName = "control_panel_logo_16x16";
        private const string DragIconName = "d_UnityEditor.HierarchyWindow";
        private const string SettingsIconName = "_Popup";
        private const string SelectIconName = "d_Search Icon";
        private const string PlusButtonLabel = "+";
        private const string XButtonLabel = "x";
        private const string OkButtonLabel = "OK";
        private const string CancelButtonLabel = "Cancel";
        private const string ScriptableObjectNotFoundMessage = "Scriptable Object not found.";
        private const string RightColumnEmptyMessage = "Add Scriptable Objects to see the details here";
        private const string AddScriptableObjectTooltip = "Add a Scriptable Object";
        private const string ObjectSelectorUpdatedEventName = "ObjectSelectorUpdated";
        private const string CreateTabButtonTooltip = "Create a new tab";
        private const string SelectFileButtonTooltip = "Select File";
        private const string ScriptableObjectsLabel = "Scriptable Objects";
        private const string DropScriptableObjectsLabel = "Drop Scriptable Objects here";
        private const string DismissButtonLabel = "Dismiss";
        private const string EditTabTooltip = "Edit tab";
        private const string CreateTabTitle = "Create New Tab";
        private const string EditTabTitle = "Edit Tab";
        private const string NameLabel = "Name";
        private const string DescriptionLabel = "Description";
        private const string CreateButtonLabel = "Create";
        private const string SaveButtonLabel = "Save";
        private const string DeleteTabLabel = "Delete Tab";
        private const string DeleteButtonLabel = "Delete";
        private const string DeleteConfirmationMessage = "Are you sure you want to delete tab '{0}'?";
        private const string EmptyNameErrorMessage = "Name cannot be empty.";
        private const string EmptyDescriptionErrorMessage = "Description cannot be empty.";
        private const string AlreadyExistNameErrorMessage = "A tab named '{0}' already exists.";
        private const string ScriptableObjectAlreadyAddedMessage = "Scriptable Object '{0}' already added.";
        private const string RemoveScriptableObjectMessage = "Are you sure you want to remove that Scriptable Object from this tab? This action cannot be undone.";
        private const string PrefixDisplayName = "...";
        private const float MinSplitterPosition = 0.2f;
        private const float MaxSplitterPosition = 0.8f;
        private const int DefaultTabHeight = 18;
        private const int DefaultTabMinWidth = 150;
        private const int TabBarButtonSize = 32;
        private const int TabBarButtonHeight = 28;
        private const int DragHandleSize = 20;
        private const int IconSize = 20;
        private const int DeleteButtonSize = 20;
        private const int DeleteButtonHeight = 18;
        private const int SettingsButtonSize = 32;
        private const int SelectButtonSize = 28;
        private const int PathSelectorHeight = 32;
        private const int PathSelectorIconHeight = 26;
        private const int DropZoneHeight = 40;
        private const int AddButtonHeight = 32;
        private const int MaxPathDisplayLength = 100;
        private const int PathTruncateOffset = 97;

        [MenuItem(MenuItemName, priority = 0)]
        internal static void OpenWindow()
        {
            ControlPanelWindow window = GetWindow<ControlPanelWindow>(false, WindowTitle, true);
            Texture2D customIcon = Resources.Load<Texture2D>(IconName);
            window.titleContent = new GUIContent(WindowTitle, customIcon);
            window.Show();
        }

        //  Data fields 
        private List<ControlPanelTabData> tabs = new List<ControlPanelTabData>();
        private int selectedTab = DefaultTabSelectedValue;
        private int selectedScriptableIndex = DefaultTabSelectedValue;
        private string selectedTabDescription;

        //  Drag & Drop 
        private int scriptableDragSource = DefaultTabSelectedValue;
        private int scriptableDragTarget = DefaultTabSelectedValue;
        private int scriptablePickerControlID;
        private int guidIndexToBeRemoved = DefaultTabSelectedValue;

        //  UI State 
        private Vector2 tabScroll;
        private Vector2 scriptableListScroll;
        private Vector2 rightColumnScrollPos;
        private float splitterPos = 0.3f;
        private bool isDraggingSplitter;
        private string alertMessage;

        //  Tab editor state (inline) 
        private enum TabEditorMode { None, Create, Edit }
        private TabEditorMode tabEditorMode = TabEditorMode.None;
        private string editTabName = string.Empty;
        private string editTabDescription = string.Empty;
        private ControlPanelTabData editTabTarget = null; // null when creating

        //  Editor inspector -
        private UnityEditor.Editor scriptableInspectorEditor;
        private ScriptableObject lastInspectedObject;

        //  GUI Styles 
        private GUIStyle tabButtonStyle;
        private GUIStyle tabSelectedStyle;
        private GUIStyle tabTooltipStyle;
        private GUIStyle tapDescriptionStyle;
        private GUIStyle welcomeMessageStyle;
        private GUIStyle selectedRowStyle;
        private GUIStyle nonSelectedNameRowStyle;
        private GUIStyle selectedNameRowStyle;
        private GUIStyle helpBoxCenteredStyle;
        private GUIContent dragIcon;

        //  Lifecycle 
        private void OnEnable()
        {
            // Avoid domain load errors
            try
            {
                InitializeStyles();
                InitializeData();
                SetMinWindowSize();
            }
            catch { }
        }

        private void OnGUI()
        {
            DrawTabsBar();

            if (tabs.Count == 0)
            {
                ShowWelcomeMessage();
                // Still allow creation inline
                DrawInlineTabEditor();
                return;
            }

            UpdateSelectedTabDescription();
            GUILayout.Space(14);
            DrawDescriptionArea();

            DrawSplitter();
            DrawMainContent();

            // Inline tab editor at the bottom
            DrawInlineTabEditor();
        }

        //  Initialization 
        private void InitializeStyles()
        {
            Texture2D selectedRowTexture = CreateSolidColorTexture(new Color(0.18f, 0.18f, 0.18f));
            dragIcon = EditorGUIUtility.IconContent(DragIconName);

            tabButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Normal
            };

            tabSelectedStyle = new GUIStyle(tabButtonStyle)
            {
                normal = { background = selectedRowTexture },
                fontStyle = FontStyle.Bold,
                fontSize = 13
            };

            tabTooltipStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };

            tapDescriptionStyle = new GUIStyle(EditorStyles.whiteBoldLabel)
            {
                wordWrap = true,
                fontStyle = FontStyle.Italic
            };

            selectedRowStyle = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { background = selectedRowTexture }
            };

            nonSelectedNameRowStyle = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(0, 0, 4, 0),
                padding = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.MiddleLeft,
                stretchWidth = true,
                fontStyle = FontStyle.Normal
            };

            selectedNameRowStyle = new GUIStyle(nonSelectedNameRowStyle)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13
            };

            helpBoxCenteredStyle = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic,
                fontSize = 11
            };
        }

        private Texture2D CreateSolidColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void InitializeData()
        {
            tabs = ControlPanelPersistence.LoadTabs();

            if (tabs.Count > 0)
            {
                selectedTab = 0;
            }
        }

        private void SetMinWindowSize()
        {
            minSize = new Vector2(1000, 800);
        }

        private void UpdateSelectedTabDescription()
        {
            if (selectedTab != DefaultTabSelectedValue)
            {
                selectedTabDescription = tabs[selectedTab].Description;
            }
        }

        //  UI Drawing 
        private void DrawSplitter()
        {
            Rect splitRect = new Rect(
                splitterPos * position.width,
                EditorGUIUtility.singleLineHeight + 4,
                4,
                position.height - EditorGUIUtility.singleLineHeight - 8
            );

            EditorGUIUtility.AddCursorRect(splitRect, MouseCursor.SplitResizeLeftRight);

            if (Event.current.type == EventType.MouseDown && splitRect.Contains(Event.current.mousePosition))
            {
                isDraggingSplitter = true;
                Event.current.Use();
            }

            if (isDraggingSplitter && Event.current.type == EventType.MouseDrag)
            {
                splitterPos = Event.current.mousePosition.x / position.width;
                splitterPos = Mathf.Clamp(splitterPos, MinSplitterPosition, MaxSplitterPosition);
                Repaint();
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseUp)
            {
                isDraggingSplitter = false;
            }
        }

        private void DrawMainContent()
        {
            EditorGUILayout.BeginHorizontal();

            float leftWidth = splitterPos * position.width;
            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(leftWidth));
            DrawLeftColumn();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            EditorGUILayout.Space();
            DrawRightColumn();
            DrawPathSelector();
            GUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void ShowWelcomeMessage()
        {
            if (welcomeMessageStyle == null)
            {
                welcomeMessageStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    wordWrap = true
                };
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(WelcomeMessage, welcomeMessageStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        //  Tabs Bar 
        private void DrawTabsBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));
            tabScroll = EditorGUILayout.BeginScrollView(tabScroll, GUILayout.Height(32));
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < tabs.Count; i++)
            {
                ControlPanelTabData tab = tabs[i];
                GUIStyle style = (i == selectedTab) ? tabSelectedStyle : tabButtonStyle;
                Rect tabRect = GUILayoutUtility.GetRect(new GUIContent(tab.TabName), style, GUILayout.Height(DefaultTabHeight), GUILayout.MinWidth(DefaultTabMinWidth));

                if (GUI.Button(tabRect, tab.TabName, style))
                {
                    selectedTab = i;
                    alertMessage = null;
                    guidIndexToBeRemoved = DefaultTabSelectedValue;
                    selectedScriptableIndex = DefaultTabSelectedValue;
                    rightColumnScrollPos = Vector2.zero;
                    scriptableListScroll = Vector2.zero;
                    
                    // Exit edit mode when switching tabs
                    if (tabEditorMode != TabEditorMode.None)
                    {
                        tabEditorMode = TabEditorMode.None;
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent(PlusButtonLabel, CreateTabButtonTooltip), EditorStyles.toolbarButton, GUILayout.Width(TabBarButtonSize), GUILayout.Height(TabBarButtonHeight)))
            {
                if (tabEditorMode == TabEditorMode.Create)
                {
                    tabEditorMode = TabEditorMode.None;
                }
                else
                {
                    // Start create mode
                    tabEditorMode = TabEditorMode.Create;
                    editTabName = string.Empty;
                    editTabDescription = string.Empty;
                    editTabTarget = null;
                }
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDescriptionArea()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(selectedTabDescription, tapDescriptionStyle);

            GUIContent settingsButtonContent = new GUIContent(EditorGUIUtility.IconContent(SettingsIconName))
            {
                tooltip = EditTabTooltip
            };

            if (GUILayout.Button(settingsButtonContent, EditorStyles.miniButton, GUILayout.Width(SettingsButtonSize), GUILayout.Height(SettingsButtonSize)))
            {
                // Start edit mode for the selected tab
                if (selectedTab >= 0 && selectedTab < tabs.Count)
                {
                    if (tabEditorMode == TabEditorMode.Edit)
                    {
                        tabEditorMode = TabEditorMode.None;
                    }
                    else
                    {
                        tabEditorMode = TabEditorMode.Edit;
                        editTabTarget = tabs[selectedTab];
                        editTabName = editTabTarget.TabName;
                        editTabDescription = editTabTarget.Description;
                    }
                }
                Repaint();
            }
            GUILayout.EndHorizontal();
        }

        //  Left Column (Scriptable Objects list) 
        private void DrawLeftColumn()
        {
            DrawLeftColumnHeader();

            if (selectedTab < 0 || selectedTab >= tabs.Count)
            {
                return;
            }

            ControlPanelTabData tab = tabs[selectedTab];

            if (tab.ScriptableObjectGuids.Count > 0 && selectedScriptableIndex == DefaultTabSelectedValue)
            {
                selectedScriptableIndex = 0;
            }

            scriptableListScroll = GUILayout.BeginScrollView(scriptableListScroll, GUILayout.ExpandHeight(true));

            for (int i = 0; i < tab.ScriptableObjectGuids.Count; i++)
            {
                DrawScriptableObjectItem(tab, i);
            }

            GUILayout.Space(4);
            GUILayout.EndScrollView();

            DrawDropZone(tab);
            DrawAddButton(tab);
            DrawAlertMessage();
            DrawRemoveConfirmation(tab);
        }

        private void DrawLeftColumnHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.Space(8);
            GUILayout.Label(ScriptableObjectsLabel, EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawScriptableObjectItem(ControlPanelTabData tab, int index)
        {
            string guid = tab.ScriptableObjectGuids[index];
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (obj == null)
            {
                return;
            }

            Rect rowRect = BeginScriptableObjectRow(index);

            DrawDragHandle();
            DrawIcon(obj);
            DrawScriptableObjectName(tab, index, obj);
            DrawDeleteButton(index);

            EditorGUILayout.EndHorizontal();

            HandleDragAndDrop(rowRect, tab, index);
        }

        private Rect BeginScriptableObjectRow(int index)
        {
            if (index == selectedScriptableIndex)
            {
                return EditorGUILayout.BeginHorizontal(selectedRowStyle, GUILayout.ExpandWidth(true));
            }

            return EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
        }

        private void DrawDragHandle()
        {
            GUILayout.Label(dragIcon, GUILayout.Width(DragHandleSize), GUILayout.Height(DragHandleSize));
        }

        private void DrawIcon(ScriptableObject obj)
        {
            Texture icon = AssetPreview.GetMiniThumbnail(obj);
            GUILayout.Label(icon, GUILayout.Width(IconSize), GUILayout.Height(IconSize));
        }

        private void DrawScriptableObjectName(ControlPanelTabData tab, int index, ScriptableObject obj)
        {
            GUIStyle nameStyle = (selectedScriptableIndex == index) ? selectedNameRowStyle : nonSelectedNameRowStyle;
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

            if (GUILayout.Button(obj.name, nameStyle, GUILayout.ExpandWidth(true)))
            {
                if (index != selectedScriptableIndex)
                {
                    tabEditorMode = TabEditorMode.None;
                    selectedScriptableIndex = index;
                    alertMessage = null;
                    guidIndexToBeRemoved = DefaultTabSelectedValue;
                    rightColumnScrollPos = Vector2.zero;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDeleteButton(int index)
        {
            if (GUILayout.Button(XButtonLabel, GUILayout.Width(DeleteButtonSize), GUILayout.Height(DeleteButtonHeight)))
            {
                tabEditorMode = TabEditorMode.None;
                alertMessage = null;
                guidIndexToBeRemoved = index;
            }
        }

        private void HandleDragAndDrop(Rect rowRect, ControlPanelTabData tab, int index)
        {
            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition) && Event.current.button == 0)
            {
                scriptableDragSource = index;
                selectedScriptableIndex = index;
                tabEditorMode = TabEditorMode.None;
            }

            if (scriptableDragSource >= 0 && Event.current.type == EventType.MouseDrag && rowRect.Contains(Event.current.mousePosition))
            {
                selectedScriptableIndex = index;
                scriptableDragTarget = index;

                if (scriptableDragSource != scriptableDragTarget)
                {
                    string temp = tab.ScriptableObjectGuids[scriptableDragSource];
                    tab.ScriptableObjectGuids.RemoveAt(scriptableDragSource);
                    tab.ScriptableObjectGuids.Insert(scriptableDragTarget, temp);
                    scriptableDragSource = scriptableDragTarget;
                    Repaint();
                }
            }

            if (Event.current.type == EventType.MouseUp && scriptableDragSource >= 0)
            {
                ControlPanelPersistence.SaveTab(tab);
                scriptableDragSource = DefaultTabSelectedValue;
                scriptableDragTarget = DefaultTabSelectedValue;
            }
        }

        private void DrawDropZone(ControlPanelTabData tab)
        {
            Rect dropRect = GUILayoutUtility.GetRect(200, DropZoneHeight, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, DropScriptableObjectsLabel, helpBoxCenteredStyle);

            if (!dropRect.Contains(Event.current.mousePosition))
            {
                return;
            }

            if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (Event.current.type == EventType.DragPerform)
                {
                    foreach (UnityEngine.Object dragged in DragAndDrop.objectReferences)
                    {
                        if (dragged is ScriptableObject so)
                        {
                            AddScriptableObjectToTab(tab, so);
                        }
                    }
                    Event.current.Use();
                }
            }
        }

        private void DrawAddButton(ControlPanelTabData tab)
        {
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent(PlusButtonLabel, AddScriptableObjectTooltip), GUILayout.Height(AddButtonHeight)))
            {
                tabEditorMode = TabEditorMode.None;
                OpenObjectPicker();
            }

            GUILayout.EndHorizontal();
            HandleObjectPicker(tab);
        }

        private void OpenObjectPicker()
        {
            alertMessage = null;
            guidIndexToBeRemoved = DefaultTabSelectedValue;
            scriptablePickerControlID = GUIUtility.GetControlID(FocusType.Passive);
            EditorGUIUtility.ShowObjectPicker<ScriptableObject>(null, false, string.Empty, scriptablePickerControlID);
        }

        private void HandleObjectPicker(ControlPanelTabData tab)
        {
            if (Event.current.commandName != ObjectSelectorUpdatedEventName)
            {
                return;
            }

            if (EditorGUIUtility.GetObjectPickerControlID() != scriptablePickerControlID)
            {
                return;
            }

            ScriptableObject picked = EditorGUIUtility.GetObjectPickerObject() as ScriptableObject;
            if (picked != null) AddScriptableObjectToTab(tab, picked);

            Repaint();
        }

        private void AddScriptableObjectToTab(ControlPanelTabData tab, ScriptableObject obj)
        {
            string pickedGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));

            if (!tab.ScriptableObjectGuids.Contains(pickedGuid))
            {
                tab.ScriptableObjectGuids.Add(pickedGuid);
                ControlPanelPersistence.SaveTab(tab);
                selectedScriptableIndex = tab.ScriptableObjectGuids.Count - 1;
            }
            else
            {
                alertMessage = string.Format(ScriptableObjectAlreadyAddedMessage, obj.name);
            }
        }

        private void DrawAlertMessage()
        {
            if (string.IsNullOrEmpty(alertMessage))
            {
                return;
            }

            EditorGUILayout.HelpBox(alertMessage, MessageType.Info);
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button(DismissButtonLabel, GUILayout.ExpandWidth(true)))
            {
                alertMessage = null;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawRemoveConfirmation(ControlPanelTabData tab)
        {
            if (guidIndexToBeRemoved == DefaultTabSelectedValue)
            {
                return;
            }

            EditorGUILayout.HelpBox(RemoveScriptableObjectMessage, MessageType.Warning);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(OkButtonLabel, GUILayout.ExpandWidth(true)))
            {
                ExecuteRemove(tab);
            }

            if (GUILayout.Button(CancelButtonLabel, GUILayout.ExpandWidth(true)))
            {
                guidIndexToBeRemoved = DefaultTabSelectedValue;
            }

            GUILayout.EndHorizontal();
        }

        private void ExecuteRemove(ControlPanelTabData tab)
        {
            tab.ScriptableObjectGuids.RemoveAt(guidIndexToBeRemoved);
            ControlPanelPersistence.SaveTab(tab);

            UpdateSelectedIndexAfterRemoval(tab);

            guidIndexToBeRemoved = DefaultTabSelectedValue;
            Repaint();
        }

        private void UpdateSelectedIndexAfterRemoval(ControlPanelTabData tab)
        {
            if (selectedScriptableIndex == guidIndexToBeRemoved)
            {
                selectedScriptableIndex = (tab.ScriptableObjectGuids.Count > 0)
                    ? Mathf.Max(0, selectedScriptableIndex - 1)
                    : DefaultTabSelectedValue;
            }
            else if (guidIndexToBeRemoved < selectedScriptableIndex)
            {
                selectedScriptableIndex--;
            }
        }

        //  Right Column (Inspector) 
        private void DrawRightColumn()
        {
            ControlPanelTabData tab = tabs[selectedTab];

            rightColumnScrollPos = EditorGUILayout.BeginScrollView(
                rightColumnScrollPos,
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true)
            );

            if (selectedScriptableIndex >= 0 && selectedScriptableIndex < tab.ScriptableObjectGuids.Count)
            {
                DrawScriptableObjectInspector(tab);
            }
            else
            {
                GUILayout.Label(RightColumnEmptyMessage, EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(4);
        }

        private void DrawScriptableObjectInspector(ControlPanelTabData tab)
        {
            string guid = tab.ScriptableObjectGuids[selectedScriptableIndex];
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (obj != null)
            {
                UpdateInspector(obj);
                if (scriptableInspectorEditor != null)
                {
                    scriptableInspectorEditor.OnInspectorGUI();
                }
            }
            else
            {
                GUILayout.Label(ScriptableObjectNotFoundMessage, EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void UpdateInspector(ScriptableObject obj)
        {
            if (lastInspectedObject != obj)
            {
                if (scriptableInspectorEditor != null)
                {
                    DestroyImmediate(scriptableInspectorEditor);
                }

                scriptableInspectorEditor = UnityEditor.Editor.CreateEditor(obj);
                lastInspectedObject = obj;
            }
        }

        //  Path Selector 
        private void DrawPathSelector()
        {
            if (selectedScriptableIndex == DefaultTabSelectedValue)
            {
                return;
            }

            ControlPanelTabData tab = tabs[selectedTab];
            string guid = tab.ScriptableObjectGuids[selectedScriptableIndex];
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string displayPath = FormatPath(path);
            ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(PathSelectorHeight));

            Texture icon = AssetPreview.GetMiniThumbnail(obj);
            GUILayout.Label(icon, GUILayout.Width(IconSize), GUILayout.Height(PathSelectorIconHeight));
            GUILayout.Label(displayPath, EditorStyles.label, GUILayout.Height(PathSelectorIconHeight), GUILayout.ExpandWidth(true));

            GUIContent selectButtonContent = new GUIContent(EditorGUIUtility.IconContent(SelectIconName))
            {
                tooltip = SelectFileButtonTooltip
            };

            if (GUILayout.Button(selectButtonContent, GUILayout.Width(SelectButtonSize), GUILayout.Height(SelectButtonSize)))
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(1);
        }

        private string FormatPath(string path)
        {
            if (path.Length <= MaxPathDisplayLength)
            {
                return path;
            }

            return PrefixDisplayName + path.Substring(path.Length - PathTruncateOffset);
        }

        //  Inline Tab Editor 
        private void DrawInlineTabEditor()
        {
            if (tabEditorMode == TabEditorMode.None)
            {
                return;
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            string title = tabEditorMode == TabEditorMode.Create ? CreateTabTitle : EditTabTitle;
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            // Name field
            EditorGUILayout.LabelField(NameLabel, EditorStyles.boldLabel);
            editTabName = EditorGUILayout.TextField(editTabName.Trim());
            GUILayout.Space(4);

            // Description field
            EditorGUILayout.LabelField(DescriptionLabel, EditorStyles.boldLabel);
            editTabDescription = EditorGUILayout.TextArea(editTabDescription, GUILayout.Height(60));

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            // Save/Create button
            if (GUILayout.Button(tabEditorMode == TabEditorMode.Create ? CreateButtonLabel : SaveButtonLabel))
            {
                if (ValidateTabInput())
                {
                    if (tabEditorMode == TabEditorMode.Create)
                    {
                        CreateNewTab();
                    }
                    else
                    {
                        UpdateCurrentTab();
                    }

                    tabEditorMode = TabEditorMode.None;
                    Repaint();
                }
            }

            // Cancel button
            if (GUILayout.Button(CancelButtonLabel))
            {
                tabEditorMode = TabEditorMode.None;
                Repaint();
            }

            // Delete button (only in edit mode)
            if (tabEditorMode == TabEditorMode.Edit && editTabTarget != null)
            {
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button(DeleteTabLabel))
                {
                    if (EditorUtility.DisplayDialog(DeleteTabLabel, string.Format(DeleteConfirmationMessage, editTabTarget.TabName), DeleteButtonLabel, CancelButtonLabel))
                    {
                        ControlPanelPersistence.DeleteTab(editTabTarget);
                        tabs = ControlPanelPersistence.LoadTabs();
                        if (tabs.Count > 0)
                        {
                            selectedTab = Mathf.Min(selectedTab, tabs.Count - 1);
                        }
                        else
                        {
                            selectedTab = DefaultTabSelectedValue;
                        }
                        selectedScriptableIndex = DefaultTabSelectedValue;
                        tabEditorMode = TabEditorMode.None;
                        Repaint();
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private bool ValidateTabInput()
        {
            if (string.IsNullOrEmpty(editTabName))
            {
                ShowAlert(EmptyNameErrorMessage);
                return false;
            }
            if (string.IsNullOrEmpty(editTabDescription))
            {
                ShowAlert(EmptyDescriptionErrorMessage);
                return false;
            }
            // Check for duplicate name
            if (tabEditorMode == TabEditorMode.Create && ControlPanelPersistence.TabNameExists(editTabName))
            {
                ShowAlert(string.Format(AlreadyExistNameErrorMessage, editTabName));
                return false;
            }
            if (tabEditorMode == TabEditorMode.Edit && editTabTarget != null && editTabTarget.TabName != editTabName && ControlPanelPersistence.TabNameExists(editTabName))
            {
                ShowAlert(string.Format(AlreadyExistNameErrorMessage, editTabName));
                return false;
            }
            return true;
        }

        private void CreateNewTab()
        {
            ControlPanelTabData newTab = ScriptableObject.CreateInstance<ControlPanelTabData>();
            newTab.TabName = editTabName;
            newTab.Description = editTabDescription;
            ControlPanelPersistence.SaveTab(newTab);

            tabs = ControlPanelPersistence.LoadTabs();
            selectedTab = tabs.IndexOf(newTab);
            selectedScriptableIndex = DefaultTabSelectedValue;
            ScrollToTab(selectedTab);
            Repaint();
        }

        private void UpdateCurrentTab()
        {
            if (editTabTarget == null)
            {
                return;
            }

            editTabTarget.TabName = editTabName;
            editTabTarget.Description = editTabDescription;
            ControlPanelPersistence.SaveTab(editTabTarget);
            tabs = ControlPanelPersistence.LoadTabs();
            selectedTab = tabs.IndexOf(editTabTarget);
            Repaint();
        }

        private void ScrollToTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= tabs.Count)
            {
                return;
            }

            float tabWidth = DefaultTabMinWidth;
            float targetX = tabIndex * tabWidth;
            float visibleWidth = position.width - TabBarButtonSize - 20;
            float centeredX = targetX - (visibleWidth / 2) + (tabWidth / 2);
            tabScroll.x = Mathf.Max(0, centeredX);
            Repaint();
        }

        private void ShowAlert(string message)
        {
            EditorUtility.DisplayDialog(WindowTitle, message, OkButtonLabel);
        }
    }
}