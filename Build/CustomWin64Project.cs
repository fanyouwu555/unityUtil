using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

public class CustomWin64Project
{

	[PostProcessBuild( 1000 )]
	public static void RunRemoteWin64Build( BuildTarget buildTarget , string pathToBuiltProject )
	{
		if( buildTarget != BuildTarget.StandaloneWindows64 )
		{
			return;
		}

		string projectDir = EditorUtil.GetProjectDir();
		string targetDir = Path.GetFileNameWithoutExtension( pathToBuiltProject );
		System.Diagnostics.Process p = new System.Diagnostics.Process();
		p.StartInfo.WorkingDirectory = projectDir;
		p.StartInfo.FileName = System.IO.Path.Combine( projectDir , "remote_win64_build.bat" );
		p.StartInfo.Arguments = targetDir;
		p.Start();

	}

	/// <summary>
	/// 删除 win64 资源中的 Manifest 文件 , 
	/// </summary>
	/// <param name="buildTarget"></param>
	/// <param name="pathToBuiltProject"></param>
	[PostProcessBuild( 1 )]
	public static void DeleteManifest( BuildTarget buildTarget , string pathToBuiltProject )
	{
		if( buildTarget != BuildTarget.StandaloneWindows64 )
		{
			return;
		}

		string path = pathToBuiltProject.Replace(".exe","_Data/StreamingAssets");
		File.Delete( Path.Combine( path , "StreamingAssets" ) );

		foreach( var file in Directory.GetFiles( path , "*.manifest" , SearchOption.AllDirectories ) )
		{
			File.Delete( file );
		};
	}




}
