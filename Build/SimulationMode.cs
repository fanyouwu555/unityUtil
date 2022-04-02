using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;










public class SimulationMode 
{
	const string menuSimulationMode = "AssetBundle/Simulation Mode";
	[MenuItem( menuSimulationMode , false , 10000 )]
	public static void Menu_SimulationMode()
	{
		string value = PlayerPrefs.GetString( "isSimulateMode" );
		bool current = ( value != "true" );
		PlayerPrefs.SetString( "isSimulateMode" , current ? "true" : "false" );
	}

	[MenuItem( menuSimulationMode , true )]
	public static bool ToggleSimulationModeValidate()
	{
		string value = PlayerPrefs.GetString( "isSimulateMode" );
		if( value == "true" )
		{
			Menu.SetChecked( menuSimulationMode , true );
			Debug.Log( "Open Load AssetBundle Mode!" );
		}
		else
		{
			Menu.SetChecked( menuSimulationMode , false );
			Debug.Log( "Open Load LocalResource Mode!" );
		}

		return true;
	}
}
