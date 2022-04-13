using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UI;

public class ReplaceImage : EditorWindow
{
	[MenuItem( "Tools/Prefab/Prefab中图片替换" , false , 2001)]
	static void Init()
	{

		ReplaceImage window = (ReplaceImage)EditorWindow.GetWindow(typeof(ReplaceImage));
		window.Show();
	}


	private string oldImagePath;
	private Texture oldImage;
	private string newImagePath;
	private Texture newImage;

	private bool sliced;

	private string prefabPath = @"Assets\Art\UI\Prefab";




	void OnGUI()
	{
		GUI.skin.button.fontSize = 12;
		GUI.skin.label.fontSize = 12;

		GUILayout.Label("替换设置");
		GUILayout.BeginHorizontal();
		if( GUILayout.Button("Old Image" , GUILayout.Width(100)) )
		{
			if( Selection.activeObject )
			{
				oldImagePath = AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());
				oldImage = Selection.activeObject as Texture;
			}
		}
		GUILayout.Label("Old Image Path:" + oldImagePath);
		GUILayout.EndHorizontal();

		if( oldImage )
		{
			GUILayout.Box(oldImage , GUILayout.Height(100) , GUILayout.Width(100));
		}

		GUILayout.BeginHorizontal();
		if( GUILayout.Button("New Image" , GUILayout.Width(100)) )
		{
			if( Selection.activeObject )
			{
				newImagePath = AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());
				newImage = Selection.activeObject as Texture;
			}
		}
		GUILayout.Label("New Image Path:" + newImagePath);
		GUILayout.EndHorizontal();

		if( newImage )
		{
			GUILayout.Box(newImage , GUILayout.Height(100) , GUILayout.Width(100));
		}

		GUILayout.BeginHorizontal();
		GUILayout.Label("Prefab路径" , GUILayout.Width(100));
		prefabPath = GUILayout.TextField(prefabPath);
		GUILayout.EndHorizontal();


		sliced = GUILayout.Toggle(sliced , "Set ImageType : sliced");


		if( GUILayout.Button("Replace") )
		{
			//PrefabTools.RelaceImageAtPrefab("Assets/_UI/UI_Textures/UI_LevelUpPanel/button_red_off.png" 
			//	, "Assets/_UI/Resources/UI_Button/button_red_off.png" 
			//	, "Assets/LuaPrefabs/Resources/LuaLevelUpPanel_Item_Need.prefab");
			if( sliced )
			{
				PrefabTools.RelaceImageAtPrefabPath(oldImagePath , newImagePath , prefabPath , ChangeImageTypeSliced);
			}
			else
			{
				PrefabTools.RelaceImageAtPrefabPath(oldImagePath , newImagePath , prefabPath);
			}
		}

	}


	private static bool ChangeImageTypeSliced( Image e )
	{
		if( e.type != Image.Type.Sliced )
		{
			e.type = Image.Type.Sliced;
			return true;
		}
		return false;
	}

}
