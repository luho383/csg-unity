using UnityEngine;
using System.Collections;

public class BspNode {
	
	public const int BspFlags_IsDestroyed 	= 0x00000001;
	public const int BspFlags_IsNew			= 0x00000002;
	
	
	public enum EBspLocation
	{	
		BspLocation_Front,
		BspLocation_Back,
		BspLocation_Planar
	};
	
	
	public Plane plane;
	public BspNode front, back, planar;
	public int flags;
	public Face face;
	
	
	public bool IsCsg()
	{
		return ((flags & BspFlags_IsDestroyed) == 0 && (flags & BspFlags_IsNew) == 0);	
	}
	
	
}
