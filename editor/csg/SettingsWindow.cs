using UnityEngine;
using System.Collections;
using UnityEditor;

public class SettingsWindow : EditorWindow
{
	// Add menu item named "My Window" to the Window menu
	[MenuItem("CSG/Settings")]
	public static void ShowWindow()
	{
		//Show existing window instance. If one doesn't exist, make one.
		EditorWindow.GetWindow( typeof(SettingsWindow), true, "fhCSG Settings" );
	}

	void OnGUI()
	{
		GUILayout.Label ("Base Settings", EditorStyles.boldLabel);
		
		// 
		GlobalSettings.Epsilonf = Mathf.Clamp( EditorGUILayout.FloatField( "Epsilon", GlobalSettings.Epsilonf ), 0.0001f, 0.2f );
		GlobalSettings.MergeCoplanars = EditorGUILayout.Toggle( "Merge Coplanars", GlobalSettings.MergeCoplanars );
		
		GlobalSettings.BspOptimization = EditorGUILayout.Popup( "Bsp Optimization", GlobalSettings.BspOptimization, new string[]{ "Worse", "Average", "Best" } );
			
		GlobalSettings.DeleteSlaves = EditorGUILayout.Toggle( "Delete Slaves", GlobalSettings.DeleteSlaves );
		
	}
}