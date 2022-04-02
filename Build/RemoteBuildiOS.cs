
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;

public partial class RemoteBuildiOS
{

	internal static string DeviOSTeamID { get { return dev.TeamId; } }
	internal static string DevBundleId { get { return dev.BundleId; } }
	internal static string DevSign { get { return dev.Sign; } }
	internal static string DevProvisioningProfileID { get { return dev.UUID; } }
	public static string DevProvisioningProfileName { get { return dev.Name; } }

	public static string ReleaseiOSTeamID { get { return appstore.TeamId; } }
	internal static string ReleaseBundleId { get { return appstore.BundleId; } }
	public static string ReleaseSignId { get { return appstore.Sign; } }
	public static string AdhocProvisioningProfileId { get { return adhoc.UUID; } }
	public static string AdhocProvisioningProfileName { get { return adhoc.Name; } }
	public static string ReleaseProvisioningProfileId { get { return appstore.UUID; } }
	public static string ReleaseProvisioningProfileName { get { return appstore.Name; } }

	private static ProvisioningProfileData _dev;
	private static ProvisioningProfileData _adhoc;
	private static ProvisioningProfileData _appstore;


	private static ProvisioningProfileData dev
	{
		get
		{
			if( null == _dev )
			{
				_dev = ProvisioningProfileData.Parse( Path.Combine( EditorUtil.GetProjectDir() , devProvisionFileName ) );
			}
			return _dev;
		}
	}
	private static ProvisioningProfileData adhoc
	{
		get
		{
			if( null == _adhoc )
			{
				_adhoc = ProvisioningProfileData.Parse( Path.Combine( EditorUtil.GetProjectDir() , adHocProvisionFileName ) );
			}
			return _adhoc;
		}
	}
	private static ProvisioningProfileData appstore
	{
		get
		{
			if( null == _appstore )
			{
				_appstore = ProvisioningProfileData.Parse( Path.Combine( EditorUtil.GetProjectDir() , appStoreProvisionFileName ) );
			}
			return _appstore;
		}
	}



#if UNITY_IOS



	[PostProcessBuild( 1000 )]
	public static void RunRemoteiOSBuild( BuildTarget buildTarget , string pathToBuiltProject )
	{
		if( buildTarget != BuildTarget.iOS )
		{
			return;
		}

		string projectDir = EditorUtil.GetProjectDir();
		string directory = pathToBuiltProject.Substring( projectDir.Length );
		GenerateBuildScript( pathToBuiltProject , directory );

		{
			string remote_build_adhoc = string.Format( @"@set DIR=%~dp0
@cd %DIR%
@call remote_build_adhoc_func.bat {0}
" , directory );

			File.WriteAllText( Path.Combine( projectDir , "打包adhoc.bat" ) , remote_build_adhoc , System.Text.Encoding.Default );
		}

		{
			string remote_build_app_store = string.Format( @"@set DIR=%~dp0
@cd %DIR%
@call remote_build_app_store_func.bat {0}
" , directory );

			File.WriteAllText( Path.Combine( projectDir , "打包app_store.bat" ) , remote_build_app_store , System.Text.Encoding.Default );
		}


		if( pathToBuiltProject.Contains( "DEBUG" ) )
		{
			System.Diagnostics.Process p = new System.Diagnostics.Process();
			p.StartInfo.WorkingDirectory = projectDir;
			p.StartInfo.FileName = System.IO.Path.Combine( projectDir , "remote_debug.bat" );
			p.StartInfo.Arguments = directory;
			p.Start();
			return;
		}

		{
#if UNITY_EDITOR_WIN
			System.Diagnostics.Process p = new System.Diagnostics.Process();
			p.StartInfo.WorkingDirectory = projectDir;
			p.StartInfo.FileName = System.IO.Path.Combine( projectDir , "remote_build.bat" );
			p.StartInfo.Arguments = directory;
			p.Start();
#endif
		}

	}




	private static string getEnableSignWithApple()
	{
		if( CustomExportProject.EnableSignWithApple )
		{
			return "/usr/libexec/PlistBuddy -c 'Delete :com.apple.developer.applesignin' Unity-iPhone.entitlements\n" +
"/usr/libexec/PlistBuddy -c 'Add :com.apple.developer.applesignin array' Unity-iPhone.entitlements\n" +
"/usr/libexec/PlistBuddy -c 'Add :com.apple.developer.applesignin: string Default' Unity-iPhone.entitlements\n";
		}
		else
		{
			return "";
		}
	}



	private static string getDefaultProductName( string bundleId )
	{
		try
		{
			return Regex.Match( bundleId , @"\.(\w+)$" , RegexOptions.Multiline ).Groups[1].Value;
		}
		catch( ArgumentException ex )
		{
			return "Unity-iPhone";
		}
	}



	/// <summary>
	/// 
	/// </summary>
	/// <param name="outputPath">输出的xcode工程目录，全路径</param>
	/// <param name="dirName">目录名</param>
	/// <returns> true Release 版 </returns>
	private static void GenerateBuildScript( string outputPath , string dirName )
	{
		{// dev
			File.WriteAllText( Path.Combine( outputPath , "build.plist" ) , string.Format( @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
	<key>method</key>
	<string>development</string>
	<key>compileBitcode</key>
	<false/>
	<key>provisioningProfiles</key>
	<dict>
		<key>{0}</key>
		<string>{1}</string>
	</dict>
</dict>
</plist>
" , DevBundleId , DevProvisioningProfileName ) , System.Text.Encoding.UTF8 );

			//-project v0.33.0-20170622-2010-jc/Unity-iPhone.xcodeproj

			string build = string.Format(
				"#!/bin/bash\n\n" +
				"ProjectPath=\"$HOME/Desktop/{0}\"\n\n" +
				"BuildPath=\"$HOME/build/{0}\"\n\n" +
				"chmod +x \"$ProjectPath/MapFileParser.sh\"\n\n" +
				"/usr/libexec/PlistBuddy -c \"Set :CFBundleIdentifier {1}\" \"$ProjectPath/Info.plist\"\n\n" +
				getEnableSignWithApple() +
				"pod install --verbose --no-repo-update\n\n" +
				"xcodebuild -workspace \"$ProjectPath/Unity-iPhone.xcworkspace\" -archivePath \"$BuildPath.xcarchive\" -sdk iphoneos -scheme \"Unity-iPhone\" -configuration \"Release\" clean\n\n" +
				"xcodebuild -workspace \"$ProjectPath/Unity-iPhone.xcworkspace\" -archivePath \"$BuildPath.xcarchive\" -sdk iphoneos -scheme \"Unity-iPhone\" -configuration \"Release\" archive DEVELOPMENT_TEAM={2} PROVISIONING_PROFILE=\"{3}\"\n\n" +
				"xcodebuild -exportArchive -archivePath \"$BuildPath.xcarchive\" -exportPath \"$ProjectPath.dev\" -exportOptionsPlist \"$ProjectPath/build.plist\"\n\n" +
				"mv \"$ProjectPath.dev/{4}.ipa\" \"$ProjectPath.dev.ipa\"\n\n" +
				"rm -rf \"$ProjectPath.dev\""
, dirName , DevBundleId , DeviOSTeamID , DevProvisioningProfileID , getDefaultProductName( DevBundleId ) );
			File.WriteAllText( Path.Combine( outputPath , "build.sh" ) , build , System.Text.Encoding.ASCII );

		}

		if( outputPath.Contains( "DEBUG" ) )
		{
			return;
		}

		{//hk adhoc
			File.WriteAllText( Path.Combine( outputPath , "build.adhoc.plist" ) , string.Format( @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
	<key>method</key>
	<string>ad-hoc</string>
	<key>compileBitcode</key>
	<false/>
	<key>provisioningProfiles</key>
	<dict>
		<key>{0}</key>
		<string>{1}</string>
	</dict>
</dict>
</plist>
" , ReleaseBundleId , AdhocProvisioningProfileName ) , System.Text.Encoding.UTF8 );


			string str = string.Format(
				"#!/bin/bash\n\n" +
				"ProjectPath=\"$HOME/Desktop/{0}\"\n\n" +
				"BuildPath=\"$HOME/build/{0}.adhoc\"\n\n" +
				"chmod +x \"$ProjectPath/MapFileParser.sh\"\n\n" +
				"/usr/libexec/PlistBuddy -c \"Set :CFBundleIdentifier {1}\" \"$ProjectPath/Info.plist\"\n\n" +
				getEnableSignWithApple() +
				"pod install --verbose --no-repo-update\n\n" +
				"xcodebuild -workspace \"$ProjectPath/Unity-iPhone.xcworkspace\" -archivePath \"$BuildPath.xcarchive\" -sdk iphoneos -scheme \"Unity-iPhone\" -configuration \"Release\" clean\n\n" +
				"xcodebuild -workspace \"$ProjectPath/Unity-iPhone.xcworkspace\" -archivePath \"$BuildPath.xcarchive\" -sdk iphoneos -scheme \"Unity-iPhone\" -configuration \"Release\" archive DEVELOPMENT_TEAM={2} PROVISIONING_PROFILE=\"{3}\" CODE_SIGN_IDENTITY=\"{4}\"\n\n" +
				"xcodebuild -exportArchive -archivePath \"$BuildPath.xcarchive\" -exportPath \"$ProjectPath.adhoc\" -exportOptionsPlist \"$ProjectPath/build.adhoc.plist\"\n\n" +
				"mv \"$ProjectPath.adhoc/{5}.ipa\" \"$ProjectPath.adhoc.ipa\"\n\n" +
				"rm -rf \"$ProjectPath.adhoc\""
, dirName , ReleaseBundleId , ReleaseiOSTeamID , AdhocProvisioningProfileId , ReleaseSignId , getDefaultProductName( ReleaseBundleId ) );
			File.WriteAllText( Path.Combine( outputPath , "build_adhoc.sh" ) , str , System.Text.Encoding.ASCII );

		}



		{// hk store
			File.WriteAllText( Path.Combine( outputPath , "build.app_store.plist" ) , string.Format( @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
	<key>method</key>
	<string>app-store</string>
	<key>compileBitcode</key>
	<false/>
	<key>uploadSymbols</key>
	<false/>
	<key>provisioningProfiles</key>
	<dict>
		<key>{0}</key>
		<string>{1}</string>
	</dict>
</dict>
</plist>
" , ReleaseBundleId , ReleaseProvisioningProfileName ) , System.Text.Encoding.UTF8 );

			string str = string.Format(
				"#!/bin/bash\n\n" +
				"ProjectPath=\"$HOME/Desktop/{0}\"\n\n" +
				"BuildPath=\"$HOME/build/{0}.app_store\"\n\n" +
				"chmod +x \"$ProjectPath/MapFileParser.sh\"\n\n" +
				"/usr/libexec/PlistBuddy -c \"Set :CFBundleIdentifier {1}\" \"$ProjectPath/Info.plist\"\n\n" +
				getEnableSignWithApple() +
				"pod install --verbose --no-repo-update\n\n" +
				"xcodebuild -workspace \"$ProjectPath/Unity-iPhone.xcworkspace\" -archivePath \"$BuildPath.xcarchive\" -sdk iphoneos -scheme \"Unity-iPhone\" -configuration \"Release\" clean\n\n" +
				"xcodebuild -workspace \"$ProjectPath/Unity-iPhone.xcworkspace\" -archivePath \"$BuildPath.xcarchive\" -sdk iphoneos -scheme \"Unity-iPhone\" -configuration \"Release\" archive DEVELOPMENT_TEAM={2} PROVISIONING_PROFILE=\"{3}\" CODE_SIGN_IDENTITY=\"{4}\"\n\n" +
				"xcodebuild -exportArchive -archivePath \"$BuildPath.xcarchive\" -exportPath \"$ProjectPath.app_store\" -exportOptionsPlist \"$ProjectPath/build.app_store.plist\"\n\n" +
				"mv \"$ProjectPath.app_store/{5}.ipa\" \"$ProjectPath.app_store.ipa\"\n\n" +
				"rm -rf \"$ProjectPath.app_store\""
				, dirName , ReleaseBundleId , ReleaseiOSTeamID , ReleaseProvisioningProfileId , ReleaseSignId , getDefaultProductName( ReleaseBundleId ) );
			File.WriteAllText( Path.Combine( outputPath , "build_app_store.sh" ) , str , System.Text.Encoding.ASCII );

		}

	}

#endif

}

