using System.Collections.Generic;
using UnityEngine;

namespace WizardsCode.ActionHubEditor
{
    /// <summary>
    /// Represents a category of actions in the Action Hub.
    /// This is a simple ScriptableObject that acts like an enum but is more flexible.
    /// To create a new category, create a new instance of this class and it will become
    /// available for selection in your Actions and will be used to group them in the UI.
    /// </summary>
    [CreateAssetMenu(fileName = "New Category", menuName = "Wizards Code/Action Hub/Category")]
    public class ActionCategory : ScriptableObject
    {
        [SerializeField, Tooltip("The name of the category as it appears in the Action Hub window.")]
        private string m_DisplayName = "TBD";

        [SerializeField, TextArea(2, 5), Tooltip("A description of the category, for displaying in tooltips and detailed views of the category.")]
        private string m_Description = "This category has not been given a description yet.";

        [SerializeField, Tooltip("The sort order of this category. Lower numbers appear first in the list of categories in the Action Hub UI.")]
        private int m_SortOrder = 1000;

        [SerializeField, Tooltip("If this is true then this category will always be shown in the Action Hub, even if there are no actions associated with it. This is useful when the category provides a means for creating new actions from its GUI.")]
        private bool m_AlwaysShowInHub = false;

        [SerializeField, Tooltip("The maximum number of actions to show in the hub by default. If there are more actions than this, a button will be shown to show all actions.")]
        private int m_MaxActionsToShow = 3;

        /// <summary>
        /// Gets or sets the display name of the category.
        /// This is the name that will be shown in the Action Hub window.
        /// </summary>
        public virtual string DisplayName { get => m_DisplayName; set => m_DisplayName = value; }

        /// <summary>
        /// Gets or sets the description of the category.
        /// This description is used for displaying tooltips and detailed views of the category.
        /// </summary>
        public virtual string Description { get => m_Description; set => m_Description = value; }

        /// <summary>
        /// Gets or sets the sort order of the category.
        /// Categories with lower sort order values appear first in the Action Hub.
        /// </summary>
        public virtual int SortOrder { get => m_SortOrder; set => m_SortOrder = value; }

        /// <summary>
        /// Gets or sets a value indicating whether this category should always be shown in the hub.
        /// If true, the category will be shown even if there are no actions associated with it.
        /// </summary>
        public virtual bool AlwaysShowInHub { get => m_AlwaysShowInHub; set => m_AlwaysShowInHub = value; }

        bool showAll = false;

        /// <summary>
        /// Renders the GUI for the action list.
        /// </summary>
        /// <param name="activeActions">The list of active actions.</param>
        /// <param name="activeTemplates">The list of active templates.</param>
        public virtual void ActionListGUI(List<Action> activeActions, List<Action> activeTemplates)
        {
            Sort(ref activeActions);
            Sort(ref activeTemplates);

            int toShow = 0;
            if (showAll)
            {
                toShow = activeActions.Count;
            }
            else
            {
                // Count the number of actions that should be shown in the hub
                foreach (Action action in activeActions)
                {
                    if (action.IncludeInHub)
                    {
                        if (++toShow >= m_MaxActionsToShow)
                        {
                            break;
                        }
                    }
                }
            }

            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal(); // Show title bar, description as tooltip and button to show more or fewer actions
                    {
                        string showing = activeActions.Count > m_MaxActionsToShow ? $"Showing {m_MaxActionsToShow} of {activeActions.Count} actions" : $"{activeActions.Count} actions";
                        string title = $"{DisplayName} - {showing}";
                        ActionHubWindow.CreateSectionHeading(title, Description);

                        if (showAll)
                        {
                            if (GUILayout.Button($"{m_MaxActionsToShow}"))
                            {
                                showAll = false;
                            }
                        } 
                        else if (toShow < activeActions.Count)
                        {
                            if (GUILayout.Button("All"))
                            {
                                showAll = true;
                            }
                        }
                    }
                    GUILayout.EndHorizontal();

                    // Display the create experiences for all templates available
                    foreach (Action createAction in activeTemplates)
                    {
                        GUILayout.BeginHorizontal("Box");
                        {
                            createAction.OnCreateGUI();
                        }
                        GUILayout.EndHorizontal();
                    }

                    // Show the actions in this category
                    int shown = 0;
                    for (int i = 0; shown < toShow; i++)
                    {
                        if (activeActions[i].IncludeInHub)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                activeActions[i].OnGUI();
                            }
                            GUILayout.EndHorizontal();

                            shown++;
                        }
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        void Sort(ref List<Action> actions) {
            
            actions.Sort((a, b) =>
            {
                int priorityComparison = a.Priority.CompareTo(b.Priority);
                if (priorityComparison != 0)
                {
                    return priorityComparison;
                }

                if (a == null || b == null)
                {
                    return 0;
                }

                int nameComparison = a.DisplayName.CompareTo(b.DisplayName);
                if (nameComparison != 0)
                {
                    return nameComparison;
                }

                return 0;
            });
        }
    }
}
