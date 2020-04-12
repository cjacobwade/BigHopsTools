using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Luckshot.Paths;

public static class SplineUtils
{
	public const float DefaultTraverseAlphaSpeed = 0.01f;

	private static void Move(PathBase path, float alpha, float prevAlpha, ref Vector3 prevPos, ref float moveAmount)
	{
		Vector3 pos = path.GetPoint(alpha);
		Vector3 toPos = pos - prevPos;

		if (toPos.magnitude < moveAmount)
		{
			prevPos = pos;
			moveAmount -= toPos.magnitude;
		}
		else
		{
			float toPosAlpha = moveAmount / toPos.magnitude;
			alpha = Mathf.Lerp(prevAlpha, alpha, toPosAlpha);
			moveAmount = 0;
		}
	}

	public static float MoveAtFixedSpeed(this PathBase path, float alpha, float speed, ref bool reversed, float traverseAlphaSpeed = DefaultTraverseAlphaSpeed)
	{
		Vector3 startPos = path.GetPoint(alpha);
		Vector3 prevPos = startPos;

		float alphaSpeed = traverseAlphaSpeed;
		if (reversed)
			alphaSpeed *= -1f;

		float moveAmount = speed;
		while (moveAmount > 0f)
		{
			float prevAlpha = alpha;
			alpha += alphaSpeed * Time.deltaTime;

			if (path.Loop)
			{
				if (alpha > 1f && prevAlpha > 0.5f)
					prevAlpha -= 1f;
				else if (alpha < 0f && prevAlpha < 0.5f)
					prevAlpha += 1f;

				alpha = Mathf.Repeat(alpha, 1f);
			}
			else
			{
				if (alpha > 1f || alpha < 0f)
				{
					reversed = !reversed;
					alphaSpeed *= -1f;

					if (alpha > 1f)
					{
						Move(path, 1f, prevAlpha, ref prevPos, ref moveAmount);
						prevAlpha = 1f;

						alpha = 1f - (alpha - 1f);
					}
					else
					{
						Move(path, 0f, prevAlpha, ref prevPos, ref moveAmount);
						prevAlpha = 0f;

						alpha = -alpha;
					}
				}
			}

			Move(path, alpha, prevAlpha, ref prevPos, ref moveAmount);
		}

		return alpha;
	}
}
