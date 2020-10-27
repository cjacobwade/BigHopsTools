﻿using UnityEditor;
using UnityEngine;

namespace Luckshot.Paths
{
	[CustomEditor(typeof(SplinePath), true)]
	public class SplinePathEditor : Editor
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

			if (spline == null)
				return;

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

			ResetHandleSettings();

			if (spline != null)
				spline.OnPathChanged(spline);
			else
				Undo.undoRedoPerformed -= UndoRedoPerformed;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			spline = target as SplinePath;
			if (spline == null)
				return;

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
				currentTool = Tools.current;
				currentPivotRotation = Tools.pivotRotation;

				ResetHandleSettings();
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

			Handles.color = Color.green.SetA(0.3f);
			Handles.SphereHandleCap(0, spline.GetPoint(0f), Quaternion.identity, 0.1f, EventType.Repaint);

			Handles.color = Color.red.SetA(0.3f);
			Handles.SphereHandleCap(0, spline.GetPoint(1f), Quaternion.identity, 0.1f, EventType.Repaint);

			if (spline.NormalType == NormalType.Perpendicular)
			{
				for (int i = 0; i < spline.Normals.Length; i++)
				{
					if (i % 3 == 0)
					{
						Vector3 pos = spline.GetControlPoint(i);
						Vector3 normal = spline.GetControlNormal(i);

						Handles.color = Color.red;
						Handles.DrawLine(pos, pos + normal * 0.8f);
					}
				}
			}

			int numIterations = 10 * spline.ControlPointCount;
			for (int i = 1; i < numIterations; i++)
			{
				float alpha = i / (float)numIterations;

				Vector3 pos = spline.GetPoint(alpha);
				Vector3 normal = spline.GetNormal(alpha);

				Handles.color = Color.green;
				Handles.DrawLine(pos, pos + normal * 0.4f);
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
				Vector3 normal = spline.GetControlNormal(selectedIndex);

				Vector3 guidePos = Vector3.zero;
				if (selectedIndex == 0)
					guidePos = spline.GetControlPoint(selectedIndex + 1);
				else
					guidePos = spline.GetControlPoint(selectedIndex - 1);

				Vector3 toGuide = (guidePos - point).normalized;

				Vector3 right = Vector3.Cross(normal, toGuide).normalized;
				normal = Vector3.Cross(-right, toGuide).normalized;

				spline.Normals[selectedIndex] = spline.transform.InverseTransformDirection(normal);

				handleRotation = Quaternion.LookRotation(toGuide, normal);

				if (spline.GetControlPointMode(selectedIndex) == BezierControlPointMode.Free)
				{
					handleRotation = Quaternion.LookRotation(normal);
					Quaternion offsetRot = Quaternion.AngleAxis(90f, handleRotation * Vector3.right);
					handleRotation = offsetRot * handleRotation;
				}
			}
			else
			{
				int nodeIndex = 0;

				if((selectedIndex - 1) % 3 == 0) // after node
					nodeIndex = selectedIndex - 1;
				else
					nodeIndex = selectedIndex + 1;

				Vector3 normal = spline.GetControlNormal(nodeIndex);

				Vector3 cp = spline.GetControlPoint(nodeIndex);
				handleRotation = Quaternion.LookRotation(point - cp, normal);
			}

			if (Tools.pivotRotation == PivotRotation.Global)
				handleRotation = Quaternion.identity;
		}

		protected virtual Vector3 ShowPoint(int index)
		{
			Vector3 point = spline.GetControlPoint(index);
			float size = HandleUtility.GetHandleSize(point);
			if (index == 0)
				size *= 2f;

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
					if (spline.NormalType == NormalType.LocalUp && Tools.current == Tool.Rotate)
						Tools.current = Tool.Move;

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

							Vector3 normal = spline.GetControlNormal(selectedIndex);
							normal = relativeRotation * normal;
							spline.Normals[selectedIndex] = spline.transform.InverseTransformDirection(normal);

							if (index > 0)
							{
								Vector3 p1 = spline.GetControlPoint(index - 1);
								Vector3 toPos = relativeRotation * (p1 - point);
								spline.SetControlPoint(index - 1, point + toPos, false);
							}

							// Need to check free because if aligned or mirrors, setting control point above
							// applies needed update to other guide point, which makes the below cause a double offset
							if (spline.GetControlPointMode(index) == BezierControlPointMode.Free &&
								index + 1 < spline.ControlPointCount)
							{
								Vector3 p2 = spline.GetControlPoint(index + 1);
								Vector3 toPos2 = relativeRotation * (p2 - point);
								spline.SetControlPoint(index + 1, point + toPos2, false);
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