using UnityEngine;
using System.Collections;

public enum CompareType
{
	Greater,
	GEqual,
	Less,
	LEqual,
	Equal,
	NotEqual
}

public static class CompareUtils
{
	public static bool CompareTo(this int value, CompareType compareType, int compareTo)
	{
		switch (compareType)
		{
			case CompareType.Greater:
				return value > compareTo;
			case CompareType.GEqual:
				return value >= compareTo;
			case CompareType.Less:
				return value < compareTo;
			case CompareType.LEqual:
				return value <= compareTo;
			case CompareType.Equal:
				return value == compareTo;
			case CompareType.NotEqual:
				return value != compareTo;
			default:
				return false;
		}
	}

	public static bool CompareTo(this float value, CompareType compareType, float compareTo)
	{
		switch (compareType)
		{
			case CompareType.Greater:
				return value > compareTo;
			case CompareType.GEqual:
				return value >= compareTo;
			case CompareType.Less:
				return value < compareTo;
			case CompareType.LEqual:
				return value <= compareTo;
			case CompareType.Equal:
				return compareTo.Equals(value);
			case CompareType.NotEqual:
				return !compareTo.Equals(value);
			default:
				return false;
		}
	}
}
