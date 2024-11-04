using NaughtyAttributes;
using System;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace WizardsCode.ActionHubEditor
{
    /// <summary>
    /// An action is a unit of work that can be performed by the Action Hub.
    /// </summary>
    [CreateAssetMenu(fileName = "New Action", menuName = "Wizards Code/Action Hub/Generic Action")]
    public class Action : ScriptableObject
    {
        [SerializeField, Tooltip("The name of the action as it appears in the Action Hub window."), ShowIf("ShowMetadataInInspector")]
        private string m_DisplayName;
        [SerializeField, TextArea(2,15), Tooltip("A description of the action, for displaying in tooltips and detailed views of the action."), ShowIf("ShowMetadataInInspector")]
        private string m_Description;
        [SerializeField, Tooltip("The category this action belongs to.")]
        private ActionCategory m_Category;
        [SerializeField, Tooltip("The priority of the item, also used as a sort order for this action. Lower numbers appear first.")]
        private int m_Priority = 1000;
        [SerializeField, Tooltip("If true the OnUpdate coroutine will be automatically started.")]
        private bool m_ActivateOnUpdateAtStart = false;

        protected virtual bool ShowMetadataInInspector => true;

        internal virtual bool IncludeInHub => true;

        public virtual string DisplayName { get => m_DisplayName; set => m_DisplayName = value; }
        public virtual string Description { get => m_Description; set => m_Description = value; }
        public virtual ActionCategory Category { get => m_Category; set => m_Category = value; }
        public int Priority { get => m_Priority; set => m_Priority = value; }
        protected bool ActivateOnUpdateAtStart { get => m_ActivateOnUpdateAtStart; set => m_ActivateOnUpdateAtStart = value; }

        protected virtual float CreateLabelWidth => 120;

        private EditorCoroutine updateCoroutine;

        protected virtual void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;

            if (updateCoroutine == null && ActivateOnUpdateAtStart)
            {
                ActivateOnUpdate();
            }
        }

        protected virtual void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        protected virtual void OnDestroy() { }

        /// <summary>
        /// This will be called when an undo or redo is performed. By default it does nothing.
        /// Subclasses can override this method to perform any necessary actions when an undo or redo is performed.
        /// </summary>
        protected virtual void OnUndoRedo()
        {
            ActionHubWindow.RefreshActions();
        }

        /// <summary>
        /// Render the complete GUI for this action. This includes the custom GUI and the end GUI.
        /// </summary>
        internal void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            OnCustomGUI();
            OnEndGUI();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Subclasses can implement this method to render the custom GUI for the action. Under normal circumstances it will be rendered by the
        /// OnGUI() method which will add the OnEngGUI() after the custom GUI.
        /// </summary>
        protected virtual void OnCustomGUI()
        {
            ActionHubWindow.CreateClickableLabel(DisplayName, Description, this);

            GUIContent content = new GUIContent("Select", "Select this item.");
            if (GUILayout.Button(content, GUILayout.Width(60)))
            {
                EditorGUIUtility.PingObject(this);
                Selection.activeObject = this;

                Debug.Log("TODO implement action: " + m_DisplayName);
            }
        }

        /// <summary>
        /// OnEndGUI adds the common GUI elements that appear at the end of the GUI for most actions.
        /// Subclasses can override this if they need to add additional GUI elements at the end of the GUI, 
        /// but in most cases subclasses should do this in the OnCustomGUI method. This is really only
        /// virtual to allow subclasses to remove the end GUI elements if they want to.
        /// </summary>
        protected virtual void OnEndGUI()
        {
            GUILayout.Space(10);

            if (GUILayout.Button("X", GUILayout.MaxWidth(20)))
            {
                if (EditorUtility.DisplayDialog($"Delete '{DisplayName}' Action", $"Are you sure you want to delete the '{DisplayName}' action?", "Yes", "No"))
                {
                    // Check if this action is part of an asset
                    string assetPath = AssetDatabase.GetAssetPath(this);
                    if (AssetDatabase.IsSubAsset(this))
                    {
                        AssetDatabase.RemoveObjectFromAsset(this);
                    }
                    else
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    ActionHubWindow.RefreshActions();
                }
            }
        }
        
        /// <summary>
        /// This is the GUI presented to users when creating a new action. Subclasses should override this method to provide
        /// action specific configuration options.
        /// 
        /// Note that this will be added to any category that is assigned to the AllowCreateInCategory attribute.
        /// </summary>
        internal virtual void OnCreateGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"No create GUI implemented for {this.GetType().Name}.");
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Save the action. Byt default the asset will be saved in `Wizards Code/User Data/Resources/Action Hub`.
        /// Subclasses can override this method to perform any necessary actions when the action is saved.
        /// Generally this only needs to be called once, when the action is first created. After that the 
        /// Asset Database handles saving the asset.
        /// </summary>
        internal virtual void OnSaveToAssetDatabase()
        {
            string path = "Assets/Wizards Code/User Data/Resources/Action Hub";
            CreateFoldersRecursively(path);

            AssetDatabase.CreateAsset(this, $"{path}/{DisplayName}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ActionHubWindow.RefreshActions();

            EditorGUIUtility.PingObject(this);
        }

        /// <summary>
        /// Call this method to start the OnUpdate coroutine. This is useful if you have set `Activate On Update` to false
        /// </summary>
        protected void ActivateOnUpdate()
        {
            updateCoroutine = EditorCoroutineUtility.StartCoroutine(OnUpdate(), this);
        }

        /// <summary>
        /// Subclasses can implement this method to perform any necessary updates that require a coroutine.
        /// This is not necessarily automatically called. By default it will not be called at all, though
        /// you can set `Hook On Update" to true in order to automatically start the coroutine. If this is
        /// set to false it is up to the subclass to decide when to start the update routine by calling
        /// `HookOnUpdate()`.
        /// 
        /// Note that when active the coroutine will be called every frame, be sure to think about efficiency.
        /// </summary>
        protected virtual IEnumerator OnUpdate()
        {
            yield return null;
        }

        #region Helper Methods
        private void OnValidate()
        {
            if (m_Priority < 0)
            {
                m_Priority = 0;
            }

            if (m_Category == null)
            {
                m_Category = ResourceLoad<ActionCategory>("Default");
            }
        }

        protected static T ResourceLoad<T>(string name) where T : UnityEngine.Object
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

        private void CreateFoldersRecursively(string path)
        {
            string[] folders = path.Split('/');
            string currentPath = "";
            foreach (string folder in folders)
            {
                if (string.IsNullOrEmpty(currentPath))
                {
                    currentPath = folder;
                }
                else
                {
                    currentPath = $"{currentPath}/{folder}";
                }

                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    string parentFolder = System.IO.Path.GetDirectoryName(currentPath);
                    string newFolderName = System.IO.Path.GetFileName(currentPath);
                    AssetDatabase.CreateFolder(parentFolder, newFolderName);
                }
            }
        }
        #endregion
    }
}
