using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using Util;



public class PrefabTools
{
	private static List<string> GetFileInPath( IEnumerable<string> allPath , SearchOption option , params string[] extension )
	{
		List<string> ret = new List<string>();

		foreach( string name in extension )
		{
			string searchPattern = "*." + name;

			foreach( string path in allPath )
			{
				string fullPath = Path.Combine( Application.dataPath , path );
				if( Directory.Exists( fullPath ) )
				{
					ret.AddRange( Directory.GetFiles( fullPath , searchPattern , option ) );
				}
			}
		}

		return ret.Select( e => e.Remove( 0 , EditorUtil.GetProjectDir().Length ) ).ToList();
	}



	private static List<string> GetAllTexture()
	{
		return GetFileInPath( BuildAssetBundleScript.uiTextureDir , SearchOption.AllDirectories , "png" , "jpg" );

	}


	/// </summary>
	/// <returns></returns>
	private static List<string> GetAlluiPrefab( bool includeSpine = false )
	{
		List<string> allPrefabDir = new List<string>();
		if( includeSpine )
		{
			allPrefabDir.AddRange( BuildAssetBundleScript.allPrefabDir );
		}
		allPrefabDir.AddRange( BuildAssetBundleScript.packageUiPrefabDir );

		return GetFileInPath( allPrefabDir , SearchOption.AllDirectories , "prefab" );
	}


	[MenuItem( "Tools/Button/ButtonText Best fit" )]
	public static void ButtonTextBestfit( MenuCommand menuCommand )
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine( "ButtonText Best fit:" );
		foreach( var file in GetAlluiPrefab() )
		{
			//直接修改prefab
			GameObject prefabObject = AssetDatabase.LoadAssetAtPath( file , typeof( GameObject ) ) as GameObject;
			bool isChange = false;
			foreach( Button button in EditorUtil.GetAllChildComponent<Button>( prefabObject.transform ) )
			{
				for( int i = 0 ; i < button.transform.childCount ; i++ )
				{
					Transform childTransform = button.transform.GetChild( i );
					if( null == childTransform )
					{
						continue;
					}

					Text childText = childTransform.GetComponent<Text>();
					if( null == childText )
					{
						continue;
					}

					if( !childText.resizeTextForBestFit )
					{
						childText.resizeTextForBestFit = true;
						childText.resizeTextMaxSize = childText.fontSize;
						int minSize = childText.resizeTextMaxSize / 2;
						childText.resizeTextMinSize = ( minSize > 14 ) ? minSize : 14;
						isChange = true;
					}
				}
			}

			if( isChange )
			{
				sb.AppendFormat( "\t{0}\n" , prefabObject.name );
				EditorUtility.SetDirty( prefabObject );
			}
		}
		Debug.Log( sb.ToString() );

		AssetDatabase.SaveAssets();
	}


	[MenuItem( "Tools/Text/Text Align By Geometry" )]
	public static void AlignByGeometry( MenuCommand menuCommand )
	{
		foreach( var file in GetAlluiPrefab() )
		{
			//直接修改prefab
			GameObject prefabObject = AssetDatabase.LoadAssetAtPath( file , typeof( GameObject ) ) as GameObject;
			AlignText( prefabObject );
			EditorUtility.SetDirty( prefabObject );//标记已改变

			Debug.Log( "Align:" + prefabObject.name );
		}

		AssetDatabase.SaveAssets();
	}

	[MenuItem( "Tools/Text/去掉字体样式，只使用 Normal" )]
	public static void FontStyle2Normal( MenuCommand menuCommand )
	{
		StringBuilder sb = new StringBuilder();
		foreach( var file in GetAlluiPrefab() )
		{
			//直接修改prefab
			GameObject prefabObject = AssetDatabase.LoadAssetAtPath( file , typeof( GameObject ) ) as GameObject;
			List<Text> allText = EditorUtil.GetAllChildComponent<Text>( prefabObject.transform );

			int current = 0;
			foreach( Text text in allText )
			{
				if( text.fontStyle != UnityEngine.FontStyle.Normal )
				{
					text.fontStyle = UnityEngine.FontStyle.Normal;
					current++;
				}
			}

			if( current > 0 )
			{
				EditorUtility.SetDirty( prefabObject );//标记已改变
				sb.AppendFormat( "{0} , {1}\n" , prefabObject.name , current );
			}
		}

		Debug.Log( "Change Font To Normal : \n" + sb );
		AssetDatabase.SaveAssets();
	}



	[MenuItem( "Tools/Text/Change Font To Arial" )]
	public static void ChangFont( MenuCommand menuCommand )
	{
		//将 Zemestro 改为Arial
		//将字体样式中粗体和斜体，都改为normal
		Font arialFont = Resources.GetBuiltinResource<Font>( "Arial.ttf" );

		foreach( var file in GetAlluiPrefab() )
		{
			bool change = false;
			GameObject prefabObject = AssetDatabase.LoadAssetAtPath( file , typeof( GameObject ) ) as GameObject;
			List<Text> allText = EditorUtil.GetAllChildComponent<Text>( prefabObject.transform );

			foreach( Text text in allText )
			{
				string[] fontNames = text.font.fontNames;
				//if( fontNames.Contains("Square721 Cn BT") | fontNames.Contains("Zemestro Std") )
				if( fontNames.Contains( "Zemestro Std" ) )
				{
					text.font = arialFont;
					text.fontStyle = UnityEngine.FontStyle.Normal;
					change = true;
				}

				if( text.fontStyle != UnityEngine.FontStyle.Normal )
				{
					text.fontStyle = UnityEngine.FontStyle.Normal;
					change = true;

				}
			}

			if( change )
			{
				EditorUtility.SetDirty( prefabObject );//标记已改变
				Debug.Log( "ChangeFont:" + prefabObject.name );
			}
		}

		AssetDatabase.SaveAssets();
	}





	private static void AlignText( GameObject root )
	{
		List<Text> allText = EditorUtil.GetAllChildComponent<Text>( root.transform );

		foreach( Text text in allText )
		{
			text.alignByGeometry = true;
		}
	}


	[MenuItem( "CONTEXT/Transform/SavePrefab" )]
	public static void SavePrefab()
	{
		GameObject source = PrefabUtility.GetPrefabParent( Selection.activeGameObject ) as GameObject;
		if( source == null )
			return;

		string prefabPath = AssetDatabase.GetAssetPath( source ).ToLower();
		if( prefabPath.EndsWith( ".prefab" ) == false )
			return;

		PrefabUtility.ReplacePrefab( Selection.activeGameObject , source , ReplacePrefabOptions.ConnectToPrefab | ReplacePrefabOptions.ReplaceNameBased );
	}





	[StructLayout( LayoutKind.Sequential , CharSet = CharSet.Auto )]
	public class OpenFileName
	{
		public int structSize = 0;
		private IntPtr dlgOwner = IntPtr.Zero;
		private IntPtr instance = IntPtr.Zero;
		public String filter = null;
		public String customFilter = null;
		public int maxCustFilter = 0;
		public int filterIndex = 0;
		public String file = null;
		public int maxFile = 0;
		public String fileTitle = null;
		public int maxFileTitle = 0;
		public String initialDir = null;
		public String title = null;
		public int flags = 0;
		public short fileOffset = 0;
		public short fileExtension = 0;
		public String defExt = null;
		private IntPtr custData = IntPtr.Zero;
		private IntPtr hook = IntPtr.Zero;
		public String templateName = null;
		private IntPtr reservedPtr = IntPtr.Zero;
		public int reservedInt = 0;
		public int flagsEx = 0;
	}
	[DllImport( "Comdlg32.dll" , CharSet = CharSet.Auto )]
	public static extern bool GetOpenFileName( [In, Out] OpenFileName ofn );


	public static string ShowSaveFileDialog()
	{
		OpenFileName ofn = new OpenFileName();
		ofn.structSize = Marshal.SizeOf( ofn );
		ofn.filter = "Text files\0*.txt\0";
		ofn.file = new String( new char[260] );
		ofn.maxFile = ofn.file.Length;
		ofn.title = "Save file";
		ofn.defExt = "txt";

		if( GetOpenFileName( ofn ) )
		{
			return ofn.file;
		}
		return null;
	}




	[MenuItem( "Assets/移除Image上的Raycast" , false , 2000 )]

	public static void RemoveImageRaycast()
	{
		if( null == Selection.activeObject )
		{
			return;
		}

		foreach( Object selectObject in Selection.objects )
		{
			string prefabPath = AssetDatabase.GetAssetPath( selectObject.GetInstanceID() );
			GameObject prefabObject = AssetDatabase.LoadAssetAtPath( prefabPath , typeof( GameObject ) ) as GameObject;

			List<Image> allImage = EditorUtil.GetAllChildComponent<Image>( prefabObject.transform );
			bool change = false;
			foreach( Image img in allImage )
			{
				if( img.raycastTarget )
				{
					img.raycastTarget = false;
					change = true;
				}
			}
			if( change )
			{
				EditorUtility.SetDirty( prefabObject );//标记已改变
			}
		}

		AssetDatabase.SaveAssets();
	}




	/// <summary>
	/// 递归得到prefab中所使用的图片名字和对应节点的位置
	/// </summary>
	/// <param name="sb"></param>
	/// <param name="obj"></param>
	/// <param name="path"></param>
	private static void ShowImagePath( StringBuilder sb , Transform obj , string path )
	{
		if( null == obj )
		{
			return;
		}

		string nodeName = path + "/" + obj.name;

		Image img = obj.GetComponent<Image>();
		if( ( null != img ) && ( null != img.sprite ) )
		{
			sb.AppendFormat( "{0} \t\t {1}\n" , img.sprite.name , nodeName );
		}

		Button btn = obj.GetComponent<Button>();
		if( null != btn )
		{
			if( null != btn.spriteState.pressedSprite )
			{
				sb.AppendFormat( "{0} \t\t {1}\n" , btn.spriteState.pressedSprite.name , nodeName );
			}
			if( null != btn.spriteState.disabledSprite )
			{

				sb.AppendFormat( "{0} \t\t {1}\n" , btn.spriteState.disabledSprite.name , nodeName );
			}
			if( null != btn.spriteState.highlightedSprite )
			{

				sb.AppendFormat( "{0} \t\t {1}\n" , btn.spriteState.highlightedSprite.name , nodeName );
			}
		}

		Text txt = obj.GetComponent<Text>();
		if( ( null != txt ) && ( null != txt.font ) && ( "Arial" != txt.font.name ) && ( "Arial_Bold" != txt.font.name ) )
		{
			sb.AppendFormat( "[{0}] \t\t {1}\n" , txt.font.name , nodeName );
		}

		TMPro.TextMeshProUGUI textPro = obj.GetComponent<TMPro.TextMeshProUGUI>();
		if( null != textPro )
		{
			sb.AppendFormat( "[{0} MeshPro] \t\t {1}\n" , textPro.font.name , nodeName );
		}

		if( obj.childCount > 0 )
		{
			for( int i = 0 ; i < obj.childCount ; i++ )
			{
				ShowImagePath( sb , obj.GetChild( i ) , nodeName );
			}
		}

	}



	[MenuItem( "Assets/显示 Prefab 使用的[图片 字体]资源(节点信息)" , false , 2001 )]
	public static void GetPrefabImageTree()
	{
		if( null == Selection.activeObject )
		{
			return;
		}

		StringBuilder sb = new StringBuilder();
		string prefabPath = AssetDatabase.GetAssetPath( Selection.activeObject.GetInstanceID() );
		sb.AppendFormat( "{0}:\n" , prefabPath );

		GameObject prefabObject = AssetDatabase.LoadAssetAtPath( prefabPath , typeof( GameObject ) ) as GameObject;

		for( int i = 0 ; i < prefabObject.transform.childCount ; i++ )
		{
			ShowImagePath( sb , prefabObject.transform.GetChild( i ) , prefabObject.name );
		}

		Debug.Log( sb );
	}


	[MenuItem( "Assets/显示 Prefab 使用的图片资源" , false , 2005 )]
	public static void GetPrefabUseResource()
	{
		if( null == Selection.activeObject )
		{
			return;
		}

		List<string> dependres = new List<string>();
		List<string> prefabPaths = new List<string>();
		foreach( Object selectObject in Selection.objects )
		{
			string prefabPath = AssetDatabase.GetAssetPath( selectObject.GetInstanceID() );
			prefabPaths.Add( prefabPath );
			dependres.AddRange( AssetDatabase.GetDependencies( prefabPath ) );
		}

		if( 0 == dependres.Count )
		{
			return;
		}

		StringBuilder sb = new StringBuilder();
		sb.AppendFormat( "{0}:Use Resource:\n" , prefabPaths.JoinToString( "," ) );

		dependres.Sort();
		dependres = dependres.Distinct().ToList();

		ImageBorwse window = (ImageBorwse)EditorWindow.GetWindow( typeof( ImageBorwse ) );
		window.SetImagePath( dependres );
		window.Show();

		foreach( string value in dependres )
		{
			if( prefabPaths.Contains( value ) )
			{
				continue;
			}

			sb.AppendFormat( "\t{0}\n" , value );
		}
		Debug.Log( sb );
	}


	private class PictureDepends
	{
		public string objPath;
		public StringBuilder sb;
		public int count;
		public string output;
	}


	[MenuItem( "Assets/查找引用此资源的 Prefab" , false , 2010 )]
	public static void GetAllPrefabDepend()
	{
		if( null == Selection.activeObject )
		{
			return;
		}

		Dictionary<string , PictureDepends> objPaths = new Dictionary<string , PictureDepends>();

		foreach( Object selectObject in Selection.objects )
		{
			string objPath = AssetDatabase.GetAssetPath( selectObject.GetInstanceID() );
			if( objPaths.ContainsKey( objPath ) )
			{
				continue;
			}
			PictureDepends value = new PictureDepends();
			value.objPath = objPath;
			value.sb = new StringBuilder();
			value.count = 0;
			value.sb.AppendFormat( "{0}:Dependencies\n" , objPath );
			objPaths.Add( objPath , value );
		}


		List<string> prefabNames = GetAlluiPrefab( true );

		foreach( string prefabName in prefabNames.Distinct() )
		{
			var same_res = objPaths.Keys.Intersect( AssetDatabase.GetDependencies( prefabName ) );

			foreach( var res in same_res )
			{
				PictureDepends tmp = null;
				if( objPaths.TryGetValue( res , out tmp ) )
				{
					tmp.sb.AppendFormat( "\t{0}\n" , prefabName );
					tmp.count++;
				}
			}
		}

		List<PictureDepends> result = objPaths.Values.ToList();
		result.ForEach( e => e.output = e.sb.ToString() );
		result.Sort( ( l , r ) =>
		 {
			 if( l.count == r.count )
			 {
				 return l.output.CompareTo( r.output );
			 }
			 return l.count.CompareTo( r.count );
		 } );

		StringBuilder sbout = new StringBuilder();
		foreach( var value in result )
		{
			sbout.Append( value.output );
		}

		string output = sbout.ToString();
		EditorUtil.CopyTextToClipboard( output );
		EditorUtil.SplitLog( Debug.Log , output );
	}



	private static string GetAssetBundleNameByPrefabFilePath( string path )
	{
		string assetBundleName = null;
		try
		{
			assetBundleName = System.Text.RegularExpressions.Regex.Match( path , @"([^_\\/]+)_(\w+)\.prefab" ).Groups[1].Value;
			return assetBundleName;
		}
		catch( ArgumentException ex )
		{
			return null;
		}
	}

	private static string GetAssetBundleNameByImagePath( string path )
	{
		string resultString = null;
		try
		{
			resultString = System.Text.RegularExpressions.Regex.Match( path , @"Assets/Art/UI(?:Addon)?/(?:Texture/)?([^_/]+)(_[\w]+)*/" ).Groups[1].Value;
			return resultString;
		}
		catch( ArgumentException ex )
		{
			return null;
		}
	}




	[MenuItem( "Tools/Prefab 错误引用资源分析" , false , 3000 )]
	public static void PrefabAnalyzeImage()
	{
		PrefabAnalyze( false );
	}

	/// <summary>
	/// prefab引用资源规则，common和effect 都可以引用
	/// </summary>
	/// <param name="imageGroup"></param>
	/// <param name="prefabGroup"></param>
	/// <returns></returns>
	private static bool CheckImageAndPrefabGroup( string imageGroup , string prefabGroup )
	{
		if( imageGroup == prefabGroup )
		{
			return true;
		}
		if( "Font" == imageGroup
			|| "common" == imageGroup
			|| "effect" == imageGroup
			)
		{
			return true;
		}

		return false;
	}



	static HashSet<string> prefaLogGroup = new HashSet<string> { "items" };

	private static bool WarrningPrefabGroup( string prefabGroup )
	{
		return !prefaLogGroup.Contains( prefabGroup );
	}

	private static string listString2Str( List<string> list )
	{
		StringBuilder sbout = new StringBuilder();
		list.ForEach( e => sbout.AppendLine( e ) );
		string output = sbout.ToString();
		return output;
	}

	public static void PrefabAnalyze( bool allType )
	{
		Dictionary<string , List<string>> data = getPrefabDependImage( allType );

		List<string> list = new List<string>();
		List<string> listWarrning = new List<string>();
		List<string> listError = new List<string>();

		foreach( var value in data )
		{
			List<string> prefabList = value.Value;
			if( null == prefabList || ( 0 == prefabList.Count ) )
			{
				continue;
			}

			string imageGroup = GetAssetBundleNameByImagePath( value.Key );

			// 图片和所在的prefab都在一个组里面
			if( prefabList.TrueForAll( e => CheckImageAndPrefabGroup( imageGroup , GetAssetBundleNameByPrefabFilePath( e ) ) ) )
			{
				continue;
			}

			string key = value.Key;
			string str = string.Format( "{0}\t{1}" , key , prefabList.Select( e => Path.GetFileName( e ) ).JoinToString( "," ) );

			if( key.Contains( "Effect/Texture" ) || key.Contains( "Spine" ) )
			{
				list.Add( str );
				continue;
			}

			if( key.Contains( "UIAddon" ) )
			{
				listError.Add( str );
				continue;
			}

			if( WarrningPrefabGroup( imageGroup ) )
			{
				listWarrning.Add( str );
			}
			else
			{
				list.Add( str );
			}
		}

		list.Sort();
		listWarrning.Sort();
		listError.Sort();

		string errorOutput = listString2Str( listError );

		EditorUtil.CopyTextToClipboard( errorOutput );
		EditorUtil.SplitLog( Debug.Log , listString2Str( list ) );
		EditorUtil.SplitLog( Debug.LogError , errorOutput );
		EditorUtil.SplitLog( Debug.LogWarning , listString2Str( listWarrning ) );
	}


	/// <summary>
	/// 得到所有prefab上依赖的图片资源
	/// </summary>
	/// <param name="allType"></param>
	/// <returns></returns>
	private static Dictionary<string , List<string>> getPrefabDependImage( bool allType )
	{
		Dictionary<string , List<string>> data = new Dictionary<string , List<string>>();
		List<string> prefabNames = GetAlluiPrefab();
		foreach( string prefabName in prefabNames.Distinct() )
		{
			string[] imgs = AssetDatabase.GetDependencies( prefabName );
			foreach( var img in imgs )
			{
				if( img.EndsWith( ".prefab" ) )
				{
					continue;
				}

				if( allType )
				{
					if( img.EndsWith( ".shader" ) || img.EndsWith( ".cs" ) )
					{
						continue;
					}
				}
				else
				{
					if( img.EndsWith( ".png" ) || img.EndsWith( ".jpg" ) || img.EndsWith( ".mat" ) )
					{
					}
					else
					{
						continue;
					}
				}

				List<string> value;
				if( !data.TryGetValue( img , out value ) )
				{
					value = new List<string>();
					data.Add( img , value );
				}
				value.Add( prefabName );
			}
		}
		return data;
	}

	public static void RelaceImageAtPrefabPath( string oldPath , string newPath , string prefabPath , Predicate<Image> act = null )
	{
		string[] prefabNames = Directory.GetFiles( prefabPath , "*.prefab" , SearchOption.TopDirectoryOnly );
		foreach( string prefabName in prefabNames )
		{
			RelaceImageAtPrefab( oldPath , newPath , prefabName , act );
		}
	}



	public static void RelaceImageAtPrefab( string oldPath , string newPath , string prefabPath , Predicate<Image> act )
	{
		GameObject prefabObject = AssetDatabase.LoadAssetAtPath( prefabPath , typeof( GameObject ) ) as GameObject;
		GameObject temp = PrefabUtility.InstantiatePrefab( prefabObject ) as GameObject;
		bool isReplace = RelaceImageAtGameObject( oldPath , newPath , prefabPath , temp , act );
		if( isReplace )
		{
			PrefabUtility.ReplacePrefab( temp , prefabObject , ReplacePrefabOptions.ReplaceNameBased );
		}
		Object.DestroyImmediate( temp );
	}


	public static bool RelaceImageAtGameObject( string oldPath , string newPath , string prefabPath , GameObject obj , Predicate<Image> act )
	{
		bool isChange = false;

		List<Image> allImage = EditorUtil.GetAllChildComponent<Image>( obj.transform );

		foreach( Image image in allImage )
		{
			string objPath = AssetDatabase.GetAssetPath( image.sprite.GetInstanceID() );
			//sb.AppendLine(objPath);

			if( objPath == oldPath )
			{
				if( oldPath != newPath )
				{
					Sprite newSprite = AssetDatabase.LoadAssetAtPath( newPath , typeof( Sprite ) ) as Sprite;
					image.sprite = newSprite;
					Debug.LogFormat( "{0} : {1}" , prefabPath , EditorUtil.GetFullPath( image.transform , obj.transform ) );
					isChange = true;
				}
				if( act != null && act( image ) )
				{
					Debug.LogFormat( "{0} : {1}" , prefabPath , EditorUtil.GetFullPath( image.transform , obj.transform ) );
					isChange = true;
				}
			}
		}


		List<Button> allButton = EditorUtil.GetAllChildComponent<Button>( obj.transform );

		foreach( Button btn in allButton )
		{
			SpriteState spriteState = btn.spriteState;
			string objPath = AssetDatabase.GetAssetPath( spriteState.pressedSprite.GetInstanceID() );

			//sb.AppendLine(objPath);
			if( objPath == oldPath )
			{
				if( oldPath != newPath )
				{
					Sprite newSprite = AssetDatabase.LoadAssetAtPath( newPath , typeof( Sprite ) ) as Sprite;
					spriteState.pressedSprite = newSprite;
					Debug.LogFormat( "{0} : {1}" , prefabPath , EditorUtil.GetFullPath( btn.transform , obj.transform ) );
					isChange = true;
				}
			}

			objPath = AssetDatabase.GetAssetPath( spriteState.highlightedSprite.GetInstanceID() );
			//sb.AppendLine(objPath);
			if( objPath == oldPath )
			{
				if( oldPath != newPath )
				{
					Sprite newSprite = AssetDatabase.LoadAssetAtPath( newPath , typeof( Sprite ) ) as Sprite;
					spriteState.highlightedSprite = newSprite;
					Debug.LogFormat( "{0} : {1}" , prefabPath , EditorUtil.GetFullPath( btn.transform , obj.transform ) );
					isChange = true;
				}
			}

			objPath = AssetDatabase.GetAssetPath( spriteState.disabledSprite.GetInstanceID() );
			//sb.AppendLine(objPath);
			if( objPath == oldPath )
			{
				if( oldPath != newPath )
				{
					Sprite newSprite = AssetDatabase.LoadAssetAtPath( newPath , typeof( Sprite ) ) as Sprite;
					spriteState.disabledSprite = newSprite;
					Debug.LogFormat( "{0} : {1}" , prefabPath , EditorUtil.GetFullPath( btn.transform , obj.transform ) );
					isChange = true;
				}
			}

			btn.spriteState = spriteState;
		}

		//Debug.Log(sb);
		return isChange;
	}




	[MenuItem( "Tools/Prefab/统计Texture在那些Prefab中使用" , false , 3000 )]
	/// <summary>
	/// 统计Texture在那些Prefab中使用
	/// </summary>
	public static void UseTextureLOG()
	{
		Dictionary<string , List<string>> textureOnUsePrefab = new Dictionary<string , List<string>>();

		List<string> prefabNames = GetAlluiPrefab( true );

		foreach( string prefabName in prefabNames )
		{
			string[] dependsRes = AssetDatabase.GetDependencies( prefabName );

			foreach( string res in dependsRes )
			{
				if( res.EndsWith( ".prefab" ) )
				{
					continue;
				}

				if( textureOnUsePrefab.ContainsKey( res ) )
				{
					textureOnUsePrefab[res].Add( prefabName );
				}
				else
				{
					textureOnUsePrefab[res] = new List<string> { prefabName };
				}
			}
		}

		StringBuilder sb = new StringBuilder();
		List<string> info = new List<string>();


		foreach( KeyValuePair<string , List<string>> value in textureOnUsePrefab )
		{
			if( value.Value.Count == 1 )
			{
				info.Add( string.Format( "{0}\t[{1}]" , value.Key.Replace( '/' , '\\' ) , value.Value.JoinToString( " , " ) ) );
			}
		}

		info.Sort();

		File.WriteAllLines( Path.Combine( Application.dataPath , "..\\TextureUseInfo.txt" ) , info.ToArray() , Encoding.UTF8 );
		Debug.Log( "TextureUse Info Success!" );
	}



	[MenuItem( "Tools/Prefab/统计所有Prefab使用的Texture情况" , false , 3001 )]
	/// <summary>
	/// 统计所有Prefab使用的Texture情况
	/// 那些图片使用的多少次,那些图片未使用
	/// </summary>
	public static void AllUseTexture()
	{
		List<string> allTexture = GetAllTexture();
		allTexture.Sort();

		allTexture = allTexture.Select( e => e.Replace( '/' , '\\' ) ).ToList();

		List<string> prefabNames = GetAlluiPrefab();


		List<string> alluseTexture = new List<string>();
		foreach( string prefabName in prefabNames )
		{
			string[] dependsRes = AssetDatabase.GetDependencies( prefabName );
			alluseTexture.AddRange( dependsRes );
		}

		alluseTexture = alluseTexture.Select( e => e.Replace( '/' , '\\' ) ).ToList();
		alluseTexture.Sort();

		Dictionary<string , int> dictionary = new Dictionary<string , int>();

		foreach( string s in alluseTexture )
		{
			int c;
			if( dictionary.TryGetValue( s , out c ) )
			{
				dictionary[s] = c + 1;
			}
			else
			{
				dictionary[s] = 1;
			}
		}

		alluseTexture = alluseTexture.Distinct().ToList();

		File.WriteAllLines( Path.Combine( Application.dataPath , "..\\count.txt" ) , dictionary.Select( e => string.Format( "{0:D3}|{1}" , e.Value , e.Key ) ).ToArray() , Encoding.UTF8 );

		File.WriteAllLines( Path.Combine( Application.dataPath , "..\\allTexture.txt" ) , allTexture.ToArray() , Encoding.UTF8 );
		File.WriteAllLines( Path.Combine( Application.dataPath , "..\\useTexture.txt" ) , alluseTexture.ToArray() , Encoding.UTF8 );

		var unuseTexture = allTexture.Except( alluseTexture );
		File.WriteAllLines( Path.Combine( Application.dataPath , "..\\unuseTexture.txt" ) , unuseTexture.ToArray() , Encoding.UTF8 );

		Debug.Log( "AllUseTexture Success!" );
	}




	public static string PathCombine( params string[] path )
	{
		string ret = path[0];
		for( int i = 1 ; i < path.Length ; i++ )
		{
			ret = System.IO.Path.Combine( ret , path[i] );
		}
		return ret;
	}



}