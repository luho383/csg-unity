using UnityEngine;
using System.Collections;

public interface CsgVisitor {
	
	// Will be called for Master Faces on Slave BSP Tree
	void ProcessMaster( CsgOperation inOperation, Face inFace, CsgOperation.EPolySide inSide, CsgOperation.OperationInfo info );
	// Will be called on Slave Faces on Master BSP Tree
	void ProcessSlave( CsgOperation inOperation, Face inFace, CsgOperation.EPolySide inSide, CsgOperation.OperationInfo info );
	
}
