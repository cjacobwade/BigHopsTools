using UnityEditor;
using UnityEngine;

namespace Luckshot.Paths
{
	[CustomEditor(typeof(LinePath), true)]
	public class LinePathInspector : Editor
	{
		protected const float handleSize = 0.04f;
		protected const float pickSize = 0.06f;

		protected LinePath linePath = null;
		protected Transform handleTransform = null;
		protected Quaternion handleRotation = Quaternion.identity;
		protected int selectedIndex = -1;

		private void OnEnable()
		{
			Undo.undoRedoPerformed += UndoRedoPerformed;
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= UndoRedoPerformed;
			Tools.hidden = false;
		}

		private void UndoRedoPerformed()
		{
			if(linePath != null)
				linePath.OnPathChanged(linePath);
			else
				Undo.undoRedoPerformed -= UndoRedoPerformed;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			linePath = target as LinePath;

			if (selectedIndex >= 0 && selectedIndex < linePath.PointCount)
			{
				DrawSelectedPointInspector();
				Tools.hidden = true;
			}
			else
			{
				Tools.hidden = false;
			}

			if (GUILayout.Button("Add Point"))
			{
				Undo.RecordObject(linePath, "Add Point");
				linePath.AddPoint();
				EditorUtility.SetDirty(linePath);
			}

			if (linePath.PointCount > 1 && GUILayout.Button("Remove Point"))
			{
				Undo.RecordObject(linePath, "Remove Point");
				linePath.RemovePoint();
				EditorUtility.SetDirty(linePath);
			}

			if (GUI.changed)
				linePath.OnPathChanged(linePath);
		}

		protected virtual void DrawSelectedPointInspector()
		{
			GUILayout.Label("Selected Point");
			EditorGUI.BeginChangeCheck();
			Vector3 point = EditorGUILayout.Vector3Field("Position", linePath.Points[selectedIndex]);

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(linePath, "Move Point");
				EditorUtility.SetDirty(linePath);
				linePath.Points[selectedIndex] = point;
			}

			// Draw Node Data if ADV SPLINE
			// Would be in AdvSplineInspector, except AdvSpline is generic meaning
			// we can't use the CustomEditor attribute and force child types to use its inspector
			// which means either this is here or every AdvSpline child needs an associated TypeInspector script
			SerializedObject so = serializedObject;
			SerializedProperty nodeDataProp = so.FindProperty(string.Format("{0}.Array.data[{1}]", "_nodeData", selectedIndex));
			if (nodeDataProp != null)
			{
				EditorGUI.BeginChangeCheck();

				EditorGUILayout.PropertyField(nodeDataProp, new GUIContent("Node Data"), true);

				if (EditorGUI.EndChangeCheck())
					so.ApplyModifiedProperties();
			}
		}

		protected virtual void OnSceneGUI()
		{
			linePath = target as LinePath;
			handleTransform = linePath.transform;
			handleRotation = Tools.pivotRotation == PivotRotation.Local ? handleTransform.rotation : Quaternion.identity;

			for (int i = 0; i < linePath.PointCount; i++)
				ShowPoint(i);

			for (int i = 1; i < linePath.PointCount; i++)
			{
				Handles.color = Color.white;
				Handles.DrawLine(
					linePath.transform.TransformPoint(linePath.Points[i]),
					linePath.transform.TransformPoint(linePath.Points[i - 1]));
			}

			if (linePath.Loop)
			{
				Handles.DrawLine(
					linePath.transform.TransformPoint(linePath.Points[0]),
					linePath.transform.TransformPoint(linePath.Points[linePath.Points.Length - 1]));
			}
		}

		protected virtual Vector3 ShowPoint(int index)
		{
			Vector3 point = handleTransform.TransformPoint(linePath.Points[index]);
			float size = HandleUtility.GetHandleSize(point);
			if (index == 0)
			{
				size *= 2f;
			}

			Handles.color = Color.white;
			if (Handles.Button(point, handleRotation, size * handleSize, size * pickSize, Handles.DotHandleCap))
			{
				selectedIndex = index;
				Repaint();
			}

			if (selectedIndex == index)
			{
				EditorGUI.BeginChangeCheck();
				point = Handles.DoPositionHandle(point, handleRotation);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(linePath, "Move Point");
					EditorUtility.SetDirty(linePath);
					linePath.Points[index] = handleTransform.InverseTransformPoint(point);
				}
			}

			return point;
		}
	}
}