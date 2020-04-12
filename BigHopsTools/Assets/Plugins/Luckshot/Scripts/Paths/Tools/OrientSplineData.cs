using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Luckshot.Paths;

[System.Serializable]
public class OrientSplineData : SplineNodeData
{
	public float angle = 0f;

	public override void Lerp(SplineNodeData a, SplineNodeData b, float alpha)
	{
		base.Lerp(a, b, alpha);

		OrientSplineData oa = (OrientSplineData)a;
		OrientSplineData ob = (OrientSplineData)b;

		angle = Mathf.Lerp(oa.angle, ob.angle, alpha);
	}
}
