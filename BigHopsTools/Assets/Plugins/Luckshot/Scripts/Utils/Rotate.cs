using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
	[SerializeField]
	private Vector3 speed = Vector3.zero;

	[SerializeField]
	private Space space = Space.World;

	[SerializeField]
	private bool randomizeOnEnable = false;

	private new Rigidbody rigidbody = null;

	private void Awake()
	{
		rigidbody = GetComponent<Rigidbody>();
	}

	private void OnEnable()
	{
		if (randomizeOnEnable)
		{
			Vector3 appliedSpeed = space == Space.World ? speed : transform.TransformVector(speed);
			Vector3 axis = appliedSpeed.normalized;

			RotateByAngle(Random.value * 360f, axis);
		}
	}

	private void FixedUpdate()
	{
		Vector3 appliedSpeed = space == Space.World ? speed : transform.TransformVector(speed);
		Vector3 axis = appliedSpeed.normalized;
		float angle = appliedSpeed.magnitude * Time.deltaTime;

		RotateByAngle(angle, axis);
	}

	private void RotateByAngle(float angle, Vector3 axis)
	{
		bool hasRigidbody = rigidbody != null;

		Quaternion newRotation = hasRigidbody ? rigidbody.rotation : transform.rotation;
		newRotation = Quaternion.AngleAxis(angle, axis) * newRotation;

		if (hasRigidbody)
			rigidbody.MoveRotation(newRotation);
		else
			transform.rotation = newRotation;
	}
}
