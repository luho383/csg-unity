using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BspHelper {

	public static void DumpTree( BspNode inNode )
	{
		Debug.Log( "Node..." );
		
		Debug.Log( "Planar Nodes" );
		BspNode planar = inNode.planar;
		while( planar != null )
		{
			Debug.Log( "Planar Node" );
			planar = planar.planar;
		}
		
		if( inNode.front != null )
		{
			Debug.Log( "FrontSide" );
			DumpTree( inNode.front );
		}
		
		if( inNode.back != null )
		{
			Debug.Log( "BackSide" );
			DumpTree( inNode.back );	
		}
		
	}
	
	
	public static void FacesFromNodes( BspNode inNode, List<Face> outFaces )
	{
		while( inNode != null )
		{
			if( (inNode.flags & BspNode.BspFlags_IsDestroyed) == 0 )
			{
				outFaces.Add( inNode.face );	
			}
			
			if( inNode.front != null )
				FacesFromNodes( inNode.front, outFaces );
			
			if( inNode.back != null )
				FacesFromNodes( inNode.back, outFaces );
			
			// 
			inNode = inNode.planar;
		}
	}
	
	public static void MergeCoplanars( BspNode inNode )
	{
		// proceed front nodes
		if( inNode.front != null )
			MergeCoplanars( inNode.front );
		
		// proceed back nodes
		if( inNode.back != null )
			MergeCoplanars( inNode.back );
		
		// first try to merge other co planar nodes
		if( inNode.planar != null )
			MergeCoplanars( inNode.planar );
		
		// 
		bool tryToMerge = (inNode.flags & BspNode.BspFlags_IsDestroyed) == 0;
		
		while( tryToMerge )
		{
			// get planar node
			BspNode planarNode = inNode.planar;
			
			// assume we are done
			tryToMerge = false;
			
			// get through all planar nodes
			while( planarNode != null )
			{
				// if we are destroyed, proceed to next
				if( (planarNode.flags & BspNode.BspFlags_IsDestroyed) != 0 )
				{
					// proceed to next
					planarNode = planarNode.planar;
					continue;
				}
				
				
				Face thisFace = inNode.face;
				Face otherFace = planarNode.face;
				
				// are we facing the same direction
				if( Vector3.Dot( thisFace.GetPlane().normal, otherFace.GetPlane().normal ) > 0.995f )
				{
					// result
					Face merged;
					
					// 
					if( thisFace.Merge( otherFace, out merged ) )
					{
						// replace this face with merged
						thisFace = merged;
						
						// set other node as destroyed
						planarNode.flags |= BspNode.BspFlags_IsDestroyed;
						
						// retry to merge
						tryToMerge = true;
					}	
				}
				
				// proceed to next
				planarNode = planarNode.planar;				
			}
			
		}
		
		
	}
	
	
}
