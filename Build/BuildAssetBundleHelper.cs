using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

/// <summary>
/// ��Resource�µ���Դ�����.data��OutPutĿ¼��
/// </summary>
public partial class BuildAssetBundleScript
{

	#region ���ͼƬ���

	/// <summary>
	/// ȥ��UIĿ¼�£�android��ios����ͼƬ������ѡ��
	/// </summary>
	public static void CheckUITexture()
	{
		List<string> files = EditorUtil.GetTextureFile( uiTextureDir );
		files.AddRange( EditorUtil.GetTextureFile( uiEffectTextureDir ) );

		StringBuilder sbTextureSize = new StringBuilder();
		StringBuilder sbTextureType = new StringBuilder();
		StringBuilder sbTextureCompression = new StringBuilder();
		StringBuilder sbTextureRGB = new StringBuilder();

		foreach( string fileName in files )
		{
			bool isChange = false;
			TextureImporter importer = AssetImporter.GetAtPath( fileName ) as TextureImporter;

			if( importer.textureType != TextureImporterType.Sprite )
			{
				importer.textureType = TextureImporterType.Sprite;
				isChange = true;
				sbTextureType.AppendLine( fileName );
			}

			if( !importer.sRGBTexture )
			{
				importer.sRGBTexture = true;
				isChange = true;
				sbTextureRGB.AppendLine( fileName );
			}

			if( importer.isReadable )
			{
				importer.isReadable = false;
				isChange = true;
				sbTextureRGB.AppendLine( fileName );
			}

			TextureImporterPlatformSettings defaultPlatformTexture = importer.GetDefaultPlatformTextureSettings();
			if( TextureImporterCompression.Compressed != defaultPlatformTexture.textureCompression )
			{
				defaultPlatformTexture.textureCompression = TextureImporterCompression.Compressed;
				importer.SetPlatformTextureSettings( defaultPlatformTexture );
				isChange = true;
				sbTextureCompression.AppendLine( fileName );
			}

			if( CustomTextureOverridden( importer , defaultPlatformTexture ) )
			{
				sbTextureSize.AppendLine( fileName );
				isChange = true;
			}

			if( isChange )
			{
				importer.SaveAndReimport();
			}
		}

		if( sbTextureSize.Length > 0 )
		{
			sbTextureSize.Insert( 0 , "TextureSize Change\n" );
			EditorUtil.SplitLog( Debug.LogWarning , sbTextureSize.ToString() );
		}

		if( sbTextureType.Length > 0 )
		{
			sbTextureType.Insert( 0 , "TextureType Change\n" );
			EditorUtil.SplitLog( Debug.LogWarning , sbTextureType.ToString() );
		}

		if( sbTextureRGB.Length > 0 )
		{
			sbTextureRGB.Insert( 0 , "TextureSRGB Change\n" );
			EditorUtil.SplitLog( Debug.LogWarning , sbTextureRGB.ToString() );
		}

		if( sbTextureCompression.Length > 0 )
		{
			sbTextureCompression.Insert( 0 , "TextureCompression Change\n" );
			EditorUtil.SplitLog( Debug.LogWarning , sbTextureCompression.ToString() );
		}
	}


	/// <summary>
	/// ����ͼƬ��ѹ��
	/// </summary>
	public static void CheckFontTexture()
	{
		List<string> files = EditorUtil.GetTextureFile( fontTextureDir );

		StringBuilder sb = new StringBuilder();

		sb.AppendLine( "Font Texture Change" );
		int changeCount = 0;
		foreach( string fileName in files )
		{
			bool isChange = false;
			TextureImporter importer = AssetImporter.GetAtPath( fileName ) as TextureImporter;

			// ��ʽ���
			TextureImporterPlatformSettings defaultPlatformTexture = importer.GetDefaultPlatformTextureSettings();
			if( TextureImporterCompression.Compressed != defaultPlatformTexture.textureCompression )
			{
				defaultPlatformTexture.textureCompression = TextureImporterCompression.Compressed;
				importer.SetPlatformTextureSettings( defaultPlatformTexture );
				isChange = true;
			}

			if( CustomTextureOverridden( importer , defaultPlatformTexture , GetFontTextureFormat ) )
			{
				isChange = true;
			}

			if( isChange )
			{
				importer.SaveAndReimport();
				sb.AppendLine( fileName );
				changeCount++;
			}
		}

		if( changeCount > 0 )
		{
			Debug.LogWarning( sb.ToString() );
		}

	}


	/// <summary>
	/// ���ͼƬ��ѹ����ʽ
	/// </summary>
	/// <param name="importer"></param>
	/// <returns></returns>
	private static TextureImporterFormat GetTextureFormat( TextureImporter importer )
	{
		if( importer.DoesSourceTextureHaveAlpha() )
		{
			return TextureImporterFormat.ASTC_RGBA_5x5;
		}
		else
		{
			return TextureImporterFormat.ASTC_RGB_6x6;
		}
	}

	private static TextureImporterFormat GetFontTextureFormat( TextureImporter importer )
	{
		if( importer.DoesSourceTextureHaveAlpha() )
		{
			return TextureImporterFormat.ASTC_RGBA_4x4;
		}
		else
		{
			return TextureImporterFormat.ASTC_RGB_4x4;
		}
	}

	private static bool CustomTextureOverriddenPlatform( TextureImporter importer , TextureImporterPlatformSettings defaultPlatformTexture
		, TextureImporterPlatformSettings setting , Func<TextureImporter , TextureImporterFormat> getTextureFormat )
	{

		bool change = false;
		if( !setting.overridden )
		{
			setting.overridden = true;
			change = true;
		}

		if( setting.maxTextureSize != defaultPlatformTexture.maxTextureSize )
		{
			setting.maxTextureSize = defaultPlatformTexture.maxTextureSize;
			change = true;
		}

		if( TextureImporterFormat.ASTC_RGBA_4x4 == setting.format
			|| TextureImporterFormat.ASTC_RGB_4x4 == setting.format
			|| TextureImporterFormat.ASTC_RGB_5x5 == setting.format
			)
		{
			// ����Ѿ�����Ϊ ASTC �ұ�Ĭ�ϸ�ʽ���ߵĻ�,�򲻽��и��ǲ���
		}
		else
		{
			TextureImporterFormat format = getTextureFormat( importer );
			if( setting.format != format )
			{
				setting.format = format;
				change = true;
			}
		}

		if( change )
		{
			importer.SetPlatformTextureSettings( setting );
		}
		return change;
	}



	private static bool CustomTextureOverridden( TextureImporter importer , TextureImporterPlatformSettings defaultPlatformTexture
		, Func<TextureImporter , TextureImporterFormat> getTextureFormat )
	{
		bool iPhone = CustomTextureOverriddenPlatform( importer , defaultPlatformTexture , importer.GetPlatformTextureSettings( "iPhone" ) , getTextureFormat );
		bool android = CustomTextureOverriddenPlatform( importer , defaultPlatformTexture , importer.GetPlatformTextureSettings( "Android" ) , getTextureFormat );
		return iPhone || android;
	}
	private static bool CustomTextureOverridden( TextureImporter importer , TextureImporterPlatformSettings defaultPlatformTexture )
	{
		bool iPhone = CustomTextureOverriddenPlatform( importer , defaultPlatformTexture , importer.GetPlatformTextureSettings( "iPhone" ) , GetTextureFormat );
		bool android = CustomTextureOverriddenPlatform( importer , defaultPlatformTexture , importer.GetPlatformTextureSettings( "Android" ) , GetTextureFormat );
		return iPhone || android;
	}


	/// <summary>
	/// ���� spine ��������ʱ����Ĭ�Ͻ�ͼƬ��ʽ����Ϊ��ѹ����RBGA�������ڴ����Դʱǿ�����ø�ʽ��
	/// ȫƽ̨Ĭ�ϲ��֣�Compression��ΪNormalѹ����ʽ
	/// iOS �޸�texture��ʽΪ RGBA ASTC 5x5
	/// android ������Ĭ��ѡ��
	/// </summary>
	public static void CheckSpineTexture()
	{
		List<string> files = EditorUtil.GetTextureFile( spineTextureDir );

		StringBuilder sb = new StringBuilder();

		sb.AppendLine( "Spine Texture Change" );
		int changeCount = 0;
		foreach( string fileName in files )
		{
			bool isChange = false;
			TextureImporter importer = AssetImporter.GetAtPath( fileName ) as TextureImporter;

			// ��ʽ���
			TextureImporterPlatformSettings defaultPlatformTexture = importer.GetDefaultPlatformTextureSettings();
			if( TextureImporterCompression.Compressed != defaultPlatformTexture.textureCompression )
			{
				defaultPlatformTexture.textureCompression = TextureImporterCompression.Compressed;
				importer.SetPlatformTextureSettings( defaultPlatformTexture );
				isChange = true;
			}

			isChange = CustomTextureOverridden( importer , defaultPlatformTexture );

			if( isChange )
			{
				importer.SaveAndReimport();
				sb.AppendLine( fileName );
				changeCount++;
			}
		}

		if( changeCount > 0 )
		{
			Debug.LogWarning( sb.ToString() );
		}
	}



	private static readonly string spine_shader_name = "Spine/SkeletonGraphic";

	[MenuItem( "AssetBundle/CheckSpineShader" , false , 0 )]
	public static void CheckSpineShader()
	{
		List<string> files = EditorUtil.GetTextureFile( girlSpineMatDir );

		Shader shader = Shader.Find( spine_shader_name );
		if( null == shader )
		{
			Debug.LogErrorFormat( "Can't Find Shader( {0} )" , spine_shader_name );
			return;
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine( "Spine Material Change" );

		int changeCount = 0;
		foreach( string fileName in files )
		{
			Material mat = (Material)AssetDatabase.LoadAssetAtPath( fileName , typeof( Material ) );
			string oldName = mat.shader.name;
			if( oldName == spine_shader_name )
			{
				continue;
			}

			mat.shader = shader;

			sb.AppendLine( string.Format( "{0}\t{1}" , fileName , mat.shader.name ) );
			EditorUtility.SetDirty( mat );
			changeCount++;
		}

		if( changeCount > 0 )
		{
			Debug.LogWarning( sb.ToString() );
			AssetDatabase.SaveAssets();
		}
	}


	#endregion




	/// <summary>
	/// ����UI��prefab�ļ����õ���Ӧ�� BundleName
	/// </summary>
	/// <param name="subjectString"></param>
	/// <returns></returns>
	public static string GetUiPrefabBundleName( string subjectString )
	{
		try
		{
			Regex regexObj = new Regex( @"^([^_]+)(_[ \w]+)?(\.prefab)?$" , RegexOptions.Multiline );
			string ret = regexObj.Match( subjectString ).Groups[1].Value.ToLower();
			if( string.IsNullOrEmpty( ret ) )
			{
				return null;
			}
			return HookBuildPrefabGroup( ret );
		}
		catch( ArgumentException )
		{
			return null;
		}


	}

	/// <summary>
	/// ��ȡlua�ļ��е����ã�������ļ��Ḳ��ǰ������á�
	/// </summary>
	/// <param name="luaFiles"></param>
	/// <returns></returns>
	public static Dictionary<string , string> readLuaAssetFile( string[] luaFiles )
	{
		StringBuilder sb = new StringBuilder();

		Dictionary<string , string> dict = new Dictionary<string , string>();
		foreach( string file in luaFiles )
		{
			Dictionary<string , string> tmp = readLuaAssetFile( file );
			foreach( var item in tmp )
			{
				if( dict.ContainsKey( item.Key ) )
				{
					//dict[item.Key] = item.Value;
					sb.AppendFormat( "File:{0} , PrefabName = {1}\n" , file , item.Key );
				}
				else
				{
					dict.Add( item.Key , item.Value );
				}
			}
		}

		if( sb.Length > 0 )
		{
			sb.Insert( 0 , "readLuaAssetFile ������ prefab����:\n" );
			EditorUtil.SplitLog( Debug.LogWarning , sb.ToString() );
		}
		return dict;
	}


	public static Dictionary<string , string> readLuaAssetFile( string luaFile )
	{
		Dictionary<string , string> dict = new Dictionary<string , string>();

		string content = File.ReadAllText( Path.Combine( Application.dataPath , luaFile ) );
		try
		{
			Regex regexObj = new Regex( @"[ \t]{(?:[ \t]*'\w+'[ \t]*,){3}[ \t]*'(\w+)'[ \t]*,[ \t]*'\w+/(\w+\.unity3d)'[ \t]*}" , RegexOptions.Multiline );
			Match matchResult = regexObj.Match( content );
			while( matchResult.Success )
			{
				string prefabName = matchResult.Groups[1].Value;
				string bundleName = matchResult.Groups[2].Value;

				if( dict.ContainsKey( prefabName ) )
				{
					Debug.LogWarning( "" );
				}
				else
				{
					dict.Add( prefabName , bundleName );
				}
				matchResult = matchResult.NextMatch();
			}
		}
		catch( ArgumentException )
		{
		}
		return dict;
	}



	private static string HookBuildPrefabGroup( string name )
	{
		if( "base" == name )
		{
			return "common_base";
		}
		else
		{
			return name;
		}
	}




}