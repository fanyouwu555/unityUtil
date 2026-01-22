using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

public static class AtlasPacker
{
	private static string UI_ROOT_PATH = "Assets/GameAssets/Launcher/Res/UI/";
	private static string ATLAS_OUTPUT_PATH = "Assets/GameAssets/Launcher/Res/SpriteAtlas/";

	[MenuItem("Tools/打包UI图集")]
	public static void PackAllUIFolders()
	{
		try
		{
			// 检查源目录是否存在
			if(!Directory.Exists(UI_ROOT_PATH))
			{
				Debug.LogError($"UI目录不存在: {UI_ROOT_PATH}");
				EditorUtility.DisplayDialog("错误" , $"UI目录不存在:\n{UI_ROOT_PATH}" , "确定");
				return;
			}

			// 确保输出目录存在
			EnsureOutputDirectory();

			// 获取UI目录下所有子文件夹
			DirectoryInfo uiDir = new DirectoryInfo(UI_ROOT_PATH);
			DirectoryInfo[] subFolders = uiDir.GetDirectories();

			if(subFolders.Length == 0)
			{
				Debug.LogWarning($"UI目录下没有子文件夹: {UI_ROOT_PATH}");
				EditorUtility.DisplayDialog("提示" , $"UI目录下没有子文件夹:\n{UI_ROOT_PATH}" , "确定");
				return;
			}

			Debug.Log($"开始打包UI图集，共发现 {subFolders.Length} 个文件夹");

			int processedCount = 0;
			List<string> createdAtlases = new List<string>();

			foreach(DirectoryInfo folder in subFolders)
			{
				try
				{
					// 更新进度条
					float progress = (float)processedCount / subFolders.Length;
					if(EditorUtility.DisplayCancelableProgressBar("打包UI图集" ,
						$"正在处理: {folder.Name} ({processedCount + 1}/{subFolders.Length})" , progress))
					{
						Debug.Log("用户取消了打包操作");
						break;
					}

					// 打包单个文件夹
					if(PackFolderToAtlas(folder))
					{
						createdAtlases.Add(folder.Name);
						processedCount++;
					}
				}
				catch(Exception ex)
				{
					Debug.LogError($"处理文件夹 {folder.Name} 时发生错误: {ex.Message}\n{ex.StackTrace}");
				}
			}

			EditorUtility.ClearProgressBar();

			// 重新导入所有相关资源
			AssetDatabase.Refresh();

			// 显式打包所有图集
			PackAllAtlases();

			Debug.Log($"UI图集打包完成！共处理 {processedCount} 个文件夹，创建了 {createdAtlases.Count} 个图集");

			// 显示结果
			if(createdAtlases.Count > 0)
			{
				string resultMessage = $"成功创建了 {createdAtlases.Count} 个图集:\n\n";
				foreach(string atlasName in createdAtlases)
				{
					resultMessage += $"• {atlasName}.spriteatlas\n";
				}
				EditorUtility.DisplayDialog("打包完成" , resultMessage , "确定");
			}
		}
		catch(Exception ex)
		{
			EditorUtility.ClearProgressBar();
			Debug.LogError($"打包图集时发生未预期的错误: {ex.Message}\n{ex.StackTrace}");
			EditorUtility.DisplayDialog("错误" , $"打包图集时发生错误:\n{ex.Message}" , "确定");
		}
	}

	/// <summary>
	/// 打包单个文件夹到图集
	/// </summary>
	private static bool PackFolderToAtlas( DirectoryInfo folder )
	{
		string folderName = folder.Name;
		string atlasName = folderName;
		string atlasPath = ATLAS_OUTPUT_PATH + atlasName + ".spriteatlas";

		Debug.Log($"开始处理文件夹: {folderName}");

		// 收集文件夹中的所有图片
		List<string> imagePaths = CollectAllImagesInFolder(folder.FullName);

		if(imagePaths.Count == 0)
		{
			Debug.LogWarning($"文件夹 {folderName} 中没有找到图片文件");
			return false;
		}

		Debug.Log($"在文件夹 {folderName} 中找到 {imagePaths.Count} 张图片");

		// 加载或创建图集
		SpriteAtlas atlas = LoadOrCreateAtlas(atlasPath , atlasName);

		if(atlas == null)
		{
			Debug.LogError($"创建图集 {atlasName} 失败");
			return false;
		}

		// 更新图集内容
		UpdateAtlasContents(atlas , imagePaths , folderName);

		// 设置图集属性
		ConfigureAtlasSettings(atlas);

		// 确保图片导入设置正确
		EnsureSpriteImportSettings(imagePaths);

		// 保存图集
		EditorUtility.SetDirty(atlas);
		AssetDatabase.SaveAssets();

		Debug.Log($"成功创建图集: {atlasName}.spriteatlas ({imagePaths.Count} 张图片)");
		return true;
	}

	/// <summary>
	/// 确保图片的导入设置正确（关键修复）
	/// </summary>
	private static void EnsureSpriteImportSettings( List<string> imagePaths )
	{
		foreach(string imagePath in imagePaths)
		{
			TextureImporter importer = AssetImporter.GetAtPath(imagePath) as TextureImporter;
			if(importer != null)
			{
				bool needsReimport = false;

				// 确保图片类型设置为Sprite
				if(importer.textureType != TextureImporterType.Sprite)
				{
					importer.textureType = TextureImporterType.Sprite;
					needsReimport = true;
				}

				// 设置sprite导入模式
				if(importer.spriteImportMode == SpriteImportMode.None)
				{
					importer.spriteImportMode = SpriteImportMode.Single;
					needsReimport = true;
				}

				// 确保图集标签正确
				if(string.IsNullOrEmpty(importer.spritePackingTag))
				{
					importer.spritePackingTag = "";
					needsReimport = true;
				}

				// 如果设置了需要重新导入
				if(needsReimport)
				{
					importer.SaveAndReimport();
				}
			}
		}
	}

	/// <summary>
	/// 收集文件夹中的所有图片文件
	/// </summary>
	private static List<string> CollectAllImagesInFolder( string folderPath )
	{
		List<string> imagePaths = new List<string>();

		if(!Directory.Exists(folderPath))
		{
			return imagePaths;
		}

		// 递归搜索所有图片文件
		SearchForImages(folderPath , imagePaths);

		return imagePaths;
	}

	/// <summary>
	/// 递归搜索图片文件
	/// </summary>
	private static void SearchForImages( string currentPath , List<string> imagePaths )
	{
		// 搜索当前目录的所有文件
		string[] files = Directory.GetFiles(currentPath);
		foreach(string file in files)
		{
			string extension = Path.GetExtension(file).ToLower();
			if(IsImageFile(extension))
			{
				// 修复路径转换问题
				string relativePath = ConvertToAssetPath(file);
				if(!string.IsNullOrEmpty(relativePath))
				{
					imagePaths.Add(relativePath);
				}
			}
		}

		// 递归搜索子目录
		string[] subDirs = Directory.GetDirectories(currentPath);
		foreach(string subDir in subDirs)
		{
			SearchForImages(subDir , imagePaths);
		}
	}

	/// <summary>
	/// 将完整文件路径转换为Unity Asset路径
	/// </summary>
	private static string ConvertToAssetPath( string fullPath )
	{
		// 统一路径分隔符
		fullPath = fullPath.Replace('\\' , '/');
		string dataPath = Application.dataPath.Replace('\\' , '/');

		// 检查路径是否在Assets目录下
		if(fullPath.StartsWith(dataPath))
		{
			// 获取相对路径
			string relativePath = fullPath.Substring(dataPath.Length);

			// 确保路径以"Assets/"开头
			if(!relativePath.StartsWith("/"))
			{
				relativePath = "/" + relativePath;
			}

			return "Assets" + relativePath;
		}

		Debug.LogWarning($"文件不在Assets目录下: {fullPath}");
		return null;
	}

	/// <summary>
	/// 检查文件是否是图片
	/// </summary>
	private static bool IsImageFile( string extension )
	{
		return extension == ".png" ||
			   extension == ".jpg" ||
			   extension == ".jpeg" ||
			   extension == ".tga" ||
			   extension == ".psd" ||
			   extension == ".bmp" ||
			   extension == ".tiff";
	}

	/// <summary>
	/// 加载或创建图集
	/// </summary>
	private static SpriteAtlas LoadOrCreateAtlas( string atlasPath , string atlasName )
	{
		// 尝试加载现有的图集
		SpriteAtlas existingAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);

		if(existingAtlas != null)
		{
			Debug.Log($"找到已存在的图集: {atlasName}");
			return existingAtlas;
		}

		// 创建新的图集
		SpriteAtlas newAtlas = new SpriteAtlas();
		newAtlas.name = atlasName;

		AssetDatabase.CreateAsset(newAtlas , atlasPath);
		AssetDatabase.SaveAssets();

		Debug.Log($"创建新图集: {atlasName}");
		return newAtlas;
	}

	/// <summary>
	/// 更新图集内容
	/// </summary>
	private static void UpdateAtlasContents( SpriteAtlas atlas , List<string> imagePaths , string folderName )
	{
		// 清空图集现有内容
		Object[] existingPackables = atlas.GetPackables();
		if(existingPackables != null && existingPackables.Length > 0)
		{
			atlas.Remove(existingPackables);
			Debug.Log($"清空图集 {folderName} 的现有内容 ({existingPackables.Length} 个对象)");
		}

		// 添加新图片到图集
		int addedCount = 0;
		List<Object> spriteObjects = new List<Object>();

		foreach(string imagePath in imagePaths)
		{
			// 关键：使用Sprite类型加载图片
			Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
			if(sprite != null)
			{
				spriteObjects.Add(sprite);
				addedCount++;
				Debug.Log($"成功加载图片: {imagePath}");
			}
			else
			{
				// 如果无法加载为Sprite，尝试加载为Texture2D并转换
				Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
				if(texture != null)
				{
					// 在Unity 2022中，也可以直接添加Texture2D
					spriteObjects.Add(texture);
					addedCount++;
					Debug.Log($"加载Texture2D: {imagePath}");
				}
				else
				{
					Debug.LogWarning($"无法加载图片: {imagePath}");
				}
			}
		}

		if(spriteObjects.Count > 0)
		{
			atlas.Add(spriteObjects.ToArray());
			Debug.Log($"向图集 {folderName} 中添加了 {addedCount} 张图片");
		}
		else
		{
			Debug.LogWarning($"没有找到可以添加到图集的图片: {folderName}");
		}
	}

	/// <summary>
	/// 配置图集设置
	/// </summary>
	private static void ConfigureAtlasSettings( SpriteAtlas atlas )
	{
		// 打包设置
		SpriteAtlasPackingSettings packSettings = new SpriteAtlasPackingSettings
		{
			blockOffset = 1 ,
			enableRotation = false ,
			enableTightPacking = true ,  // 启用紧密打包
			padding = 4 ,
		};
		atlas.SetPackingSettings(packSettings);

		// 纹理设置
		SpriteAtlasTextureSettings textureSettings = new SpriteAtlasTextureSettings
		{
			readable = false ,
			generateMipMaps = false ,
			sRGB = true ,
			filterMode = FilterMode.Bilinear ,
		};
		atlas.SetTextureSettings(textureSettings);

		// 平台设置 - iOS
		ConfigurePlatformSettings(atlas , BuildTarget.iOS , "iPhone" ,
			TextureImporterFormat.PVRTC_RGB4);

		// 平台设置 - Android
		ConfigurePlatformSettings(atlas , BuildTarget.Android , "Android" ,
			TextureImporterFormat.ASTC_6x6);

		// 平台设置 - PC
		ConfigurePlatformSettings(atlas , BuildTarget.StandaloneWindows , "Standalone" ,
			TextureImporterFormat.DXT5);
	}

	/// <summary>
	/// 配置平台设置
	/// </summary>
	private static void ConfigurePlatformSettings( SpriteAtlas atlas , BuildTarget target ,
		string platformName , TextureImporterFormat format )
	{
		TextureImporterPlatformSettings platformSettings = atlas.GetPlatformSettings(platformName);
		platformSettings.overridden = true;
		platformSettings.maxTextureSize = 2048;
		platformSettings.textureCompression = TextureImporterCompression.Compressed;
		platformSettings.format = format;
		atlas.SetPlatformSettings(platformSettings);
	}

	/// <summary>
	/// 确保输出目录存在
	/// </summary>
	private static void EnsureOutputDirectory()
	{
		if(!Directory.Exists(ATLAS_OUTPUT_PATH))
		{
			Directory.CreateDirectory(ATLAS_OUTPUT_PATH);
			Debug.Log($"创建输出目录: {ATLAS_OUTPUT_PATH}");
		}
	}

	/// <summary>
	/// 显式打包所有图集
	/// </summary>
	private static void PackAllAtlases()
	{
		try
		{
			string[] atlasFiles = Directory.GetFiles(ATLAS_OUTPUT_PATH , "*.spriteatlas");
			List<SpriteAtlas> atlases = new List<SpriteAtlas>();

			foreach(string file in atlasFiles)
			{
				string relativePath = "Assets" + file.Replace(Application.dataPath , "").Replace('\\' , '/');
				SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(relativePath);

				if(atlas != null)
				{
					atlases.Add(atlas);
				}
			}

			if(atlases.Count > 0)
			{
				// 使用SpriteAtlasUtility.PackAtlases方法
				SpriteAtlasUtility.PackAtlases(atlases.ToArray() , EditorUserBuildSettings.activeBuildTarget);
				Debug.Log($"已打包 {atlases.Count} 个图集");
			}
		}
		catch(Exception ex)
		{
			Debug.LogError($"打包图集时发生错误: {ex.Message}\n{ex.StackTrace}");
		}
	}

	/// <summary>
	/// 测试图集内容
	/// </summary>
	[MenuItem("Tools/打包UI图集/测试图集内容")]
	public static void TestAtlasContents()
	{
		string[] atlasFiles = Directory.GetFiles(ATLAS_OUTPUT_PATH , "*.spriteatlas");

		Debug.Log($"找到 {atlasFiles.Length} 个图集文件:");

		foreach(string file in atlasFiles)
		{
			string relativePath = "Assets" + file.Replace(Application.dataPath , "").Replace('\\' , '/');
			SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(relativePath);

			if(atlas != null)
			{
				Object[] packables = atlas.GetPackables();
				Debug.Log($"图集: {Path.GetFileName(file)}, 包含 {packables?.Length ?? 0} 个对象");

				if(packables != null && packables.Length > 0)
				{
					foreach(Object obj in packables)
					{
						Debug.Log($"  - {obj.name} ({obj.GetType().Name})");
					}
				}
				else
				{
					Debug.LogWarning($"图集 {atlas.name} 是空的！");
				}
			}
		}
	}

	/// <summary>
	/// 打开UI源目录
	/// </summary>
	[MenuItem("Tools/打包UI图集/打开UI目录")]
	public static void OpenUIDirectory()
	{
		string fullPath = Path.Combine(Application.dataPath , "GameAssets/Launcher/Res/UI");
		if(!Directory.Exists(fullPath))
		{
			Directory.CreateDirectory(fullPath);
		}
		EditorUtility.RevealInFinder(fullPath);
	}

	/// <summary>
	/// 打开图集输出目录
	/// </summary>
	[MenuItem("Tools/打包UI图集/打开图集目录")]
	public static void OpenAtlasDirectory()
	{
		string fullPath = Path.Combine(Application.dataPath , "GameAssets/Launcher/Res/SpriteAtlas");
		if(!Directory.Exists(fullPath))
		{
			Directory.CreateDirectory(fullPath);
		}
		EditorUtility.RevealInFinder(fullPath);
	}

	/// <summary>
	/// 清空所有图集
	/// </summary>
	[MenuItem("Tools/打包UI图集/清空所有图集")]
	public static void ClearAllAtlases()
	{
		if(!EditorUtility.DisplayDialog("确认" , "确定要清空所有图集吗？此操作不可恢复。" , "确定" , "取消"))
		{
			return;
		}

		try
		{
			if(Directory.Exists(ATLAS_OUTPUT_PATH))
			{
				string[] atlasFiles = Directory.GetFiles(ATLAS_OUTPUT_PATH , "*.spriteatlas");

				foreach(string file in atlasFiles)
				{
					string relativePath = "Assets" + file.Replace(Application.dataPath , "").Replace('\\' , '/');
					AssetDatabase.DeleteAsset(relativePath);
				}

				AssetDatabase.Refresh();
				Debug.Log($"清空了 {atlasFiles.Length} 个图集文件");
				EditorUtility.DisplayDialog("完成" , $"已清空 {atlasFiles.Length} 个图集文件" , "确定");
			}
		}
		catch(Exception ex)
		{
			Debug.LogError($"清空图集时发生错误: {ex.Message}");
			EditorUtility.DisplayDialog("错误" , $"清空图集时发生错误:\n{ex.Message}" , "确定");
		}
	}
}