using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WizardsCode.ActionHubEditor
{
    /// <summary>
    /// A ToDOAction is a simple and quick way of creating notes for things that need to be done.
    /// They are intended to be used as a temporary measure to remind you of taks at hand.
    /// Once marked complete the action will be removed from the list.
    /// 
    /// Note this is not a replacement for a proper task management system. It is intended for 
    /// quick notes only. They are especially useful for capturing the small steps that need
    /// to be carried out to complete a larger task, whete the larger task is managed in a
    /// more complete task management system.
    /// </summary>
    [CreateAssetMenu(fileName = "New ToDo Action", menuName = "Wizards Code/Action Hub/ToDo")]
    public class ToDoAction : Action
    {
        [SerializeField, Tooltip("Has this ToDo been completed?")]
        private bool m_IsComplete = false;
        [SerializeField, Tooltip("An optional reference to an object relating to this ToDo item. Users will be able to quickly navigate to this object from the Action Hub.")]
        private Object[] m_RelatedObjects = new Object[0];

        internal override bool IncludeInHub => !m_IsComplete;

        public Object[] RelatedObjects
        {
            get { return m_RelatedObjects; }
            set { m_RelatedObjects = value; }
        }

        private string newItemName;
        private int newPriority;

        protected override void OnCustomGUI()
        {
            GUILayout.BeginHorizontal("box");
            {
                ActionHubWindow.CreateClickableLabel(DisplayName, Description, this);

                for (int i = 0; i < RelatedObjects.Length; i++)
                {
                    RelatedObjects[i] = EditorGUILayout.ObjectField(RelatedObjects[i], typeof(Object), false, GUILayout.Width(150));
                }

                if (GUILayout.Button("Add Object", GUILayout.Width(100)))
                {
                    Array.Resize(ref m_RelatedObjects, m_RelatedObjects.Length + 1);
                    m_RelatedObjects[m_RelatedObjects.Length - 1] = null;
                }

                Priority = EditorGUILayout.DelayedIntField(Priority, GUILayout.Width(50));

                if (GUILayout.Button("Complete", GUILayout.Width(ActionHubWindow.Window.ActionButtonWidth)))
                {
                    Undo.RecordObject(this, "Mark ToDo Action Complete");
                    m_IsComplete = true;
                    EditorUtility.SetDirty(this);
                }
            }
            GUILayout.EndHorizontal();
        }

        internal override void OnCreateGUI()
        {
            GUILayout.BeginHorizontal("box");
            {
                EditorGUILayout.LabelField("Create ToDo", GUILayout.Width(CreateLabelWidth));
                newItemName = EditorGUILayout.TextField(newItemName, GUILayout.ExpandWidth(true));
                
                newPriority = EditorGUILayout.IntField(newPriority, GUILayout.Width(50));

                bool isValidName = IsValidName(newItemName);
                EditorGUI.BeginDisabledGroup(!isValidName);
                {
                    if (GUILayout.Button("Add", GUILayout.Width(ActionHubWindow.Window.ActionButtonWidth)))
                    {
                        // create a new ToDoAction and save it to the AssetDatabase
                        ToDoAction newAction = ScriptableObject.CreateInstance<ToDoAction>();
                        newAction.name = newItemName;
                        newAction.DisplayName = newItemName;
	                    newAction.Description = string.Empty;
                        newAction.Priority = newPriority;
                        newAction.Category = Category;

                        newAction.OnSaveToAssetDatabase();

                        newItemName = string.Empty;
                        newPriority = 1000;
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndHorizontal();
        }

        private bool IsValidName(string name)
        {
            // Check if the name is empty or null
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            // Check for invalid characters
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                if (name.Contains(c.ToString()))
                {
                    return false;
                }
            }

            // Check for uniqueness within the parent asset
            string categoryPath = AssetDatabase.GetAssetPath(this);
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(categoryPath);
            foreach (Object asset in subAssets)
            {
                if (asset.name == name)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnValidate()
        {
            if (Category == null)
            {
                Category = Action.ResourceLoad<ActionCategory>("ToDo");
                ActionHubWindow.RefreshActions();
            }
        }
    }
}
