using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ScriptableObjectUtils
{
	public static T CreateAsset<T>(string pathOverride = "", string nameOverride = "") where T : ScriptableObject
	{
		T asset = ScriptableObject.CreateInstance<T> ();
#if UNITY_EDITOR
		string path = string.IsNullOrEmpty(pathOverride) ? "Assets/Resources" : pathOverride;
		string name = string.IsNullOrEmpty(nameOverride) ? typeof(T).ToString() : nameOverride;

		string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + name + ".asset");
		
		AssetDatabase.CreateAsset (asset, assetPathAndName);
		
		AssetDatabase.SaveAssets ();
		AssetDatabase.Refresh();
		EditorUtility.FocusProjectWindow ();
		Selection.activeObject = asset;
#endif

		return asset;
	}
}