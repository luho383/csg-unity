using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CsgOperation {
	
	public enum ECsgOperation
	{
		CsgOper_Additive,
		CsgOper_Subtractive,
		CsgOper_Intersect,
		CsgOper_DeIntersect
	};
	
	// 
	const int SIDE_Inside 	= 0;
	const int SIDE_Outside 	= 1;
	
	
	public enum EPolySide
	{
		PolySide_Outside,
		PolySide_Inside,
		PolySide_Planar_Outside,
		PolySide_CoPlanar_Outside,
		PolySide_Planar_Inside,
		PolySide_CoPlanar_Inside
	};
	
	private enum EProcessState
	{
		Process_Master,
		Process_Slave,
	}
	
	private struct DeferredFace
	{
		public BspNode node;
		public Face face;		
	};
	
	public struct OperationInfo
	{
		// 
		public BspNode leafNode;
		public BspNode.EBspLocation leafLocation;
		// planar stuff
		public bool wasPlanar;
		public bool processingBack;
		public BspNode	backNode;
		public int planarSide;
	};
	
	
	private EProcessState	processState;
//	private ECsgOperation	csgOperation;
	private CsgVisitor		csgVisitor;
	private BspNode			currentNode;
	
	private List<DeferredFace> deferredFaces;
	
	public CsgOperation( CsgVisitor inVisitor )
	{
		// 
		csgVisitor = inVisitor;
		// 
		deferredFaces = new List<DeferredFace>(8);
	}
	
	
	// 
	public void Perform( ECsgOperation inOper, CSGObject inMaster, CSGObject inSlave )
	{
		// we are processing our slave faces
		processState = EProcessState.Process_Slave;
		
		// process faces against master tree
		PerformFaces( inMaster.rootNode, inSlave.faces );
		
		// process face from master tree
		processState = EProcessState.Process_Master;
		
		// perform master faces on slave bsp tree
		PerformTree( inMaster.rootNode, inSlave.rootNode );
		
		// check if how do we need to process generated faces
		if( inOper == ECsgOperation.CsgOper_Additive || inOper == ECsgOperation.CsgOper_Subtractive )
		{
			// add deferred faces to master tree...
			for( int i = 0; i < deferredFaces.Count; i++ )
			{
				Face defFace = ((DeferredFace)deferredFaces[i]).face;
				BspNode startNode = ((DeferredFace)deferredFaces[i]).node;
				
				// testing
				startNode = inMaster.rootNode;
				// add node to master tree
				BspGen.AddNodeRecursive( startNode, defFace, BspNode.BspFlags_IsNew );
			}
		}
		else
		{
			// clear old faces list
			inMaster.faces.Clear();
			
			// copy created faces
			for( int i = 0; i < deferredFaces.Count; i++ )
			{
				inMaster.faces.Add( deferredFaces[i].face );
			}
			
		}
		
		// clear deferred faces
		deferredFaces.Clear();
		
	}
	
	//
	private void PerformFaces( BspNode inRoot, List<Face> inFaces )
	{
		for( int i = 0; i < inFaces.Count; ++i )
		{
			OperationInfo info;
			InitializeOper( out info );
			
			PerformNode( inRoot, inFaces[i] as Face, SIDE_Outside, info );
		}
	}
	
	private void PerformTree( BspNode inNode, BspNode inOtherRoot )
	{
		while( inNode != null )
		{
			// if we are new, stop processing our node...
			if( (inNode.flags & BspNode.BspFlags_IsNew) != 0 )
				return;
			
			// if we are not destroyed
			if( (inNode.flags & BspNode.BspFlags_IsDestroyed) == 0 )
			{
				OperationInfo info;
				InitializeOper( out info );
				
				// save current node
				currentNode = inNode;
				
				// find last coplanar node
				BspNode lastPlanar = inNode;
				while( lastPlanar.planar != null )
					lastPlanar = lastPlanar.planar;
				
				// perform nodes face with other tree
				PerformNode( inOtherRoot, inNode.face, SIDE_Outside, info );
				
				// nobody removed us
				if( (inNode.flags & BspNode.BspFlags_IsDestroyed) == 0 )
				{
					// remove all planar nodes added (if any)
					lastPlanar.planar = null;
				}
			}
			
			
			// process front node
			if( inNode.front != null )
			{
				PerformTree( inNode.front, inOtherRoot );	
			}
			
			// process back node
			if( inNode.back != null )
			{
				PerformTree( inNode.back, inOtherRoot );	
			}
			
			// process next planar node
			inNode = inNode.planar;
			
		}
	}
	
	private void PerformNode( BspNode inNode, Face inFace, int nodeSide, OperationInfo info )
	{
		while( inNode != null )
		{
			Face.EPlaneSide side = inFace.Side( inNode.plane );			
			
			switch( side )
			{
			case Face.EPlaneSide.Side_Front:
				
				// 
				nodeSide = nodeSide | (inNode.IsCsg() ? 1 : 0);
				
				// leaf node
				if( inNode.front == null )
				{
					// set operation infos
					info.leafNode = inNode;
					info.leafLocation = BspNode.EBspLocation.BspLocation_Front;
					
					// we are done, process face
					ProcessFace( inFace, SIDE_Outside, info );
				}
				
				// get to next front node (if any)
				inNode = inNode.front;
				
				break;
			case Face.EPlaneSide.Side_Back:
				
				int backSide = inNode.IsCsg() ? 0 : 1;
				// 
				nodeSide = nodeSide & backSide;
				
				// leaf node
				if( inNode.back == null )
				{
					// set leaf infos
					info.leafNode = inNode;
					info.leafLocation = BspNode.EBspLocation.BspLocation_Back;
					
					// we are done, process face
					ProcessFace( inFace, SIDE_Inside, info );
				}
				
				// get to next front node (if any)
				inNode = inNode.back;
				break;
			case Face.EPlaneSide.Side_Split:
				
				// split face and process front and back
				Face frontFace, backFace;
				
				// 
				inFace.Split( inNode.plane, out frontFace, out backFace );
				
				// TODO: set polygon cutted flags
				frontFace.flags |= Face.FaceFlags_WasCutted;
				backFace.flags |= Face.FaceFlags_WasCutted;
				
				
				// front node is a leaf node
				if( inNode.front == null )
				{
					// 	
					info.leafNode = inNode;
					info.leafLocation = BspNode.EBspLocation.BspLocation_Front;
		
					
					ProcessFace( frontFace, SIDE_Outside, info );
				}
				else
				{
					PerformNode( inNode.front, frontFace, nodeSide, info );
				}
				
				// Prcess back node with back face
				if( inNode.back == null )
				{
					// 
					info.leafNode = inNode;
					info.leafLocation = BspNode.EBspLocation.BspLocation_Back;
		
					
					ProcessFace( backFace, SIDE_Inside, info );
				}
				else
				{
					// process back node with new face
					PerformNode( inNode.back, backFace, nodeSide, info );	
				}
				
				// stop loop
				inNode = null;
				break;
			case Face.EPlaneSide.Side_Planar:
				
				BspNode front, back;
				
				if( info.wasPlanar == true )
				{
					Debug.Log( "Reentering Planar Nodes!" );	
				}
				
				
				// set operation infos
				info.wasPlanar = true;
				info.backNode = null;
				info.processingBack = false;
				
				if( Vector3.Dot( inFace.GetPlane().normal, inNode.plane.normal ) >= 0.0f )
				{
					// same order as we face in the same order
					front = inNode.front;
					back = inNode.back;
					
					// we are for now outside (as we are looking outside)
					info.planarSide = SIDE_Outside;
				}
				else
				{
					// reverse order as we are facing in the opposite direction
					front = inNode.back;
					back = inNode.front;
					
					// we are now inside as we are looking to the inside
					info.planarSide = SIDE_Inside;
				}
				
				// we are leaf node (coplanar face)
				if( front == null && back == null )
				{
					// set leaf stuff
					info.leafNode = inNode;
					info.leafLocation = BspNode.EBspLocation.BspLocation_Planar;
									
					// process node
					info.processingBack = true;
					
					// process face
					ProcessFace( inFace, InverseSide(info.planarSide), info );
					
					// stop loop
					inNode = null;
				}
				else if( front == null && back != null )
				{
					// only back nodes
					info.processingBack = true;
					
					// process back
					inNode = back;
				}
				else
				{
					
					// tread like we were on front side (maybe we do have a back node)
					info.processingBack = false;
					
					// remember back node
					info.backNode = back;
					
					// process front
					inNode = front;
				}
				
				break;
			}
			
			
		}
	}
	
	// 
	private void ProcessFace( Face inFace, int inNodeSide, OperationInfo info )
	{
		EPolySide polySide;
		
		// never on a planar node, really easy 
		if( info.wasPlanar == false )
		{
			// set polyside
			polySide = (inNodeSide == SIDE_Outside) ? EPolySide.PolySide_Outside : EPolySide.PolySide_Inside;
			
			// 
			RouteOper( null, inFace, polySide, info );
		}
		else if( info.processingBack )
		{
			// 
			if( inNodeSide == info.planarSide )
			{
				polySide = (inNodeSide == SIDE_Inside) ? EPolySide.PolySide_Planar_Inside : EPolySide.PolySide_Planar_Outside;	
			}
			else
			{	
				polySide = (info.planarSide == SIDE_Inside) ? EPolySide.PolySide_CoPlanar_Inside : EPolySide.PolySide_CoPlanar_Outside;	
			}
		
			
			RouteOper( null, inFace, polySide, info );
		}
		else
		{
			// 
			int backNodeSide = InverseSide(info.planarSide);
			
			// 
			info.planarSide = inNodeSide;
			
			// back node is empty
			if( info.backNode == null )
			{
				// back tree is empty
				inNodeSide = backNodeSide;
				
				// back node is empty
				if( inNodeSide == info.planarSide )
				{
					polySide = (inNodeSide == SIDE_Inside) ? EPolySide.PolySide_Planar_Inside : EPolySide.PolySide_Planar_Outside;	
				}
				else
				{	
					polySide = (info.planarSide == SIDE_Inside) ? EPolySide.PolySide_CoPlanar_Inside : EPolySide.PolySide_CoPlanar_Outside;	
				}
		
				RouteOper( null, inFace, polySide, info );
				
			}
			else
			{
				info.processingBack = true;
				
				// TODO: conversion
				inNodeSide = backNodeSide;
				
				// 
				PerformNode( info.backNode, inFace, inNodeSide, info );
				
			}
			
		}
		
	}
	
	// 
	void RouteOper( BspNode inNode, Face inFace, EPolySide inSide, OperationInfo info )
	{
		if( processState == EProcessState.Process_Master )
		{
		 	csgVisitor.ProcessMaster( this, inFace, inSide, info );
		}
		else
		{
			csgVisitor.ProcessSlave( this, inFace, inSide, info );
		}
	}
	
	
	/* Intersection
	
	
	// Master Faces against Slave Tree
	private void AdditiveMaster( Face inFace, EPolySide inSide, OperationInfo info )
	{
		Debug.Log( inSide );
		
		switch( inSide )
		{
		case EPolySide.PolySide_Outside:
		case EPolySide.PolySide_Planar_Front:
			// discard original node
			currentNode.flags |= BspNode.BspFlags_IsDestroyed;
			break;
		case EPolySide.PolySide_Inside:
		case EPolySide.PolySide_Planar_Back:
		case EPolySide.PolySide_CoPlanar_Back:
		case EPolySide.PolySide_CoPlanar_Front:
			// add cutted polygons
			if( (inFace.flags & Face.FaceFlags_WasCutted) != 0 )
				BspGen.AddNode( currentNode, BspNode.EBspLocation.BspLocation_Planar, inFace, BspNode.BspFlags_IsNew );
			break;
			
		}
	}
	
	// Slave Polygons against Master Tree
	private void AdditiveSlave( Face inFace, EPolySide inSide, OperationInfo info )
	{
		
//		Debug.Log( inSide );
		
		switch( inSide )
		{
		case EPolySide.PolySide_Outside:
		case EPolySide.PolySide_CoPlanar_Front:
		case EPolySide.PolySide_Planar_Front:
			break;
		case EPolySide.PolySide_Inside:
		case EPolySide.PolySide_Planar_Back:
		case EPolySide.PolySide_CoPlanar_Back:
			
			// add to deferred faces
			DeferredFace defFace;
			defFace.face = inFace;
			defFace.node = currentNode;
			deferredFaces.Add( defFace );
			break;
			
		}
	}
	*/
	
	// Callback for Visitor
	public void AddPlanarFace( Face inFace )
	{
		BspGen.AddNode( currentNode, BspNode.EBspLocation.BspLocation_Planar, inFace, BspNode.BspFlags_IsNew );	
	}
	
	public void AddDeferredFace( Face inFace )
	{
		// add to deferred faces
		DeferredFace defFace;
		defFace.face = inFace;
		defFace.node = currentNode;
		deferredFaces.Add( defFace );	
	}
	
	public void MarkNodeAsDestroyed()
	{
		currentNode.flags |= BspNode.BspFlags_IsDestroyed;
	}
	
	
	private static EPolySide InverseSide( EPolySide side )
	{
		return (side == EPolySide.PolySide_Inside) ? EPolySide.PolySide_Outside : EPolySide.PolySide_Inside;	
	}
	
	private static int InverseSide( int inSide )
	{
		return inSide == SIDE_Inside ? SIDE_Outside : SIDE_Inside;	
	}
	
	private static void InitializeOper( out OperationInfo outInfo )
	{
		outInfo.leafNode = null;
		outInfo.leafLocation = BspNode.EBspLocation.BspLocation_Planar;
		
		outInfo.wasPlanar = false;
		outInfo.processingBack = false;
		outInfo.backNode = null;
		outInfo.planarSide = SIDE_Outside;
	}
	
	
}
