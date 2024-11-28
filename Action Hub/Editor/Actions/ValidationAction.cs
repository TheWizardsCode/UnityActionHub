using NaughtyAttributes;
using PlasticGui.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Component = UnityEngine.Component;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace WizardsCode.ActionHubEditor
{
    /// <summary>
    /// An Action that performs a validation task. Each of these actions has a list of components that they are 
    /// to validate. The action will then check each of these components and report back on their validity.
    /// 
    /// Any component will have a number of validation actions that can be performed on it. Some of these are
    /// provided by default, others can be added by the user. In order to add custom validations the user
    /// should implement the `bool IsValid(out string message)` method. This method returns true if
    /// all tests passed, otherwise it returns the first failure message encountereed.
    /// 
    /// In addition you can use validation attributes from NaughtyAttributes to flag Required fields and to
    /// write custom validation rules that will also be used in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "New Validation Action", menuName = "Wizards Code/Action Hub/Validation")]
    public class ValidationAction : Action
    {
        [SerializeField, Tooltip("The components to validate.")]
        private MonoScript m_ComponentScript;

        private DateTime m_LastValidationTime;
        private ValidationFailures m_LastValidationFailures = default;

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

                StringBuilder sb = new StringBuilder($"Validation report as of {LastValidationTime.ToString("g")}.\n\n");
                if (LastValidationFailures != null && !LastValidationFailures.HasFailures)
                {
                    sb.Append($"All {ComponentScript.name} tests passed.");
                }
                else
                {
                    if (LastValidationFailures != null)
                    {
                        sb.AppendLine($"There are {LastValidationFailures.Count} prefabs with at least one validation failure, as follows:\n");
                        foreach (ValidationFailure failure in LastValidationFailures.failures)
                        {
                            sb.AppendLine($"\t{failure.Obj.name}: {failure.Message}");
                        }
                    }
                }

                return sb.ToString();
            }
        }

        protected override bool ShowMetadataInInspector => false;

        private MonoScript newComponentScript;

        protected override void OnCustomGUI()
        {
            EditorGUILayout.BeginHorizontal("box");
            {
                if (ComponentScript != null)
                {
                    string lastRun = LastValidationTime == default(DateTime) ? "Not run yet" : $"last run {LastValidationTime.ToString("g")}";
                    ActionHubWindow.CreateClickableLabel($"Validate {ComponentScript.name} ({lastRun})", LastValidationReport, ComponentScript);

                    if (GUILayout.Button("Validate", GUILayout.Width(ActionHubWindow.Window.ActionButtonWidth)))
                    {
                        ClearConsole();

                        LastValidationTime = DateTime.Now;
                        LastValidationFailures = ValidateComponents(ComponentScript);
                    }
                }
                else
                {
                    ActionHubWindow.CreateClickableLabel($"{name} (Incomplete)", "Component has not been set yet.", ComponentScript, true);
                    ComponentScript = (MonoScript)EditorGUILayout.ObjectField(ComponentScript, typeof(MonoScript), false);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static ValidationFailures ValidateComponents(MonoScript componentScript)
        {
            if (componentScript == null)
            {
                throw new ArgumentNullException("componentScript", "Component script must be set.");
            }
            if (!componentScript.GetClass().IsSubclassOf(typeof(Component))
                && !componentScript.GetClass().IsSubclassOf(typeof(ScriptableObject)))
            {
                throw new ArgumentException("Component script must be a class that inherits from Component or ScriptableObject.");
            }

            ValidationFailures failures;
            int testedCount;
            if (componentScript.GetClass().IsSubclassOf(typeof(ScriptableObject)))
            {
                failures = ValidateScriptableObject(componentScript, out testedCount);
            }
            else
            {
                failures = ValidateComponent(componentScript, out testedCount);
            }

            if (failures.HasFailures)
            {
                StringBuilder sb = new StringBuilder($"Validation of {componentScript.name} failed with {failures.Count} failures from {testedCount} prefabs. The failures are as follows:\n\n");
                foreach (ValidationFailure failure in failures.failures)
                {
                    sb.AppendLine($"{failure.Obj.name}: {failure.Message}");
                }

                bool createTodo = EditorUtility.DisplayDialog("Validation Failed", sb.ToString() + "\n\nDo you want to create a ToDo item for each new issue?", "Yes", "No");
                Debug.LogError(sb.ToString());

                if (createTodo)
                {
                    foreach (ValidationFailure failure in failures.failures)
                    {
                        string name = $"{failure.Message} - {componentScript.name} on {failure.Obj.name}";

                        ToDoAction action = ScriptableObject.CreateInstance<ToDoAction>();
                        if (action == null)
                        {
                            continue; // don't record a failure twice
                        }

                        action = CreateInstance<ToDoAction>();
                        action.name = name;
                        action.DisplayName = action.name;
                        action.Description = $"Validation of {componentScript.name} failed for {failure.Obj.name}: \"{failure.Message}\"";
                        action.Priority = 25;
                        action.RelatedObjects = new Object[] { failure.Obj };
                        action.Category = Action.ResourceLoad<ActionCategory>("Quality");

                        action.OnSaveToAssetDatabase();
                    }
                }
            }
            else
            {
                string result = $"Validation of {testedCount} prefabs containing {componentScript.name} passed fully.";
                EditorUtility.DisplayDialog("Validation Passed", result, "OK");

                Debug.Log(result);
            }
            return failures;
        }

        private static ValidationFailures ValidateScriptableObject(MonoScript componentScript, out int testedCount)
        {
            ValidationFailures failures = new ValidationFailures();
            EditorUtility.DisplayProgressBar($"Validating {componentScript.name}", $"Finding ScriptableObjects using {componentScript.name}", 1 / 100f);

            int checkedCount = 0;
            testedCount = 0;

            Type scriptableObjectType = componentScript.GetClass();
            if (scriptableObjectType == null)
            {
                throw new InvalidOperationException("The class type could not be found from the provided MonoScript.");
            }

            Object[] scriptableObjects = Resources.LoadAll("", scriptableObjectType);
            foreach (ScriptableObject so in scriptableObjects)
            {
                checkedCount++;
                EditorUtility.DisplayProgressBar($"Validating {componentScript.name} ({failures.Count} failures so far.)", $"Examining {componentScript.name} ({checkedCount} of {scriptableObjects.Length}, {failures.Count} failures so far.)", checkedCount / scriptableObjects.Length);

                testedCount++;

                Type type = so.GetType();

                if (!ValidateRequiredFields(so, out string message))
                {
                    failures.AddFailure(so as Object, message);
                }

                MethodInfo methodInfo = type.GetMethod("IsValid", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (methodInfo != null)
                {
                    object[] parameters = new object[] { null };
                    bool passed = (bool)methodInfo.Invoke(so, parameters);

                    message = (string)parameters[0];

                    if (!passed)
                    {
                        failures.AddFailure(so as Object, message);
                    }
                }
                else
                {
                    failures.AddFailure(so, $"IsValid method not found for {so} on {so.name}.");
                }
            }

            EditorUtility.ClearProgressBar();

            return failures;
        }

        private static ValidationFailures ValidateComponent(MonoScript componentScript, out int testedCount)
        {
            ValidationFailures failures = new ValidationFailures();
            EditorUtility.DisplayProgressBar($"Validating {componentScript.name}", $"Finding prefabs using {componentScript.name}", 1 / 100f);

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Dev", "Assets/_Marketing", "Assets/_Rogue Wave" });

            int checkedCount = 0;
            testedCount = 0;
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
                        object[] parameters = new object[] { null };
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

            return failures;
        }

        private static bool ValidateRequiredFields(Object objectUnderTest, out string message)
        {
            message = string.Empty;

            FieldInfo[] fields = objectUnderTest.GetType().GetFields();
            foreach (var field in fields)
            {
                var required = field.GetCustomAttributes(typeof(RequiredAttribute), true);
                if (required.Length > 0)
                {
                    if (field.GetValue(objectUnderTest) == null)
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
            EditorGUILayout.BeginHorizontal("box");
            {
                ActionHubWindow.CreateLabel("Create Validation Action");
                newComponentScript = (MonoScript)EditorGUILayout.ObjectField(newComponentScript, typeof(MonoScript), false);
                if (GUILayout.Button("Create", GUILayout.Width(ActionHubWindow.Window.ActionButtonWidth)))
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
            EditorGUILayout.EndHorizontal();
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
        public string Report
        {
            get
            {
                int count = 0;
                StringBuilder sb = new StringBuilder();
                foreach (ValidationFailure failure in failures)
                {
                    count++;
                    sb.AppendLine($"{count.ToString("D3")}. {failure.Obj.name}: {failure.Message}");
                }
                return sb.ToString();
            }
        }

        public void AddFailure(Object obj, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "Validation failed, with no explanation message. The current stack trace is:\n" + new StackTrace(true).ToString();
            }
            failures.Add(new ValidationFailure(obj, message));
        }
    }

    public class ValidationFailure
    {
        public Object Obj { get; private set; }
        public string Message { get; private set; }

        public ValidationFailure(Object obj, string message)
        {
            Obj = obj;
            Message = message;
        }
    }
}
