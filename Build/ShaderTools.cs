using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;



public class BuildShader 
{




	[MenuItem( "AssetBundle/Shader/预加载Shader " , false , 3000 )]
	public static void Menu_ShaderInclude()
	{
		List<string> shaderList = new List<string>();

		shaderList.Add("Legacy Shaders/Diffuse" );
		shaderList.Add("Hidden/CubeBlur" );
		shaderList.Add("Hidden/CubeCopy" );
		shaderList.Add("Hidden/CubeBlend" );
		shaderList.Add("Sprites/Default" );
		shaderList.Add("UI/Default" );
		shaderList.Add("UI/DefaultETC1" );
		shaderList.Add("Hidden/VideoDecode" );
		shaderList.Add("Hidden/Compositing" );
		shaderList.Add("UI/Gray" );
		shaderList.Add("Legacy Shaders/Particles/Additive" );


		shaderList.Add( "Sprites/Diffuse" );
		shaderList.Add( "Standard" );
		shaderList.Add( "Standard (Specular setup)" );
		shaderList.Add( "Hidden/TerrainEngine/Details/Vertexlit" );
		shaderList.Add( "Mobile/Bumped Diffuse" );
		shaderList.Add( "Mobile/Bumped Specular" );
		shaderList.Add( "Mobile/Diffuse" );
		shaderList.Add( "Mobile/Unlit (Supports Lightmap)" );
		shaderList.Add( "Mobile/Particles/Additive" );
		shaderList.Add( "Mobile/Particles/Alpha Blended" );
		shaderList.Add( "Mobile/Particles/VertexLit Blended" );
		shaderList.Add( "Mobile/Particles/Multiply" );
		shaderList.Add( "Mobile/Skybox" );
		shaderList.Add( "Mobile/VertexLit" );
		shaderList.Add( "Legacy Shaders/Bumped Diffuse" );
		shaderList.Add( "Legacy Shaders/Bumped Specular" );
		shaderList.Add( "Legacy Shaders/VertexLit" );

		shaderList.Add( "Particles/Additive" );
		shaderList.Add( "Particles/~Additive-Multiply" );
		shaderList.Add( "Particles/Additive (Soft)" );
		shaderList.Add( "Particles/Alpha Blended" );
		shaderList.Add( "Particles/Blend" );
		shaderList.Add( "Particles/Multiply" );

		SerializedObject graphicsSettings = new SerializedObject( AssetDatabase.LoadAllAssetsAtPath( "ProjectSettings/GraphicsSettings.asset" )[0] );
		SerializedProperty it = graphicsSettings.GetIterator();
		SerializedProperty dataPoint;
		while( it.NextVisible( true ) )
		{
			if( it.name == "m_AlwaysIncludedShaders" )
			{
				it.ClearArray();

				for( int i = 0 ; i < shaderList.Count ; i++ )
				{
					it.InsertArrayElementAtIndex( i );
					dataPoint = it.GetArrayElementAtIndex( i );
					dataPoint.objectReferenceValue = Shader.Find( shaderList[i] );
				}

				graphicsSettings.ApplyModifiedProperties();
			}
		}
	}

	[MenuItem( "AssetBundle/Shader/清除预加载Shader " , false , 3001 )]
	public static void Menu_ClearShaderInclude()
	{

		SerializedObject graphicsSettings = new SerializedObject( AssetDatabase.LoadAllAssetsAtPath( "ProjectSettings/GraphicsSettings.asset" )[0] );
		SerializedProperty it = graphicsSettings.GetIterator();
		SerializedProperty dataPoint;
		while( it.NextVisible( true ) )
		{
			if( it.name == "m_AlwaysIncludedShaders" )
			{
				it.ClearArray();

				graphicsSettings.ApplyModifiedProperties();
			}
		}
	}

	[MenuItem( "AssetBundle/Shader/Set -- ShaderName" , false , 3002 )]
	public static void Menu_SetShaderName()
	{
		Dictionary<string , string> shaderName = new Dictionary<string , string>();
		SetShaderName( ref shaderName );

		// 添加 newBundleName
		foreach( KeyValuePair<string , string> pair in shaderName )
		{
			AssetImporter importer = AssetImporter.GetAtPath( pair.Key );
			importer.assetBundleName = pair.Value;
			importer.SaveAndReimport();
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		//	Debug.LogFormat("ShaderNum:{0} }" , shaderName.Count);

	}



	[MenuItem( "AssetBundle/Shader/Clear -- ShaderName" , false , 3002 )]
	public static void Menu_ClearShaderName()
	{
		Dictionary<string , string> shaderName = new Dictionary<string , string>();
		SetShaderName( ref shaderName );

		// 添加 newBundleName
		foreach( KeyValuePair<string , string> pair in shaderName )
		{
			AssetImporter importer = AssetImporter.GetAtPath( pair.Key );
			importer.assetBundleName = null;
			importer.SaveAndReimport();
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		AssetDatabase.RemoveUnusedAssetBundleNames();

		Debug.LogFormat( "ShaderNum:{0} }" , shaderName.Count );

	}






	public static void SetShaderName( ref Dictionary<string , string> shaderName )
	{
		int length = Application.dataPath.Length - "Assets".Length;

		int path_length = "Assets\\Art\\".Length;

		List<string> list = new List<string>();

		list.AddRange( Directory.GetFiles( PrefabTools.PathCombine( Application.dataPath , @"Art\Shader" ) , "*.*" , SearchOption.TopDirectoryOnly ) );

		foreach( string prefab in list )
		{
			string tempPath = prefab.Remove( 0 , length );
			if( tempPath.EndsWith( ".shader" ) == false )
			{
				continue;
			}
			tempPath = tempPath.Replace( "\\" , "/" );

			string prefabName = tempPath.Remove( 0 , path_length ).ToLower();
			string ext = Path.GetExtension( prefabName );
			if( !string.IsNullOrEmpty( ext ) )
			{
				prefabName = prefabName.Remove( prefabName.Length - ext.Length , ext.Length );
			}

			string assetName = "shader.unity3d";
			if( string.IsNullOrEmpty( assetName ) )
			{
				continue;
			}

			shaderName.Add( tempPath , assetName );
		}
	}




}
