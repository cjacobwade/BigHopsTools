using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CirclePath))]
public class CirclePathEditor : Editor
{
	private CirclePath circlePath = null;

	private void OnEnable()
	{
		Undo.undoRedoPerformed += UndoRedoPerformed;

		circlePath = target as CirclePath;
		circlePath.OnPathChanged(circlePath);
	}

	private void OnDisable()
	{
		Undo.undoRedoPerformed -= UndoRedoPerformed;
		Tools.hidden = false;
	}

	private void UndoRedoPerformed()
	{
		if (circlePath != null)
			circlePath.OnPathChanged(circlePath);
		else
			Undo.undoRedoPerformed -= UndoRedoPerformed;
	}

	public override void OnInspectorGUI()
	{
		float prevFillAmount = circlePath.FillAmount;
		bool prevLoop = circlePath.Loop;

		base.OnInspectorGUI();

		circlePath = target as CirclePath;

		SerializedObject so = new SerializedObject(circlePath);

		if (GUI.changed)
		{
			if(prevLoop != circlePath.Loop)
			{
				if (circlePath.Loop && circlePath.FillAmount != 1f)
					circlePath.SetFillAmount(1f);

				if (prevLoop && circlePath.FillAmount == 1f)
					circlePath.SetFillAmount(0.99f);

				prevFillAmount = circlePath.FillAmount;
			}

			if(prevFillAmount != circlePath.FillAmount)
			{
				if (circlePath.FillAmount == 1f && !circlePath.Loop)
					circlePath.SetLoop(true);

				if(prevFillAmount == 1f && circlePath.Loop)
					circlePath.SetLoop(false);

				prevLoop = circlePath.Loop;
			}

			circlePath.OnPathChanged(circlePath);
		}
	}

	protected virtual void OnSceneGUI()
	{
		circlePath = target as CirclePath;

		Vector3 prevPathPos = circlePath.GetPoint(0f);

		int iterations = 100;
		for (int i = 1; i <= iterations; i++)
		{
			Vector3 pathPos = circlePath.GetPoint(i / (float)iterations);
			Handles.DrawLine(pathPos, prevPathPos);

			prevPathPos = pathPos;
		}

		int numIterations = 10 * Mathf.CeilToInt(circlePath.CircleRadius);
		for (int i = 1; i < numIterations; i++)
		{
			float alpha = i / (float)numIterations;

			Vector3 pos = circlePath.GetPoint(alpha);
			Vector3 normal = circlePath.GetNormal(alpha);

			Handles.color = Color.green;
			Handles.DrawLine(pos, pos + normal * 0.4f);
		}
	}
}
