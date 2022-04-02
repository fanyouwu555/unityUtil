using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using Spine.Unity;

public class FtueRectPosition : MonoBehaviour
{

	[MenuItem( "Ftue/RectPosition" )]
	static void RectPosition()
	{
		GameObject targetObject = UnityEngine.GameObject.Find( "GameGUI/CanvasPrefab" );
		GameObject obj = Selection.activeGameObject;
		Vector3 wp = obj.transform.TransformPoint( UnityEngine.Vector3.zero );
		Vector3 lp = targetObject.transform.InverseTransformPoint( wp );

		RectTransform rectTrans = obj.transform.GetComponent<RectTransform>();
		Debug.LogFormat( "{0},{1};{2},{3}" , UnityEngine.Mathf.Floor( lp.x ) , UnityEngine.Mathf.Floor( lp.y ) , UnityEngine.Mathf.Floor( rectTrans.rect.width ) , UnityEngine.Mathf.Floor( rectTrans.rect.height ) );
	}
}
