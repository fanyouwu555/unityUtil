using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using UnityEngine.UI;
using Object = UnityEngine.Object;



public class EditorUtil
{
	public static string GetFullPath( Transform obj , Transform root )
	{
		string name = obj.name;
		while( obj.parent != null && obj.parent.parent != null && obj.parent != root )
		{
			name = obj.parent.name + "/" + name;
			obj = obj.parent;
		}

		return name;
	}


	public static List<T> GetAllChildComponent<T>( Transform root )  where T : Component
	{
		List<T> ret = InternalGetAllChildComponent<T>( root );
		T tmp = root.GetComponent<T>();
		if( null != tmp )
		{
			ret.Add( tmp );
		}
		return ret;
	}





	public static List<T> InternalGetAllChildComponent<T>( Transform root ) where T : Component
	{
		List<T> ret = new List<T>();

		for( int i = 0 ; i < root.childCount ; i++ )
		{
			Transform child = root.GetChild( i );
			T tmp = child.GetComponent<T>();
			if( null != tmp )
			{
				ret.Add( tmp );
			}

			ret.AddRange( InternalGetAllChildComponent<T>( child ) );
		}

		return ret;
	}




	public static string GetProjectDir()
	{
		int index = Application.dataPath.LastIndexOf( "Assets" );
		return Application.dataPath.Substring( 0 , index );
	}


	public static bool IsSamePath( string path1 , string path2 )
	{
		return path1.Replace( "/" , "\\" ).TrimEnd( '\\' ) == path2.Replace( "/" , "\\" ).TrimEnd( '\\' );
	}



	private static byte[] FixBOM( byte[] text )
	{
		if( text[0] == 0XEF && text[1] == 0xBB && text[2] == 0XBF )
		{
			byte[] ret = new byte[text.Length - 3];
			System.Array.Copy( text , 3 , ret , 0 , text.Length - 3 );
			return ret;
		}
		else
		{
			//			return text;
			return System.Text.Encoding.UTF8.GetBytes( System.Text.Encoding.Default.GetString( text ) );
		}
	}




	public static void CopyTextToClipboard( string txt )
	{
		TextEditor te = new TextEditor();
		te.text = txt;
		te.SelectAll();
		te.Copy();
	}







	public static void DeleteDir( string destDir )
	{
		if( Directory.Exists( destDir ) )
		{
			foreach( string file in Directory.GetFiles( destDir , "*" , SearchOption.AllDirectories ) )
			{
				File.Delete( file );
			}

			foreach( string dir in Directory.GetDirectories( destDir , "*" , SearchOption.AllDirectories ) )
			{
				if( Directory.Exists( dir ) )
				{
					Directory.Delete( dir , true );
				}
			}
		}

	}

	public static void CopyDir( string srcDir , string destDir , string searchPattern , Action<string> newAction )
	{
		// copy
		{
			string[] path = Directory.GetDirectories( srcDir , "*" , SearchOption.AllDirectories );
			for( int i = 0 ; i < path.Length ; i++ )
			{
				string dest = path[i].Replace( srcDir , destDir );
				if( !Directory.Exists( dest ) )
				{
					Directory.CreateDirectory( dest );
				}
			}

			//string projectPath = GetProjectDir();
			string[] file = Directory.GetFiles( srcDir , searchPattern , SearchOption.AllDirectories );
			for( int i = 0 ; i < file.Length ; i++ )
			{
				newAction( file[i] );
			}
		}
	}


	public static void SplitLog( Action<string> log , string str , int maxlen = 15 * 1024 )
	{
		while( str.Length > maxlen )
		{
			int index = str.LastIndexOf( '\n' , maxlen );
			if( index < 0 )
			{
				break;
			}
			log( str.Substring( 0 , index ) );
			str = str.Substring( index + 1 );
		}

		log( str );
	}




	public static bool CompareFile( string fileName1 , string fileName2 )
	{
		if( !File.Exists( fileName1 ) )
		{
			return false;
		}

		if( !File.Exists( fileName2 ) )
		{
			return false;
		}

		FileInfo info1 = new FileInfo( fileName1 );
		FileInfo info2 = new FileInfo( fileName2 );

		if( info1.Length != info2.Length )
		{
			return false;
		}

		return UnityHelper.GetFileMD5( fileName1 ) == UnityHelper.GetFileMD5( fileName2 );
	}







	public static List<string> GetTextureFile( string[] pathNames )
	{
		int length = Application.dataPath.Length - "Assets".Length;

		List<string> ret = new List<string>();
		foreach( var pathName in pathNames )
		{

			string path = Path.Combine( Application.dataPath , pathName );
			if( !Directory.Exists( path ) )
			{
				continue;
			}

			List<string> files = Directory.GetFiles( path , "*.png" , SearchOption.AllDirectories )
						.Select( e => e.Remove( 0 , length ).Replace( "\\" , "/" ) ).ToList();
			ret.AddRange( files );
			IEnumerable<string> files2 = Directory.GetFiles( path , "*.jpg" , SearchOption.AllDirectories )
				.Select( e => e.Remove( 0 , length ).Replace( "\\" , "/" ) );

			ret.AddRange( files2 );
		}

		return ret;
	}


	/// <summary>
	/// 查找目录下的资源,非递归.
	/// </summary>
	/// <param name="path"></param>
	/// <param name="searchPattern"></param>
	/// <returns></returns>
	public static List<string> GetAssetsFiles( string path , string searchPattern , SearchOption searchOption = SearchOption.TopDirectoryOnly )
	{
		string fullPath = PrefabTools.PathCombine( Application.dataPath , path );
		if( !Directory.Exists( fullPath ) )
		{
			return new List<string>();
		}

		int length = Application.dataPath.Length - "Assets".Length;
		return Directory.GetFiles( fullPath , searchPattern , searchOption )
			.Select( e => e.Remove( 0 , length ).Replace( "\\" , "/" ) ).ToList();
	}
	public static List<string> GetAssetsFiles( string path , string[] searchPattern , SearchOption searchOption = SearchOption.TopDirectoryOnly )
	{
		List<string> ret = new List<string>();
		foreach( string value in searchPattern )
		{
			ret.AddRange( GetAssetsFiles( path , value , searchOption ) );
		}
		return ret;
	}

	public static List<string> GetAssetsFiles( string[] path , string searchPattern , SearchOption searchOption = SearchOption.TopDirectoryOnly )
	{
		List<string> ret = new List<string>();
		foreach( string value in path )
		{
			ret.AddRange( GetAssetsFiles( value , searchPattern , searchOption ) );
		}
		return ret;
	}



	/// <summary>
	/// 查找目录下的目录
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public static List<string> GetAssetsDirectory( string path )
	{
		string fullPath = PrefabTools.PathCombine( Application.dataPath , path );
		if( !Directory.Exists( fullPath ) )
		{
			return new List<string>();
		}
		return Directory.GetDirectories( fullPath , "*" , SearchOption.TopDirectoryOnly ).Where( e => !isEmptyDirectory( e ) ).ToList();
	}
	public static List<string> GetAssetsDirectory( string[] path )
	{
		List<string> ret = new List<string>();
		foreach( var value in path )
		{
			ret.AddRange( GetAssetsDirectory( value ) );
		}
		return ret;
	}


	/// <summary>
	/// 空目录返回true
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public static bool isEmptyDirectory( string path )
	{
#if UNITY_5
		return 0 == Directory.GetFiles( path ).Length;
#else
		IEnumerator<string> value = Directory.EnumerateFiles( path ).GetEnumerator();
		return !value.MoveNext();
#endif
	}




	public static void DeleteFile( string path )
	{
		if( File.Exists( path ) )
		{
			File.Delete( path );
		}
	}

	public static void DeleteFiles( IEnumerable<string> files )
	{
		foreach( string file in files )
		{
			DeleteFile( file );
		}
	}



	public static void ZipFile( string projectDir , string file , string targetFile )
	{
		// Debug.Log("ZipFile : " + file);
		System.Diagnostics.Process p = new System.Diagnostics.Process();
		p.StartInfo.UseShellExecute = false;
		p.StartInfo.CreateNoWindow = true;
		p.StartInfo.RedirectStandardOutput = true;
		p.StartInfo.WorkingDirectory = projectDir;
		p.StartInfo.FileName = System.IO.Path.Combine( projectDir , @"Tools\Lzma.exe" );
		p.StartInfo.Arguments = string.Format( "\"{0}\" \"{1}\"" , file , targetFile );
		p.Start();
		p.WaitForExit();
	}


	private static string fixPathEndWithSeparator( string dir )
	{
		dir = dir.Replace( '\\' , '/' );
		if( !dir.EndsWith( "/" ) )
		{
			dir += '/';
		}
		return dir;
	}

	public static List<string> ZipDir( List<string> endsWith , string sourceDir , string targetDir , Action<string , string , string> ZipFileAction )
	{
		string projectDir = EditorUtil.GetProjectDir();
		List<string> zipFileList = new List<string>();
		targetDir = fixPathEndWithSeparator( targetDir );
		sourceDir = fixPathEndWithSeparator( sourceDir );

		EditorUtil.CopyDir( targetDir , sourceDir , "*" , delegate ( string file )
		{
			if( endsWith.Any( e => file.EndsWith( e ) ) )
			{
				return;
			}

			zipFileList.Add( file.Remove( 0 , targetDir.Length ).Replace( '\\' , '/' ) );

			string targetFile = file.Replace( targetDir , sourceDir );

			//if( File.GetLastWriteTime(targetFile) > File.GetLastWriteTime(file) )
			//{
			//	return;
			//}

			if( null != ZipFileAction )
			{
				ZipFileAction( projectDir , file , targetFile );
			}
		} );
		return zipFileList;
	}


	public static void DeleteFolder( FileSystemInfo[] folders )
	{
		foreach( FileSystemInfo folder in folders )
		{
			if( folder is DirectoryInfo )
			{
				DirectoryInfo subdir = new DirectoryInfo( folder.FullName );
				if( subdir != null )
				{
					subdir.Delete( true );          //删除子目录和文件
				}

				string folderMetafile = folder.FullName + ".meta";
				FileInfo file = new FileInfo( folderMetafile );
				if( file != null )
				{
					file.Delete();
				}

				// folder.Delete();
			}
		}

	}


	public static bool isTextureFile( string filename )
	{
		return ( filename.EndsWith( ".png" , true , null ) || filename.EndsWith( ".jpg" , true , null ) );
	}
}

