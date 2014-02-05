using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BspGen {
	
	public const int BspOptm_Worse = 0;
	public const int BspOptm_Average = 1;
	public const int BspOptm_Best = 2;
	
	private int bspOptm;
	
	// 
	public BspGen( int inOptm )
	{
		bspOptm = inOptm;	
	}
	
	// 
	public BspNode GenerateBspTree( List<Face> inFaces )
	{
		if( inFaces == null )
		{
			Debug.Log("Not able to create Tree with no Faces");
			return null;
		}
		
		Debug.Log( "Building Bsp Tree for " + inFaces.Count + " Faces" );
		
		// create root node
		BspNode root = new BspNode();
		// partition all faces
		Partition( root, inFaces );
		
		// copy bsp faces back to array list (resulting faces...)
		GetFaces( root, inFaces );
		
		Debug.Log( "Resulting in " + inFaces.Count + " Faces" );
		
		// result
		return root;
	}
	
	// 
	void Partition( BspNode inNode, List<Face> inFaces )
	{
		List<Face> frontFaces = new List<Face>();
		List<Face> backFaces = new List<Face>();
		Face nodeFace;
		
		// find best splitter plane for this
		bool ret = FindSplitter( inFaces, out nodeFace );
		
		// return has to be true!!!
		if( ret == false )
		{
			Debug.DebugBreak();
			Debug.Log("Error processing Mesh!");
			return;
		}
		
		// setup node
		inNode.front = null;
		inNode.back = null;
		inNode.planar = null;
		inNode.face = nodeFace;
		inNode.plane = nodeFace.GetPlane();
		
		
		// split remaining faces into lists
		for( int i = 0; i < inFaces.Count; ++i )
		{
			// get face
			Face face = inFaces[i] as Face;
			
			// do not process our self
			if( face == nodeFace )
				continue;
			
			
			Face.EPlaneSide side = face.Side( inNode.plane );
			
			switch( side )
			{
			case Face.EPlaneSide.Side_Front:
				frontFaces.Add( face );
				break;
			case Face.EPlaneSide.Side_Back:
				backFaces.Add( face );
				break;
			case Face.EPlaneSide.Side_Planar:
				// get last planar node
				BspNode lastPlanar = inNode;
				while( lastPlanar.planar != null )
					lastPlanar = lastPlanar.planar;
				
				// create new planar node
				BspNode planar = lastPlanar.planar = new BspNode();
				
				// setup planar node
				planar.front = null;
				planar.back = null;
				planar.planar = null;
				planar.face = face;
				planar.plane = face.GetPlane();
				
				break;
			case Face.EPlaneSide.Side_Split:
				// TODO...
				Face front, back;
				// split face into two parts...
				ret = face.Split( inNode.plane, out front, out back );
				
				if( ret == false )
					Debug.DebugBreak();
				
				// add to front and back
				frontFaces.Add( front );
				backFaces.Add( back );
				break;
			}
		}
		
		// optimizing a bit, clear in array list as we do not need it any more
		inFaces.Clear();
		
		// process front faces
		if( frontFaces.Count > 0 )
		{
			inNode.front = new BspNode();
			// partition front faces
			Partition( inNode.front, frontFaces );
		}
		
		
		// process back faces
		if( backFaces.Count > 0 )
		{
			inNode.back = new BspNode();
			// partition back faces
			Partition( inNode.back, backFaces );
		}
	}
	
	//
	bool FindSplitter( List<Face> inFaces, out Face outFace )
	{
		int increase = 1;
		int bestValue = 9999999;
		
		// reset out face...
		outFace = null;
		
		// setup optimization...
		switch( bspOptm )
		{
		case BspOptm_Worse:
			increase = Mathf.Max( 1, inFaces.Count / 24 );
			break;
		case BspOptm_Average:
			increase = Mathf.Max( 1, inFaces.Count / 12 );
			break;
		case BspOptm_Best:
		default:
			increase = 1;
			break;
		}
		
		// find best splitter plane
		for( int i = 0; i < inFaces.Count; i += increase )
		{
			// statistics
			int numSplits = 0, numFront = 0, numBack = 0, numPlanar = 0;
		
			// 
			Face splitterFace = inFaces[i] as Face;
			
			// 
			Plane splitterPlane = splitterFace.GetPlane();
			
			// sort all faces to side where it lies...
			for( int j = 0; j < inFaces.Count; ++j )
			{
				Face.EPlaneSide side = (inFaces[j] as Face).Side( splitterPlane );	
				
				switch( side )
				{
				case Face.EPlaneSide.Side_Front:
					numFront++;
					break;
				case Face.EPlaneSide.Side_Back:
					numBack++;
					break;
				case Face.EPlaneSide.Side_Planar:
					numPlanar++;
					break;
				case Face.EPlaneSide.Side_Split:
					numSplits++;
					break;
				default:
					//ERROR
					Debug.DebugBreak();
					break;
				}
			}
			
			// 
			int val = numSplits * 5 + Mathf.Abs( numFront - numBack ) + numPlanar;
		
			if( val < bestValue )
			{
				bestValue = val;
				outFace = splitterFace;
			}
		}
		
		// if we have a face found, return true
		return outFace != null;
	}
	
	// 
	public static void GetFaces( BspNode inNode, List<Face> outFaces )
	{
		while( inNode != null )
		{
			// if we are not destroyed
			if( (inNode.flags & BspNode.BspFlags_IsDestroyed) == 0 )
			{
				// add to array list
				outFaces.Add( inNode.face );
			}
			
			
			if( inNode.front != null )
			{
				GetFaces( inNode.front, outFaces );	
			}
			
			if( inNode.back != null )
			{
				GetFaces( inNode.back, outFaces );	
			}
			
			// get to the next planar node
			inNode = inNode.planar;
		}
	}
	
	
	public static BspNode AddNodeRecursive( BspNode inNode, Face inFace, int inFlags )
	{
		while( inNode != null )
		{
			Face.EPlaneSide planeSide = inFace.Side( inNode.plane );	
			
			switch( planeSide )
			{
			case Face.EPlaneSide.Side_Front:
				
				if( inNode.front == null )
					return AddNode( inNode, BspNode.EBspLocation.BspLocation_Front, inFace, inFlags );
				
				inNode = inNode.front;
				break;
			case Face.EPlaneSide.Side_Back:
				
				if( inNode.back == null )
					return AddNode( inNode, BspNode.EBspLocation.BspLocation_Back, inFace, inFlags );
				
				inNode = inNode.back;
				break;
			case Face.EPlaneSide.Side_Planar:
				return AddNode( inNode, BspNode.EBspLocation.BspLocation_Planar, inFace, inFlags );
				
			case Face.EPlaneSide.Side_Split:
				
				Face frontFace, backFace;
				
				inFace.Split( inNode.plane, out frontFace, out backFace );
				
				if( inNode.front == null )
				{
					AddNode( inNode, BspNode.EBspLocation.BspLocation_Front, frontFace, inFlags );
				}
				else
				{
					AddNodeRecursive( inNode.front, frontFace, inFlags );	
				}
				
				if( inNode.back == null )
				{
					AddNode( inNode, BspNode.EBspLocation.BspLocation_Back, inFace, inFlags );	
				}
				else
				{
					AddNodeRecursive( inNode.back, backFace, inFlags );	
				}
				
				inNode = null;
				break;
			}
		}
		
		// happens when face get splitted...
		return null;
	}
	
	public static BspNode AddNode( BspNode inParent, BspNode.EBspLocation inLocation, Face inFace, int inFlags )
	{
		BspNode newNode = new BspNode();
		
		newNode.plane = inFace.GetPlane();
		newNode.face = inFace;
		newNode.front = newNode.back = newNode.planar = null;
		newNode.flags = inFlags;
		
		if( inLocation == BspNode.EBspLocation.BspLocation_Front )
		{
			// check that front node is null
			inParent.front = newNode;
			
		}
		else if( inLocation == BspNode.EBspLocation.BspLocation_Back )
		{
			// TODO: check that back node is null
			inParent.back = newNode;	
		}
		else if( inLocation == BspNode.EBspLocation.BspLocation_Planar )
		{
			// go to the last planar node
			BspNode lastPlanar = inParent;
			
			while( lastPlanar.planar != null )
				lastPlanar = lastPlanar.planar;
			
			// add planar node
			lastPlanar.planar = newNode;
		}
		
		return newNode;
	}
	
}
