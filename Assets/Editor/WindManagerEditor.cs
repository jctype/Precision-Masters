using UnityEngine;
using UnityEditor;
using WindSystem;

namespace WindSystem.EditorTools
{
    [CustomEditor(typeof(WindManager))]
    public class WindManagerEditor : Editor
    {
        private WindManager windManager;

        private void OnEnable()
        {
            windManager = (WindManager)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // Add custom editor functionality here if needed
        }
    }
}
