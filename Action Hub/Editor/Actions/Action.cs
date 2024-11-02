using NaughtyAttributes;
using System;
using UnityEditor;
using UnityEngine;

namespace WizardsCode.ActionHubEditor
{
    /// <summary>
    /// An action is a unit of work that can be performed by the Action Hub.
    /// </summary>
    [CreateAssetMenu(fileName = "New Action", menuName = "Wizards Code/Action Hub/Action/Generic Action")]
    public class Action : ScriptableObject
    {
        [SerializeField, Tooltip("The name of the action as it appears in the Action Hub window."), ShowIf("ShowMetadataInInspector")]
        private string m_DisplayName;
        [SerializeField, TextArea(2,15), Tooltip("A description of the action, for displaying in tooltips and detailed views of the action."), ShowIf("ShowMetadataInInspector")]
        private string m_Description;
        [SerializeField, Tooltip("The category this action belongs to."), Required, ShowIf("ShowCategoryInInspector")]
        private ActionCategory m_Category;
        [SerializeField, Tooltip("The priority of the item, also used as a sort order for this action. Lower numbers appear first.")]
        private int m_Priority = 1000;

        protected virtual bool ShowMetadataInInspector => true;
        protected virtual bool ShowCategoryInInspector => ShowMetadataInInspector;

        public virtual bool IncludeInHub => true;
        public virtual string DisplayName { get => m_DisplayName; set => m_DisplayName = value; }
        public virtual string Description { get => m_Description; set => m_Description = value; }
        public virtual ActionCategory Category { get => m_Category; set => m_Category = value; }
        public virtual int Priority { get => m_Priority; set => m_Priority = value; }

        protected virtual void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        protected virtual void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        /// <summary>
        /// This will be called when an undo or redo is performed. By default it does nothing.
        /// Subclasses can override this method to perform any necessary actions when an undo or redo is performed.
        /// </summary>
        protected virtual void OnUndoRedo()
        {
            ActionHubWindow.RefreshAssets();
        }

        public virtual void Do()
        {
            EditorGUIUtility.PingObject(this);
            Selection.activeObject = this;

            Debug.Log("TODO implement action: " + m_DisplayName);
        }

        #region Helper Methods


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

        internal virtual void OnStartGUI()
        {
            GUIContent content = new GUIContent(DisplayName, Description);
            if (GUILayout.Button(content))
            {
                Do();
            }
        }

        internal virtual void OnEndGUI()
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
                    ActionHubWindow.RefreshAssets();
                }
            }
        }
        #endregion
    }
}
