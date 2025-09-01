#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using WindSystem;   // <-- ensures it sees WindSource and WindManager

namespace WindSystem.EditorTools   // <-- prevents name collisions with runtime code
{
    [CustomEditor(typeof(WindSource))]
    public class WindSourceEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            WindSource ws = (WindSource)target;
            if (FindObjectOfType<WindManager>() == null)
            {
                EditorGUILayout.HelpBox(
                    "⚠ No WindManager found in scene. This source will have no effect.",
                    MessageType.Warning
                );
            }
        }
    }
}
