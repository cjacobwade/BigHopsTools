using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ProjectileUtils
{
	// https://gamedev.stackexchange.com/questions/114522/how-can-i-launch-a-gameobject-at-a-target-if-i-am-given-everything-except-for-it
	// more in depth formula explanation here https://gamedev.stackexchange.com/questions/71392/how-do-i-determine-a-good-path-for-2d-artillery-projectiles/71440#71440
	public static Vector3 GetLaunchVector3(Vector3 start, Vector3 end, float speed)
	{
		Vector3 toEnd = end - start;

		// Set up the terms we need to solve the quadratic equations.
		float gravitySqr = Physics.gravity.sqrMagnitude;
		float b = speed * speed + Vector3.Dot(toEnd, Physics.gravity);
		float discriminant = b * b - gravitySqr * toEnd.sqrMagnitude;

		// Check whether the target is reachable at max speed or less.
		if (discriminant < 0)
		{
			// Target is too far to hit with our speed, so we'll throw as far as possible
			discriminant = 0f; // Zero gives us the optimal arc
		}

		float discRoot = Mathf.Sqrt(discriminant);

		// Highest shot with the given max speed:
		float tHigh = Mathf.Sqrt((b + discRoot) * 2f / gravitySqr);

		// Most direct shot with the given max speed:
		float tDirect = Mathf.Sqrt((b - discRoot) * 2f / gravitySqr);

		// Lowest-speed arc available:
		float tIgnoreSpeed = Mathf.Sqrt(Mathf.Sqrt(toEnd.sqrMagnitude * 4f / gravitySqr));

		// Convert from time-to-hit to a launch velocity:
		Vector3 velocity = toEnd / tDirect - Physics.gravity * tDirect / 2f;
		return velocity;
	}
}
