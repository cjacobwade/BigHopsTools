using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class AutoCacheAttribute : PropertyAttribute
{
	public bool searchChildren = false;
	public bool searchAncestors = false;

	public AutoCacheAttribute(bool inSearchChildren = false, bool inSearchAncestors = false)
	{
		searchChildren = inSearchChildren;
		searchAncestors = inSearchAncestors;
	}
}
