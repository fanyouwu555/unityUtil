using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;



public partial class BuildLuaCode
{
	static private readonly string luaByteCodePath = "LuaCode";

	static private readonly string luaCodeBundleName = "luaCode.unity3d";

	//architecture
	private static void CompilerLuaCode( BuildTarget architecture , string luaCodePath , string luaCodeFile , string target , string projectDir )
	{
		// Debug.Log("CompilerLuaCode : " + luaCodeFile);
		string exeFileName = null;
		switch( architecture )
		{
			case BuildTarget.Android:
				exeFileName = System.IO.Path.Combine( projectDir , "Tools/luac.exe" );
				break;
			case BuildTarget.StandaloneWindows:
				exeFileName = System.IO.Path.Combine( projectDir , "Tools/luac32.exe" );
				break;
			case BuildTarget.StandaloneWindows64:
			case BuildTarget.iOS:
				exeFileName = System.IO.Path.Combine( projectDir , "Tools/luac64.exe" );
				break;
			default:
				Debug.LogError( "!!!!!!!!!!!! Unknown Target Architecture !!!!!!!!!!!!" );
				break;
		}

		if( string.IsNullOrEmpty( exeFileName ) )
		{
			System.IO.File.Copy( luaCodeFile , target );
		}
		else
		{
			System.Diagnostics.Process p = new System.Diagnostics.Process();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.CreateNoWindow = true;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.WorkingDirectory = luaCodePath;
			p.StartInfo.FileName = exeFileName; //需要启动的程序名       
			p.StartInfo.Arguments = string.Format( "-o \"{0}\" \"{1}\"" , target , luaCodeFile );
			p.Start();
			p.WaitForExit();
		}
	}





#if UNITY_STANDALONE_WIN
	[MenuItem( "Build/LuaCodeWin64" , false , 2001 )]
#endif
	public static void BuildWin64LuaCode()
	{
		BuildLuaCodeAssetBundle( BuildTarget.StandaloneWindows64 );
	}



#if UNITY_ANDROID
	[MenuItem( "Build/LuaCodeAndroid" , false , 2002 )]
#endif
	public static void BuildAndroidLuaCode()
	{
		BuildLuaCodeAssetBundle( BuildTarget.Android );
	}



#if UNITY_IOS
	[MenuItem("Build/LuaCodeiOS" , false , 2003)]
#endif
	public static void BuildiOSLuaCode()
	{
		BuildLuaCodeAssetBundle( BuildTarget.iOS );
	}


#if UNITY_STANDALONE_WIN
	[MenuItem( "Build/快速编译 LuaCode Win64" , false , 2005 )]
#endif
	public static void BuildWin64LuaCodeOnlyCode()
	{
		BuildLuaCodeAssetBundle( BuildTarget.StandaloneWindows64 , false );
	}


#if UNITY_ANDROID
	[MenuItem( "Build/快速编译 LuaCode Android" , false , 2005 )]
#endif
	public static void BuildAndroidLuaCodeOnlyCode()
	{
		BuildLuaCodeAssetBundle( BuildTarget.Android , false );
	}

#if UNITY_IOS
	[MenuItem( "Build/快速编译 LuaCode iOS" , false , 2005 )]
#endif
	public static void BuildiOSLuaCodeOnlyCode()
	{
		BuildLuaCodeAssetBundle( BuildTarget.iOS , false );
	}



#if UNITY_ANDROID
	[MenuItem("Build/单打AssetsBundle Android", false, 2016)]
#endif
	public static void BuildAndroidAssetsBundle()
	{
		BuildOnlyAssetBundle(BuildTarget.Android);
	}

#if UNITY_IOS
	[MenuItem( "Build/单打AssetsBundle Android iOS" , false , 2016 )]
#endif
	public static void BuildiOSAssetsBundle()
	{
		BuildOnlyAssetBundle(BuildTarget.iOS);
	}




	/// <summary>
	/// 得到lua代码的路径
	/// </summary>
	/// <returns>key 从Lua目录开始的相对路径,value lua代码的绝对路径</returns>
	private static Dictionary<string , string> getAllLuaCode()
	{
		Dictionary<string , string> ret = new Dictionary<string , string>();
		int lua_length = ".lua".Length;

		for( int i = 0 ; i < luaSourceCodePaths.Length ; i++ )
		{
			string luaCodeDir = Path.Combine( Application.dataPath , luaSourceCodePaths[i] ).Replace( '/' , '\\' );
			int length = luaCodeDir.EndsWith( "\\" ) ? luaCodeDir.Length : luaCodeDir.Length + 1;

			foreach( string fullFilePath in Directory.GetFiles( luaCodeDir , "*.lua" , SearchOption.AllDirectories ) )
			{
				string tmpFilePath = fullFilePath.Replace( '/' , '\\' );
				string key = tmpFilePath.Substring( length , tmpFilePath.Length - length - lua_length );
				if( ret.ContainsKey( key ) )
				{
					ret[key] = tmpFilePath;
				}
				else
				{
					ret.Add( key , tmpFilePath );
				}
			}
		}

		return ret;
	}




	private static void CompilerAllLuaCode( BuildTarget architecture )
	{
		string projectDir = EditorUtil.GetProjectDir();
		string luaCodeByteDir = Path.Combine( Application.dataPath , luaByteCodePath ).Replace( '/' , '\\' );
		if( !Directory.Exists( luaCodeByteDir ) )
		{
			Directory.CreateDirectory( luaCodeByteDir );
		}

		Dictionary<string , string> codeFilesList = getAllLuaCode();

		// 删除 luacode中多余的bytes文件
		{
			int length = luaCodeByteDir.EndsWith( "\\" ) ? luaCodeByteDir.Length : luaCodeByteDir.Length + 1;

			List<string> allBytesFile = Directory.GetFiles( luaCodeByteDir , "*.bytes" , SearchOption.AllDirectories ).ToList();
			int bytes_length = ".bytes".Length;

			foreach( string bytesFile in allBytesFile )
			{
				string tmpBytesFile = bytesFile.Substring( length , bytesFile.Length - length - bytes_length );
				string luaFile = tmpBytesFile.Replace( '/' , '\\' );

				if( !codeFilesList.ContainsKey( luaFile ) )
				{
					File.Delete( bytesFile );
					File.Delete( bytesFile + ".meta" );
				}
			}
		}

		foreach( var item in codeFilesList )
		{
			string file = item.Value;

			string randomFileName = Path.GetTempFileName();
			string codeFile = item.Key + ".lua";
			CompilerLuaCode( architecture , file.Substring( 0 , file.Length - codeFile.Length ) , codeFile , randomFileName , projectDir );

			string targetFile = Path.Combine( luaCodeByteDir , item.Key + ".bytes" );
			{
				string tmpDir = Path.GetDirectoryName( targetFile );
				if( !Directory.Exists( tmpDir ) )
				{
					Directory.CreateDirectory( tmpDir );
				}
			}
			if( EditorUtil.CompareFile( randomFileName , targetFile ) )
			{
				continue;
			}

			File.Copy( randomFileName , targetFile , true );
			File.Delete( randomFileName );
		}
	}



	/// <summary>
	/// 1.打包资源
	/// 2.生成 PreLoadUnity3dList.lua
	/// 3.更新 Version.lua
	/// 4.将Lua目录下的代码编译输出到到LuaCode并增加扩展名.bytes
	/// 5.再次打包资源
	/// 6.压缩 UnzipStreamingAssets 文件
	/// 7.压缩 Lua代码，文件名为 md5file(StreamingAssets\luacode.unity3d) _ md5file(UnzipStreamingAssets\luacode.unity3d)
	/// </summary>
	/// <param name="isFullAsset">true打包所有资源,false只打包android资源</param>
	private static void BuildLuaCodeAssetBundle( BuildTarget architecture , bool isFullAsset = true )
	{
		if( EditorUserBuildSettings.activeBuildTarget != architecture )
		{
			return;
		}

		BuildProtoFile();

		// Assets/Lua 的svn版本号写入到 Assets/Lua/Version.lua
		int svnRevision = 0;
		foreach( var luaSourceCodePath in luaSourceCodePaths )
		{
			string tmp = BuildHelper.GetSvnRevision( Path.Combine( EditorUtil.GetProjectDir() , Path.Combine( "Assets" , luaSourceCodePath ) ) );
			int tmpValue;
			if( int.TryParse( tmp , out tmpValue ) )
			{
				if( tmpValue > svnRevision )
				{
					svnRevision = tmpValue;
				}
			}
		}

		BuildHelper.UpdateLuaVersion( string.Format( "{0}" , svnRevision ) );
		// Assets的svn版本好写入到  Assets/Resources/svnRevision.txt
		BuildHelper.UpdateSvnRevision();
		// 编译lua代码
		CompilerAllLuaCode( architecture );

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		// lua代码和的 proto文件打包为luacode
		int length = Application.dataPath.Length - "Assets".Length;
		AssetBundleBuild luaCodeBuilds = new AssetBundleBuild { assetBundleName = luaCodeBundleName };
		List<string> luaCodeRes = new List<string>();
		foreach( string file in Directory.GetFiles( Path.Combine( Application.dataPath , luaByteCodePath ) , "*.bytes" , SearchOption.AllDirectories ) )
		{
			string tempPath = file.Remove( 0 , length );
			luaCodeRes.Add( tempPath );
		}
		foreach( string file in Directory.GetFiles( Path.Combine( Application.dataPath , "proto" ) , "*.bytes" , SearchOption.AllDirectories ) )
		{
			string tempPath = file.Remove( 0 , length );
			luaCodeRes.Add( tempPath );
		}
		luaCodeRes.Sort();
		luaCodeBuilds.assetNames = luaCodeRes.ToArray();

		List<AssetBundleBuild> builds = new List<AssetBundleBuild>();
		List<AssetBundleBuild> allPrefabBuilds = null;

		//  输出 打包信息
		//using( FileStream fs = File.Create( Path.Combine("e:\\tmp_log.txt") ) )
		//{
		//	foreach( AssetBundleBuild item in builds )
		//	{
		//		foreach( var assetName in item.assetNames )
		//		{
		//			string value = string.Format( "{0} | {1}\n" , item .assetBundleName , assetName );
		//			byte[] inxfo = new UTF8Encoding( true ).GetBytes( value );
		//			fs.Write( inxfo , 0 , inxfo.Length );
		//		}
		//		byte[] info = new UTF8Encoding( true ).GetBytes( "\n\n\n" );
		//		fs.Write( info , 0 , info.Length );
		//	}
		//}

		if( isFullAsset )
		{
			allPrefabBuilds = BuildAssetBundleScript.GetAllPrefabAssetBundleBuild();
			builds.AddRange( allPrefabBuilds );
		}
		else
		{
			builds.Clear();
		}

		builds.Add( luaCodeBuilds );
		BuildAssetBundleScript.BuildAssetBundle( architecture , builds.ToArray() );

		BuildAssetBundleScript.CompressionAllFile();

		AssetDatabase.Refresh();

		ZipLuaCode();
		Debug.Log( "Build LuaCode :" + architecture );
	}


	private static void BuildOnlyAssetBundle(BuildTarget architecture)
	{
		bool isFullAsset = true;

		if (EditorUserBuildSettings.activeBuildTarget != architecture)
		{
			return;
		}

		List<AssetBundleBuild> builds = new List<AssetBundleBuild>();
		List<AssetBundleBuild> allPrefabBuilds = null;

		//  输出 打包信息
		//using( FileStream fs = File.Create( Path.Combine("e:\\tmp_log.txt") ) )
		//{
		//	foreach( AssetBundleBuild item in builds )
		//	{
		//		foreach( var assetName in item.assetNames )
		//		{
		//			string value = string.Format( "{0} | {1}\n" , item .assetBundleName , assetName );
		//			byte[] inxfo = new UTF8Encoding( true ).GetBytes( value );
		//			fs.Write( inxfo , 0 , inxfo.Length );
		//		}
		//		byte[] info = new UTF8Encoding( true ).GetBytes( "\n\n\n" );
		//		fs.Write( info , 0 , info.Length );
		//	}
		//}

		if (isFullAsset)
		{
			allPrefabBuilds = BuildAssetBundleScript.GetAllPrefabAssetBundleBuild();
			builds.AddRange(allPrefabBuilds);
		}
		else
		{
			builds.Clear();
		}

		BuildAssetBundleScript.BuildAssetBundle(architecture, builds.ToArray());

		BuildAssetBundleScript.CompressionAllFile();

		AssetDatabase.Refresh();

	//	ZipLuaCode();
		Debug.Log("Build LuaCode :" + architecture);
	}




	private static void ZipLuaCode()
	{
		string projectDir = EditorUtil.GetProjectDir();
		string zipCodeMd5 = UnityHelper.GetFileMD5( Path.Combine( Application.streamingAssetsPath , luaCodeBundleName ) );

		string zipFileName = string.Format( "{0}" , zipCodeMd5 );

		if( File.Exists( zipFileName ) )
		{
			File.Delete( zipFileName );
		}

		System.Diagnostics.Process p = new System.Diagnostics.Process();
		p.StartInfo.WorkingDirectory = projectDir;
		p.StartInfo.FileName = System.IO.Path.Combine( projectDir , "Tools/zip.exe" );

		string zipPath = "";
		foreach( var luaSourceCodePath in luaSourceCodePaths )
		{
			zipPath += string.Format( @" .\Assets\{0}\ " , luaSourceCodePath );
		}

		p.StartInfo.Arguments = string.Format( @"-5 -r {0}.zip " + zipPath + @" -xi *.meta" , zipFileName );
		p.Start();
	}




	[MenuItem( "Build/Build Proto" )]
	/// <summary>
	/// 生成proto.bytes 和 ResultInfo.lua
	/// </summary>
	public static void BuildProtoFile()
	{
		string projectDir = EditorUtil.GetProjectDir();
		string workingDirectory = Path.Combine( projectDir , "ProtoGen" );

		//lua32.exe GenResultInfo.lua
		//lua32.exe GenProtoIDMap.lua

		{
			System.Diagnostics.Process p = new System.Diagnostics.Process();
			p.StartInfo.WorkingDirectory = workingDirectory;
			p.StartInfo.FileName = Path.Combine( workingDirectory , "lua32.exe" );
			p.StartInfo.Arguments = "GenResultInfo.lua";
			p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			p.Start();
			p.WaitForExit();
		}

		{
			System.Diagnostics.Process p = new System.Diagnostics.Process();
			p.StartInfo.WorkingDirectory = workingDirectory;
			p.StartInfo.FileName = Path.Combine( workingDirectory , "lua32.exe" );
			p.StartInfo.Arguments = "GenProtoIDMap.lua";
			p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			p.Start();
			p.WaitForExit();
		}

		{

			string protoFileName = Path.Combine( Application.dataPath , "proto" , "proto.bytes" );
			System.Diagnostics.Process p = new System.Diagnostics.Process();
			p.StartInfo.WorkingDirectory = Path.Combine( workingDirectory , "proto" ); ;
			p.StartInfo.FileName = Path.Combine( workingDirectory , "protoc.exe" );
			p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			p.StartInfo.Arguments = "--descriptor_set_out=" + protoFileName + " *.proto";
			p.Start();
			p.WaitForExit();
		}
		Debug.Log( "Build Proto Success" );
		AssetDatabase.Refresh();
	}


}

