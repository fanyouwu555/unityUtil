using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using System.Linq;
using System;
using UnityEngine.AI;
using UnityEngine.UI;

[InitializeOnLoad]
public class CustomHierarchy
{
    // 总的开关用于开启或关闭 显示图标以及彩色文字
    public static bool EnableCustomHierarchy = true;
    public static bool EnableCustomHierarchyLabel = true;

    static CustomHierarchy()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HierarchWindowOnGui;
    }

    // 用于覆盖原有文字的LabelStyle
    private static GUIStyle LabelStyle(Color color)
    {
        var style = new GUIStyle(((GUIStyle) "Label"))
        {
            padding =
            {
                left = EditorStyles.label.padding.left + 16,
                top = EditorStyles.label.padding.top + 1
            },
            normal =
            {
                textColor = color
            }
        };
        return style;
    }

    // 绘制Rect
    private static Rect CreateRect(Rect selectionRect,int index)
    {
        var rect = new Rect(selectionRect);
        rect.x += rect.width - 20 - (20 * index);
        rect.width = 18;
        return rect;
    }

    private static Rect CreateToggleRect(Rect selectionRect)
    {
        var rect = new Rect(selectionRect);
        rect.x += rect.width - 20;
        rect.width = 18;
        return rect;
    }

    // 绘制图标
    private static void DrawIcon<T>(Rect rect)
    {
        // 获得Unity内置的图标
        var icon = EditorGUIUtility.ObjectContent(null, typeof(T)).image;
        GUI.Label(rect, icon);
    }

    // 综合以上，根据类型，绘制图标和文字
    private static void DrawRectIcon<T>(Rect selectionRect, GameObject go,Color textColor, ref int order,ref GUIStyle style) where T : Component
    {
        //if (go.GetComponent<T>())
        if (go.HasComponent<T>(false)) // 使用扩展方法HasComponent
        {
            //// 绘制新的Label覆盖原有名字
            //if (EnableCustomHierarchyLabel)
            //{
            //    // 字体样式
            //    style = LabelStyle(textColor);
            //}

            // 图标的绘制排序
            order += 1;
            var rect = CreateRect(selectionRect, order);

            // 绘制图标
            DrawIcon<T>(rect);
        }
    }
    [MenuItem("GameObject/CopyName",false,-1)]
    static void CopyName()
    {
		var go = Selection.activeGameObject;
		var name = go.name;
		EditorGUIUtility.systemCopyBuffer = name;
	}
    // 绘制Hiercrch
    static void HierarchWindowOnGui(int instanceId, Rect selectionRect)
    {


    //    if (Event.current != null && selectionRect.Contains(Event.current.mousePosition)
	   //      && Event.current.button == 1 && Event.current.type <=  EventType.MouseUp)
	   //  {
	   //      GameObject obj = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
			 ////这里可以判断selectedGameObject的条件
	   //      if (obj)
	   //      {
    //            GUIUtility.systemCopyBuffer = obj.name;
	
	   //      }		
    //         return;
	   //  }
        if (!EnableCustomHierarchy) return;
        try
        {
            // CheckBox // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
            var rectCheck = CreateToggleRect(selectionRect);

            // 通过ID获得Obj
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            var go = (GameObject)obj;

            // 绘制Checkbox 
            go.SetActive(GUI.Toggle(rectCheck, go.activeSelf, string.Empty));

			//var rectIcon = CreateRect( selectionRect , 1 );
			//if( GUI.Button( rectIcon , AssetDatabase.LoadAssetAtPath<Texture>( "Assets/unity.png" ) ) )
			//{

			//}
			// -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- 
			//// 图标的序列号
			//var index = 0;
			//GUIStyle style = null;

			//         // is Static 
			//         if (go.isStatic)
			//         {
			//             index += 1;
			//	var rectIcon = CreateRect( selectionRect , index );
			//	GUI.Label( rectIcon , "S" );
			//}

			//// 文字颜色定义 
			//var colorText = new Color(0/255f,126/255f,0f);
			//var colorButton = new Color(255/255f,0/255f,0f);
			//var colorScrollRect = new Color(255/255f,0/255f,0f);
			//var colorImage = new Color(126/255f,126/255f,0f);

			//DrawRectIcon<Text>(selectionRect, go, colorText, ref index, ref style);
			//DrawRectIcon<Button>(selectionRect, go, colorButton, ref index, ref style);
			//DrawRectIcon<ScrollRect>(selectionRect, go, colorScrollRect, ref index, ref style);
			//DrawRectIcon<Image>(selectionRect, go, colorImage, ref index, ref style);

			// Draw //  -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- 
			// 可以在此修改，根据需要删减需要绘制的内容
			//// Renderer
			//// 绘制Label来覆盖原有的名字
			//if (style != null && go.activeInHierarchy)
			//{
			//    GUI.Label(selectionRect, go.name, style);
			//}
		}
        catch (Exception)
        {
        }
    }
}
public static class ExtensionMethods
{
    /// <summary>
    /// 检测是否含有组件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="go"></param>
    /// <param name="checkChildren">是否检测子层级</param>
    /// <returns></returns>
    public static bool HasComponent<T>(this GameObject go, bool checkChildren) where T : Component
    {
        if (!checkChildren)
        {
            return go.GetComponent<T>();
        }
        else
        {
            return go.GetComponentsInChildren<T>().FirstOrDefault() != null;
        }
    }
}
// EndScript // 
