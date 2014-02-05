using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Face : System.ICloneable {
		
	public const int FaceFlags_WasCutted 	= 0x00000001;
	
	public enum EPlaneSide
	{
		Side_Front,
		Side_Back,
		Side_Planar,
		Side_Split
	}
	
	
	public Vector3[] 	vertices;
	public Vector2[]	uv;
	public Material		material;
	public int			flags;
	
	// Get Plane from Vertices
	public Plane GetPlane()
	{
		Plane plane = new Plane( vertices[0], vertices[1], vertices[2] );
		return plane;
	}
	
	public void Transform( Matrix4x4 inMatrix )
	{		
		for( int i = 0; i < vertices.Length; ++i )
		{
			vertices[i] = inMatrix.MultiplyPoint( vertices[i] );
		}
	}
	
	// Check on which Side this Face lie (given the Input Plane)
	public EPlaneSide Side( Plane inPlane )
	{
		// 
		int numBack = 0, numFront = 0, numPlanar = 0;
		
		for( int i = 0; i < vertices.Length; ++i )
		{
			float dist = inPlane.GetDistanceToPoint( vertices[i] );
			
			// FIXME: do this work right??
			if( dist > GlobalSettings.Epsilonf )
				numFront++;
			else if( dist < -GlobalSettings.Epsilonf )
				numBack++;
			else
			{
				numPlanar++;
				numFront++;
				numBack++;
			}
		}
		
		if( numPlanar == vertices.Length )
			return EPlaneSide.Side_Planar;
		
		if( numFront == vertices.Length )
			return EPlaneSide.Side_Front;
		
		if( numBack == vertices.Length )
			return EPlaneSide.Side_Back;
		
		return EPlaneSide.Side_Split;
	}
	
	// Split this Face with given Plane
	public bool Split( Plane inPlane, out Face outFront, out Face outBack )
	{
		outFront = new Face();
		outBack = new Face();
		
		float[] distance = new float[vertices.Length + 1];
		EPlaneSide[] side = new EPlaneSide[vertices.Length + 1];
		
		
		for( int i = 0; i < vertices.Length; ++i )
		{
			distance[i] = inPlane.GetDistanceToPoint( vertices[i] );
			side[i] = Side( inPlane, vertices[i] );		
		}
		
		distance[vertices.Length] = distance[0];
		side[vertices.Length] = side[0];
		
		for( int i = 0; i < vertices.Length; ++i )
		{
			// if we lie on plane, add them to both
			if( side[i] == EPlaneSide.Side_Planar )
			{
				outFront.AddVertex( vertices[i], uv[i] );
				outBack.AddVertex( vertices[i], uv[i] );
				// nothing todo with this vertex
				continue;
			}
			
			// if we are on the front, add it to front face
			if( side[i] == EPlaneSide.Side_Front )
			{
				outFront.AddVertex( vertices[i], uv[i] );	
			}
			// if we are on the back, add it to the back side
			else if( side[i] == EPlaneSide.Side_Back )
			{
				outBack.AddVertex( vertices[i], uv[i] );
			}
			
			// check if the next vertex is planar or on the same side, then we do not split
			if( side[i+1] == EPlaneSide.Side_Planar || side[i] == side[i+1] )
				continue;
			
			// create split point
			Vector3 nextVector = vertices[ (i+1) % vertices.Length];
			Vector2 nextUV = uv[ (i+1) % uv.Length];
			Vector3 newVector, newUV;
			
			// if we were on the front
			if( side[i] == EPlaneSide.Side_Front )
			{
				float t = distance[i] / (distance[i] - distance[i+1]);
				
				newVector = vertices[i] + t * (nextVector - vertices[i]);
				newUV = uv[i] + t * (nextUV - uv[i]);
			}
			else // back side...
			{
				float t = distance[i+1] / (distance[i+1] - distance[i]);
				
				newVector = nextVector + t * (vertices[i] - nextVector);
				newUV = nextUV + t * (uv[i] - nextUV);
			}
			
			// split points are added
			
			// add to front
			outFront.AddVertex( newVector, newUV );
			
			// add to back
			outBack.AddVertex( newVector, newUV );
		}
	
		// Debugging checks
		if( outFront.vertices.Length < 3 || outBack.vertices.Length < 3 )
			Debug.Log("Degenerate Faces");
		
		
		// todo...
		outFront.material = material;
		
		// 
		outBack.material = material;
		
		return true;
	}
					
	public void AddVertex( Vector3 inVec, Vector2 inUV )
	{
		// do we have any vertices yet?
		if( vertices == null )
		{
			// 
			vertices = new Vector3[1];
			uv = new Vector2[1];
			//
			vertices[0] = inVec;
			uv[0] = inUV;
		}
		else
		{
			// NICER WAY FOR THIS...
			Vector3[] newVertices = new Vector3[vertices.Length + 1];
			Vector2[] newUVs = new Vector2[uv.Length + 1];
			// copy vertices and assign new one
			vertices.CopyTo( newVertices, 0 );
			vertices = newVertices;
			vertices[vertices.Length-1] = inVec;
			// copy uvs and assign new one
			uv.CopyTo( newUVs, 0 );
			uv = newUVs;
			uv[vertices.Length-1] = inUV;
		}
	}
			
	
	public void Reverse()
	{
		// TODO: reverse uvs
		
		Vector3[] newVertices = new Vector3[vertices.Length];
		Vector2[] newUVs = new Vector2[uv.Length];
		
		for( int i = 0; i < vertices.Length; ++i )
		{
			newVertices[i] = vertices[vertices.Length - i - 1];	
		}
		
		for( int i = 0; i < uv.Length; ++i )
		{
			newUVs[i] = uv[uv.Length - i - 1];
		}
		
		vertices = newVertices;
		uv = newUVs;
	}
				
	
	public bool Merge( Face inOther, out Face outFace )
	{
		outFace = null;
		
		
		// do not share same material
		if( material != inOther.material || vertices.Length < 1 )
			return false;
		
		Vector3 p1, p2;
		Vector3 p3, p4;
		int i = 0, j = 0;
		
		// just to fix compiler error
		p1 = vertices[0];
		p2 = vertices[1%vertices.Length];
		
		// check if we share an edge
		for( i = 0; i < vertices.Length; ++i )
		{
			// get edge
			p1 = vertices[i];
			p2 = vertices[(i+1) % vertices.Length];
			
			
			// go through all edges of other face
			for( j = 0; j < inOther.vertices.Length; ++j )
			{
				// get other edge
				p3 = inOther.vertices[j];
				p4 = inOther.vertices[(j+1) % inOther.vertices.Length];
				
				// check if we are sharing an edge
				if( p1.Equals( p4 ) && p2.Equals( p3 ) )
					break;
				
			}
			
			// found edge
			if( j < inOther.vertices.Length )
				break;
			
		}
		
		// no edge found
		if( i == vertices.Length )
			return false;
		
		//  ...
		Vector3 back = vertices[ (i+vertices.Length-1) % vertices.Length ];
		Vector3 delta = p1 - back;
		
		Vector3 normal = Vector3.Cross( GetPlane().normal, delta );
		normal.Normalize();
		
		back = inOther.vertices[ (j+2) % inOther.vertices.Length ];
		delta = back - p1;
		
		float dot = Vector3.Dot( delta, normal );
		
		// not a convex polygon
		if( dot > GlobalSettings.Epsilonf )
			return false;
		
		// if they are co linear
		bool keep1 = (dot < -GlobalSettings.Epsilonf);
		
		// ...
		back = vertices[ (i+2) % vertices.Length ];
		delta = back - p2;
		normal = Vector3.Cross( GetPlane().normal, delta );
		normal.Normalize();
		
		back = inOther.vertices[(j+inOther.vertices.Length-1)%inOther.vertices.Length];
		delta = back - p2;
		dot = Vector3.Dot( delta, normal );
		
		// not convex
		if( dot > GlobalSettings.Epsilonf )
			return false;
		
		bool keep2 = (dot < -GlobalSettings.Epsilonf);
		
		bool keep = false;
		
		// create out face
		outFace = new Face();
		
		outFace.flags = flags;
		outFace.material = material;
	
		// copy vertices from this
		for( int k = (i+1) % vertices.Length; k != i; k = (k+1)%vertices.Length )
		{
			if( !keep && k == (i+1)%vertices.Length && !keep2 )
				continue;
			
			// copy vector
			outFace.AddVertex( vertices[k], uv[k] );
			
		}
		
		// copy vertices from other
		for( int k = (j+1) % inOther.vertices.Length; k != j; k = (k+1)%inOther.vertices.Length )
		{
			if( !keep && k == (j+1)%inOther.vertices.Length && !keep1 )
				continue;
			
			outFace.AddVertex( inOther.vertices[k], inOther.uv[k] );	
		}
				
		return true;
	}
	
	
	static public EPlaneSide Side( Plane inPlane, Vector3 inVec )
	{
		float dist = inPlane.GetDistanceToPoint( inVec );
			
		// using own Epsilon -> Unity3D Epsilon is too small for correct CSG
		if( dist > GlobalSettings.Epsilonf )
			return EPlaneSide.Side_Front;
		else if( dist < -GlobalSettings.Epsilonf )
			return EPlaneSide.Side_Back;
		else
			return EPlaneSide.Side_Planar;
	}
	
	/// <summary>
	/// Merges the coplanars. Very SLOW operation.
	/// </summary>
	/// <param name='inFaces'>
	/// In faces.
	/// </param>
	static public void MergeCoplanars( List<Face> inFaces )
	{
		// 
		for( int i = 0; i < inFaces.Count; ++i )
		{
			for( int j = i+1; j < inFaces.Count; ++j )
			{
			
				// if we are sharing the same side
				if( Vector3.Dot( inFaces[i].GetPlane().normal, inFaces[j].GetPlane().normal ) > 0.985f )
				{
					Face newFace;
					
					if( inFaces[i].Merge( inFaces[j], out newFace ) )
					{
						// replace us with new face
						inFaces[i] = newFace;
						
						// remove other
						inFaces.RemoveAt( j );
						
						// restart at the beginning
						i = 0;
						j = 1;
						continue;
					}
					
				}
			}
		}
		
		
	}
	
	public void CalculateTangents( out Vector4[] outTangents )
	{
        int triangleCount = vertices.Length - 2;
        int vertexCount = vertices.Length;

        Vector3[] tan1 = new Vector3[vertexCount];
        Vector3[] tan2 = new Vector3[vertexCount];

       	outTangents = new Vector4[vertexCount];

        for(long a = 0; a < triangleCount; a++ )
        {
            long i1 = a+0;
            long i2 = a+1;
            long i3 = a+2;

            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];
            Vector3 v3 = vertices[i3];

            Vector2 w1 = uv[i1];
            Vector2 w2 = uv[i2];
            Vector2 w3 = uv[i3];

            float x1 = v2.x - v1.x;
            float x2 = v3.x - v1.x;
            float y1 = v2.y - v1.y;
            float y2 = v3.y - v1.y;
            float z1 = v2.z - v1.z;
            float z2 = v3.z - v1.z;

            float s1 = w2.x - w1.x;
            float s2 = w3.x - w1.x;
            float t1 = w2.y - w1.y;
            float t2 = w3.y - w1.y;

            float r = 1.0f / (s1 * t2 - s2 * t1);

            Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
            Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

            tan1[i1] += sdir;
            tan1[i2] += sdir;
            tan1[i3] += sdir;

            tan2[i1] += tdir;
            tan2[i2] += tdir;
            tan2[i3] += tdir;
        }

        for (long a = 0; a < vertexCount; ++a)
        {
        //    Vector3 n = mesh.normals[a];
			// per face normal...
          	Vector3 n = GetPlane().normal;
			Vector3 t = tan1[a];

            Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
            outTangents[a] = new Vector4(tmp.x, tmp.y, tmp.z);

            outTangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
        }
	}
	
	
	public object Clone()
	{
		// create new Clone
		Face clone = new Face();
		
		// copy vertices
		clone.vertices = new Vector3[vertices.Length];
		System.Array.Copy( vertices, clone.vertices, vertices.Length );
		
		// copy uvs
		clone.uv = new Vector2[uv.Length];
		System.Array.Copy( uv, clone.uv, uv.Length );
		// copy material
		clone.material = material;
		// FIXME: clone flags???
		clone.flags = 0;
		return clone;
	}
	
	public void Dump()
	{
		Debug.Log("Number of Vertices: " + vertices.Length );
		foreach( Vector3 v in vertices )
		{
			Debug.Log( "X: " + v.x + " Y: " + v.y + " Z: " + v.z ); 
		}
		
	}
	
}
