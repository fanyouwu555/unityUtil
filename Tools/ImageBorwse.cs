using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UI;



public class ImageBorwse : EditorWindow
{
	private const float defaultImageWidth = 150;
	private const float defaultImageHeight = 150;
	private const int Column = 6;
	private string currentSelectPath;

	List<string> imagePath = new List<string>();
	public void SetImagePath( List<string> path )
	{
		imagePath = path;
	}



	Vector2 scrollPosition = new Vector2(0 , 0);

	void OnGUI()
	{
		scrollPosition = GUILayout.BeginScrollView(scrollPosition , false , true);
		if( null != currentSelectPath )
		{
			GUILayout.Label(currentSelectPath);
		}

		int count = 0;
		foreach( string path in imagePath )
		{
			Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
			if( null == tex )
			{
				continue;
			}

			count++;
			if( count % Column == 1 )
			{
				GUILayout.BeginHorizontal();
			}

			if( GUILayout.Button(tex , GUILayout.Width(defaultImageWidth) , GUILayout.Height(defaultImageHeight)) )
			{
				currentSelectPath = path;
				if( EditorApplication.isPlaying )
				{
					EditorUtil.CopyTextToClipboard(path);
				}
				else
				{
					EditorGUIUtility.PingObject(tex);
				}
			}

			if( count % Column == 0 )
			{
				GUILayout.EndHorizontal();
			}
		}

		GUILayout.EndScrollView();
	}




}