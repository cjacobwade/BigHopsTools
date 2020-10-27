using UnityEditor;
using UnityEngine;

namespace Luckshot.Paths
{
	[CustomEditor(typeof(LinePath), true)]
	public class LinePathEditor : Editor
	{
		protected const float handleSize = 0.04f;
		protected const float pickSize = 0.06f;

		protected LinePath linePath = null;
		protected Transform handleTransform = null;
		protected Quaternion handleRotation = Quaternion.identity;
		protected int selectedIndex = -1;

		private Tool currentTool = Tool.None;
		private PivotRotation currentPivotRotation = PivotRotation.Local;

		private bool flipNormal = false;

		private void OnEnable()
		{
			Undo.undoRedoPerformed += UndoRedoPerformed;

			linePath = target as LinePath;

			linePath.OnPathChanged(linePath);
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= UndoRedoPerformed;
			Tools.hidden = false;
		}

		private void UndoRedoPerformed()
		{
			ResetHandleSettings();

			if (linePath != null)
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

			if (linePath.PointCount > 2 && GUILayout.Button("Remove Point"))
			{
				Undo.RecordObject(linePath, "Remove Point");
				linePath.RemovePoint();
				EditorUtility.SetDirty(linePath);
			}

			if(GUILayout.Button("Reset Normals"))
			{
				Undo.RecordObject(linePath, "Remove Point");

				if (linePath.PointCount <= 2)
				{
					Vector3 pos = linePath.GetPoint(0);
					Vector3 pos2 = linePath.GetPoint(1);

					Vector3 toNext = (pos2 - pos).normalized;
					Vector3 right = Vector3.Cross(Vector3.up, toNext).normalized;

					Vector3 normal = Vector3.Cross(-right, toNext);
					if (flipNormal)
						normal *= -1f;

					for (int i = 0; i < linePath.Normals.Length; i++)
						linePath.Normals[i] = normal;
				}
				else
				{
					for (int i = 0; i < linePath.Normals.Length; i++)
					{
						Vector3 pos = linePath.GetPoint(i);

						int prevIndex = linePath.SafePointIndex(i - 1);
						Vector3 prevPos = linePath.GetPoint(prevIndex);

						int nextIndex = linePath.SafePointIndex(i + 1);
						Vector3 nextPos = linePath.GetPoint(nextIndex);

						Vector3 toNext = (nextPos - pos).normalized;
						Vector3 toPrev = (prevPos - pos).normalized;

						Vector3 normal = Vector3.Cross(toPrev, toNext).normalized;
						if (flipNormal)
							normal *= -1f;

						linePath.Normals[i] = linePath.transform.InverseTransformDirection(normal);
					}
				}

				flipNormal = !flipNormal;

				ResetHandleSettings();

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

			if (currentTool != Tools.current ||
				currentPivotRotation != Tools.pivotRotation)
			{
				currentTool = Tools.current;
				currentPivotRotation = Tools.pivotRotation;

				ResetHandleSettings();
			}

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

			Handles.color = Color.green.SetA(0.3f);
			Handles.SphereHandleCap(0, linePath.GetPoint(0f), Quaternion.identity, 0.1f, EventType.Repaint);

			Handles.color = Color.red.SetA(0.3f);
			Handles.SphereHandleCap(0, linePath.GetPoint(1f), Quaternion.identity, 0.1f, EventType.Repaint);

			if (linePath.NormalType == NormalType.Perpendicular)
			{
				for (int i = 0; i < linePath.Normals.Length; i++)
				{
					Vector3 pos = linePath.GetPoint(i);
					Vector3 normal = linePath.GetNormal(i);

					Handles.color = Color.red;
					Handles.DrawLine(pos, pos + normal * 0.8f);
				}
			}

			int numIterations = 10 * linePath.PointCount;
			for (int i = 1; i < numIterations; i++)
			{
				float alpha = i / (float)numIterations;

				Vector3 pos = linePath.GetPoint(alpha);
				Vector3 normal = linePath.GetNormal(alpha);

				Handles.color = Color.green;
				Handles.DrawLine(pos, pos + normal * 0.4f);
			}
		}

		private void ResetHandleSettings()
		{
			if (selectedIndex == -1)
				return;

			if (Tools.pivotRotation == PivotRotation.Global)
			{
				handleRotation = Quaternion.identity;
				return;
			}

			Vector3 pos = linePath.GetPoint(selectedIndex);
			Vector3 normal = linePath.GetNormal(selectedIndex);

			if (currentTool == Tool.Rotate)
			{
				Quaternion offsetRot = Quaternion.FromToRotation(handleRotation * Vector3.up, normal);
				handleRotation = offsetRot * handleRotation;
			}
			else
			{
				if (selectedIndex == 0)
				{
					Vector3 guidePos = linePath.GetPoint(selectedIndex + 1);
					Vector3 toGuide = guidePos - pos;
					if (toGuide == Vector3.zero)
						toGuide = Vector3.forward;

					handleRotation = Quaternion.LookRotation(toGuide, normal);
				}
				else
				{
					Vector3 guidePos = linePath.GetPoint(selectedIndex - 1);
					Vector3 toGuide = guidePos - pos;
					if (toGuide == Vector3.zero)
						toGuide = Vector3.forward;

					handleRotation = Quaternion.LookRotation(toGuide, normal);
				}
			}			
		}

		protected virtual Vector3 ShowPoint(int index)
		{
			Vector3 point = linePath.GetPoint(index);
			float size = HandleUtility.GetHandleSize(point);
			if (index == 0)
			{
				size *= 2f;
			}

			Handles.color = Color.white;
			if (Handles.Button(point, handleRotation, size * handleSize, size * pickSize, Handles.DotHandleCap))
			{
				selectedIndex = index;
				ResetHandleSettings();
				Repaint();
			}

			if (selectedIndex == index)
			{
				if (Tools.current == Tool.Rotate)
				{
					EditorGUI.BeginChangeCheck();

					Quaternion newRotation = Handles.DoRotationHandle(handleRotation, point);
					Quaternion relativeRotation = newRotation * Quaternion.Inverse(handleRotation);
					handleRotation = newRotation;

					if (EditorGUI.EndChangeCheck())
					{
						Undo.RecordObject(linePath, "Rotate Point");
						EditorUtility.SetDirty(linePath);

						Vector3 worldNormal = linePath.GetNormal(index);
						worldNormal = relativeRotation * worldNormal;
						linePath.Normals[index] = linePath.transform.InverseTransformDirection(worldNormal);
					}

					return point;
				}

				EditorGUI.BeginChangeCheck();
				point = Handles.DoPositionHandle(point, handleRotation);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(linePath, "Move Point");
					EditorUtility.SetDirty(linePath);
					linePath.Points[index] = handleTransform.InverseTransformPoint(point);
					linePath.OnPathChanged(linePath);
				}
			}

			return point;
		}
	}
}