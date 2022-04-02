#if UNITY_IOS

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public partial class CustomExportProject
{

	[MenuItem( "Build/TestXCodeProject" , false , 1000 )]

	public static void TestXCodeProject()
	{
		//ChangeXcodePlist(BuildTarget.iOS,@"F:\WorkDir\GTA\GTA_iOS\cmg1.0.12-6578-20210201-1054-23-DEBUG");
	}

	[PostProcessBuild( 0 )]
	public static void ChangeXcodePlist( BuildTarget buildTarget , string pathToBuiltProject )
	{
		if( buildTarget != BuildTarget.iOS )
		{
			return;
		}

		{
			// plist 设置
			string plistPath = Path.Combine( pathToBuiltProject , "Info.plist" );
			PlistDocument plist = new PlistDocument();
			plist.ReadFromString( File.ReadAllText( plistPath ) );

			// Get root
			PlistElementDict rootDict = plist.root;
			//rootDict.SetString("UIRequiredDeviceCapabilities" , "arm64");
			PlistElementArray deviceCapabilityArray = rootDict.CreateArray( "UIRequiredDeviceCapabilities" );
			deviceCapabilityArray.AddString( "arm64" );

			rootDict.SetString( "NSPhotoLibraryUsageDescription" , "We need to access photos library to allow users manually pick images meant to be sent as attachment for help and support reasons." );


			{//
				PlistElement schemes = rootDict["LSApplicationQueriesSchemes"];
				PlistElementArray schemesArray = null;
				if( null != schemes )
				{
					schemesArray = schemes.AsArray();
				}
				else
				{
					schemesArray = rootDict.CreateArray( "LSApplicationQueriesSchemes" );
				}
				foreach( string vaue in appQueriesSchemes )
				{
					schemesArray.AddString( vaue );
				}
			}
			{

				PlistElement schemes = rootDict["NSAppTransportSecurity"];
				PlistElementDict schemesDict = null;

				if( null != schemes )
				{
					schemesDict = schemes.AsDict();
				}
				else
				{
					schemesDict = rootDict.CreateDict( "NSAppTransportSecurity" );
				}
				schemesDict.SetBoolean( "NSAllowsLocalNetworking" , true );
			}
			// Write to file


			rootDict.values.Remove( "UIApplicationExitsOnSuspend" );

			plist.WriteToFile( plistPath );
		}


		{
			// 工程设置
			string projPath = Path.Combine( pathToBuiltProject , "Unity-iPhone.xcodeproj/project.pbxproj" );

			PBXProject pbxProj = new PBXProject();
			pbxProj.ReadFromFile( projPath );

			string targetGuid = pbxProj.TargetGuidByName( "Unity-iPhone" );
			pbxProj.SetBuildProperty( targetGuid , "ONLY_ACTIVE_ARCH" , "YES" );
			pbxProj.SetBuildProperty( targetGuid , "ARCHS" , "arm64" );

			pbxProj.SetBuildProperty( targetGuid , "ENABLE_BITCODE" , "NO" );
			pbxProj.SetBuildProperty( targetGuid , "PRODUCT_BUNDLE_IDENTIFIER" , "" );

			{
				string searchPath = pbxProj.GetBuildPropertyForAnyConfig( targetGuid , "LD_RUNPATH_SEARCH_PATHS" );
				if( !searchPath.Contains( "swift" ) )
				{
					pbxProj.SetBuildProperty( targetGuid , "LD_RUNPATH_SEARCH_PATHS" , "/usr/lib/swift " + searchPath );
				}
			}

			fixHelpShift( pbxProj , targetGuid );

			if( pathToBuiltProject.Contains( "DEBUG" ) )
			{
				pbxProj.SetBuildProperty( targetGuid , "CODE_SIGN_IDENTITY" , RemoteBuildiOS.DevSign );
				pbxProj.SetBuildProperty( targetGuid , "PROVISIONING_PROFILE" , RemoteBuildiOS.DevProvisioningProfileID );
				pbxProj.SetBuildProperty( targetGuid , "DEVELOPMENT_TEAM" , RemoteBuildiOS.DeviOSTeamID );
			}

			AddLibToProject( pbxProj , targetGuid , "libsqlite3.tbd" );
			AddLibToProject( pbxProj , targetGuid , "libz.tbd" );
			AddLibToProject( pbxProj , targetGuid , "libicucore.tbd" );

			//if( RemoteBuildiOS.EnableSignWithApple )
			//{
			//	AddLibToProject( pbxProj , targetGuid , "AuthenticationServices.framework" );
			//}
			// pbxProj.UpdateBuildProperty(targetGuid , "OTHER_CFLAGS", new string[] { "-D_7ZIP_ST" } , new string[] { });

			// swsdk appsflyer 需要
			pbxProj.AddFrameworkToProject( targetGuid , "AdServices.framework" , true );

			pbxProj.RemoveFrameworkFromProject( targetGuid , "CoreLocation.framework" );
			pbxProj.WriteToFile( projPath );
		}
	}


	//添加lib方法
	static void AddLibToProject( PBXProject inst , string targetGuid , string lib )
	{
		string fileGuid = inst.AddFile( "usr/lib/" + lib , "Frameworks/" + lib , PBXSourceTree.Sdk );
		inst.AddFileToBuild( targetGuid , fileGuid );
	}

	static void fixHelpShift( PBXProject pbxProj , string targetGuid )
	{
		// helpshift 7.7.1 
		// 参考 https://developers.helpshift.com/ios/getting-started/#cocoapods
		pbxProj.SetBuildProperty( targetGuid , "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES" , "YES" );
		pbxProj.SetBuildProperty( targetGuid , "LD_RUNPATH_SEARCH_PATHS" , "/usr/lib/swift $(inherited) @executable_path/Frameworks" );

		pbxProj.UpdateBuildProperty( targetGuid , "LIBRARY_SEARCH_PATHS" , new string[]{
			"$(TOOLCHAIN_DIR)/usr/lib/swift-5.0/$(PLATFORM_NAME)"
			} , new string[] { } );
			
		// https://developers.helpshift.com/unity/troubleshooting-ios/
		// error : Building for iOS, but the embedded framework ‘Helpshift.framework’ was built for iOS + iOS Simulator.
		pbxProj.SetBuildProperty( targetGuid , "VALIDATE_WORKSPACE" , "YES" );
	}


	// 修改xcode 代码
	// iPhoneX 边界操作优化
	//
	[PostProcessBuild( 2 )]
	public static void ChangeXcode_UnityViewControllerBaseiOS_mm( BuildTarget buildTarget , string pathToBuiltProject )
	{
		if( buildTarget != BuildTarget.iOS )
		{
			return;
		}

		string filePath = Path.Combine( pathToBuiltProject , unityViewControllerBaseFileName );
		string source = System.IO.File.ReadAllText( filePath );
		string replaceSourceContent1 = @"
- (UIRectEdge)preferredScreenEdgesDeferringSystemGestures
{
	return UIRectEdgeAll;
}
".Replace( "\r\n" , "\n" ).Replace( "    " , "\t" );
		try
		{
			source = Regex.Replace( source , @"-[ \t]+\(UIRectEdge\)preferredScreenEdgesDeferringSystemGestures[\r\n]*{[^}]*}" , "/*\n$0\n*/" + replaceSourceContent1 , RegexOptions.Multiline );
		}
		catch( ArgumentException )
		{
		}

		string replaceSourceContent = @"
- (void)viewDidLoad
{
	[super viewDidLoad];

	if (@available(iOS 11.0, *))
	{
		[self setNeedsUpdateOfScreenEdgesDeferringSystemGestures];
	}
 }
".Replace( "\r\n" , "\n" ).Replace( "    " , "\t" );
		try
		{
			source = Regex.Replace( source , @"-[ \t]+\(BOOL\)prefersHomeIndicatorAutoHidden[\r\n]*{[^}]*}" , "/*\n$0\n*/" + replaceSourceContent , RegexOptions.Multiline );
		}
		catch( ArgumentException )
		{
		}
		System.IO.File.WriteAllText( filePath , source );
	}


	[PostProcessBuild( 3 )]
	public static void ChangeXcode_UnityAppController_mm( BuildTarget buildTarget , string pathToBuiltProject )
	{
		if( buildTarget != BuildTarget.iOS )
		{
			return;
		}

		string filePath = Path.Combine( pathToBuiltProject , "Classes/UnityAppController.mm" );

		string source = System.IO.File.ReadAllText( filePath );
		try
		{
			source = source.Insert( 0 , "extern \"C\" void applicationOpenURL(const char* url );\n" );

			string regexText = "AppController_SendNotificationWithArg(kUnityOnOpenURL, notifData);";

			//extern "C" void applicationOpenURL(const char* url )
			int index = source.IndexOf( regexText );
			if( index >= 0 )
			{
				source = source.Insert( index + regexText.Length , "applicationOpenURL([[url absoluteString] UTF8String]);" );
			}

			System.IO.File.WriteAllText( filePath , source );
		}
		catch( ArgumentException )
		{
			// Syntax error in the regular expression
		}


	}




	/// <summary>
	/// 删除 iOS 工程中的 manifest 文件 , 
	/// </summary>
	/// <param name="buildTarget"></param>
	/// <param name="pathToBuiltProject"></param>
	[PostProcessBuild( 4 )]
	public static void DeleteManifest( BuildTarget buildTarget , string pathToBuiltProject )
	{
		if( buildTarget != BuildTarget.iOS )
		{
			return;
		}

		string path = Path.Combine( pathToBuiltProject , @"Data\Raw" );

		File.Delete( Path.Combine( path , "StreamingAssets" ) );

		foreach( var file in Directory.GetFiles( path , "*.manifest" , SearchOption.AllDirectories ) )
		{
			File.Delete( file );
		};
	}



	// iOS 原生多语言相关
	[PostProcessBuild( 5 )]
	public static void NativeMultiLanguage( BuildTarget buildTarget , string pathToBuiltProject )
	{
		if( buildTarget != BuildTarget.iOS )
		{
			return;
		}
		// 工程设置
		string projPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";

		PBXProject pbxProj = new PBXProject();
		pbxProj.ReadFromFile( projPath );

		string targetGuid = pbxProj.TargetGuidByName( "Unity-iPhone" );

		// iOS原生多语言文件
		string[] allLanguageDirectory = Directory.GetDirectories( System.IO.Path.Combine( EditorUtil.GetProjectDir() , "OtherProjectFile/iOS" ) , "*.lproj" , SearchOption.TopDirectoryOnly );
		foreach( string lang in allLanguageDirectory )
		{
			string langDir = Path.GetFileName( lang );
			var fileGuid = pbxProj.AddFolderReference( langDir , langDir , PBXSourceTree.Source );
			pbxProj.AddFileToBuild( targetGuid , fileGuid );
		}

		pbxProj.WriteToFile( projPath );

		//pathToBuiltProject
		string srcPath = System.IO.Path.Combine( EditorUtil.GetProjectDir() , "OtherProjectFile/iOS" );
		EditorUtil.CopyDir( srcPath , pathToBuiltProject , "*" , delegate ( string file )
			 {
				 string destFileName = file.Replace( srcPath , pathToBuiltProject );
				 File.Copy( file , destFileName , true );
			 }
		);
	}



	const string pod_post_install = @"
post_install do |installer|
  installer.pods_project.targets.each do |target|
    target.build_configurations.each do |config|
      config.build_settings['EXPANDED_CODE_SIGN_IDENTITY'] = """"
      config.build_settings['CODE_SIGNING_REQUIRED'] = ""NO""
      config.build_settings['CODE_SIGNING_ALLOWED'] = ""NO""
    end
  end
end
";

	[PostProcessBuild( 998 )]
	//must be between 40 and 50 to ensure that it's not overriden by Podfile generation (40) and that it's added before "pod install" (50)
	public static void PostProcessBuild_iOS( BuildTarget target , string pathToBuiltProject )
	{
		if( target != BuildTarget.iOS )
		{
			return;
		}

		string podFilePath = Path.Combine( pathToBuiltProject , "Podfile" );
		string podFile = File.ReadAllText( podFilePath );

		if( podFile.Contains( "post_install" ) )
		{
			return;
		}

		if( podFile.Contains( "use_frameworks!" ) )
		{
			podFile = podFile.Replace( "use_frameworks!" , "use_frameworks!" + pod_post_install );
		}
		else
		{
			podFile += "\r\nuse_frameworks!" + pod_post_install;
		}

		File.WriteAllText( podFilePath , podFile );
	}



}

#endif