using UnityEngine;
using System.Collections;


public class UnionVisitor : CsgVisitor {

	
	public void ProcessMaster( CsgOperation inOperation, Face inFace, CsgOperation.EPolySide inSide, CsgOperation.OperationInfo info )
	{
		switch( inSide )
		{
		case CsgOperation.EPolySide.PolySide_Outside:
		case CsgOperation.EPolySide.PolySide_Planar_Outside:
			// add cutted polygons
			if( (inFace.flags & Face.FaceFlags_WasCutted) != 0 )
				inOperation.AddPlanarFace( inFace );
			break;
		case CsgOperation.EPolySide.PolySide_Inside:
		case CsgOperation.EPolySide.PolySide_Planar_Inside:
		case CsgOperation.EPolySide.PolySide_CoPlanar_Outside:
		case CsgOperation.EPolySide.PolySide_CoPlanar_Inside:
			// discard original node
			inOperation.MarkNodeAsDestroyed();
			break;
		}
	}
	
	public void ProcessSlave( CsgOperation inOperation, Face inFace, CsgOperation.EPolySide inSide, CsgOperation.OperationInfo info )
	{
		switch( inSide )
		{
		case CsgOperation.EPolySide.PolySide_Outside:
		case CsgOperation.EPolySide.PolySide_CoPlanar_Outside:
		case CsgOperation.EPolySide.PolySide_Planar_Outside:
			// add to deferred faces
			inOperation.AddDeferredFace( inFace );
			break;
		case CsgOperation.EPolySide.PolySide_Inside:
		case CsgOperation.EPolySide.PolySide_Planar_Inside:
		case CsgOperation.EPolySide.PolySide_CoPlanar_Inside:
			break;
		}
	}
	
}
