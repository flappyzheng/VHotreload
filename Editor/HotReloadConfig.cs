#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using System;

namespace VHotReload
{
    public class HotReloadConfig : ScriptableObject
    {
        public List<AssemblyDefinitionAsset> additionalReferences = new List<AssemblyDefinitionAsset>();
    }

    [CustomEditor(typeof(HotReloadConfig))]
    public class HotReloadConfigEditor : Editor
    {
        private SerializedProperty additionalReferences;

        private void OnEnable()
        {
            additionalReferences = serializedObject.FindProperty(nameof(HotReloadConfig.additionalReferences));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawAssemblyDefinitionArrayProperty(additionalReferences, "热重载程序集引用");
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAssemblyDefinitionArrayProperty(SerializedProperty property, string label)
        {
            EditorGUILayout.LabelField(label);
            EditorGUI.indentLevel++;

            for (int i = 0; i < property.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"项 {i + 1}:", GUILayout.Width(60));

                // 显示当前引用的 AssemblyDefinitionAsset
                var asset = property.GetArrayElementAtIndex(i).objectReferenceValue;

                var newAsset = (AssemblyDefinitionAsset)EditorGUILayout.ObjectField(asset, typeof(AssemblyDefinitionAsset), false);
                if (newAsset != null && newAsset != asset)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = newAsset;
                }

                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    property.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();



                EditorGUILayout.Space();
            }

            if (GUILayout.Button("添加"))
            {
                property.InsertArrayElementAtIndex(property.arraySize);
            }

            EditorGUI.indentLevel--;
        }
    }
}
#endif