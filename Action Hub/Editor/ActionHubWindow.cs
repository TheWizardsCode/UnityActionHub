using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WizardsCode.ActionHubEditor
{
    /// <summary>
    /// The main window for the Action Hub. 
    /// This window is used to manage and take the actions that are available in the Action Hub.
    /// </summary>
    public class ActionHubWindow : EditorWindow
    {
        private List<Action> m_Actions = new List<Action>();
        private List<ActionCategory> m_Categories = new List<ActionCategory>();
        private ActionCategory m_DefaultCategory;
        private static ActionHubWindow window;
        private Vector2 m_MainWindowScrollPosition;

        [SerializeField]
        private List<Object> lastSelectedItems = new List<Object>();
        [SerializeField]
        private List<Object> accessedFolders = new List<Object>();
        private static Color m_HeadingBackgroundColour = new Color(0, 0.25f, 0, 1);

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;

            LoadActions();

            m_DefaultCategory = ResourceLoad<ActionCategory>("Default");
            if (m_DefaultCategory == null)
            {
                Debug.LogError("Default category not found!");
            }
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
        }

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

            if (lastSelectedItems.Contains(selectedObject))
            {
                lastSelectedItems.Remove(selectedObject);
            }

            lastSelectedItems.Insert(0, selectedObject);

            if (lastSelectedItems.Count > 10)
            {
                lastSelectedItems.RemoveAt(10);
            }

            EditorUtility.SetDirty(this);
            Repaint();
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
                    if (!accessedFolders.Contains(selectedObject))
                    {
                        accessedFolders.Add(selectedObject);
                        if (accessedFolders.Count > 10)
                        {
                            accessedFolders.RemoveAt(0);
                        }
                    }

                    EditorUtility.SetDirty(this);
                    Repaint();
                }
            }
        }

        [MenuItem("Tools/Wizards Code/Action Hub")]
        public static void ShowWindow()
        {
            window = GetWindow<ActionHubWindow>("Action Hub");
            window.minSize = new Vector2(500, window.minSize.y);
            window.maxSize = new Vector2(window.maxSize.x, window.maxSize.y);
        }

        private void LoadActions()
        {
            m_Actions = new List<Action>(Resources.LoadAll<Action>(""));

            // Load all ActionCategory assets
            m_Categories = new List<ActionCategory>(Resources.LoadAll<ActionCategory>(""));
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
        }

        void OnGUI()
        {
            m_MainWindowScrollPosition = GUILayout.BeginScrollView(m_MainWindowScrollPosition, GUILayout.Width(position.width), GUILayout.ExpandHeight(true));
            {
                OnActionCategoriesGUI();

                OnRecentlySelectedGUI();
            }
            GUILayout.EndScrollView();
        }


        private void OnRecentlySelectedGUI()
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            {
                CreateHeading("Recently Accessed Folders", "Folders that have been recently accessed");

                if (accessedFolders.Count == 0)
                {
                    CreateLabel("No folders accessed", "No folders accessed");
                }
                else
                {
                    for (int i = accessedFolders.Count - 1; i >= 0; i--)
                    {
                        Object folder = accessedFolders[i];
                        if (folder != null)
                        {
                            CreateClickableLabel(folder.name, AssetDatabase.GetAssetPath(folder), folder);
                        }

                        Separator(2);
                    }
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            {
                CreateHeading("Recently Selected Items", "Items that have been recently selected");

                if (lastSelectedItems.Count == 0)
                {
                    CreateLabel("No items recorded", "No items recorded");
                }
                else
                {
                    for (int i = 0; i < lastSelectedItems.Count; i++)
                    {
                        Object item = lastSelectedItems[i];
                        if (item != null)
                        {
                            if (item is SceneAsset)
                            {
                                SceneAction sceneAction = ScriptableObject.CreateInstance<SceneAction>();
                                sceneAction.name = item.name;
                                sceneAction.Scene = ((SceneAsset)item).name;

                                EditorGUILayout.BeginHorizontal();
                                sceneAction.OnStartGUI();
                                sceneAction.OnEndGUI();
                                EditorGUILayout.EndHorizontal();
                            }
                            else if (item is Action)
                            {
                                EditorGUILayout.BeginHorizontal();
                                ((Action)item).OnStartGUI();
                                ((Action)item).OnEndGUI();
                                EditorGUILayout.EndHorizontal();
                            }
                            else
                            {
                                CreateClickableLabel(item.name, AssetDatabase.GetAssetPath(item), item);
                            }
                        }

                        Separator(2);
                    }
                }
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void OnActionCategoriesGUI()
        {
            ActionCategory currentCategory = null;
            foreach (ActionCategory category in m_Categories)
            {
                bool categoryHasActions = false;
                foreach (Action action in m_Actions)
                {
                    if (action.Category == category && action.IncludeInHub)
                    {
                        categoryHasActions = true;
                        break;
                    }
                }

                if (categoryHasActions || category.AlwaysShowInHub)
                {
                    if (currentCategory != category)
                    {
                        currentCategory?.OnEndGUI();
                        currentCategory = category;
                        currentCategory.OnStartGUI();
                    }

                    foreach (Action action in m_Actions)
                    {
                        if (action.Category == category && action.IncludeInHub)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    action.OnStartGUI();
                                    action.OnEndGUI();
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                }
            }

            currentCategory?.OnEndGUI(); // we start this in the loop but need to end it here as the last category will not have been swapped out
        }

        public static void RefreshAssets()
        {
            if (window == null)
            {
                window = GetWindow<ActionHubWindow>("Action Hub");
            }

            if (window != null)
            {
                window.LoadActions();
                window.OnEnable();
                window.Repaint();
            }
        }

        private static void Separator(float height = 5)
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
        internal static void CreateClickableLabel(string text, string tooltip, Object obj, bool showIcon = true, Color backgroundColour = default, params GUILayoutOption[] options)
        {
            GUIContent content = new GUIContent(text, tooltip);
            GUIContent objectIcon = EditorGUIUtility.ObjectContent(obj, obj.GetType());
            
            Rect labelRect = GUILayoutUtility.GetRect(content, GUI.skin.label, options);

            if (showIcon)
            {
                Rect iconRect = new Rect(labelRect.x, labelRect.y, labelRect.height, labelRect.height);
                EditorGUI.DrawRect(iconRect, backgroundColour); 
                GUI.Label(iconRect, objectIcon.image);

                labelRect.x += iconRect.width;
            }

            EditorGUI.DrawRect(labelRect, backgroundColour);
            GUI.Label(labelRect, content);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && labelRect.Contains(e.mousePosition))
            {
                if (e.clickCount == 1)
                {
                    EditorGUIUtility.PingObject(obj);
                }
                else if (e.clickCount == 2)
                {
                    Selection.activeObject = obj;
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

            CreateClickableLabel(label, tooltip, obj, showIcon, m_HeadingBackgroundColour, options);
        }

        internal static void CreateLabel(string text, string tooltip, Color backgroundColour = default, params GUILayoutOption[] options)
        {
            GUIContent content = new GUIContent(text, tooltip); 
            Rect labelRect = GUILayoutUtility.GetRect(content, GUI.skin.label, options);

            EditorGUI.DrawRect(labelRect, backgroundColour);
            GUI.Label(labelRect, content);
        }

        internal static void CreateHeading(string label, string tooltip, params GUILayoutOption[] options)
        {
            options = options.Concat(new GUILayoutOption[]
            {
                GUILayout.ExpandWidth(true),
                GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.1f)
            }).ToArray();

            CreateLabel(label, tooltip, m_HeadingBackgroundColour, options);
        }

    }
}
