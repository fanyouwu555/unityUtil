using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.Android.AppBundle.Editor.Internal.BuildTools;
using UnityEditor;
using Google.Android.AppBundle.Editor;
using Google.Android.AppBundle.Editor.AssetPacks;
using UnityEngine;

public class AabBuilderHelper
{
	private const string AndroidPlayerFileName = "tmp.aab";

	private static string GetTempFolder()
	{
		// Create a new temp folder with each build. Some developers prefer a random path here since there may be
		// multiple builds running concurrently, e.g. on an automated build machine. See Issue #69.
		// Note: this plugin doesn't clear out old temporary build folders, so disk usage will grow over time.
		// Note: we use the 2 argument Path.Combine() to support .NET 3.
		return Path.Combine( Path.Combine( Path.GetTempPath() , "play-unity-build" ) , Path.GetRandomFileName() );
	}




	private static AppBundleBuilder CreateAppBundleBuilder( string workingDirectoryPath )
	{
		var androidSdk = new AndroidSdk();
		var javaUtils = new JavaUtils();
		var androidSdkPlatform = new AndroidSdkPlatform( androidSdk );
		var androidBuildTools = new AndroidBuildTools( androidSdk );
		var jarSigner = new JarSigner( javaUtils );
		return new AppBundleBuilder(
			new AndroidAssetPackagingTool( androidBuildTools , androidSdkPlatform ) ,
			new AndroidBuilder( androidSdkPlatform ) ,
			new BundletoolHelper( javaUtils ) ,
			jarSigner ,
			workingDirectoryPath ,
			new ZipUtils( javaUtils ) );
	}



	private static AppBundleRunner CreateAppBundleRunner()
	{
		var androidSdk = new AndroidSdk();
		var javaUtils = new JavaUtils();
		var adb = new AndroidDebugBridge( androidSdk );
		var bundletool = new BundletoolHelper( javaUtils );
		return new AppBundleRunner( adb , bundletool );
	}



	private static void UnZipApks( string apkSetFilePath )
	{
		var javaUtils = new JavaUtils();
		var zip = new ZipUtils( javaUtils );

		javaUtils.Initialize( new BuildToolLogger() );

		string tmpDir = Path.Combine( Path.GetTempPath() , Path.GetFileNameWithoutExtension( apkSetFilePath ) );
		if( !Directory.Exists( tmpDir ) )
		{
			Directory.CreateDirectory( tmpDir );
		}
		//Debug.Log( "TempDir" + tmpDir );
		zip.UnzipFile( apkSetFilePath , tmpDir );

		string newApkFileName = Path.Combine( Path.GetDirectoryName( apkSetFilePath ) , Path.GetFileNameWithoutExtension( apkSetFilePath ) + ".universal.apk" );
		File.Move( Path.Combine( tmpDir , "universal.apk" ) , newApkFileName );
		EditorUtil.DeleteDir( tmpDir );
		Directory.Delete( tmpDir );
		File.Delete( apkSetFilePath );
	}



	/// <summary>
	/// 
	/// </summary>
	/// <param name="aabFilePath"></param>
	/// <returns></returns>
	public static bool ReBuildAabFile( string aabFilePath )
	{
		string tmpFolder = GetTempFolder();

		var appBundleBuilder = CreateAppBundleBuilder(tmpFolder);
		if( !appBundleBuilder.Initialize( new BuildToolLogger() ) )
		{
			return false;
		}

		var assetPackConfig = AssetPackConfigSerializer.LoadConfig();
		assetPackConfig.SplitBaseModuleAssets = true;

		var workingDirectory = new DirectoryInfo( appBundleBuilder.WorkingDirectoryPath );
		if( workingDirectory.Exists )
		{
			workingDirectory.Delete( true );
		}
		workingDirectory.Create();

		File.Move( aabFilePath , Path.Combine(tmpFolder ,AndroidPlayerFileName ) );

		string errorMessage = appBundleBuilder.CreateBundle( aabFilePath , assetPackConfig );
		if( errorMessage != null )
		{
			// DisplayRunError("Creating apk set", errorMessage);
			return false;
		}

		var buildToolLogger = new BuildToolLogger();
		var appBundleRunner = CreateAppBundleRunner();
		if( !appBundleRunner.Initialize( buildToolLogger ) )
		{
			buildToolLogger.DisplayErrorDialog( "Failed to initialize AppBundleRunner." );
			return false;
		}

		string apkSetFilePath;
		errorMessage = appBundleRunner.ConvertAabToApkSet( aabFilePath , BundletoolBuildMode.Universal , out apkSetFilePath );
		if( errorMessage != null )
		{
			// DisplayRunError("Creating apk set", errorMessage);
			return false;
		}

		UnZipApks( apkSetFilePath );

		return true;
	}





}
