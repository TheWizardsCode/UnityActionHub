using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WizardsCode.ActionHubEditor
{

    [CreateAssetMenu(fileName = "CountdownTimer", menuName = "Wizards Code/Action Hub/Countdown Timer")]
    public class CountdownTimer : Action
    {
        [SerializeField, Tooltip("The duration of the countdown in seconds.")]
        private float m_Duration = 1500; // 25 minutes

        public float Duration { get => m_Duration; set => m_Duration = value; }

        bool m_IsRunning = false;
        float m_EndTime;

        protected override void OnCustomGUI()
        {
            EditorGUILayout.BeginHorizontal("box");
            {
                ActionHubWindow.CreateClickableLabel(DisplayName, Description, this);

                if (m_IsRunning)
                {
                    ActionHubWindow.CreateLabel($"Time remaining: {FormatTime(m_EndTime - Time.realtimeSinceStartup)}");
                }

                if (!m_IsRunning && GUILayout.Button($"Start {m_Duration.ToString("F0")} second timer", GUILayout.Width(ActionHubWindow.Window.ActionButtonWidth)))
                {
                    m_EndTime = Time.realtimeSinceStartup + m_Duration;
                    m_IsRunning = true;
                    ActivateOnUpdate();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        internal override void OnCreateGUI()
        {
            GUILayout.BeginHorizontal("box");
            {
                EditorGUILayout.LabelField("Create Countdown", GUILayout.Width(CreateLabelWidth));

                DisplayName = EditorGUILayout.TextField(DisplayName, GUILayout.ExpandWidth(true));

                EditorGUILayout.LabelField("Duration (seconds)", GUILayout.Width(150));
                Duration = EditorGUILayout.FloatField(Duration, GUILayout.Width(75));

                if (m_Duration < 0)
                {
                    m_Duration = 0;
                }

                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(DisplayName));
                if (GUILayout.Button("Add", GUILayout.Width(100)))
                {
                    // create a new ToDoAction and save it to the AssetDatabase
                    CountdownTimer newAction = ScriptableObject.CreateInstance<CountdownTimer>();
                    newAction.DisplayName = DisplayName;
                    newAction.Duration = Duration;
                    newAction.Category = Category;

                    newAction.OnSaveToAssetDatabase();

                    // Reset the template values
                    DisplayName = string.Empty;
                    Duration = 1500;

                    ActionHubWindow.RefreshActions();
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
        }

        private string FormatTime(float totalSeconds)
        {
            int days = (int)(totalSeconds / 86400);
            totalSeconds %= 86400;
            int hours = (int)(totalSeconds / 3600);
            totalSeconds %= 3600;
            int minutes = (int)(totalSeconds / 60);
            int seconds = (int)(totalSeconds % 60);

            string result = string.Empty;
            if (days > 0) result += $"{days}:";
            if (days > 0 || hours > 0) result += $"{hours}:";
            if (days > 0 || hours > 0 || minutes > 0) result += $"{minutes.ToString("D2")}:";
            if (seconds > 0 || result == string.Empty) result += $"{seconds.ToString("D2")}";

            if (result.EndsWith(":"))
            {
                result = result.Substring(0, result.Length - 1);
            }

            if (result == "00")
            {
                result = "Fin";
            }

            return result.Trim();
        }

        protected override IEnumerator OnUpdate()
        {
            WaitForSeconds waitOneSecond = new WaitForSeconds(1);

            while (m_IsRunning)
            {
                if (ActionHubWindow.Status.ContainsKey(this))
                {
                    ActionHubWindow.Status[this] = $"{FormatTime(m_EndTime - Time.realtimeSinceStartup)}";
                } else
                {
                    ActionHubWindow.Status.Add(this, $"{FormatTime(m_EndTime - Time.realtimeSinceStartup)}");
                }

                ActionHubWindow.ForceRepaint();


                if (Time.realtimeSinceStartup > m_EndTime)
                {
                    ActionHubWindow.Status.Remove(this);
                    m_IsRunning = false;
                    break;
                }

                yield return waitOneSecond;
            }

            ActionHubWindow.ShowConfirmationDialog(DisplayName + " Complete", $"{DisplayName} has finished.");
        }

    }
}
