using UnityEditor;
using UnityEngine;

namespace Luckshot.Paths
{
	[CustomEditor(typeof(SplinePath), true)]
	public class SplinePathInspector : Editor
	{
		protected const int stepsPerCurve = 10;
		protected const float directionScale = 0.5f;
		protected const float handleSize = 0.04f;
		protected const float pickSize = 0.06f;

		protected static Color[] modeColors = { Color.white, Color.yellow, Color.cyan };

		protected SplinePath spline = null;
		protected Transform handleTransform = null;
		protected Quaternion handleRotation = Quaternion.identity;
		protected Vector3 handleScale = Vector3.one;
		protected int selectedIndex = -1;

		private Tool currentTool = Tool.None;
		private PivotRotation currentPivotRotation = PivotRotation.Local;

		private void OnEnable()
		{
			Undo.undoRedoPerformed += UndoRedoPerformed;

			spline = target as SplinePath;

			if(!Application.IsPlaying(spline))
				spline.OnPathChanged(spline);
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= UndoRedoPerformed;
			Tools.hidden = false;
		}

		private void UndoRedoPerformed()
		{
			spline = target as SplinePath;

			if (spline != null)
				spline.OnPathChanged(spline);
			else
				Undo.undoRedoPerformed -= UndoRedoPerformed;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			spline = target as SplinePath;

			if (selectedIndex >= 0 && selectedIndex < spline.ControlPointCount)
			{
				DrawSelectedPointInspector();
				Tools.hidden = true;
			}
			else
			{
				Tools.hidden = false;
			}

			if (GUILayout.Button("Add Curve"))
			{
				Undo.RecordObject(spline, "Add Curve");
				spline.AddCurve();
				EditorUtility.SetDirty(spline);
			}

			if (spline.CurveCount > 1 && GUILayout.Button("Remove Curve"))
			{
				Undo.RecordObject(spline, "Remove Curve");
				spline.RemoveCurve();
				EditorUtility.SetDirty(spline);
			}

			if (GUI.changed)
				spline.OnPathChanged(spline);
		}

		protected virtual void DrawSelectedPointInspector()
		{
			GUILayout.Label("Selected Point", EditorStyles.boldLabel);
			EditorGUI.BeginChangeCheck();
			Vector3 point = EditorGUILayout.Vector3Field("Position", spline.GetControlPoint(selectedIndex));

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Move Point");
				EditorUtility.SetDirty(spline);
				spline.SetControlPoint(selectedIndex, point, true);
			}

			EditorGUI.BeginChangeCheck();
			BezierControlPointMode mode = (BezierControlPointMode)EditorGUILayout.EnumPopup("Mode", spline.GetControlPointMode(selectedIndex));

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Change Point Mode");
				spline.SetControlPointMode(selectedIndex, mode);
				EditorUtility.SetDirty(spline);
			}

			// Draw Node Data if ADV SPLINE
			// Would be in AdvSplineInspector, except AdvSpline is generic meaning
			// we can't use the CustomEditor attribute and force child types to use its inspector
			// which means either this is here or every AdvSpline child needs an associated TypeInspector script
			if (selectedIndex % 3 == 0)
			{
				int advNodeIndex = selectedIndex / 3;
				SerializedObject so = serializedObject;
				SerializedProperty nodeDataProp = so.FindProperty(string.Format("{0}.Array.data[{1}]", "_nodeData", advNodeIndex));
				if (nodeDataProp != null)
				{
					EditorGUI.BeginChangeCheck();

					EditorGUILayout.PropertyField(nodeDataProp, new GUIContent("Node Data"), true);

					if (EditorGUI.EndChangeCheck())
						so.ApplyModifiedProperties();
				}
			}
		}

		protected virtual void OnSceneGUI()
		{
			spline = target as SplinePath;
			handleTransform = spline.transform;

			if (currentTool != Tools.current ||
				currentPivotRotation != Tools.pivotRotation)
			{
				ResetHandleSettings();
				currentTool = Tools.current;
				currentPivotRotation = Tools.pivotRotation;
			}

			Vector3 p0 = ShowPoint(0);
			for (int i = 1; i < spline.ControlPointCount; i += 3)
			{
				Vector3 p1 = ShowPoint(i);
				Vector3 p2 = ShowPoint(i + 1);
				Vector3 p3 = ShowPoint(i + 2);

				Handles.color = Color.gray;
				Handles.DrawLine(p0, p1);
				Handles.DrawLine(p2, p3);

				Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
				p0 = p3;
			}

			ShowDirections();
		}

		void ShowDirections()
		{
			Handles.color = Color.green;
			Vector3 point = spline.GetPoint(0f);
			Handles.DrawLine(point, point + spline.GetDirection(0f) * directionScale);
			int steps = stepsPerCurve * spline.CurveCount;
			for (int i = 1; i <= steps; i++)
			{
				point = spline.GetPoint(i / (float)steps);
				Handles.DrawLine(point, point + spline.GetDirection(i / (float)steps) * directionScale);
			}
		}

		private void ResetHandleSettings()
		{
			if (selectedIndex == -1)
				return;

			handleScale = Vector3.one;

			Vector3 point = spline.GetControlPoint(selectedIndex);
			if (selectedIndex % 3 == 0)
			{
				if (selectedIndex == 0)
				{
					Vector3 p2 = spline.GetControlPoint(selectedIndex + 1);
					handleRotation = Quaternion.LookRotation(point - p2, Vector3.up);
				}
				else
				{
					Vector3 p1 = spline.GetControlPoint(selectedIndex - 1);
					handleRotation = Quaternion.LookRotation(p1 - point, Vector3.up);
				}
			}
			else
				handleRotation = Quaternion.identity;

			handleRotation = Tools.pivotRotation == PivotRotation.Local ? handleRotation : Quaternion.identity;
		}

		protected virtual Vector3 ShowPoint(int index)
		{
			Vector3 point = spline.GetControlPoint(index);
			float size = HandleUtility.GetHandleSize(point);
			if (index == 0)
			{
				size *= 2f;
			}

			Handles.color = modeColors[(int)spline.GetControlPointMode(index)];
			if (Handles.Button(point, spline.transform.rotation, size * handleSize, size * pickSize, Handles.DotHandleCap))
			{
				selectedIndex = index;

				ResetHandleSettings();

				Repaint();
			}

			if (selectedIndex == index)
			{
				if(index % 3 == 0)
				{
					if (Tools.current == Tool.Rotate)
					{
						EditorGUI.BeginChangeCheck();

						Quaternion newRotation = Handles.DoRotationHandle(handleRotation, point);
						Quaternion relativeRotation = newRotation * Quaternion.Inverse(handleRotation);
						handleRotation = newRotation;

						if (EditorGUI.EndChangeCheck())
						{
							Undo.RecordObject(spline, "Rotate Point");
							EditorUtility.SetDirty(spline);

							if (index > 0)
							{
								Vector3 p1 = spline.GetControlPoint(index - 1);
								Vector3 toPos = relativeRotation * (p1 - point);
								spline.SetControlPoint(index - 1, point + toPos, true);
							}

							if (index + 1 < spline.ControlPointCount)
							{
								Vector3 p2 = spline.GetControlPoint(index + 1);
								Vector3 toPos2 = relativeRotation * (p2 - point);
								spline.SetControlPoint(index + 1, point + toPos2, true);
							}
						}

						return point;
					}
					else if(Tools.current == Tool.Scale)
					{
						EditorGUI.BeginChangeCheck();

						Vector3 newScale = Handles.ScaleHandle(handleScale, point, handleRotation, HandleUtility.GetHandleSize(point));

						float min = Mathf.Min(
							newScale.x / handleScale.x,
							newScale.y / handleScale.y, 
							newScale.z / handleScale.z);

						float max = Mathf.Max(
							newScale.x / handleScale.x,
							newScale.y / handleScale.y,
							newScale.z / handleScale.z);

						float scale = (max - 1f) > (1f - min) ? max : min; 

						handleScale = newScale;

						if (EditorGUI.EndChangeCheck())
						{
							Undo.RecordObject(spline, "Rotate Point");
							EditorUtility.SetDirty(spline);

							if (index > 0)
							{
								Vector3 p1 = spline.GetControlPoint(index - 1);
								Vector3 toPos = scale * (p1 - point);
								spline.SetControlPoint(index - 1, point + toPos, true);
							}

							if (index + 1 < spline.ControlPointCount)
							{
								Vector3 p2 = spline.GetControlPoint(index + 1);
								Vector3 toPos2 = scale * (p2 - point);
								spline.SetControlPoint(index + 1, point + toPos2, true);
							}
						}


						return point;
					}
				}

				Tools.current = Tool.Move;

				EditorGUI.BeginChangeCheck();
				point = Handles.DoPositionHandle(point, handleRotation);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(spline, "Move Point");
					EditorUtility.SetDirty(spline);
					spline.SetControlPoint(index, point, true);
				}
			}

			return point;
		}
	}
}