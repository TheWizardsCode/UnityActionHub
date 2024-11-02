using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

namespace WizardsCode.ActionHubEditor
{
    /// <summary>
    /// An ActionCategory is a way to group actions in the Action Hub.
    /// This is a simple ScriptableObject that acts like an enum but is more flexible.
    /// To create a new category, create a new instance of this class and it will become
    /// available for selection in your Actions and will be used to group them in the UI.
    /// </summary>
    [CreateAssetMenu(fileName = "New Action Category", menuName = "Wizards Code/Action Hub/Action Category")]
    public class ActionCategory : ScriptableObject
    {
        [SerializeField, Tooltip("The name of the category as it appears in the Action Hub window.")]
        private string m_DisplayName = "TBD";
        [SerializeField, TextArea(2, 5), Tooltip("A description of the category, for displaying in tooltips and detailed views of the category.")]
        private string m_Description = "This category has not been given a description yet.";
        [SerializeField, Tooltip("The sort order of this category. Lower numbers appear first.")]
        private int m_SortOrder = 1000;
        [SerializeField, Tooltip("If this is true then this category will always be shown im the Action Hub, even if there are no actions associated with it. This is useful when the category provides a means for creating new actions from it's GUI.")]
        private bool m_AlwaysShowInHub = false;

        public virtual string DisplayName { get => m_DisplayName; set => m_DisplayName = value; }
        public virtual string Description { get => m_Description; set => m_Description = value; }
        public virtual int SortOrder { get => m_SortOrder; set => m_SortOrder = value; }
        public virtual bool AlwaysShowInHub { get => m_AlwaysShowInHub; set => m_AlwaysShowInHub = value; }

        protected float Width => EditorGUIUtility.currentViewWidth - 20;

        public void OnStartGUI()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(Width)); // this is ended in OnEndGUI

            GUILayout.BeginVertical();
            {
                ActionHubWindow.CreateClickableHeading(DisplayName, Description, this);

                GUILayout.FlexibleSpace();

                OnActionManagementUI();
            }
            GUILayout.EndVertical();
        }

        public virtual void OnActionManagementUI()
        {
        }

        public virtual void OnEndGUI()
        {
            GUILayout.EndVertical(); // this is started in OnStartGUI
        }
    }
}
