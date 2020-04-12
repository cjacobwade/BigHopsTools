using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(SingleLayer))]
public class SingleLayerDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);

		List<string> layerNames = new List<string>();
		List<int> layerNums = new List<int>();
		for(int i =0; i < 32; i++)
		{
			string layerName = LayerMask.LayerToName(i);
			if (!string.IsNullOrEmpty(layerName))
			{
				layerNames.Add(layerName);
				layerNums.Add(i);
			}
		}

		SerializedProperty layerProp = property.FindPropertyRelative("layer");

		int selectionIndex = layerNums.IndexOf(layerProp.intValue);
		if (selectionIndex == -1)
			selectionIndex = 0;

		int nameIndex = EditorGUI.Popup(position, label.text, selectionIndex, layerNames.ToArray());
		layerProp.intValue = layerNums[nameIndex];

		EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{ return EditorGUIUtility.singleLineHeight; }
}
