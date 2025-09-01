// Assets/Editor/WindSystem/WindManagerEditor.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using WindSystem;

namespace WindSystem.EditorTools
{
    [CustomEditor(typeof(WindManager))]
    public class WindManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector controls first
            DrawDefaultInspector();

            // Provide helpful guardrails
            WindManager wm = (WindManager)target;
            if (wm == null) return;

            if (wm.config == null)
            {
                EditorGUILayout.HelpBox(
                    "⚠ No WindConfig assigned. Wind system will not function. You can create one: Assets → Create → Game Systems → Wind Config",
                    MessageType.Warning
                );

                if (GUILayout.Button("Create Default WindConfig"))
                {
                    var asset = ScriptableObject.CreateInstance<WindConfig>();
                    string path = "Assets/Configs/DefaultWindConfig.asset";
                    AssetDatabase.CreateAsset(asset, path);
                    AssetDatabase.SaveAssets();
                    wm.config = asset;
                    EditorUtility.SetDirty(wm);
                }
            }
            else
            {
                if (!Application.isPlaying && wm.config.drawInEditMode)
                {
                    EditorGUILayout.HelpBox("Wind preview (arrows) will be visible in Scene view while editing.", MessageType.Info);
                }
            }
        }
    }
}
#endif
