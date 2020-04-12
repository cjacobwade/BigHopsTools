using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using System.Linq;

// This is a data structure to enforce a collection to have exactly one entry for each enum value
#region EXAMPLE USAGE
// 	public class EnumelTest : MonoBehaviour
// 	{
// 		[System.Serializable]
// 		public class EnumelTestClass : Enumel<EnumelTestEnum>
// 		{
// 			public float someData = 0f;
// 			public int someData2 = 0;
// 			public GameObject someData3 = null;
// 		}
// 
// 		public enum EnumelTestEnum
// 		{ A, B, C, D }
// 
// 		[SerializeField]
// 		List<EnumelTestClass> _enumel = new List<EnumelTestClass>();
// 
// 		private void OnValidate()
// 		{
// 			EnumelUtils.ValidateData<EnumelTestEnum, EnumelTestClass>(_enumel);
// 		}
// 	}
#endregion // EXAMPLE USAGE

public class Enumel<T> where T : struct
{
	[HideInInspector]
	public string name = string.Empty;

	[HideInInspector]
	public T enumType = default(T);
}

public static class EnumelUtils
{
	// Call this in OnValidate of a MonoBehaviour that uses this type to enforce
	public static void ValidateData<T, K>(List<K> data) where T : struct where K : Enumel<T>
	{
		Debug.Assert(typeof(T).IsEnum, "Must use enum for T");

		T[] enumValues = (T[])Enum.GetValues(typeof(T));
		if (data.Count != enumValues.Length)
		{
			List<K> dataToAdd = new List<K>();
			List<K> dataToRemove = new List<K>();
			for (int i = 0; i < enumValues.Length; i++)
			{
				List<K> dataMatches = data.Where(cd => cd.enumType.Equals(enumValues[i])).ToList();

				if (dataMatches.Count == 0)
				{
					Type enumelType = typeof(K);
					ConstructorInfo ctor = enumelType.GetConstructor(new Type[] {});
					K newData = (K)ctor.Invoke(new object[] {});
					newData.name = enumValues[i].ToString();
					newData.enumType = enumValues[i];
					
					dataToAdd.Add(newData);
				}

				if (dataMatches.Count > 1)
				{
					for (int j = 1; j < dataMatches.Count; j++)
						dataToRemove.Add(dataMatches[j]);
				}
			}

			for(int i = 0; i < data.Count; i++)
			{
				if(!dataToRemove.Contains(data[i]) &&
					Array.IndexOf(enumValues, data[i].enumType) == -1)
				{
					dataToRemove.Add(data[i]);
				}
			}

			foreach (K toRemove in dataToRemove)
				data.Remove(toRemove);

			foreach (K toAdd in dataToAdd)
				data.Add(toAdd);
		}
	}
}
