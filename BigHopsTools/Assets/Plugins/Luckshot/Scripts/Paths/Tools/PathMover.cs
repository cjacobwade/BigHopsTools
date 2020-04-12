using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Luckshot.Paths;
using NaughtyAttributes;

public class PathMover : MonoBehaviour
{
	[SerializeField]
	private PathBase path = null;

	[SerializeField]
	private new Rigidbody rigidbody = null;

	[SerializeField]
	private float speed = 1f;

	private float lineAlpha = 0f;
	private bool reversed = false;

	[SerializeField]
	private bool pauseAtEnds = false;

	[SerializeField, ShowIf("pauseAtEnds")]
	private float pauseAtEndsTime = 1f;
	private float lastEndTime = 0f;

	private void Awake()
	{
		lineAlpha = path.GetNearestAlpha(rigidbody.position);
		rigidbody.position = path.GetPoint(lineAlpha);
	}

	private void FixedUpdate()
	{
		if (rigidbody == null || path == null)
			return;

		if (!pauseAtEnds || Time.time > lastEndTime + pauseAtEndsTime)
		{
			bool wasReversed = reversed;
			lineAlpha = path.MoveAtFixedSpeed(lineAlpha, speed * Time.deltaTime, ref reversed);

			if (wasReversed != reversed)
				lastEndTime = Time.time;

			rigidbody.position = path.GetPoint(lineAlpha);
		}
	}
}
