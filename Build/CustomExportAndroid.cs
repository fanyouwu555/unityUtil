#if UNITY_ANDROID

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public partial class CustomExportAndroid
{

	/// <summary>
	/// 删除 src\main\res\values\strings.xml 中 app_name 值
	/// </summary>
	/// <param name="buildTarget"></param>
	/// <param name="pathToBuiltProject"></param>
	[PostProcessBuild( 10 )]
	public static void RunRemoteAndroidBuild( BuildTarget buildTarget , string pathToBuiltProject )
	{
		if( buildTarget != BuildTarget.Android )
		{
			return;
		}

		pathToBuiltProject = Path.Combine( pathToBuiltProject , ProjectBuild.productName );
		string stringsPath = Path.Combine( pathToBuiltProject , @"src\main\res\values\strings.xml" );
		if( File.Exists( stringsPath ) )
		{
			string content = File.ReadAllText( stringsPath );
			string value = "<string name=\"app_name\">" + ProjectBuild.productName + "</string>";
			content = content.Replace( value , "" );
			// File.WriteAllText( stringsPath , content );
		}


	}


	/// <summary>
	/// 将 implementation project(':unity-android-resources') 依赖移动到最上
	/// </summary>
	/// <param name="buildTarget"></param>
	/// <param name="pathToBuiltProject"></param>
	[PostProcessBuild( 1 )]
	public static void CopyFileToGradleProject( BuildTarget buildTarget , string pathToBuiltProject )
	{
		if( buildTarget != BuildTarget.Android )
		{
			return;
		}

		pathToBuiltProject = Path.Combine( pathToBuiltProject , ProjectBuild.productName );

		string path = Path.Combine( pathToBuiltProject , @"build.gradle" );
		string gradleContent = File.ReadAllText( path );

		try
		{
			gradleContent = Regex.Replace( gradleContent , @"dependencies\s+{([^}]*?)implementation project\(':unity-android-resources'\)(.*?)}" , @"dependencies {
	implementation project(':unity-android-resources')
$1
$2}" , RegexOptions.Singleline | RegexOptions.Multiline );
			File.WriteAllText( path , gradleContent );
		}
		catch( ArgumentException )
		{
			// Syntax error in the regular expression
		}
	}




	/// <summary>
	/// 删除android资源中的 Manifest 文件 , 
	/// </summary>
	/// <param name="buildTarget"></param>
	/// <param name="pathToBuiltProject"></param>
	[PostProcessBuild( 1 )]
	public static void DeleteManifest( BuildTarget buildTarget , string pathToBuiltProject )
	{
		if( buildTarget != BuildTarget.Android )
		{
			return;
		}

		pathToBuiltProject = Path.Combine( pathToBuiltProject , ProjectBuild.productName );
		string path = Path.Combine( pathToBuiltProject , @"src\main\assets" );

		File.Delete( Path.Combine( path , "StreamingAssets" ) );

		foreach( var file in Directory.GetFiles( path , "*.manifest" , SearchOption.AllDirectories ) )
		{
			File.Delete( file );
		};
	}


	public static readonly string googleServicesFileName = "google-services.json";
	public static readonly string swsdkConfigFileName = "sdkConfig.json";



	/// <summary>
	/// swsdk中相关的已经文件修改正
	/// </summary>
	/// <param name="buildTarget"></param>
	/// <param name="pathToBuiltProject"></param>
	[PostProcessBuild( 2 )]
	public static void SWSdkFix( BuildTarget buildTarget , string pathToBuiltProject )
	{
		if( buildTarget != BuildTarget.Android )
		{
			return;
		}

		string androidProjectDir = Path.Combine( pathToBuiltProject , ProjectBuild.productName );
		// 拷贝 google-services.json 到工程目录
		string googleServiceFileSrc = System.IO.Path.Combine( Application.dataPath , "Resources" , googleServicesFileName );
		if( File.Exists( googleServiceFileSrc ) )
		{
			string targetFileName = Path.Combine( androidProjectDir , googleServicesFileName );
			File.Copy( googleServiceFileSrc , targetFileName , true );
		}

		// android工程的assets路径
		string androidAssetsDir = Path.Combine( androidProjectDir , @"src\main\assets" );
		//删除 assets目录下的 google-services.json
		EditorUtil.DeleteFile( Path.Combine( androidAssetsDir , googleServicesFileName ) );

		//分包的话可能 sdkConfig.json 不存在
		string swsdkConfigFile = Path.Combine( androidAssetsDir , swsdkConfigFileName );
		if( !File.Exists( swsdkConfigFile ) )
		{
			if( !Directory.Exists( androidAssetsDir ) )
			{
				Directory.CreateDirectory( androidAssetsDir );
			}
			string srcPath = System.IO.Path.Combine( Application.dataPath , "Resources" , swsdkConfigFileName );
			File.Copy( srcPath , swsdkConfigFile , true );
		}
	}


	private static string GetAndroidVersionCode( string androidProjectRoot )
	{
		try
		{
			string buildFilePath = Path.Combine( androidProjectRoot , "build.gradle" );
			string gradleContent = File.ReadAllText( buildFilePath );
			return Regex.Match( gradleContent , @"versionCode[ \t]+(\d+)" , RegexOptions.Multiline ).Groups[1].Value;
		}
		catch( Exception )
		{
			return ProjectBuild.androidVersionCode.ToString();
		}
	}

	/// <summary>
	/// 使用android obb之后的文件修正
	/// </summary>
	/// <param name="buildTarget"></param>
	/// <param name="pathToBuiltProject"></param>
	[PostProcessBuild( 3 )]
	public static void APKExpansionFilesFix( BuildTarget buildTarget , string pathToBuiltProject )
	{
		if( buildTarget != BuildTarget.Android )
		{
			return;
		}

		string androidProjectRoot = Path.Combine( pathToBuiltProject , ProjectBuild.productName );
		string obbFileName = ProjectBuild.productName + ".main.obb";

		string obbFilePath = Path.Combine( androidProjectRoot , obbFileName );
		if( !File.Exists( obbFilePath ) )
		{
			return;
		}

		string projectDir = EditorUtil.GetProjectDir();

		// 将obb中的部分文件提取到assets目录
		// unzip -o "Call me a Gangsta.main.obb" assets\luacode.unity3d assets\version.txt assets\bin -d src\main
		{
			System.Diagnostics.Process p = new System.Diagnostics.Process();
			p.StartInfo.WorkingDirectory = androidProjectRoot;
			p.StartInfo.FileName = Path.Combine( projectDir , @"Tools\unzip.exe" );
			p.StartInfo.Arguments = string.Format( @"-o ""{0}"" assets\luacode.unity3d assets\ui\login.unity3d assets\version.txt assets\bin\* -d src\main" , obbFilePath );
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.UseShellExecute = false;
			p.Start();
			p.WaitForExit( 1000 );
			string output = p.StandardOutput.ReadToEnd();
			Debug.Log( output );
		}

		// 将obb文件复制一份
		// main.100.com.ulugame.cmlkr.ios.obb
		//Debug.Log( "pathToBuiltProject:" + androidProjectRoot );

		string androidVersionCode = GetAndroidVersionCode( androidProjectRoot );

		string newObbFileName = string.Format( "main.{0}.{1}.obb" , androidVersionCode , ProjectBuild.GetAndroidBundleIdentifier() );
		string newObbFilePath = Path.Combine( androidProjectRoot , newObbFileName );
		EditorUtil.DeleteFile( newObbFilePath );
		File.Copy( obbFilePath , newObbFilePath );

		// 修改version.txt
		if( fixObbVersion( androidProjectRoot ) )
		{
			System.Diagnostics.Process p = new System.Diagnostics.Process();
			p.StartInfo.WorkingDirectory = androidProjectRoot;
			p.StartInfo.FileName = Path.Combine( projectDir , @"Tools\zip.exe" );
			p.StartInfo.Arguments = string.Format( @"-f {0}  assets\version.txt" , newObbFileName );
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.UseShellExecute = false;
			p.Start();
			p.WaitForExit( 2000 );
			string output = p.StandardOutput.ReadToEnd();
			Debug.Log( output );
			EditorUtil.DeleteDir( Path.Combine( androidProjectRoot , "assets" ) );
		}

		// 删除obb中的 bin目录
		// zip -d 111.obb  assets\bin\*
		{
			System.Diagnostics.Process p = new System.Diagnostics.Process();
			p.StartInfo.WorkingDirectory = androidProjectRoot;
			p.StartInfo.FileName = Path.Combine( projectDir , @"Tools\zip.exe" );
			p.StartInfo.Arguments = string.Format( @"-d {0} assets\bin\* assets\{1} *.manifest assets\StreamingAssets" , newObbFileName , googleServicesFileName );
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.UseShellExecute = false;
			p.Start();
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.UseShellExecute = false;
			string output = p.StandardOutput.ReadToEnd();
			Debug.Log( output );
		}

	}


	private static bool fixObbVersion( string androidProjectRoot )
	{
		// version.txt 中的版本号降低1
		string versionFilePath = Path.Combine( androidProjectRoot , @"src\main\assets\version.txt" );
		string fileListPath = Path.Combine( androidProjectRoot , @"src\main\assets\filelist.txt" );
		if( !File.Exists( versionFilePath ) )
		{
			Debug.LogWarningFormat( "'{0}' not exist" , versionFilePath );
			return false;
		}

		string versionContent = File.ReadAllText( versionFilePath );

		int minor = 0;
		int major = 0;
		try
		{
			Match match = Regex.Match( versionContent , @"(\d+)[ \t](\d+)" , RegexOptions.Multiline );
			minor = int.Parse( match.Groups[1].Value );
			major = int.Parse( match.Groups[2].Value );
		}
		catch( ArgumentException )
		{
			File.Delete( versionFilePath );
			File.Delete( fileListPath );
			return false;
		}

		string newVersionContent = string.Format( "{0} {1}" , minor - 1 , major );
		File.WriteAllText( versionFilePath , newVersionContent );
		File.WriteAllText( fileListPath , "" );

		if( !Directory.Exists( "assets" ) )
		{
			Directory.CreateDirectory( "assets" );
		}

		string tempVersionDir = Path.Combine( androidProjectRoot , @"assets" );
		if( !Directory.Exists( tempVersionDir ) )
		{
			Directory.CreateDirectory( tempVersionDir );
		}

		string tempVersionPath = Path.Combine( tempVersionDir , @"version.txt" );
		File.WriteAllText( tempVersionPath , newVersionContent );
		return true;
	}







}

#endif