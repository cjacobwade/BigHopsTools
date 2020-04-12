using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Luckshot.Paths;

public class CirclePath : PathBase
{
	[SerializeField]
	private float circleRadius = 1f;

	[SerializeField, Range(0f, 1f)]
	private float fillAmount = 1f;

	private bool prevLoop = false;

	public override bool Loop
	{ get { return base.Loop && fillAmount == 1f; } }

	private void OnValidate()
	{
		if(prevLoop != loop)
		{
			if (loop)
				fillAmount = 1f;

			prevLoop = loop;
		}
	}

	public override Vector3 GetPoint(float alpha)
	{
		Quaternion rotationOffset = Quaternion.AngleAxis(alpha * 360f * fillAmount, Vector3.up);
		return transform.TransformPoint(rotationOffset * Vector3.forward * circleRadius);
	}

	public override float GetNearestAlpha(Vector3 position, int iterations = 10)
	{
		Vector3 toPosition = position - transform.position;
		float angle = Vector3.SignedAngle(transform.forward, toPosition.normalized, transform.up);

		float alpha = 0;
		if (angle > 0f)
			alpha = angle / 360f;
		else
			alpha = (360f + angle) / 360f;

		if(alpha > fillAmount)
		{
			float halfRemainder = (1f - fillAmount) / 2f;
			if (alpha > fillAmount + halfRemainder)
				alpha = 0f;
			else
				alpha = fillAmount;
		}

		return alpha / fillAmount;
	}

	public override Vector3 GetVelocity(float alpha)
	{ return GetDirection(alpha); }

	public override Vector3 GetDirection(float alpha)
	{
		Quaternion rotationOffset = Quaternion.AngleAxis(alpha * 360f * fillAmount, Vector3.up);
		Vector3 toCirclePos = rotationOffset * Vector3.forward;
		Vector3 tangent = Vector3.Cross(Vector3.up, toCirclePos).normalized;
		return transform.TransformDirection(tangent);
	}

	public override Vector3 GetNormal(float t)
	{ return (GetPoint(t) - transform.position).normalized;}

	public override float GetLength()
	{ return circleRadius * Mathf.PI * 2f; }

	private void OnDrawGizmosSelected()
	{
		Vector3 prevPathPos = GetPoint(0f);

		int iterations = 100;
		for(int i = 1; i <= iterations; i++)
		{
			Vector3 pathPos = GetPoint(i / (float)iterations);
			Gizmos.DrawLine(pathPos, prevPathPos);

			prevPathPos = pathPos;
		}
	}
}
