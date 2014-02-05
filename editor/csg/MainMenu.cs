using UnityEngine;
using UnityEditor;
using System.Collections;

public class MainMenu : MonoBehaviour {
			
	
    [MenuItem ("CSG/Create Cube")]
    static void CreateCube () {
		// 
		BrushBuilder builder = new CubeBuilder();
		builder.Build();
    }
	
	
	[MenuItem ("CSG/Union")]
    static void Union () {
		
		// we need at least two selected objects
		if( Selection.gameObjects.Length > 1 )
		{

			CSGObject obj = Selection.activeGameObject.GetComponent<CSGObject>();	
			
			if( obj )
			{
				obj.PerformCSG(CsgOperation.ECsgOperation.CsgOper_Additive, Selection.gameObjects);	
			}
			
			if( GlobalSettings.DeleteSlaves )
			{
				// destroy slaves if we want to
				
				foreach( GameObject go in Selection.gameObjects )
				{
					if( Selection.activeGameObject != go && go.GetComponent<CSGObject>() )
					{
						GameObject.DestroyImmediate( go );
					}
				}
			}
			
		}
    }
	
	[MenuItem ("CSG/Subtract")]
    static void Subtract () {
		
		// we need at least two selected objects
		if( Selection.gameObjects.Length > 1 )
		{

			CSGObject obj = Selection.activeGameObject.GetComponent<CSGObject>();	
			
			if( obj )
			{
				obj.PerformCSG(CsgOperation.ECsgOperation.CsgOper_Subtractive, Selection.gameObjects);	
			}
			
			
			if( GlobalSettings.DeleteSlaves )
			{
				foreach( GameObject go in Selection.gameObjects )
				{
					// if we are not the active game object and are a CSG Object
					if( Selection.activeGameObject != go && go.GetComponent<CSGObject>() )
					{
						GameObject.DestroyImmediate( go );
					}
				}
				
			}
		}
    }
	
	[MenuItem ("CSG/Add Component")]
	static void AddComponent() {
		for( int i = 0; i < Selection.gameObjects.Length; ++i )
		{
			GameObject go = Selection.gameObjects[i];
			CSGObject obj = go.GetComponent<CSGObject>();
			
			Mesh mesh = go.GetComponent<MeshFilter>() != null ? go.GetComponent<MeshFilter>().sharedMesh : null;
						
			if( !obj && mesh )
			{
				// TODO: add bsp tree generation on add... 
				CSGObject csg = go.AddComponent<CSGObject>();
				MeshFilter filter = go.GetComponent<MeshFilter>();
				
				// clone mesh as every csg object needs his own mesh
				filter.sharedMesh = ObjectCloner.CloneMesh( filter.sharedMesh );
				// 
				csg.CreateFromMesh();
			}
			else
			{
				Debug.Log("No Mesh or already an CSG Object");
			}
		}
		
	}
	
	
	[MenuItem ("CSG/Debug/Build BSP Tree")]
	static void BuildTree() {
		
		for( int i = 0; i < Selection.gameObjects.Length; ++i )
		{
			GameObject go = Selection.gameObjects[i];
			CSGObject obj = go.GetComponent<CSGObject>();
		
			if( obj )
			{
				obj.CreateFromMesh();
				
				BspGen gen = new BspGen( GlobalSettings.BspOptimization );
				
				obj.rootNode = gen.GenerateBspTree( obj.faces );
			}
		}
		
	}
	
	[MenuItem ("CSG/Debug/BSP Faces to Mesh")]
	static void FacesToMesh() {
		
		for( int i = 0; i < Selection.gameObjects.Length; ++i )
		{
			GameObject go = Selection.gameObjects[i];
			CSGObject obj = go.GetComponent<CSGObject>();
		
			if( obj )
			{
				obj.TransferFacesToMesh();
			}
		}
	}
	
	[MenuItem ("CSG/Debug/Merge BSP Faces")]
	static void MergeBspFaces() {
		
		for( int i = 0; i < Selection.gameObjects.Length; ++i )
		{
			GameObject go = Selection.gameObjects[i];
			CSGObject obj = go.GetComponent<CSGObject>();
		
			if( obj )
			{
				obj.MergeFaces();
			}
		}
	}
	
	
	
	[MenuItem ("CSG/Debug/Mesh to BSP Faces")]
	static void MeshToFaces() {
		
		for( int i = 0; i < Selection.gameObjects.Length; ++i )
		{
			GameObject go = Selection.gameObjects[i];
			CSGObject obj = go.GetComponent<CSGObject>();
		
			if( obj )
			{
				obj.TransferFacesToMesh();
			}
		}
	}
	
	
	[MenuItem ("CSG/Debug/Mesh Optimize")]
	static void MeshOptimize() {
		
		for( int i = 0; i < Selection.gameObjects.Length; ++i )
		{
			GameObject go = Selection.gameObjects[i];
			CSGObject obj = go.GetComponent<CSGObject>();
		
			if( obj )
			{
				obj.Optimize();
			}
		}
	}
	
	
	
	[MenuItem ("CSG/Debug/Draw Debug")]
	static void DrawDebug() {
		
		
		for( int i = 0; i < Selection.gameObjects.Length; ++i )
		{
			GameObject go = Selection.gameObjects[i];
			CSGObject obj = go.GetComponent<CSGObject>();
		
			if( obj )
			{
				obj.DrawDebug();
			}
		}
			
	}
	
	
}
