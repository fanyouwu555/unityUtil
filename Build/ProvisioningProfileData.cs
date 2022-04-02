using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;

public class ProvisioningProfileData
{
	private string teamId;
	private string bundleId;
	private string sign;
	private string uUID;
	private string name;

	public string TeamId { get { return teamId; } private set { teamId = value; } }
	public string BundleId { get { return bundleId; } private set { bundleId = value; } }
	public string Sign { get { return sign; } private set { sign = value; } }
	public string UUID { get { return uUID; } private set { uUID = value; } }
	public string Name { get { return name; } private set { name = value; } }


	public override string ToString()
	{
		return string.Format( @"TeamIdentifier = {0}
application-identifier = {1}
Sign = {2}
UUID = {3}
Name = {4}" , TeamId , bundleId , Sign , UUID , Name );
	}


	private static string GetProvisioningProfile( string filename )
	{
		byte[] buff = File.ReadAllBytes( filename );

		do
		{
			int index = Array.IndexOf( buff , (byte)0x0a );
			if( index >= 0 )
			{
				string str = System.Text.Encoding.UTF8.GetString( buff , index , buff.Length - index );
				if( !string.IsNullOrEmpty( str ) )
				{
					int start = str.IndexOf( "<plist version=\"1.0\">" );
					int end = str.IndexOf( "</plist>" );
					if( start >= 0 && end >= 0 )
					{
						return str.Substring( start , end - start );
					}
				}
			}
			else
			{
				return null;
			}
		} while( true );
	}


	private static string GetStringValueByKey( string plistFile , string key )
	{
		try
		{
			string e = string.Format( @"<key>{0}</key>" , key );
			return Regex.Match( plistFile , e + @"[\t\r\n]*<string>([^<]+)</string>" , RegexOptions.IgnoreCase ).Groups[1].Value;
		}
		catch( Exception )
		{
		}
		return null;
	}




	private static ProvisioningProfileData PraseProvisioningProfile( string pListFileContent )
	{
		ProvisioningProfileData ret = new ProvisioningProfileData();

		ret.UUID = GetStringValueByKey( pListFileContent , "UUID" );
		ret.TeamId = GetStringValueByKey( pListFileContent , "com.apple.developer.team-identifier" );
		ret.Name = GetStringValueByKey( pListFileContent , "Name" );

		string appId = GetStringValueByKey( pListFileContent , "application-identifier" );
		if( appId.StartsWith( ret.TeamId ) )
		{
			ret.BundleId = appId.Substring( ret.TeamId.Length + 1 );
		}
		else
		{
			return null;
		}

		string teamName = GetStringValueByKey( pListFileContent , "TeamName" );
		string aps = GetStringValueByKey( pListFileContent , "aps-environment" );

		if( string.IsNullOrEmpty( aps ) || ( "development" == aps ) )
		{
			ret.Sign = "iPhone Developer";
		}
		else if( "production" == aps )
		{
			ret.Sign = "iPhone Distribution";
		}
		else
		{
			return null;
		}

		return ret;
	}


	public static ProvisioningProfileData Parse( string fileName )
	{
		string content = GetProvisioningProfile( fileName );
		if( null != content )
		{
			return PraseProvisioningProfile( content );
		}
		return null;
	}

}
