using UnityEngine;
using System.Collections;


public class IntersectVisitor : CsgVisitor {
	
	// TODO
	
	public void ProcessMaster( CsgOperation inOperation, Face inFace, CsgOperation.EPolySide inSide, CsgOperation.OperationInfo info )
	{
		switch( inSide )
		{
		case CsgOperation.EPolySide.PolySide_Outside:
		case CsgOperation.EPolySide.PolySide_Planar_Outside:
		case CsgOperation.EPolySide.PolySide_CoPlanar_Outside:
		case CsgOperation.EPolySide.PolySide_CoPlanar_Inside:
			break;
		case CsgOperation.EPolySide.PolySide_Inside:
		case CsgOperation.EPolySide.PolySide_Planar_Inside:
			// add to deferred faces
			inOperation.AddDeferredFace( inFace );
			break;
		}
	}
	
	public void ProcessSlave( CsgOperation inOperation, Face inFace, CsgOperation.EPolySide inSide, CsgOperation.OperationInfo info )
	{
		switch( inSide )
		{
		case CsgOperation.EPolySide.PolySide_Outside:
		case CsgOperation.EPolySide.PolySide_CoPlanar_Inside:
		case CsgOperation.EPolySide.PolySide_Planar_Outside:
			break;
		case CsgOperation.EPolySide.PolySide_Inside:
		case CsgOperation.EPolySide.PolySide_Planar_Inside:
		case CsgOperation.EPolySide.PolySide_CoPlanar_Outside:
			// add to deferred faces
			inOperation.AddDeferredFace( inFace );
			break;
		}
	}
	
}
