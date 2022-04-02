using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Callbacks;







public partial class ProjectBuild
{

	static private readonly string serverIpFileName = "serverip.txt";
	static private readonly string serverTypeFileName = "servertype.txt";

	private static void GenXLuaCode()
	{
		isGenXLuaCode = true;
		CSObjectWrapEditor.Generator.GenAll();
		isGenXLuaCode = false;
	}



	private static void SetServerAddress( string server )
	{
		{
			string serverType = Path.Combine( Application.dataPath , "Resources/" + serverTypeFileName );
			if( server == liveServer )
			{
				File.WriteAllText( serverType , "live" );
			}
			else
			{
				File.Delete( serverType );
			}
			AssetDatabase.ImportAsset( serverType , ImportAssetOptions.ForceUpdate );
		}

		{
			string filePath = Path.Combine( Application.dataPath , "Resources/" + serverIpFileName );
			File.WriteAllText( filePath , server );

			AssetDatabase.ImportAsset( filePath , ImportAssetOptions.ForceUpdate );
		}
	}



	static EditorBuildSettingsScene[] GetBuildScenes()
	{
		return Array.FindAll( EditorBuildSettings.scenes , e => e != null && e.enabled );
	}

	/// <summary>
	/// 正则匹配,得到捕获的第一个组
	/// </summary>
	/// <param name="text"></param>
	/// <param name="reg"></param>
	/// <returns></returns>
	private static string GetMatchGroup( string text , string reg )
	{
		try
		{
			Match matchResults = Regex.Match( text , reg , RegexOptions.Multiline );
			if( matchResults.Success )
			{
				return matchResults.Groups[1].Value;
			}
		}
		catch( ArgumentException )
		{
		}
		return null;
	}


	/// <summary>
	/// 读取(Resources/serverip.txt)得到服务器的类型,live demo 或其他
	/// </summary>
	/// <returns></returns>
	static public string GetServerAddressType()
	{
		string text = null;
		try
		{
			string filePath = Path.Combine( Application.dataPath , "Resources/" + serverIpFileName );
			text = File.ReadAllText( filePath );
		}
		catch( Exception )
		{
			return "";
		}

		if( liveServer == text )
		{
			return "live";
		}
		else if( demoServer == text )
		{
			return "demo";
		}

		string tmp = GetMatchGroup( text , @"\d+\.\d+\.\d+\.(\d+)" );
		return null != tmp ? tmp : "";
	}
}