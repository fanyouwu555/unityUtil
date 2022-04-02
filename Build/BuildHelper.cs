using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

public class BuildHelper
{
	static private readonly string svnRevisionFileName = "svnRevision.txt";



	private static string GetBuildTime()
	{
		return System.DateTime.Now.ToString( "yyyy/MM/dd-HH:mm:ss" );
	}




	/// <summary>
	/// 将Assets\Lua的最后修改的版本号[Last Changed Rev]写入到 Version.lua
	/// </summary>
	/// <param name="svnRevision"></param>
	public static void UpdateLuaVersion( string svnRevision )
	{
		int svnVersion = 0;
		if( !int.TryParse( svnRevision , out svnVersion ) )
		{
			return;
		}

		System.Text.UTF8Encoding x = new System.Text.UTF8Encoding();

		string filePath = Path.Combine( Application.dataPath , "Lua/Version.lua" );
		string code = File.ReadAllText( filePath );

		try
		{
			Match m = Regex.Match( code , @"Version\.svnRevision[ \t]*=[ \t]*(\d+)" , RegexOptions.Multiline );
			int oldsvnVersion = 0;
			if( int.TryParse( m.Groups[1].Value , out oldsvnVersion ) )
			{
				if( svnVersion == oldsvnVersion )
				{
					Debug.Log( "LuaCode No Change" );
					return;
				}
			}
		}
		catch( ArgumentException )
		{
		}

		try
		{
			code = Regex.Replace( code , @"Version\.svnRevision[ \t]*=[ \t]*(\d+)" , "Version.svnRevision = " + svnVersion , RegexOptions.Multiline );
			code = Regex.Replace( code , @"Version\.buildTime[ \t]*=[ \t]*'([^']*)'" , "Version.buildTime = '" + GetBuildTime() + "'" , RegexOptions.Multiline );
			File.WriteAllText( filePath , code , new System.Text.UTF8Encoding( true ) );
		}
		catch( ArgumentException )
		{
		}
	}



	// 将svn版本号写到 Resources 目录下指定文件
	public static string UpdateSvnRevision()
	{
		string svnRevision = GetSvnRevision( EditorUtil.GetProjectDir() );

		string filePath = Path.Combine( Application.dataPath , "Resources/" + svnRevisionFileName );
		if( null != svnRevision )
		{
			File.WriteAllText( filePath , svnRevision );
			AssetDatabase.ImportAsset( filePath , ImportAssetOptions.ForceUpdate );
		}
		else
		{
			File.Delete( filePath );
		}
		return svnRevision;
	}

	/// <summary>
	/// 得到指定目录的最后修改的版本号 [Last Changed Rev]
	/// </summary>
	/// <returns></returns>
	public static string GetSvnRevision( string dir )
	{
		string svnPath = GetSvnFullPath();

		System.Diagnostics.Process p = new System.Diagnostics.Process();
		p.StartInfo.UseShellExecute = false;
		p.StartInfo.CreateNoWindow = true;
		p.StartInfo.RedirectStandardOutput = true;
		p.StartInfo.WorkingDirectory = dir;
		p.StartInfo.FileName = svnPath;
		p.StartInfo.Arguments = "info";
		p.Start();

		try
		{
			string output = p.StandardOutput.ReadToEnd();
			return Regex.Match( output , @"Last Changed Rev: (\d+)" ).Groups[1].Value;
		}
		catch( ArgumentException )
		{
			return null;
		}
	}



	public static string GetSvnFullPath()
	{
		string projectDir = EditorUtil.GetProjectDir();
		try
		{
			string config = File.ReadAllText( Path.Combine( projectDir , "Config.bat" ) , System.Text.Encoding.Default );
			foreach( Match item in Regex.Matches( config , @"@set SvnPath=([^\r\n]+)" ) )
			{
				string svnPath = item.Groups[1].Value;
				string fullPath = Path.Combine( svnPath , "svn.exe" );
				if( File.Exists( fullPath ) )
				{
					return fullPath;
				}
			}
		}
		catch( ArgumentException )
		{
			Debug.LogError( "Config.Bat 中找不到 SvnPath" );
			return null;
		}

		return null;
	}



	public static string SetBuildTime()
	{
		string buildtime = DateTime.Now.ToString( "yyyyMMdd-HHmm" );
		string buildtimePath = BuildDateTimeFileName();
		File.WriteAllText( buildtimePath , buildtime );

		AssetDatabase.ImportAsset( buildtimePath , ImportAssetOptions.ForceUpdate );
		return buildtime;
	}



	private static string BuildDateTimeFileName()
	{
		return Application.dataPath + System.IO.Path.DirectorySeparatorChar + "Resources" + System.IO.Path.DirectorySeparatorChar + "buildtime.txt";
	}








}
