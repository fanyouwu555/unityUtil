using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

/// <summary>
/// 把Resource下的资源打包成.data到OutPut目录下
/// </summary>
public partial class BuildAssetBundleScript
{

	[MenuItem( "AssetBundle/设置 BundleName" , false , 1000 )]
	public static void Menu_NewSetBundleName()
	{
		CheckSpineTexture();
		CheckFontTexture();
		CheckUITexture();
		UpdateAllSpineAssets();
		UpdateAllAudioAssets();
		UpdateAllFightAssets();

		Dictionary<string , string> oldBundleName = new Dictionary<string , string>();
		foreach( var bundleName in AssetDatabase.GetAllAssetBundleNames() )
		{
			if( bundleName == "luacode.unity3d" )
			{
				continue;
			}

			foreach( var ddd in AssetDatabase.GetAssetPathsFromAssetBundle( bundleName ) )
			{
				oldBundleName.Add( ddd , bundleName );
			}
		}

		Dictionary<string , string> uiFileTag = new Dictionary<string , string>();
		HashSet<string> removeUiFileTag = new HashSet<string>();
		SetUIAssetBundleName( uiFileTag , removeUiFileTag );
		///*
		GenPreLoadUnity3dList();

		// ShaderName
		Dictionary<string , string> shaderName = new Dictionary<string , string>();
		//BuildShader.SetShaderName( ref shaderName );

		StringBuilder sb = new StringBuilder();

		// 删除 oldBundleName
		foreach( KeyValuePair<string , string> pair in oldBundleName )
		{
			AssetImporter importer = AssetImporter.GetAtPath( pair.Key );
			importer.assetBundleName = null;
			importer.SaveAndReimport();
			sb.AppendFormat( "DEL\t{0}\n" , pair.Key );
		}

		// spritePackingTag
		int changeTagCount = 0;
		foreach( KeyValuePair<string , string> pair in uiFileTag )
		{
			TextureImporter importer = AssetImporter.GetAtPath( pair.Key ) as TextureImporter;

			if( importer.spritePackingTag != pair.Value )
			{
				importer.spritePackingTag = pair.Value;
				importer.SaveAndReimport();
				changeTagCount++;
			}
		}

		foreach( string path in removeUiFileTag )
		{
			TextureImporter importer = AssetImporter.GetAtPath( path ) as TextureImporter;
			if( null == importer )
			{
				continue;
			}
			if( string.IsNullOrEmpty( importer.spritePackingTag ) )
			{
				continue;
			}

			importer.spritePackingTag = "";
			importer.SaveAndReimport();
			changeTagCount++;
		}

		foreach( KeyValuePair<string , string> pair in shaderName )
		{
			AssetImporter importer = AssetImporter.GetAtPath( pair.Key );
			importer.assetBundleName = pair.Value;
			importer.SaveAndReimport();
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		if( sb.Length > 0 )
		{
			EditorUtil.SplitLog( Debug.Log , sb.ToString() );
		}
		Debug.LogFormat( "设置 BundleName 结束 !! delete = {0} , ChangeTag = {1}" , oldBundleName.Count , changeTagCount );
		// */
	}




	[MenuItem( "AssetBundle/Tools/删除 -- AssetBundle文件" , false )]
	public static void Menu_DeleteAssetBundleFile()
	{
		CleanBuild();
		Debug.Log( "DeleteAssetBundleFile OK!" );
	}



	private static List<string> notCleanFile = new List<string>
	{
		"slg_sqlite.db",
		"slg_language.db",
	};
	/// <summary>
	/// 清除生成的文件
	/// 1.删除 StreamingAssets 目录下的所有文件
	/// </summary>
	private static void CleanBuild()
	{
		// 删除文件夹
		EditorUtil.DeleteDir( Application.streamingAssetsPath );
		Directory.CreateDirectory( Application.streamingAssetsPath );
	}




	public const BuildAssetBundleOptions bundleOptions = BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.ChunkBasedCompression;

	public const string UnZipAssetBundlesPath = "UnzipStreamingAssets";
	public const string AssetBundlesOutputPath = @"Assets\" + UnZipAssetBundlesPath;


	public static void BuildAssetBundle( BuildTarget architecture , AssetBundleBuild[] builds = null )
	{
		if( EditorUserBuildSettings.activeBuildTarget != architecture )
		{
			Debug.LogError( "Current Active Build Target = " + EditorUserBuildSettings.activeBuildTarget );
			return;
		}

		Debug.Log( "开始打包!" );

		string outputPath = Application.streamingAssetsPath;
		if( !Directory.Exists( outputPath ) )
		{
			Directory.CreateDirectory( outputPath );
		}

		//根据BuildSetting里面所激活的平台进行打包 设置过AssetBundleName的都会进行打包
		if( null != builds )
		{
			BuildPipeline.BuildAssetBundles( outputPath , builds , bundleOptions , architecture );
		}
		else
		{
			BuildPipeline.BuildAssetBundles( outputPath , bundleOptions , architecture );
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		Debug.Log( "打包完成!" );
	}




	[MenuItem( "AssetBundle/Tools/CompressionAllFile" , false )]
	public static void CompressionAllFile()
	{
		if( !Directory.Exists( Application.streamingAssetsPath ) )
		{
			Directory.CreateDirectory( Application.streamingAssetsPath );
		}

		Debug.Log( "CompressionAllFile Begin : " + System.DateTime.Now.ToLocalTime() );

		string sourceDir = Application.streamingAssetsPath;
		string targetDir = AssetBundlesOutputPath;
		List<string> endsWith = new List<string> { ".unity3d" , ".meta" , ".manifest" , "filelist.txt" , "version.txt" , UnZipAssetBundlesPath };

		List<string> zipFileList = EditorUtil.ZipDir( endsWith , sourceDir , targetDir , EditorUtil.ZipFile );

		Debug.Log( "CompressionAllFile End : " + System.DateTime.Now.ToLocalTime() );
		File.WriteAllLines( Path.Combine( Application.streamingAssetsPath , "zipFileList" ) , zipFileList.ToArray() );
	}



	public static void GenZipFileList()
	{
		//List<string> endsWith = new List<string> { ".unity3d" , ".meta" , ".manifest" , "filelist.txt" , "version.txt" , "StreamingAssets" , UnZipAssetBundlesPath , "zipFileList" };
		List<string> fileList = Directory.GetFiles( Application.streamingAssetsPath , "*.db" , SearchOption.AllDirectories ).ToList()
			.Select( e => e.Substring( Application.streamingAssetsPath.Length + 1 ).Replace( '\\' , '/' ) ).ToList();

		//fileList.RemoveAll( e => endsWith.Any( file => e.EndsWith( file ) ) );
		File.WriteAllLines( Path.Combine( Application.streamingAssetsPath , "zipFileList" ) , fileList.ToArray() );
	}






	private static string GetFileNameByTypePath( string path , string tp )
	{
		try
		{
			return Regex.Match( path , @"([^\\/]+[\\/])*([^\\/]+)\." + tp , RegexOptions.IgnoreCase ).Groups[2].Value;
		}
		catch( ArgumentException )
		{
			return "";
		}
	}


	private static void addToAssetBunldeInfo( Dictionary<string , List<string>> assetInfo , string key , string value )
	{
		List<string> tmp = null;
		if( assetInfo.TryGetValue( key , out tmp ) )
		{
			tmp.Add( value );
		}
		else
		{
			assetInfo.Add( key , new List<string>() { value } );
		}
	}



	//[MenuItem( "AssetBundle", false )]
	public static void TestCode()
	{

	}



	public static List<AssetBundleBuild> GetAllPrefabAssetBundleBuild()
	{
		//int length = Application.dataPath.Length - "Assets".Length;
		Dictionary<string , List<string>> assetInfo = new Dictionary<string , List<string>>();

		// Font
		{
			int length = Application.dataPath.Length - "Assets".Length;
			foreach( string dir in fontTextureDir )
			{
				string fullPath = Path.Combine( Application.dataPath , dir );
				if( !Directory.Exists( fullPath ) )
				{
					continue;
				}

				foreach( string prefab in Directory.GetFiles( fullPath , "*.*" , SearchOption.TopDirectoryOnly ) )
				{
					if( prefab.EndsWith( ".fontsettings" , true , null )
						|| prefab.EndsWith( ".mat" , true , null )
						|| prefab.EndsWith( ".asset" , true , null )
						|| prefab.EndsWith( ".otf" , true , null ) || prefab.EndsWith( ".ttf" , true , null ) )
					{
						string tempPath = prefab.Remove( 0 , length ).Replace( "\\" , "/" );

						//fileAssetBundleName.Add( tempPath , "ui/font.unity3d" );
						addToAssetBunldeInfo( assetInfo , "ui/font.unity3d" , tempPath );
					}
				}
			}
		}

		// Ui Prefab
		{
			Dictionary<string , string> tmp = new Dictionary<string , string>();
			foreach( var prefabDir in packageUiPrefabDir )
			{
				int len = "Assets".Length + prefabDir.Trim( '/' , '\\' ).Length + 2;
				foreach( string tempPath in EditorUtil.GetAssetsFiles( prefabDir , "*.prefab" ) )
				{
					string key = tempPath.Remove( 0 , len );
					tmp[key] = tempPath;
				}
			}

			foreach( var value in tmp )
			{
				string tempPath = value.Value;
				string name = GetUiPrefabBundleName( Path.GetFileNameWithoutExtension( tempPath ) );
				addToAssetBunldeInfo( assetInfo , "ui/" + name + ".unity3d" , tempPath );
			}
		}


		// Prefab
		{
			string[] codeFileNames = new string[] { @"Lua\Data\AllAssets.lua" , @"Lua\Data\AllFightAssets.lua" , @"Lua\Data\AllSpineAssets.lua" };
			string[][] prefabDirs = new string[][] { allPrefabDir , fightPrefabDir , spinePrefabDir };

			HashSet<string> hashFilePath = new HashSet<string>();
			for( int i = 0 ; i < codeFileNames.Length ; i++ )
			{
				// 读取 AllAssets.lua AllFightAssets.lua AllSpineAssets.lua 中的bundle名称, 进行设置
				Dictionary<string , string> tmpAssetsInfo = readLuaAssetFile( codeFileNames[i] );

				foreach( var item in tmpAssetsInfo )
				{
					string prefabName = item.Key;
					string bundleName = item.Value;

					string filename = null;
					foreach( var dir in prefabDirs[i] )
					{
						string tmpFileName = Path.Combine( dir , prefabName + ".prefab" );
						string fullFileName = Path.Combine( Application.dataPath , tmpFileName );

						if( File.Exists( fullFileName ) )
						{
							filename = tmpFileName;
						}
					}

					if( !string.IsNullOrEmpty( filename ) )
					{
						string lowFileName = filename.ToLower();

						if( !hashFilePath.Contains( lowFileName ) )
						{
							addToAssetBunldeInfo( assetInfo , "prefab/" + bundleName.ToLower() , "Assets/" + filename );
							hashFilePath.Add( lowFileName );
						}
					}
				}
			}
		}

		// Audio
		foreach( var item in readLuaAssetFile( @"Lua\Data\AllAudioAssets.lua" ) )
		{
			//audioAssetsbDir
			string audioFileName = item.Key;
			string bundleName = item.Value;

			string filename = null;
			foreach( var dir in audioAssetsbDir )
			{
				string tmpFileName = Path.Combine( dir , audioFileName + ".mp3" );
				string fullFileName = Path.Combine( Application.dataPath , tmpFileName );

				if( File.Exists( fullFileName ) )
				{
					filename = tmpFileName;
				}
			}
			if( !string.IsNullOrEmpty( filename ) )
			{
				addToAssetBunldeInfo( assetInfo , "audio/" + bundleName.ToLower() , "Assets/" + filename );
			}
		}


		//Art Texture
		foreach( string textureDir in uiTextureDir )
		{
			foreach( string prefabDir in EditorUtil.GetAssetsDirectory( textureDir ) )
			{
				string dirName = Path.GetFileName( prefabDir );
				string bundleName = string.Format( "ui/{0}.unity3d" , dirName.ToLower() );
				string path = Path.Combine( textureDir , dirName );

				foreach( string tempPath in EditorUtil.GetAssetsFiles( path , new string[] { "*.png" , "*.jpg" , "*.mat" } , SearchOption.AllDirectories ) )
				{
					addToAssetBunldeInfo( assetInfo , bundleName , tempPath );
				}
			}
		}

		// Effect
		foreach( string textureDir in uiEffectTextureDir )
		{
			string bundleName = "ui/effect.unity3d";
			foreach( string tempPath in EditorUtil.GetAssetsFiles( textureDir , new string[] { "*.png" , "*.jpg" , "*.mat" } , SearchOption.AllDirectories ) )
			{
				addToAssetBunldeInfo( assetInfo , bundleName , tempPath );
			}
		}

		// File.WriteAllText( "E:\\art_texture_gen.txt" , sb.ToString() );



		//StringBuilder sb2 = new StringBuilder();
		foreach( var bundleName in AssetDatabase.GetAllAssetBundleNames() )
		{
			if( assetInfo.ContainsKey( bundleName ) )
			{
				continue;
			}
			assetInfo.Add( bundleName , new List<string>() );
			//	 Array.ForEach( AssetDatabase.GetAssetPathsFromAssetBundle( bundleName ) , e => { sb2.AppendFormat( "{0}|{1}\r\n" , bundleName , e ); } );
		}
		//File.WriteAllText( "E:\\system.txt" , sb2.ToString() );


		List<AssetBundleBuild> ret = new List<AssetBundleBuild>();
		//StringBuilder sb3 = new StringBuilder();
		foreach( KeyValuePair<string , List<string>> tmp in assetInfo )
		{
			string[] oldRes = AssetDatabase.GetAssetPathsFromAssetBundle( tmp.Key );
			tmp.Value.AddRange( oldRes );
			tmp.Value.Sort( ( l , r ) => string.Compare( l.ToLower() , r.ToLower() ) );

			//tmp.Value.ForEach( e => { sb3.AppendFormat( "{0}|{1}\r\n" , tmp.Key , e ); } );

			ret.Add( new AssetBundleBuild { assetBundleName = tmp.Key , assetNames = tmp.Value.ToArray() } );
		}
		//File.WriteAllText( "E:\\newGen.txt" , sb3.ToS//tring() );

		return ret;
	}



	/// <summary>
	/// 
	/// </summary>
	/// <param name="fileAssetBundlePackTag"></param>
	/// <param name="removeFilePackTag"></param>
	public static void SetUIAssetBundleName( Dictionary<string , string> fileAssetBundlePackTag , HashSet<string> removeFilePackTag )
	{
		int length = Application.dataPath.Length - "Assets".Length;

		//Texture
		foreach( string uiTextureDir in tagUITextureActivityDir )
		{
			string fullTextureDir = Path.Combine( Application.dataPath , uiTextureDir );
			if( !Directory.Exists( fullTextureDir ) )
			{
				continue;
			}

			foreach( string textureDir in Directory.GetDirectories( fullTextureDir , "*" , SearchOption.TopDirectoryOnly ) )
			{
				// texture下一级的目录名就是资源的包名
				string dirName = Path.GetFileName( textureDir );
				string packName = dirName;

				// 排除不需要设置packing Tag的图片资源( mat文件依赖的图片资源 )

				foreach( string path in Directory.GetFiles( textureDir , "*.mat" , SearchOption.AllDirectories ) )
				{
					string tempPath = path.Remove( 0 , length ).Replace( "\\" , "/" );

					foreach( string item in AssetDatabase.GetDependencies( tempPath ) )
					{
						if( item.EndsWith( ".mat" ) )
						{
							continue;
						}
						string depImagePath = item.Replace( "\\" , "/" );

						if( !removeFilePackTag.Contains( depImagePath ) )
						{
							removeFilePackTag.Add( depImagePath );
						}
					}
				}

				foreach( string path in Directory.GetFiles( textureDir , "*.*" , SearchOption.TopDirectoryOnly ).Where( EditorUtil.isTextureFile ) )
				{
					string tempPath = path.Remove( 0 , length ).Replace( "\\" , "/" );

					if( removeFilePackTag.Contains( tempPath ) )
					{
						continue;
					}

					fileAssetBundlePackTag.Add( tempPath , packName );
				}

				foreach( string subTextureDir in Directory.GetDirectories( textureDir , "*" , SearchOption.TopDirectoryOnly ) )
				{
					string subTagName = packName + "_" + Path.GetFileName( subTextureDir );
					foreach( string path in Directory.GetFiles( subTextureDir , "*.*" , SearchOption.AllDirectories ).Where( EditorUtil.isTextureFile ) )
					{
						string tempPath = path.Remove( 0 , length ).Replace( "\\" , "/" );
						if( removeFilePackTag.Contains( tempPath ) )
						{
							continue;
						}

						fileAssetBundlePackTag.Add( tempPath , subTagName );
					}
				}
			}
		}


		//Effect Texture
		foreach( string prefabRootDir in tagEffectTextureDir )
		{
			string fullPrefabDir = Path.Combine( Application.dataPath , prefabRootDir );
			if( !Directory.Exists( fullPrefabDir ) )
			{
				continue;
			}
			string[] effectDirs = Directory.GetDirectories( fullPrefabDir , "*" , SearchOption.TopDirectoryOnly );

			foreach( string dir in effectDirs )
			{
				string packName = Path.GetFileName( dir );
				foreach( string textureFile in Directory.GetFiles( dir , "*.*" , SearchOption.AllDirectories ).Where( EditorUtil.isTextureFile ) )
				{
					string tempPath = textureFile.Remove( 0 , length ).Replace( "\\" , "/" );

					fileAssetBundlePackTag.Add( tempPath , packName );
				}
			}

			foreach( string textureFile in Directory.GetFiles( fullPrefabDir , "*" , SearchOption.TopDirectoryOnly ).Where( EditorUtil.isTextureFile ) )
			{
				string tempPath = textureFile.Remove( 0 , length ).Replace( "\\" , "/" );
				fileAssetBundlePackTag.Add( tempPath , "" );
			}
		}

		// other controller
		foreach( string prefabDir in tagOtherDir )
		{
			string fullPrefabDir = Path.Combine( Application.dataPath , prefabDir );
			if( !Directory.Exists( fullPrefabDir ) )
			{
				continue;
			}
			List<string> picPath = new List<string>();
			picPath.AddRange( Directory.GetFiles( fullPrefabDir , "*.controller" , SearchOption.AllDirectories ) );
			foreach( string prefab in picPath )
			{
				string tempPath = prefab.Remove( 0 , length ).Replace( "\\" , "/" );
				string prefabFileName = GetFileNameByTypePath( tempPath , "controller" ).ToLower();
				int index = prefabFileName.IndexOf( '_' );
				string name = index < 0 ? prefabFileName : prefabFileName.Substring( 0 , index );
				//fileAssetBundleName.Add( tempPath , "ui/" + name.ToLower() + ".unity3d" );
			}
		}

	}



	/// <summary>
	/// 将美术多语言资源,根据Art下对应资源的路径放到相应的目标目录.
	/// 如果targetPath和artPath相同,那么将对art目录下的文件执行替换操作。
	/// </summary>
	/// <param name="resPaths">美术资源路径</param>
	/// <param name="artPath">Art</param>
	/// <param name="targetPath">目标UI(如Art_kr),可与artPath相同</param>
	/// <param name="subDirs">artPath下面的子路径,如 UI Effect</param>
	private static void autoMoveLanguageResoureToArt( string[] resPaths , string artPath , string targetPath , string[] subDirs )
	{
		Dictionary<string , Tuple<string , string>> resDict = getNewRes( resPaths );

		HashSet<string> replaceRes = new HashSet<string>( resDict.Keys );


		bool samePath = EditorUtil.IsSamePath( artPath , targetPath );

		int length = Application.dataPath.Length - "Assets".Length;
		StringBuilder sb = new StringBuilder();
		StringBuilder samePic = new StringBuilder();

		// 遍历Art 目录下的资源
		foreach( string subDir in subDirs )
		{
			string path = Path.Combine( artPath , subDir );
			foreach( string fullPath in System.IO.Directory.GetFiles( path , "*.*" , System.IO.SearchOption.AllDirectories ) )
			{
				if( fullPath.EndsWith( ".png" ) || fullPath.EndsWith( ".jpg" ) || fullPath.EndsWith( ".mp3" ) )
				{
				}
				else
				{
					continue;
				}

				string resName = System.IO.Path.GetFileName( fullPath );
				Tuple<string , string> resFullPath;
				if( !resDict.TryGetValue( resName , out resFullPath ) )
				{
					continue;
				}

				replaceRes.Remove( resName );

				string tmp = fullPath.Substring( artPath.Length , fullPath.Length - artPath.Length - resName.Length );
				string newFullPathDir = targetPath + tmp;
				if( !System.IO.Directory.Exists( newFullPathDir ) )
				{
					System.IO.Directory.CreateDirectory( newFullPathDir );
				}

				string newFullPath = System.IO.Path.Combine( newFullPathDir , samePath ? resName : resFullPath.Item1 );
				if( EditorUtil.CompareFile( resFullPath.Item2 , newFullPath ) )
				{
					samePic.AppendFormat( "{0} -> {1}\n" , resFullPath.Item2 , newFullPath );

					TextureImporter originImporter = AssetImporter.GetAtPath( fullPath.Remove( 0 , length ).Replace( "\\" , "/" ) ) as TextureImporter;
					TextureImporter newImporter = AssetImporter.GetAtPath( newFullPath.Remove( 0 , length ).Replace( "\\" , "/" ) ) as TextureImporter;
					if( ( null != newImporter ) && ( null != originImporter ) && ( newImporter.maxTextureSize != originImporter.maxTextureSize ) )
					{
						newImporter.maxTextureSize = originImporter.maxTextureSize;
						newImporter.SaveAndReimport();
					}
					continue;
				}

				if( File.Exists( newFullPath ) )
				{
					File.Delete( newFullPath );
				}
				System.IO.File.Copy( resFullPath.Item2 , newFullPath , true );

				AssetImporter importer = AssetImporter.GetAtPath( newFullPath.Remove( 0 , length ).Replace( "\\" , "/" ) );
				if( null != importer )
				{
					importer.SaveAndReimport();
				}

				sb.AppendFormat( "{0} -> {1}\n" , resFullPath.Item2 , newFullPath );
			}
		}

		// replaceRes
		if( replaceRes.Count > 0 )
		{
			string changeError = "无对应资源:\n" + string.Join( "\n" , replaceRes.ToArray() );
			EditorUtil.SplitLog( Debug.LogWarning , changeError );
		}

		if( samePic.Length > 0 )
		{
			samePic.Insert( 0 , "相同资源:\n" );
			EditorUtil.SplitLog( Debug.Log , samePic.ToString() );
		}
		if( sb.Length > 0 )
		{
			sb.Insert( 0 , "执行操作:\n" );
			EditorUtil.SplitLog( Debug.LogWarning , sb.ToString() );
		}

		Debug.LogFormat( "完成 : {0} -> {1}" , string.Join( "," , resPaths ) , string.Join( "," , subDirs ) );
	}

	/// <summary>
	/// 得到新的资源
	/// </summary>
	/// <param name="resPaths"></param>
	/// <returns></returns>
	private static Dictionary<string , Tuple<string , string>> getNewRes( string[] resPaths )
	{
		Regex regexObj = new Regex( @"([\w ]+)_(?:\w{2,4})(\.\w+)" , RegexOptions.Singleline );

		// 策划提供的资源
		StringBuilder newResourceList = new StringBuilder();
		Dictionary<string , Tuple<string , string>> resDict = new Dictionary<string , Tuple<string , string>>();
		foreach( string resPath in resPaths )
		{
			foreach( string fullname in System.IO.Directory.GetFiles( resPath , "*.*" , System.IO.SearchOption.AllDirectories ) )
			{
				if( fullname.EndsWith( ".png" ) || fullname.EndsWith( ".jpg" ) || fullname.EndsWith( ".mp3" ) )
				{
				}
				else
				{
					continue;
				}

				string filename = System.IO.Path.GetFileName( fullname );

				string resName;
				try
				{
					Match m = regexObj.Match( filename );
					resName = m.Groups[1].Value + m.Groups[2].Value;
				}
				catch( ArgumentException )
				{
					//changeError.AppendLine( filename);
					continue;
				}

				if( resDict.ContainsKey( resName ) )
				{
					newResourceList.AppendLine( fullname );
				}
				else
				{
					resDict.Add( resName , new Tuple<string , string>( filename , fullname ) );
				}
			}
		}

		if( newResourceList.Length > 0 )
		{
			newResourceList.Insert( 0 , "未分类名称重复:\n" );
			EditorUtil.SplitLog( Debug.LogWarning , newResourceList.ToString() );
		}

		return resDict;
	}


}

