using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;

public static class StringUtils
{
	public static string[] SplitByCase(this string inString)
	{
		StringBuilder builder = new StringBuilder();
		for (int i = 0; i < inString.Length; i++)
		{
			if (i > 0 && !Char.IsUpper(inString[i - 1]) && Char.IsUpper(inString[i]))
				builder.Append(',');

			builder.Append(inString[i]);
		}
		return builder.ToString().Split(',');
	}

	public static string SplitIntoWordsByCase(this string inString)
	{
		StringBuilder builder = new StringBuilder();
		for (int i = 0; i < inString.Length; i++)
		{
			if (i > 0 && !Char.IsUpper(inString[i - 1]) && Char.IsUpper(inString[i]))
				builder.Append(' ');

			builder.Append(inString[i]);
		}
		return builder.ToString();
	}

	public static string UppercaseFirst(this string s)
	{
		if (string.IsNullOrEmpty(s))
			return string.Empty;

		return char.ToUpper(s[0]) + s.Substring(1);
	}
}
