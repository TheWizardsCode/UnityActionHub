using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UIElements;

namespace WizardsCode.ActionHubEditor
{
    /// <summary>
    /// The main window for the Action Hub. 
    /// This window is used to manage and take the actions that are available in the Action Hub.
    /// </summary>
    public class ActionHubWindow : EditorWindow
    {
        public static Dictionary<Action, string> Status = new Dictionary<Action, string>();

        public static ActionHubWindow Window;

        private List<Action> m_Actions = new List<Action>();
        private List<Action> m_ActionTemplates = new List<Action>();
        private List<ActionCategory> m_Categories = new List<ActionCategory>();
        private ActionCategory m_DefaultCategory;
        private Vector2 m_MainWindowScrollPosition;
        [SerializeField]
        private int m_MaxRecentItemsToShow = 3;

        [SerializeField]
        private List<Object> recentlySelectedItems = new List<Object>();
        [SerializeField]
        private List<Object> recentlySelectedFolders = new List<Object>();

        private static Color m_HeadingBackgroundColour = new Color(0, 0.25f, 0, 1);
        private VisualElement root;
	    private GUISkin guiSkin;
	    private string newItemName = string.Empty;

        float m_Columns = 10;
        public float TotalWidth => position.width;
        public float CommonActionsWidth => position.width / m_Columns;
        public float ContentWidth => position.width - CommonActionsWidth;
        public float ActionButtonWidth => ContentWidth / m_Columns;
        public float ActionLabelWidth => ContentWidth - ActionButtonWidth;

        float FolderSectionWidth => 100f;
        float ItemSectionWidth => ContentWidth - FolderSectionWidth - 2;
        int MaxRecentItemsToShow {
            get => m_MaxRecentItemsToShow;
            set => m_MaxRecentItemsToShow = Mathf.Max(1, value);
        }
        float ItemHeight => EditorGUIUtility.singleLineHeight;

        /// <summary>
        /// Called when the window is enabled. This is used to register for events and initialise the data.
        /// </summary>
        private void OnEnable()
        {
            root = rootVisualElement;
            guiSkin = Resources.Load<GUISkin>("ActionHubSkin");

            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;

            LoadAllData();

            m_DefaultCategory = ResourceLoad<ActionCategory>("Default");
            if (m_DefaultCategory == null)
            {
                Debug.LogError("Default category not found!");
            }

            Window = this;
        }

        /// <summary>
        /// Called when the window is disabled. This is used to unregister for events.
        /// </summary>    
        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;

            if (Window == this)
            {
                Window = null;
            }
        }

        /// <summary>
        /// A handler for the OnSelectionChanged event. This is used to record the last 10 items and folders that were selected.
        /// These are then displayed in the Action Hub window for easy access.
        /// </summary>
        private void OnSelectionChanged()
        {
            if (Selection.objects.Length == 0)
            {
                return;
            }

            Object selectedObject = Selection.objects[0];
            string assetPath = AssetDatabase.GetAssetPath(selectedObject);

            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            if (recentlySelectedItems.Contains(selectedObject))
            {
                recentlySelectedItems.Remove(selectedObject);
            }

            recentlySelectedItems.Add(selectedObject);

            if (recentlySelectedItems.Count > 100)
            {
                recentlySelectedItems.RemoveAt(0);
            }

            if (Window != null)
            {
                EditorUtility.SetDirty(Window);
                Repaint();
            }
        }

        private void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            if (Event.current.type == EventType.MouseDown 
                && Event.current.button == 0 
                && selectionRect.Contains(Event.current.mousePosition))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                {
                    Object selectedObject = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (!recentlySelectedFolders.Contains(selectedObject))
                    {
                        recentlySelectedFolders.Add(selectedObject);
                        if (recentlySelectedFolders.Count > 10)
                        {
                            recentlySelectedFolders.RemoveAt(0);
                        }
                    }

                    EditorUtility.SetDirty(Window);
                    Repaint();
                }
            }
        }

        [MenuItem("Tools/Wizards Code/Action Hub")]
        public static void ShowWindow()
        {
            if (Window == null)
            {
                Window = GetWindow<ActionHubWindow>("Action Hub");
            }
            else
            {
                Window.Focus();
            }
            Window.minSize = new Vector2(500, Window.minSize.y);
            Window.maxSize = new Vector2(Window.maxSize.x, Window.maxSize.y);
        }

        /// <summary>
        /// Force a repaint of the window
        /// </summary>
        public static void ForceRepaint()
        {
            Window.Repaint();
        }

        /// <summary>
        /// Load all Actions and Action Templates available to this Action Hub.
        /// 
        /// Actions are items that the user is working with.
        /// Action Templates define what the user can create in the Action Hub, and where.
        /// 
        /// Action Templates are instances of an Action Scriptable Object (or subclas) with default settings.
        /// One of these default settings is the category that the Action belongs to. This is used to determine
        /// where to place the Create UI for the Action in the Action Hub. That is, the create UI will be
        /// placed in the category that the Action Template is assigned to.
        /// 
        /// Note, the user may also be able to create some Actions in the editor using the Create Asset menu itens.
        /// </summary>
        private void LoadAllData()
        {
            m_ActionTemplates.Clear();
            m_Actions = new List<Action>(Resources.LoadAll<Action>(""));

            // Load all ActionCategory assets
            m_Categories = new List<ActionCategory>(Resources.LoadAll<ActionCategory>(""));
            m_Categories.Sort((a, b) =>
            {
                int priorityComparison = a.SortOrder.CompareTo(b.SortOrder);
                if (priorityComparison != 0)
                {
                    return priorityComparison;
                }

                return a.DisplayName.CompareTo(b.DisplayName);
            });

            Dictionary<string, int> categorySortOrder = new Dictionary<string, int>();
            foreach (var category in m_Categories)
            {
                categorySortOrder[category.DisplayName] = category.SortOrder;
            }
            
            // Sort actions by priority, then by name, and then by category sort order
            m_Actions.Sort((a, b) =>
            {
                int priorityComparison = a.Priority.CompareTo(b.Priority);
                if (priorityComparison != 0)
                {
                    return priorityComparison;
                }

                int nameComparison = a.DisplayName.CompareTo(b.DisplayName);
                if (nameComparison != 0)
                {
                    return nameComparison;
                }

                string categoryAName = a.Category?.DisplayName ?? m_DefaultCategory.DisplayName;
                string categoryBName = b.Category?.DisplayName ?? m_DefaultCategory.DisplayName;

                int categoryAOrder = categorySortOrder.ContainsKey(categoryAName) ? categorySortOrder[categoryAName] : int.MaxValue;
                int categoryBOrder = categorySortOrder.ContainsKey(categoryBName) ? categorySortOrder[categoryBName] : int.MaxValue;

                return categoryAOrder.CompareTo(categoryBOrder);
            });

            for (int i = m_Actions.Count - 1; i >= 0; i--)
            {
                if (m_Actions[i].name.ToLower().EndsWith("template"))
                {
                    m_ActionTemplates.Add(m_Actions[i]);
                    m_Actions.RemoveAt(i);
                }
            }
        }

        void OnGUI()
        {
            string title = "Action Hub";
            string tooltip = "Action Hub Stats:";
            if (Status.Count > 0)
            {
                title += " (";
                foreach (var item in Status)
                {
                    title += $"{item.Value}";
                    tooltip += $"{item.Key.DisplayName}: {item.Value}";
                    if (item.Key != Status.Last().Key)
                    {
                        title += ", ";
                    }
                }
                title += ")";
            }  
            titleContent = new GUIContent(title, tooltip);

            if (guiSkin != null)
            {
                GUI.skin = guiSkin;
            }

            if (Event.current.type == EventType.ContextClick)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Force a Refresh"), false, () => RefreshActions());
                menu.ShowAsContext();
            }

            m_MainWindowScrollPosition = GUILayout.BeginScrollView(m_MainWindowScrollPosition, GUILayout.Width(TotalWidth), GUILayout.ExpandHeight(true));
            {
                RecentlySelectedGUI();
                ActionCategoriesGUI();
            }
            GUILayout.EndScrollView();
        }

        private void RecentlySelectedGUI()
        {
            float totalHeight = (MaxRecentItemsToShow * ItemHeight) + EditorGUIUtility.singleLineHeight * 1.1f;

            GUILayout.BeginHorizontal();
            {
                FoldersGUI();
                ItemsGUI();
            }
            GUILayout.EndHorizontal();
        }

        private void FoldersGUI()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(FolderSectionWidth), GUILayout.ExpandWidth(false));
            {
                GUILayout.BeginHorizontal();
                {
                    CreateHeading("Folders", "Folders that have been recently selected. Adjust the number visible using the input field on the right.");
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.BeginVertical("Box");
                {
                    if (recentlySelectedFolders.Count == 0)
                    {
                        CreateLabel("No folders accessed", "No folders accessed");
                    }
                    else
                    {
                        for (int i = recentlySelectedFolders.Count - 1; i >= 0 && recentlySelectedFolders.Count - i <= MaxRecentItemsToShow; i--)
                        {
                            Object folder = recentlySelectedFolders[i];
                            if (folder != null)
                            {
                                CreateClickableLabel(folder.name, AssetDatabase.GetAssetPath(folder), folder, true, GUILayout.MaxWidth(FolderSectionWidth));
                            }
                            else
                            {
                                recentlySelectedFolders.RemoveAt(i);
                            }
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
        }

        private void ItemsGUI()
        {
            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal();
                {
                    CreateHeading("Items", "Items that have been recently selected. Adjust the number visible using the input field on the right.", GUILayout.MaxWidth(ItemSectionWidth));

                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        // MaxRecentItemsToShow = EditorGUILayout.IntField(MaxRecentItemsToShow, GUILayout.Width(25));

                        if (GUILayout.Button("3", GUILayout.Width(30)))
                        {
                            MaxRecentItemsToShow = 3;
                        }

                        if (MaxRecentItemsToShow < recentlySelectedItems.Count && GUILayout.Button("All", GUILayout.Width(30)))
                        {
                            MaxRecentItemsToShow = recentlySelectedItems.Count;
                        }
                    } 
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.BeginVertical("Box");
                {
                    if (recentlySelectedItems.Count == 0)
                    {
                        CreateLabel("No items recorded", "No items recorded");
                    }
                    else
                    {
                        for (int i = recentlySelectedItems.Count - 1; i >= 0 && recentlySelectedItems.Count - i <= MaxRecentItemsToShow; i--)
                        {
                            Object item = recentlySelectedItems[i];
                            if (item != null)
                            {
                                if (item is SceneAsset)
                                {
                                    SceneAction sceneAction = ScriptableObject.CreateInstance<SceneAction>();
                                    sceneAction.name = item.name;
                                    sceneAction.Scene = ((SceneAsset)item).name;

                                    sceneAction.OnGUI();
                                }
                                else if (item is Action)
                                {
                                    ((Action)item).OnGUI();
                                }
                                else
                                {
                                	GUILayout.BeginHorizontal();
                                	{
	                                	CreateClickableLabel(item.name, AssetDatabase.GetAssetPath(item), item, true);
	                                	newItemName = EditorGUILayout.TextField(newItemName);
	                                	EditorGUI.BeginDisabledGroup(newItemName.Length < 5);
	                                	{
		                                	if (GUILayout.Button("New Todo", GUILayout.Width(ActionHubWindow.Window.ActionButtonWidth))) {
		                                		ToDoAction newAction = ScriptableObject.CreateInstance<ToDoAction>();
			                                	newAction.name = newItemName;
			                                	newAction.DisplayName = newAction.name;
			                                	newAction.RelatedObjects = new Object[] { item };
			                                	newAction.Description = "";
			                                	newAction.Priority = 1000;
	
			                                	newAction.OnSaveToAssetDatabase();
			                                	newItemName = string.Empty;
			                                	
			                                	GUI.FocusControl(null);
		                                	}
	                                	}
	                                	EditorGUI.EndDisabledGroup();
                                	}
                                	GUILayout.EndHorizontal();
                                }
                            }
                            else
                            {
                                recentlySelectedItems.RemoveAt(i);
                            }
                        }
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        private void ActionCategoriesGUI()
        {
            List<Action> createAllowed = new List<Action>();
            List<Action> categoryActions = new List<Action>();

            foreach (ActionCategory category in m_Categories)
            {
                createAllowed.Clear();
                categoryActions.Clear();

                // Find all the actions templates that allow creation in this category
                foreach (Action action in m_ActionTemplates)
                {
                    if (action.Category == category)
                    {
                        createAllowed.Add(action);
                    }
                }

                // Find all the actions that are in this category
                foreach (Action action in m_Actions)
                {

                    if (action.Category == category && action.IncludeInHub)
                    {
                        categoryActions.Add(action);
                    }
                }

                // If there are actions in this category, or the category is always shown in the hub, or there are actions that can be created in this category display the category UI
                if (categoryActions.Count > 0 || category.AlwaysShowInHub || createAllowed.Count > 0)
                {
                    category.ActionListGUI(categoryActions, createAllowed);
                }
            }
        }

        public static void RefreshActions()
        {
            if (Window == null)
            {
                return;
            }

            Window.LoadAllData();
            Window.OnEnable();
            Window.Repaint();
        }

        private static void Separator(float height = 2)
        {
            Color initialColor = GUI.color;
            GUI.color = Color.white;
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(height));
            GUI.color = initialColor;
        }

        private T ResourceLoad<T>(string name) where T : UnityEngine.Object
        {
            T[] resources = Resources.LoadAll<T>("");
            foreach (var resource in resources)
            {
                if (resource.name == name)
                {
                    return resource;
                }
            }
            return default(T);
        }

        /// <summary>
        /// Create a label with a tooltip that responds to mouse events.
        /// A single click pings the object, a double click selects it.
        /// The tooltip is the description.
        /// </summary>
        internal static void CreateClickableLabel(string text, string tooltip, Object obj, bool showIcon = true, params GUILayoutOption[] options)
        {
            GUIContent content = new GUIContent(text, tooltip);

            if (showIcon)
            {
                GUIContent objectIcon = EditorGUIUtility.ObjectContent(obj, obj.GetType());
                content.image = objectIcon.image;
            }

            GUILayoutOption[] labelOptions = options.Concat(new GUILayoutOption[]
            {
                GUILayout.ExpandWidth(true),
                GUILayout.Height(EditorGUIUtility.singleLineHeight)
            }).ToArray();

            EditorGUILayout.LabelField(content, labelOptions);

            Event e = Event.current;
            Rect labelRect = GUILayoutUtility.GetLastRect();
            if (e.type == EventType.MouseDown && labelRect.Contains(e.mousePosition))
            {
                if (e.clickCount == 1)
                {
                    EditorGUIUtility.PingObject(obj);
                }
                else if (e.clickCount == 2)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
                e.Use();
            }
        }

        internal static void CreateClickableHeading(string label, string tooltip, Object obj, bool showIcon = true, params GUILayoutOption[] options)
        {
            options = options.Concat(new GUILayoutOption[] { 
                GUILayout.ExpandWidth(true),
                GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.1f)
            }).ToArray();

            CreateClickableLabel(label, tooltip, obj, showIcon, options);
        }

        internal static void CreateLabel(string text, string tooltip = null, params GUILayoutOption[] options)
        {
            GUIContent content = new GUIContent(text, tooltip);
            options = options.Concat(new GUILayoutOption[] {
                GUILayout.ExpandWidth(true)
            }).ToArray();

            EditorGUILayout.LabelField(content);
        }

        internal static void CreateHeading(string label, string tooltip, params GUILayoutOption[] options)
        {
            options = options.Concat(new GUILayoutOption[]
            {
                GUILayout.ExpandWidth(true),
                GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.1f)
            }).ToArray();

            CreateLabel(label, tooltip, options);
        }

        internal static void CreateSectionHeading(string label, string tooltip, params GUILayoutOption[] options)
        {
            options = options.Concat(new GUILayoutOption[]
            {
                GUILayout.ExpandWidth(true),
                GUILayout.Width(ActionHubWindow.Window.TotalWidth),
                GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.1f)
            }).ToArray();

            CreateLabel(label, tooltip, options);
        }

        /// <summary>
        /// Show a modal dialog.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        internal static void ShowConfirmationDialog(string title, string content)
        {
            EditorUtility.DisplayDialog(title, content, "OK");
        }

        internal static void ShowOptionDialog(string title, string content, string option1 = "Yes", string option2 = "No")
        {
            EditorUtility.DisplayDialog(title, content, option1, option2);
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
