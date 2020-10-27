using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Luckshot.Paths;

public class OrientSpline : AdvSpline<OrientSplineData>
{
	OrientSplineData interpData = new OrientSplineData();

	protected virtual void OnDrawGizmosSelected()
	{
		for(float iter = 0f; iter <= 1f; iter += 0.05f)
		{
			Vector3 point = GetPoint(iter);
			InterpNodeData(interpData, iter);

			Vector3 direction = GetVelocity(iter).normalized;
			Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
			Vector3 normal = Vector3.Cross(direction, right).normalized;
			Vector3 up = Quaternion.Euler(direction * interpData.angle) * normal;

			Gizmos.DrawLine(point, point + up);
		}
	}
}
