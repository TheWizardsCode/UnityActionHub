using NaughtyAttributes;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WizardsCode.ActionHubEditor
{
    [CreateAssetMenu(fileName = "New Scene Action", menuName = "Wizards Code/Action Hub/Scene")]
    public class SceneAction : Action
    {
        [SerializeField, Tooltip("The scene to operate on."), Scene]
        private string m_Scene;
        [SerializeField, Tooltip("Is this an additive scene or not?")]
        bool m_IsAdditive = false;

        protected override bool ShowMetadataInInspector => false;

        public override string DisplayName => Scene;
        public override string Description => "Perform an action on the scene " + Scene;
        public string Scene { get => m_Scene; set => m_Scene = value; }
        public bool IsAdditive { get => m_IsAdditive; set => m_IsAdditive = value; }

        string newScene;

        protected override void OnCustomGUI()
        {
            // Check if the scene is present in the build settings
            bool sceneInBuildSettings = false;
            string scenePath = null;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path.Contains(m_Scene))
                {
                    sceneInBuildSettings = true;
                    scenePath = scene.path;
                    break;
                }
            }

            if (!sceneInBuildSettings)
            {
                OnMissingSceneGUI();
            }
            else
            {
                OnValidSceneGUI(scenePath);
            }
        }

        internal override void OnCreateGUI()
        {
            GUILayout.BeginHorizontal("box");
            {
                EditorGUILayout.LabelField("Create Scene Action", GUILayout.Width(CreateLabelWidth));

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
                    if (GUILayout.Button("Add Scene", GUILayout.Width(100)))
                    {
                        // create a new ToDoAction and save it to the AssetDatabase
                        SceneAction newAction = ScriptableObject.CreateInstance<SceneAction>();
                        newAction.name = newScene;
                        newAction.Scene = newScene;
                        newAction.Category = Category;

                        newAction.OnSaveToAssetDatabase();

                        newScene = string.Empty;

                        ActionHubWindow.RefreshActions();
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndHorizontal();
        }

        private void OnValidSceneGUI(string scenePath)
        {
            // Scene is in build settings, show normal GUI
            GUIContent content = new GUIContent(DisplayName, Description);
            GUILayout.Label(content);

            // Add a GUI toggle for IsAdditive
            IsAdditive = EditorGUILayout.ToggleLeft(new GUIContent("Is Additive", "If the actions are additive then any changes to load status will not impact other scenes. If it is not additive then changes to load status will unload other scenes"), IsAdditive);

            OpenSceneMode openSceneMode = IsAdditive ? OpenSceneMode.Additive : OpenSceneMode.Single;

            if (GUILayout.Button(new GUIContent("Load", "Load the scene.")))
            {
                EditorSceneManager.OpenScene(scenePath, openSceneMode);
            }

            EditorGUI.BeginDisabledGroup(SceneManager.sceneCount == 1);
            if (GUILayout.Button(new GUIContent("Remove", "Remove the scene.")))
            {
                Scene scene = SceneManager.GetSceneByPath(scenePath);
                if (scene.IsValid())
                {
                    if (IsAdditive)
                    {
                        EditorSceneManager.CloseScene(scene, false);
                    }
                    else
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button(new GUIContent("Play", "Start the application in this scene.")))
            {
                EditorSceneManager.OpenScene(scenePath, openSceneMode);
                EditorApplication.isPlaying = true;
            }
        }

        private void OnMissingSceneGUI()
        {
            // Scene is not in build settings, provide options
            GUILayout.Label("Scene not found in build settings.", EditorStyles.boldLabel);

            // Dropdown to select from scenes in build settings
            List<string> sceneNames = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                sceneNames.Add(System.IO.Path.GetFileNameWithoutExtension(scene.path));
            }

            int selectedSceneIndex = sceneNames.IndexOf(m_Scene);
            selectedSceneIndex = EditorGUILayout.Popup("Select Scene", selectedSceneIndex, sceneNames.ToArray());

            if (selectedSceneIndex >= 0 && selectedSceneIndex < sceneNames.Count)
            {
                m_Scene = sceneNames[selectedSceneIndex];
            }

            if (GUILayout.Button("Ping Action Definition"))
            {
                EditorGUIUtility.PingObject(this);
                Selection.activeObject = this;
            }
        }

        private void OnValidate()
        {
            if (Category == null)
            {
                Category = Action.ResourceLoad<ActionCategory>("Scene");
                ActionHubWindow.RefreshActions();
            }
        }
    }
}
