using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

[CustomPropertyDrawer(typeof(LensManagerBase), true)]
public class LensManagerBaseDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);

		SerializedProperty requestsProp = property.FindPropertyRelative("activeRequests");
		SerializedProperty valuesProp = property.FindPropertyRelative("evaluateValues");
		SerializedProperty resultProp = property.FindPropertyRelative("cachedResult");

		string formatString = string.Format("{0} ({1}) - Num Requests: {2}", label.text, GetPropertyLabel(resultProp), requestsProp.arraySize);

		bool playingAndAnyRequests = Application.IsPlaying(property.serializedObject.targetObject) && requestsProp.arraySize > 0;
		GUIStyle style = playingAndAnyRequests ? EditorStyles.boldLabel : EditorStyles.label;
		style.richText = true;

		property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, formatString, true);
		if (property.isExpanded)
		{
			EditorGUI.indentLevel++;

			for (int i = 0; i < requestsProp.arraySize; i++)
			{
				var requestProp = requestsProp.GetArrayElementAtIndex(i);

				string name = requestProp.FindPropertyRelative("Name").stringValue;
				int instanceID = requestProp.FindPropertyRelative("instanceID").intValue;

				SerializedProperty valueProp = valuesProp.GetArrayElementAtIndex(i);
				string valueText = GetPropertyLabel(valueProp);
				string formatText = !string.IsNullOrEmpty(name) ? "{0}: {1}" : "{1}";

				EditorGUILayout.LabelField(string.Format(formatText, name, valueText));
				int controlID = GUIUtility.GetControlID(FocusType.Passive) - 1;

				EventType eventType = Event.current.GetTypeForControl(controlID);
				if (eventType == EventType.MouseDown && instanceID != -1)
					EditorGUIUtility.PingObject(instanceID);
			}

			EditorGUI.indentLevel--;
		}

		EditorGUI.EndProperty();
	}

	private string GetPropertyLabel(SerializedProperty prop)
	{
		switch (prop.type)
		{
			case "float":
				return prop.floatValue.ToString();
			case "Vector2":
				return prop.vector2Value.ToString();
			case "Vector3":
				return prop.vector3Value.ToString();
			case "Vector4":
				return prop.vector4Value.ToString();
			case "int":
				return prop.intValue.ToString();
			case "Color":
				return prop.colorValue.ToString();
			case "Quaternion":
				return prop.quaternionValue.ToString();
			case "string":
				return prop.stringValue;
			case "bool":
				return prop.boolValue.ToString();
			default:
				return string.Empty;
		}
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{ return 0f; }
}
