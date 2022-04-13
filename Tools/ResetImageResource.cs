using UnityEditor;
using UnityEngine;
using System.Collections;
using System.IO;


public class ResetImageResource
{

	[MenuItem("Tools/取消UI图片MipMaps")]
	public static void ChangeImage( MenuCommand menuCommand )
	{
		foreach( var file in Directory.GetFiles(@"Assets\Art\UI\Texture\" , "*.*" , SearchOption.AllDirectories) )
		{
			SetPngMipMaps(file , false);
		}

		foreach( var file in Directory.GetFiles(@"Assets\Art\Effect\Texture" , "*.*" , SearchOption.AllDirectories) )
		{
			SetPngMipMaps(file , false);
		}

		Debug.Log("取消UI图片MipMaps");
	}

	private static void SetPngMipMaps( string file , bool enable )
	{
		TextureImporter textureImporter = AssetImporter.GetAtPath(file) as TextureImporter;
		if( null != textureImporter && ( textureImporter.mipmapEnabled != enable ) )
		{
			textureImporter.mipmapEnabled = enable;
			AssetDatabase.ImportAsset(file);
		}
	}






















}

