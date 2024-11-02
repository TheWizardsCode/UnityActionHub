using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
    [CreateAssetMenu(fileName = "New ToDo Action", menuName = "Wizards Code/Action Hub/Action/ToDo Action")]
    public class ToDoAction : Action
    {
        [SerializeField, Tooltip("Has this ToDo been completed?")]
        private bool m_IsComplete = false;

        protected override bool ShowCategoryInInspector => false;

        public override bool IncludeInHub => !m_IsComplete;

        public override void Do()
        {
            Undo.RecordObject(this, "Mark ToDo Action Complete");
            m_IsComplete = true;
            EditorUtility.SetDirty(this);
        }

        internal override void OnStartGUI()
        {
            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal();
                {
                    ActionHubWindow.CreateClickableLabel(DisplayName, Description, this);
                    if (GUILayout.Button("Mark Complete", GUILayout.Width(120)))
                    {
                        Do();
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void OnValidate()
        {
            if (Category == null)
            {
                Category = Action.ResourceLoad<ActionCategory>("ToDo");
                ActionHubWindow.RefreshAssets();
            }
        }
    }
}
