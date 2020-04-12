using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

[CustomPropertyDrawer(typeof(AutoCacheAttribute))]
public class AutoCacheAttributeDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		if (property.hasMultipleDifferentValues)
		{
			EditorGUI.PropertyField(position, property, label);
			return;
		}

		AutoCacheAttribute autoCache = attribute as AutoCacheAttribute;

		if (property.objectReferenceValue == null)
		{
			Component component = property.serializedObject.targetObject as Component;
			if (component != null)
			{
				Type fieldType = GetFieldType(property);
				if (fieldType != null)
				{
					// TODO: Must be some way to get this to work with arrays
					Component targetComponent = component.gameObject.GetComponent(fieldType);

					if (targetComponent == null && autoCache.searchChildren)
						targetComponent = component.gameObject.GetComponentInChildren(fieldType);

					if (targetComponent == null && autoCache.searchAncestors)
						targetComponent = component.gameObject.GetComponentInParent(fieldType);

					if (targetComponent != null)
					{
						property.objectReferenceValue = targetComponent;
					}
				}
			}
		}

		EditorGUI.PropertyField(position, property, label);
	}

	public static Type GetFieldType(SerializedProperty property)
	{
		Type parentType = property.serializedObject.targetObject.GetType();

		FieldInfo fi = parentType.GetField(property.propertyPath, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		if (fi != null)
			return fi.FieldType;

		return null;
	}
}
