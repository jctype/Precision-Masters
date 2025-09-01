#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[CustomEditor(typeof(BallisticsManager))]
public class BallisticsManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BallisticsManager bm = (BallisticsManager)target;

        if (bm.windManager == null)
        {
            EditorGUILayout.HelpBox("ℹ No WindManager assigned. Bullets will simulate without wind.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("✔ WindManager assigned. Bullets will sample wind each step.", MessageType.None);
        }
    }
}
