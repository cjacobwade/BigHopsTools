using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;

[CustomEditor(typeof(Decal))]
public class DecalEditor : Editor
{
	private static int lastInstanceID = 0;

	private List<Material> materials = new List<Material>();

	private void OnEnable()
	{
		string[] matGuids = AssetDatabase.FindAssets("Decal t:Material");
		for (int i = 0; i < matGuids.Length; i++)
			matGuids[i] = AssetDatabase.GUIDToAssetPath(matGuids[i]);

		materials.Clear();
		for (int i = 0; i < matGuids.Length; i++)
			materials.Add(AssetDatabase.LoadAssetAtPath<Material>(matGuids[i]));

		Decal decal = target as Decal;
		if (decal != null)
		{
			SerializedObject so = new SerializedObject(decal);

			bool anyChanged = false;

			Material mat = null;
			SerializedProperty matProp = so.FindProperty("material");
			mat = matProp.objectReferenceValue as Material;
			if (mat == null)
			{
				mat = materials[0];
				matProp.objectReferenceValue = mat;
				anyChanged = true;
			}

			if (mat != null && mat.mainTexture != null)
			{
				SerializedProperty spriteProp = so.FindProperty("sprite");
				Sprite sprite = spriteProp.objectReferenceValue as Sprite;
				if (sprite == null)
				{
					string texPath = AssetDatabase.GetAssetPath(mat.mainTexture);
					Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texPath);
					for (int i = 0; i < assets.Length; i++)
					{
						sprite = assets[i] as Sprite;
						if (sprite != null)
						{
							spriteProp.objectReferenceValue = sprite;
							anyChanged = true;
							break;
						}
					}
				}
			}

			if (anyChanged)
			{
				so.ApplyModifiedProperties();
				decal.BuildDecal();
			}
		}
	}

	private void OnSceneGUI()
	{
		Decal decal = target as Decal;
		if (decal != null && decal.transform.hasChanged)
		{
			if (decal.GetInstanceID() != lastInstanceID)
			{
				decal.GetComponent<MeshFilter>().sharedMesh = null;
				lastInstanceID = decal.GetInstanceID();
			}

			UnityEngine.Profiling.Profiler.BeginSample("Decal");
			decal.BuildDecal();
			UnityEngine.Profiling.Profiler.EndSample();
			decal.transform.hasChanged = false;
		}
	}

	public override void OnInspectorGUI()
	{
		Decal decal = target as Decal;

		SerializedObject so = new SerializedObject(decal);

		SerializedProperty matProp = so.FindProperty("material");

		Material mat = matProp.objectReferenceValue as Material;
		mat = DrawAssetChooser("Materials", mat, materials);
		matProp.objectReferenceValue = mat;

		if (mat != null && mat.mainTexture != null)
		{
			string texPath = AssetDatabase.GetAssetPath(mat.mainTexture);
			Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texPath);

			List<Sprite> sprites = new List<Sprite>();
			for (int i = 0; i < assets.Length; i++)
			{
				Sprite texSprite = assets[i] as Sprite;
				if (texSprite != null)
					sprites.Add(texSprite);
			}

			SerializedProperty spriteProp = so.FindProperty("sprite");
			Sprite sprite = spriteProp.objectReferenceValue as Sprite;

			if (sprites.Count > 0)
			{
				sprite = DrawAssetChooser("Sprite", sprite, sprites);
				sprite = DrawSpriteList(sprite, sprites);
			}

			spriteProp.objectReferenceValue = sprite;
		}

		EditorGUILayout.Separator();
		if (GUILayout.Button("Build"))
			decal.BuildDecal();

		if (GUI.changed)
		{
			so.ApplyModifiedProperties();
			decal.BuildDecal();
			GUI.changed = false;
		}
	}

	private T DrawAssetChooser<T>(string label, T obj, List<T> objects) where T : Object
	{
		int index = objects.IndexOf(obj);
		string[] names = new string[objects.Count];
		for (int i = 0; i < names.Length; i++)
			names[i] = objects[i].name;

		using (new GUILayout.HorizontalScope())
		{
			obj = EditorGUILayout.ObjectField(label, obj, typeof(T), false) as T;
			GUILayout.Space(5);
			index = EditorGUILayout.Popup(index, names);
		}

		if (index == -1)
			return objects[0] ?? null;
		else
			return objects[index];
	}

	public static Sprite DrawSpriteList(Sprite sprite, List<Sprite> list)
	{
		foreach (var item in DrawGrid(list))
		{
			Rect rect = item.Value;

			Texture2D texture = item.Key.texture;
			Rect uvRect = item.Key.rect;
			uvRect.x /= texture.width;
			uvRect.y /= texture.height;
			uvRect.width /= texture.width;
			uvRect.height /= texture.height;

			if (item.Key == sprite)
				EditorGUI.DrawRect(rect, Color.blue);

			GUI.DrawTextureWithTexCoords(rect, texture, uvRect);
			if (GUI.Button(rect, GUIContent.none, GUI.skin.label))
				sprite = item.Key;
		}
		return sprite;
	}

	private static IEnumerable<KeyValuePair<T, Rect>> DrawGrid<T>(List<T> list)
	{
		var xCount = 5;
		var yCount = Mathf.CeilToInt((float)list.Count / xCount);

		var i = 0;
		foreach (var rect in DrawGrid(xCount, yCount))
		{
			if (i < list.Count)
				yield return new KeyValuePair<T, Rect>(list[i], rect);

			i++;
		}
	}

	private static IEnumerable<Rect> DrawGrid(int xCount, int yCount)
	{
		var id = GUIUtility.GetControlID("Grid".GetHashCode(), FocusType.Keyboard);
		using (new GUILayout.VerticalScope(GUI.skin.box))
		{
			for (var y = 0; y < yCount; y++)
			{
				using (new GUILayout.HorizontalScope())
				{
					for (var x = 0; x < xCount; x++)
					{
						Rect rect = GUILayoutUtility.GetAspectRect(1);
						if (Event.current.type == EventType.MouseDown &&
							rect.Contains(Event.current.mousePosition))
						{
							GUIUtility.hotControl = GUIUtility.keyboardControl = id;
						}

						yield return rect;
					}
				}
			}
		}
	}
}
