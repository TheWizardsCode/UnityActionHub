using NaughtyAttributes;
using NUnit.Framework.Interfaces;
using PlasticPipe.Certificates;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Component = UnityEngine.Component;
using Debug = UnityEngine.Debug;

namespace WizardsCode.ActionHubEditor
{
    /// <summary>
    /// An Action that performs a validation task. Each of these actions has a list of components that they are 
    /// to validate. The action will then check each of these components and report back on their validity.
    /// 
    /// Any component will have a number of validation actions that can be performed on it. Some of these are
    /// provided by default, others can be added by the user. In order to add custom validations the user
    /// should implement the `private void ValidationFailures IsValid()` method. This method should return a
    /// a ValidationFailures object that contains a list of ValidationFailure's. Each of these objects
    /// describes a failure in the validation process.
    /// 
    /// In addition you can use vlidation attributes from NaughtyAttributes to flag Required fields and to
    /// write custom validation rules that will also be used in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "New Validation Action", menuName = "Wizards Code/Action Hub/Validation")]
    public class ValidationAction : Action
    {
        [SerializeField, Tooltip("The components to validate.")]
        private MonoScript m_ComponentScript;

        private DateTime m_LastValidationTime;
        private ValidationFailures m_LastValidationFailures;

        public MonoScript ComponentScript { get => m_ComponentScript; set => m_ComponentScript = value; }
        public ValidationFailures LastValidationFailures { get => m_LastValidationFailures; set => m_LastValidationFailures = value; }
        public DateTime LastValidationTime { get => m_LastValidationTime; set => m_LastValidationTime = value; }
        public string LastValidationReport
        {
            get
            {
                if (LastValidationTime == default(DateTime))
                {
                    return "No validation has been run yet.";
                }

                StringBuilder sb = new StringBuilder($"Last validation run was at {LastValidationTime.ToShortDateString()}.\n\n");
                if (!LastValidationFailures.HasFailures)
                {
                    sb.Append($"All {ComponentScript.name} tests passed.");
                }
                else
                {
                    sb.AppendLine($"There are {LastValidationFailures.Count} prefabs with at least one validation failure, as follows:\n");
                    foreach (ValidationFailure failure in LastValidationFailures.failures)
                    {
                        sb.AppendLine($"\t{failure.Prefab.name}: {failure.Message}");
                    }
                }

                return sb.ToString();
            }
        }

        protected override bool ShowMetadataInInspector => false;

        private MonoScript newComponentScript;

        protected override void OnCustomGUI()
        {
            GUILayout.BeginHorizontal("box");
            {
                if (ComponentScript != null)
                {
                    ActionHubWindow.CreateClickableLabel($"Validate {ComponentScript.name}", LastValidationReport, ComponentScript);

                    if (GUILayout.Button("Validate", GUILayout.Width(100)))
                    {
                        ClearConsole();

                        LastValidationTime = DateTime.Now;
                        LastValidationFailures = ValidateComponents(ComponentScript);
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    {
                        ActionHubWindow.CreateClickableLabel($"{name} (Incomplete)", "Component has not been set yet.", ComponentScript);
                        ComponentScript = (MonoScript)EditorGUILayout.ObjectField(ComponentScript, typeof(MonoScript), false);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndHorizontal();
        }

        private static ValidationFailures ValidateComponents(MonoScript componentScript)
        {   
            ValidationFailures failures = new ValidationFailures();

            EditorUtility.DisplayProgressBar($"Validating {componentScript.name}", $"Finding prefabs using {componentScript.name}", 1 / 100f);

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Dev", "Assets/_Marketing", "Assets/_Rogue Wave" });

            int checkedCount = 0;
            int testedCount = 0;
            EditorUtility.DisplayProgressBar($"Validating {componentScript.name}", $"Found {guids.Length} prefabs.", checkedCount / guids.Length);

            foreach (string guid in guids)
            {
                checkedCount++;
                EditorUtility.DisplayProgressBar($"Validating {componentScript.name} ({failures.Count} failures so far.)", $"Examining {componentScript.name} ({checkedCount} of {guids.Length}, {failures.Count} failures so far.)", checkedCount / guids.Length);

                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefabUnderTest = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Component componentUnderTest = prefabUnderTest.GetComponent(componentScript.GetClass());

                if (componentUnderTest != null)
                {
                    testedCount++;

                    Type type = componentUnderTest.GetType();

                    if (!ValidateRequiredFields(componentUnderTest, out string message))
                    {
                        failures.AddFailure(prefabUnderTest, message);
                    }

                    MethodInfo methodInfo = type.GetMethod("IsValid", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (methodInfo != null)
                    {
                        object[] parameters = new object[] { null, null };
                        bool passed = (bool)methodInfo.Invoke(componentUnderTest, parameters);

                        message = (string)parameters[0];

                        if (!passed)
                        {
                            failures.AddFailure(prefabUnderTest, message);
                        }
                    }
                    else
                    {
                        throw new MissingMethodException($"IsValid method not found for {componentUnderTest} on {prefabUnderTest.name}.");
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            if (failures.HasFailures)
            {
                StringBuilder sb = new StringBuilder($"Validation of {componentScript.name} failed with {failures.Count} failures from {testedCount} prefabs. The failures are as follows:\n\n");
                foreach (ValidationFailure failure in failures.failures)
                {
                    sb.AppendLine($"{failure.Prefab.name}: {failure.Message}");
                }

                EditorUtility.DisplayDialog("Validation Failed", sb.ToString(), "OK");
                Debug.LogError(sb.ToString());
            }
            else
            {
                string result = $"Validation of {testedCount} prefabs with {componentScript.name} passed.";
                EditorUtility.DisplayDialog("Validation Passed", result, "OK");

                Debug.Log(result);
            }
            return failures;
        }

        private static bool ValidateRequiredFields(Component componentUnderTest, out string message)
        {
            message = string.Empty;

            FieldInfo[] fields = componentUnderTest.GetType().GetFields();
            foreach (var field in fields)
            {
                var required = field.GetCustomAttributes(typeof(RequiredAttribute), true);
                if (required.Length > 0)
                {
                    if (field.GetValue(componentUnderTest) == null)
                    {
                        message = $"Field {field.Name} is required but not set.";
                        return false;
                    }
                }
            }

            return true;
        }

        internal override void OnCreateGUI()
        {
            GUILayout.BeginHorizontal("box");
            {
                ActionHubWindow.CreateLabel("Create Validation Action");
                newComponentScript = (MonoScript)EditorGUILayout.ObjectField(newComponentScript, typeof(MonoScript), false);
                if (GUILayout.Button("Create Validation", GUILayout.Width(100)))
                {
                    ValidationAction action = CreateInstance<ValidationAction>();
                    action.ComponentScript = newComponentScript;
                    action.name = "Validate " + action.ComponentScript.name;
                    action.Description = $"Validate all components of type {action.ComponentScript.name} in the project. Validation is performed by executing the method IsValid() in addition to some automated tests for specific components.";
                    action.DisplayName = action.name;
                    action.Category = Category;

                    action.OnSaveToAssetDatabase();

                    ComponentScript = null;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void ClearConsole()
        {
            Type logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            MethodInfo clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
            clearMethod.Invoke(null, null);
        }
    }

    [Serializable]
    public class ValidationFailures
    {
        public List<ValidationFailure> failures = new List<ValidationFailure>();

        public bool HasFailures => Count > 0;
        public int Count => failures.Count;

        public void AddFailure(GameObject prefab, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "Validation failed, with no explanation message. The current stack trace is:\n" + new StackTrace(true).ToString();
            }
            failures.Add(new ValidationFailure(prefab, message));
        }
    }

    public class ValidationFailure
    {
        public GameObject Prefab { get; private set; }
        public string Message { get; private set; }

        public ValidationFailure(GameObject prefab, string message)
        {
            Prefab = prefab;
            Message = message;
        }
    }
}
