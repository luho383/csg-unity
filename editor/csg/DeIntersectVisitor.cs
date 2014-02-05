using UnityEngine;
using System.Collections;


public class DeIntersectVisitor : CsgVisitor {

	// TODO
	
	public void ProcessMaster( CsgOperation inOperation, Face inFace, CsgOperation.EPolySide inSide, CsgOperation.OperationInfo info )
	{
		switch( inSide )
		{
		case CsgOperation.EPolySide.PolySide_Outside:
		case CsgOperation.EPolySide.PolySide_Planar_Outside:
		case CsgOperation.EPolySide.PolySide_CoPlanar_Outside:
			break;
		case CsgOperation.EPolySide.PolySide_Inside:
		case CsgOperation.EPolySide.PolySide_CoPlanar_Inside:
		case CsgOperation.EPolySide.PolySide_Planar_Inside:
			// clone face
			Face newFace = (Face)inFace.Clone();
			newFace.Reverse();
			// add to deferred faces
			inOperation.AddDeferredFace( newFace );
			break;
		}
	}
	
	public void ProcessSlave( CsgOperation inOperation, Face inFace, CsgOperation.EPolySide inSide, CsgOperation.OperationInfo info )
	{
		switch( inSide )
		{
		case CsgOperation.EPolySide.PolySide_Outside:
		case CsgOperation.EPolySide.PolySide_Planar_Outside:
			// add to deferred faces
			inOperation.AddDeferredFace( inFace );
			break;
		case CsgOperation.EPolySide.PolySide_Inside:
		case CsgOperation.EPolySide.PolySide_CoPlanar_Inside:
		case CsgOperation.EPolySide.PolySide_Planar_Inside:
		case CsgOperation.EPolySide.PolySide_CoPlanar_Outside:
			break;
		}
	}
	
}
