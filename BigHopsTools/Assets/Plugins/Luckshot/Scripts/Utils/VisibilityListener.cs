using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibilityListener : MonoBehaviour
{
	private bool isVisible = false;
	public bool IsVisible
	{ get { return isVisible; } }

	public event System.Action<bool> OnVisibilityChanged = delegate { };

	void OnBecameVisible()
	{
		isVisible = true;
		OnVisibilityChanged(true);
	}

	void OnBecameInvisible()
	{
		isVisible = false;
		OnVisibilityChanged(false);
	}
}
