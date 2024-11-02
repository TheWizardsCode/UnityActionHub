using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

namespace WizardsCode.ActionHubEditor
{
    public class SceneCategory : ActionCategory
    {
        string newScene;

        public override void OnActionManagementUI()
        {
            GUILayout.BeginHorizontal();
            {
                // Dropdown to select from scenes in build settings
                List<string> sceneNames = new List<string>();
                foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                {
                    sceneNames.Add(System.IO.Path.GetFileNameWithoutExtension(scene.path));
                }

                int selectedSceneIndex = sceneNames.IndexOf(newScene);
                selectedSceneIndex = EditorGUILayout.Popup("Select scene to add", selectedSceneIndex, sceneNames.ToArray(), GUILayout.MinWidth(300));

                if (selectedSceneIndex >= 0 && selectedSceneIndex < sceneNames.Count)
                {
                    newScene = sceneNames[selectedSceneIndex];
                }

                // Disable the button if newItem is invalid
                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newScene));
                {
                    if (GUILayout.Button("Add Action", GUILayout.Width(100)))
                    {
                        // create a new ToDoAction and save it to the AssetDatabase
                        SceneAction newAction = ScriptableObject.CreateInstance<SceneAction>();
                        newAction.name = newScene;
                        newAction.Category = this;
                        newAction.Scene = newScene;

                        // save the new asset as a child of this category
                        AssetDatabase.AddObjectToAsset(newAction, this);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        EditorGUIUtility.PingObject(newAction);
                        newScene = string.Empty;

                        ActionHubWindow.RefreshAssets();
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndHorizontal();
        }
    }
}
