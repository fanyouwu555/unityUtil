using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System.Linq;
using Spine.Unity;
using System.Text;

public class GenerateSpinePrefab : EditorWindow
{
	public static readonly string PrefabPath = @"Assets/Art/Spine/prefab_model.prefab";
	private static string assetsRoot = "Assets/";
	private string spinePath = @"Art/Spine/";
	private string prefabGeneratePath = @"Art/Prefab/";

	[MenuItem( "Tools/Generate Spine prefab" )]
	static void Init()
	{

		GenerateSpinePrefab window = (GenerateSpinePrefab)EditorWindow.GetWindow( typeof( GenerateSpinePrefab ) );
		window.Show();
	}


	void OnGUI()
	{
		GUI.skin.button.fontSize = 12;
		GUI.skin.label.fontSize = 12;

		GUILayout.Label( "Generate Spine" );
		GUILayout.BeginHorizontal();
		if( GUILayout.Button( "spine path" , GUILayout.Width( 200 ) ) )
		{
			{
				spinePath = getSelectFolder();
			}
		}
		GUILayout.Label( "Spine Resource Path:" + spinePath );
		GUILayout.EndHorizontal();



		GUILayout.BeginHorizontal();
		if( GUILayout.Button( "prefab generate path " , GUILayout.Width( 200 ) ) )
		{
			{
				prefabGeneratePath = getSelectFolder();
			}
		}
		GUILayout.Label( "prefab Generate Path:" + prefabGeneratePath );
		GUILayout.EndHorizontal();


		if( GUILayout.Button( "Generate Prefab" ) )
		{
			Generate( spinePath , prefabGeneratePath );
		}

	}

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

	private static List<string> GetAllSpineAsset( string[] SpineDir )
	{
		return GetFileInPath( SpineDir , SearchOption.AllDirectories , "skel.bytes" );

	}


	public static void Generate( string spinePath , string genPath )
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine( "Generate Spine prefab:" );

		StringBuilder es = new StringBuilder();
		es.AppendLine( "Delete Exists Spine prefab:" );

		StringBuilder err = new StringBuilder();
		err.AppendLine( "error Spine Data:" );

		string TargetPath = assetsRoot + genPath;
		string[] SpineDir = new string[] { spinePath };
		List<string> listPath = GetAllSpineAsset( SpineDir );
		foreach( string asssetPath in listPath )
		{
			string dirName = asssetPath.Remove( asssetPath.LastIndexOf( ".skel.bytes" ) );
			string sname = Path.GetFileNameWithoutExtension( asssetPath );
			if( File.Exists( TargetPath + sname + ".prefab" ) )
			{
				es.AppendFormat( " \t{0}\n" , sname );
				File.Delete( TargetPath + sname + ".prefab" );
				//continue;
			}
			string assetData = string.Concat( dirName , "_SkeletonData.asset" );
			bool isSucc = GeneratePrefab( assetData , sname , TargetPath );
			if( isSucc )
				sb.AppendFormat( "\t{0}\n" , sname );
			else
				err.AppendFormat( "\t{0}\n" , sname );

		}
		Debug.Log( sb.ToString() + es.ToString() + err.ToString() );
	}

	private static bool GeneratePrefab( string assetData , string sname , string TargetPath )
	{
		SkeletonDataAsset skeletonData = AssetDatabase.LoadAssetAtPath( assetData , typeof( SkeletonDataAsset ) ) as SkeletonDataAsset;
		if( !skeletonData )
		{
			return false;
		}
		GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>( PrefabPath );
		GameObject cloneObj = GameObject.Instantiate<GameObject>( modelPrefab );
		SkeletonGraphic data = cloneObj.transform.GetComponent<SkeletonGraphic>();
		data.skeletonDataAsset = skeletonData;
		data.startingAnimation = "Idle";
		sname =sname.Split('.')[0];
		string genPrefabFullName = string.Concat( TargetPath , sname , ".prefab" );
		PrefabUtility.SaveAsPrefabAsset( cloneObj , genPrefabFullName );
		GameObject.DestroyImmediate( cloneObj );
		return true;
	}


	public static string getSelectFolder()
	{

		string path = assetsRoot;
		foreach( UnityEngine.Object obj in Selection.GetFiltered( typeof( UnityEngine.Object ) , SelectionMode.Assets ) )
		{
			path = AssetDatabase.GetAssetPath( obj );
			if( !string.IsNullOrEmpty( path ) && File.Exists( path ) )
			{
				path = Path.GetDirectoryName( path );
				break;
			}
		}

		path = path.Remove( 0 , assetsRoot.Length ).Replace( "\\" , "/" );
		if( !path.EndsWith( "/" ) )
		{
			path += '/';
		}
		return path;
	}


}
