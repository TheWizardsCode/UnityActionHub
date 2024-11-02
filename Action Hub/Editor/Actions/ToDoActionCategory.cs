using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WizardsCode.ActionHubEditor
{
    public class ToDoActionCategory : ActionCategory
    {
        private string newItem;

        public override void OnActionManagementUI()
        {   
            GUILayout.BeginHorizontal();
            {
                newItem = EditorGUILayout.TextField(newItem);
                bool isValidName = IsValidName(newItem);

                EditorGUI.BeginDisabledGroup(!isValidName);
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Add ToDo", GUILayout.Width(100)))
                    {
                        CreateTodoItem();
                        newItem = string.Empty;
                    }
                    GUILayout.FlexibleSpace();
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndHorizontal();
        }

        private void CreateTodoItem()
        {
            // create a new ToDoAction and save it to the AssetDatabase
            ToDoAction newAction = ScriptableObject.CreateInstance<ToDoAction>();
            newAction.name = newItem;
            newAction.Category = this;
            newAction.DisplayName = newItem;
            newAction.Description = "This is a new ToDo item that has been added to the Action Hub. No description is available yet.";

            // save the new asset as a child of this category
            AssetDatabase.AddObjectToAsset(newAction, this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(newAction);
            Selection.activeObject = newAction;

            ActionHubWindow.RefreshAssets();
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
    }
}
