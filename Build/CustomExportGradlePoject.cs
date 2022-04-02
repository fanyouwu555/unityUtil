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

public class CustomExportGradlePoject
{

	[MenuItem( "Build/TestExportGradle" , false , 0 )]
	public static void TestCode()
	{
		// RunRemoteAndroidBuild( BuildTarget.Android , @"F:\WorkDir\CMX\CMX_Android\ft1.0.1-1533-20220118-1814-live" );
	}







	[PostProcessBuild( 1000 )]
	public static void RunRemoteAndroidBuild( BuildTarget buildTarget , string pathToBuiltProject )
	{
		if( buildTarget != BuildTarget.Android )
		{
			return;
		}

		string batFileName = "remote_android_build.bat";
		bool aab2Apk = false;
		if( pathToBuiltProject.EndsWith( "-live" ) )
		{
			if( Application.isBatchMode )
			{
				batFileName = "remote_android_build_full_aab.bat";
			}
			else
			{
				//只编译 aab文件
				batFileName = "remote_android_build_aab.bat";
				aab2Apk = true;
			}
		}

		string projectDir = EditorUtil.GetProjectDir();
		string apkFileName = pathToBuiltProject.Substring( projectDir.Length );

		System.Diagnostics.Process p = new System.Diagnostics.Process();
		p.StartInfo.WorkingDirectory = projectDir;
		p.StartInfo.FileName = Path.Combine( projectDir , batFileName );
		p.StartInfo.Arguments = string.Format( "{0} \"{1}\"" , apkFileName , ProjectBuild.productName );
		p.Start();
		p.WaitForExit();

		if( aab2Apk )
		{
			ProjectBuild.initAndroidKeyStore();
		//	AabBuilderHelper.ReBuildAabFile( pathToBuiltProject + ".aab" );
		}
	}





	private static void fixRepositories( ref string content )
	{
		if( content.Contains( "https://jitpack.io" ) )
		{
			return;
		}

		content = content.Replace( "apply plugin: 'android-library'" , @"apply plugin: 'android-library'

repositories {
	google()
	jcenter()
	mavenCentral()
	maven {
		url 'https://maven.google.com/'
		name 'Google'
	}
	maven {
		url 'https://jitpack.io'
	}
}

" );

	}

	private static void fixRepositories_buildscript( ref string content )
	{
		try
		{
			content = Regex.Replace( content , @"buildscript[ \t\r\n]+{[ \t\r\n]+repositories[ \t\r\n]+{[ \t\r\n]+((mavenCentral|google)\(\)[ \t\r\n]+)+}" , @"buildscript {
	repositories {
		mavenCentral()
		google()
		jcenter()
	}" , RegexOptions.Multiline );
		}
		catch( ArgumentException )
		{
		}
	}

	/// <summary>
	/// 移除 AndroidManifest.xml 中的sdk版本信息
	/// </summary>
	/// <param name="androidManifestfileName"></param>
	private static void removeSdkVersion( string androidManifestfileName )
	{
		if( !File.Exists( androidManifestfileName ) )
		{
			return;
		}

		string srcManifest = File.ReadAllText( androidManifestfileName );

		try
		{
			srcManifest = Regex.Replace( srcManifest , @"<uses-sdk[ \t]+android:(minSdkVersion|targetSdkVersion)[^>]+>" , "" , RegexOptions.Multiline );
			File.WriteAllText( androidManifestfileName , srcManifest );

		}
		catch( ArgumentException )
		{
		}

	}


	/// <summary>
	/// 增加 minSdkVersion
	/// </summary>
	/// <param name="content"></param>
	private static void fixMinSdkVersion( ref string content )
	{
		if( !content.Contains( "minSdkVersion" ) )
		{
			try
			{
				string targetVersion = Regex.Match( content , @"targetSdkVersion (\d+)" , RegexOptions.Multiline ).Groups[1].Value;
				content = Regex.Replace( content , @"targetSdkVersion \d+" , string.Format( @"minSdkVersion {0}
		targetSdkVersion {1}" , (int)PlayerSettings.Android.minSdkVersion , targetVersion ) , RegexOptions.Multiline );
			}
			catch( ArgumentException )
			{
			}
		}
		else
		{
			content = Regex.Replace( content , @"minSdkVersion \d+" , "minSdkVersion " + (int)PlayerSettings.Android.minSdkVersion );
		}
	}


	/// <summary>
	/// 设置 targetSdkVersion 的值为 ProjectBuild.androidTargetSdkVersion
	/// </summary>
	/// <param name="content"></param>
	private static void fixTargetSdkVersion( ref string content , int targetSdkVersion = 28 )
	{
		try
		{
			content = Regex.Replace( content , @"targetSdkVersion \d+" , "targetSdkVersion " + ProjectBuild.androidTargetSdkVersion );
		}
		catch( ArgumentException )
		{
		}
	}


	/// <summary>
	/// 修改 gradle 中 targetSdkVersion minSdkVersion 相关的版本号信息
	/// </summary>
	/// <param name="content"></param>
	private static void fixSdkVersion( ref string content )
	{
		fixMinSdkVersion( ref content );
		fixTargetSdkVersion( ref content );
	}




	private static void fixGradleProject( string pathToBuiltProject )
	{
		{
			string path = Path.Combine( pathToBuiltProject , @"fcm\build.gradle" );
			if( File.Exists( path ) )
			{
				string gradleContent = File.ReadAllText( path );
				fixSdkVersion( ref gradleContent );
				fixRepositories( ref gradleContent );
				fixRepositories_buildscript( ref gradleContent );
				File.WriteAllText( path , gradleContent );
			}
		}

		{
			string path = Path.Combine( pathToBuiltProject , @"GoogleMobileAdsAppLovinMediation\build.gradle" );
			if( File.Exists( path ) )
			{
				string gradleContent = File.ReadAllText( path );
				fixSdkVersion( ref gradleContent );
				fixRepositories( ref gradleContent );
				fixRepositories_buildscript( ref gradleContent );
				File.WriteAllText( path , gradleContent );
			}
		}


		{
			string path = Path.Combine( pathToBuiltProject , @"unity-android-resources\build.gradle" );
			if( File.Exists( path ) )
			{
				string gradleContent = File.ReadAllText( path );
				fixSdkVersion( ref gradleContent );
				fixRepositories( ref gradleContent );
				fixRepositories_buildscript( ref gradleContent );
				File.WriteAllText( path , gradleContent );
			}
		}


		{
			string path = Path.Combine( pathToBuiltProject , @"IAB_lib\build.gradle" );
			if( File.Exists( path ) )
			{
				string gradleContent = File.ReadAllText( path );
				fixRepositories( ref gradleContent );
				fixRepositories_buildscript( ref gradleContent );
				File.WriteAllText( path , gradleContent );
			}
		}


		// 修改 GoogleMobileAdsMediationTestSuite
		{
			string path = Path.Combine( pathToBuiltProject , @"GoogleMobileAdsMediationTestSuite\build.gradle" );
			if( File.Exists( path ) )
			{
				string gradleContent = File.ReadAllText( path );
				fixGradleBuild( ref gradleContent );
				fixSdkVersion( ref gradleContent );
				fixRepositories( ref gradleContent );

				File.WriteAllText( path , gradleContent );
			}
		}

		// 修改 GoogleMobileAdsPlugin
		{
			string path = Path.Combine( pathToBuiltProject , @"GoogleMobileAdsPlugin\build.gradle" );
			if( File.Exists( path ) )
			{
				string gradleContent = File.ReadAllText( path );
				fixGradleBuild( ref gradleContent );
				fixSdkVersion( ref gradleContent );
				fixRepositories( ref gradleContent );

				File.WriteAllText( path , gradleContent );
			}
		}

		{
			string path = Path.Combine( pathToBuiltProject , @"build.gradle" );
			string gradleContent = File.ReadAllText( path );
			fixSdkVersion( ref gradleContent );
			File.WriteAllText( path , gradleContent );
		}

	}




	[PostProcessBuild( 1 )]
	public static void CopyFileToGradleProject( BuildTarget buildTarget , string pathToBuiltProject )
	{
		if( buildTarget != BuildTarget.Android )
		{
			return;
		}

		pathToBuiltProject = Path.Combine( pathToBuiltProject , ProjectBuild.productName );

		removeSdkVersion( Path.Combine( pathToBuiltProject , @"src\main\AndroidManifest.xml" ) );
		removeSdkVersion( Path.Combine( pathToBuiltProject , @"fcm\AndroidManifest.xml" ) );
		removeSdkVersion( Path.Combine( pathToBuiltProject , @"IAB_lib\AndroidManifest.xml" ) );
		removeSdkVersion( Path.Combine( pathToBuiltProject , @"unity-android-resources\AndroidManifest.xml" ) );

		fixGradleProject( pathToBuiltProject );

		{
			string build_gradle = Path.Combine( pathToBuiltProject , @"gradle.properties" );
			File.WriteAllText( build_gradle , @"android.useDeprecatedNdk=true
android.useAndroidX=true
android.enableJetifier=true" );

		}

		string srcPath = System.IO.Path.Combine( EditorUtil.GetProjectDir() , "OtherProjectFile/Android" );
		EditorUtil.CopyDir( srcPath , pathToBuiltProject , "*" , delegate ( string file )
		 {
			 string destFileName = file.Replace( srcPath , pathToBuiltProject );
			 File.Copy( file , destFileName , true );
		 }
		);

	}



	private static void fixGradleBuild( ref string gradleContent )
	{
		if( gradleContent.Contains( "https://maven.google.com/" ) )
		{
			return;
		}

		// 源
		gradleContent = gradleContent.Replace( "mavenCentral()" , @"google()
		jcenter()
		mavenCentral()
		maven {
				url 'https://maven.google.com/'
			name 'Google'
		}
" );

		// gradle 3.4.0
		try
		{
			gradleContent = Regex.Replace( gradleContent , @"classpath 'com\.android\.tools\.build:gradle:\d\.\d\.\d'" , @"classpath 'com.android.tools.build:gradle:3.4.0'" , RegexOptions.Multiline );
		}
		catch( ArgumentException )
		{
		}

	}


}

#endif