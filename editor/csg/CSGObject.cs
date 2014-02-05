using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CSGObject : MonoBehaviour {
	
	public const int TexMode_Original 	= 0;
	public const int TexMode_Planar 	= 1;
	
	// Variables
	public BspNode		rootNode;	//!< 
	public List<Face>	faces;		//!< 
	public int 			texMode;	//!< 
	public float		globalTexOffsetU = 0.0f;
	public float 		globalTexOffsetV = 0.0f;
	public float 		globalTexScaleU  = 1.0f;
	public float		globalTexScaleV  = 1.0f;
		
	/// <summary>
	/// Draws the Debug Informations, really nothing special...
	/// </summary>
	public void DrawDebug()
	{
		foreach( Face f in faces )
		{			
			for( int i = 0; i < f.vertices.Length; ++i )
			{
				Debug.DrawLine( f.vertices[i], f.vertices[(i+1)%f.vertices.Length], new Color(1.0f,0.0f,1.0f,1.0f), 20.0f, true );		
			}
		}
	}
	
	private bool intersect( CSGObject inOther )
	{
		MeshFilter thisMeshFilter = GetComponent<MeshFilter>();
		MeshFilter otherMeshFilter = inOther.GetComponent<MeshFilter>();
		
		if( thisMeshFilter == null || otherMeshFilter == null )
			return false;
			
		Mesh thisMesh = thisMeshFilter.sharedMesh;
		Mesh otherMesh = otherMeshFilter.sharedMesh;
		
		if( thisMesh == null || otherMesh == null )
			return false;
		
		return thisMesh.bounds.Intersects( otherMesh.bounds );
	}
		
	/// <summary>
	/// Performs CSG Operation on this Object (Master) with given Slaves.
	/// </summary>
	/// <param name='inOper'>
	/// In oper.
	/// </param>
	/// <param name='inSlaves'>
	/// In slaves.
	/// </param>
	public void PerformCSG( CsgOperation.ECsgOperation inOper, GameObject[] inSlaves )
	{
		// 
		CreateFromMesh();
		
		// create bsp generator
		BspGen gen = new BspGen( GlobalSettings.BspOptimization );
				
		rootNode = gen.GenerateBspTree( faces );
		
		List<Face> savedFaces = new List<Face>();
		
		// 
  		foreach ( GameObject g in inSlaves )
  		{
			CSGObject slave = g.GetComponent<CSGObject>();
			
			// if we have a csg object and we are not our self
			// and intersecting
			if( slave && g != gameObject && intersect(slave) )
			{	
       			Debug.Log(g.name);
				
				// 
				slave.CreateFromMesh();
				
				// ....
				BspGen genSlave = new BspGen( GlobalSettings.BspOptimization );
				slave.rootNode = genSlave.GenerateBspTree( slave.faces );
				
				CsgVisitor visitor = null;
				
				switch( inOper )
				{
				case CsgOperation.ECsgOperation.CsgOper_Additive:
					visitor = new UnionVisitor();
					break;
				case CsgOperation.ECsgOperation.CsgOper_Subtractive:
					visitor = new SubtractiveVisitor();
					break;
				case CsgOperation.ECsgOperation.CsgOper_Intersect:
					visitor = new IntersectVisitor();
					break;
				case CsgOperation.ECsgOperation.CsgOper_DeIntersect:
					visitor = new DeIntersectVisitor();
					break;
				default:
					visitor = null;
					break;
				}
				
				
				CsgOperation oper = new CsgOperation( visitor );
				
				oper.Perform( inOper, this, slave );
				
				// save faces
				savedFaces.AddRange( faces );
			}
  		}
		
		
		// If we want to merge Coplanars after every Operation
		if( GlobalSettings.MergeCoplanars )
		{
			MergeFaces();	
		}
  		
		// for additive or subtracte operation, built faces list from bsp tree
		// for others, use faces directly
		if( inOper == CsgOperation.ECsgOperation.CsgOper_Additive || inOper == CsgOperation.ECsgOperation.CsgOper_Subtractive )
		{
		
			// create new face list
			List<Face> newFaces = new List<Face>();
			// create faces from bsp nodes
			BspHelper.FacesFromNodes( rootNode, newFaces );
			// copy to face list
			faces = newFaces;	
			
		}
		else
		{
			// copy saved faces
			faces = savedFaces;	
		}
		
		
	//	Face.MergeCoplanars( faces );
		
		// copy to unity structure
		TransferFacesToMesh();
		
		
		// dumb tree
//		BspHelper.DumpTree( rootNode );
		
	}
	
	/// <summary>
	/// Creates CSG Object from Mesh.
	/// </summary>
	public void CreateFromMesh()
	{		
		MeshFilter mf = GetComponent<MeshFilter>();
		MeshRenderer mr = GetComponent<MeshRenderer>();
		
		if( mf == null || mr == null )
			return;
			
		Mesh mesh = mf.sharedMesh;
		Material mat = mr.sharedMaterial;
			
		faces = new List<Face>();
		
		// import all sub meshes
		for( int i = 0; i < mesh.subMeshCount; ++i )
		{
			// return default material if not valid index
			mat = (mr.sharedMaterials.Length > i) ? mr.sharedMaterials[i] : mr.sharedMaterial;
			
			ImportSubMesh( mesh, i, mat );
		}
		
		// 
		TransformFaces();
	}
	
	
	// transform to world space
	private void TransformFaces()
	{
		foreach( Face f in faces )
		{
			f.Transform( transform.localToWorldMatrix );
		}
	}
	
	
	/// <summary>
	/// Imports the sub mesh.
	/// </summary>
	/// <param name='inMesh'>
	/// In mesh.
	/// </param>
	/// <param name='inSubMesh'>
	/// In sub mesh.
	/// </param>
	/// <param name='inMaterial'>
	/// In material.
	/// </param>
	private void ImportSubMesh( Mesh inMesh, int inSubMesh, Material inMaterial )
	{
		// 
		int[] tris = inMesh.GetTriangles( inSubMesh );
		
		
		for( int i = 0; i < tris.Length; i += 3 )
		{
			Face newFace = new Face();
			
			// copy triangle
			newFace.vertices = new Vector3[3];
			newFace.vertices[0] = inMesh.vertices[ tris[i + 0] ];
			newFace.vertices[1] = inMesh.vertices[ tris[i + 1] ];
			newFace.vertices[2] = inMesh.vertices[ tris[i + 2] ];
			
			// copy triangle uv
			newFace.uv = new Vector2[3];
			newFace.uv[0] = inMesh.uv[ tris[i+0] ];
			newFace.uv[1] = inMesh.uv[ tris[i+1] ];
			newFace.uv[2] = inMesh.uv[ tris[i+2] ];
			
			// set material
			newFace.material = inMaterial;
		
			// add to face list
			faces.Add( newFace );
		}
	}
	
		
	
	/// <summary>
	/// Single Mesh importing, do not support Sub Meshes.
	/// </summary>
	/// <param name='inMesh'>
	/// In mesh.
	/// </param>
	/// <param name='inMaterial'>
	/// In material.
	/// </param>
	private void ImportMesh( Mesh inMesh, Material inMaterial )
	{
		// ONLY SUPPORTS TRIANGLES AT THE MOMENT
		faces = new List<Face>();
		
		for( int i = 0; i < inMesh.triangles.Length; i += 3 )
		{
			Face newFace = new Face();
			
			// copy triangle
			newFace.vertices = new Vector3[3];
			newFace.vertices[0] = inMesh.vertices[ inMesh.triangles[i + 0] ];
			newFace.vertices[1] = inMesh.vertices[ inMesh.triangles[i + 1] ];
			newFace.vertices[2] = inMesh.vertices[ inMesh.triangles[i + 2] ];
			
			// copy triangle uv
			newFace.uv = new Vector2[3];
			newFace.uv[0] = inMesh.uv[ inMesh.triangles[i+0] ];
			newFace.uv[1] = inMesh.uv[ inMesh.triangles[i+1] ];
			newFace.uv[2] = inMesh.uv[ inMesh.triangles[i+2] ];
			
			// set material
			newFace.material = inMaterial;
		
			// add to face list
			faces.Add( newFace );
		}
		
	}
	
	
	/// <summary>
	/// Transfers all Faces to our Mesh.
	/// </summary>
	public void TransferFacesToMesh()
	{
		MeshFilter mf = GetComponent<MeshFilter>();
	
		// no mesh or no faces
		if( mf == null || faces == null )
			return;
			
		// get mesh
		Mesh mesh = mf.sharedMesh;	
		
		// count vertices and indices
		int numVertices = 0;
		int numIndices = 0;
		
		// list of unique materials
		List<Material> allMaterials = new List<Material>();
		
		// 
		foreach( Face f in faces )
		{
//			f.Dump();	
		
			// count number of vertices
			numVertices += f.vertices.Length;
			numIndices += (f.vertices.Length - 2) * 3;
			
			// add unique materials
			if( !allMaterials.Contains( f.material ) )
				allMaterials.Add( f.material );
		}
		
		// TODO: add debug message
		if( allMaterials.Count == 0 )
			return;
		
		
		// clear all states
		mesh.Clear();
		
		// set submesh count
		mesh.subMeshCount = allMaterials.Count;
		
		// set default materials
		renderer.sharedMaterial = allMaterials[0];
		
		// copy shared materials into new array
		Material[] tmpMatArray = new Material[allMaterials.Count];
		allMaterials.CopyTo( tmpMatArray ); 
		
		// 
		renderer.sharedMaterials = tmpMatArray;
		
		// 
		Debug.Log( "Building Mesh with " + numVertices + " Vertices" );
		
		// 
		Vector3[] newVertices = new Vector3[numVertices];
		Vector2[] newUVs = new Vector2[numVertices];
		Vector4[] newTangents = new Vector4[numVertices];
		
		// 
		int vertCounter = 0;
				
		// Materials sorted Vertices and Indices
		foreach( Material m in allMaterials )
		{
			// find faces with material
			foreach( Face f in faces )
			{
				// if not the same material
				if( f.material != m )
					continue;
			
				// reset vertices
				numVertices = 0;
			
				// 
				TextureMapper texMapper = new TextureMapper( f.GetPlane(), globalTexScaleU, globalTexScaleV, globalTexOffsetU, globalTexOffsetV, 0.0f );
				
				
				Vector4[] tangents;
				
				f.CalculateTangents( out tangents );
				
				// copy vertices
				// TODO: replace with normal for loop for better index access
				foreach( Vector3 v in f.vertices )
				{
					newVertices[vertCounter + numVertices] = transform.worldToLocalMatrix.MultiplyPoint( v );
				
					// ADD Option for creating uvs or using existing...
					
					// if we want planar mapping or we have no original (existing) uvs
					if( texMode == TexMode_Planar || f.uv == null )
					{
						// create uvs
						newUVs[vertCounter + numVertices] = texMapper.Project( v );
					}
					else
					{
						// copy existing uvs
						newUVs[vertCounter + numVertices] = f.uv[numVertices];
					}
						
					// 
					newTangents[vertCounter + numVertices] = tangents[numVertices];
					
					
					numVertices++;
				}
					
				
				// increase number of vertices
				vertCounter += numVertices;
			}
						
		}
		
		// apply vertices
		mesh.vertices = newVertices;
		mesh.uv = newUVs;
		mesh.tangents = newTangents;
		
		
		// reset values
		vertCounter = 0;
		int indexCounter = 0;
		int matIndex = 0;
		
		// Materials sorted Vertices and Indices
		foreach( Material m in allMaterials )
		{
			int numTriangles = 0;
			
			// find faces with material
			foreach( Face f in faces )
			{
				// if not the same material
				if( f.material != m )
					continue;
			
				// sum up all triangles
				numTriangles += (f.vertices.Length - 2) * 3;
			}
			
			int[] newTriangles = new int[numTriangles];
			
			// reset index counter
			indexCounter = 0;
			
			// find faces with material
			foreach( Face f in faces )
			{
				// if not the same material
				if( f.material != m )
					continue;
			
				
				// number of triangle (triangle fan -> triangles)
				int triangleCount = (f.vertices.Length - 2);
				// 
				int indexOffset = 0;
					
				// copy indices 
				for( int i = 0; i < triangleCount; ++i )
				{
					newTriangles[indexCounter + indexOffset] = vertCounter;	
				
					newTriangles[indexCounter + indexOffset + 1] = vertCounter + i + 1;
					newTriangles[indexCounter + indexOffset + 2] = vertCounter + i + 2;
			
					indexOffset += 3;
				}
				
				
				// increase indices counter
				indexCounter += indexOffset;	
				
				// increase vertices counter
				vertCounter += f.vertices.Length;
			}
			
			
			// upload to mesh
			mesh.SetTriangles( newTriangles, matIndex );
			
			// 
			matIndex++;
		}
		
		
		// 			
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
	
		
		// Optimize Mesh
		mesh.Optimize();
				
	}
	
	/// <summary>
	/// Merges the Coplanar Faces in our Bsp Tree.
	/// </summary>
	public void MergeFaces()
	{
		if( rootNode != null )
		{
		//	BspHelper.MergeCoplanars( rootNode );
		}
	}
	
	/// <summary>
	/// Optimize this instance.
	/// </summary>
	public void Optimize()
	{
		// 
		Face.MergeCoplanars( faces );	
		// 
		TransferFacesToMesh();
	}
	
	
}
