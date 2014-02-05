using UnityEngine;
using System.Collections;

public class TextureMapper {

	Vector3[]	basis;
	float[]		offset = new float[2];
	Vector2		scale;
	float		rotation;
	
	
	public TextureMapper( Plane inPlane, float inScaleU, float inScaleV, float inOffsetU, float inOffsetV, float inRotation )
	{
		Vector3[] baseAxis = new Vector3[]{
				new Vector3( 0.0f, 1.0f, 0.0f ), new Vector3( 1.0f, 0.0f, 0.0f ), new Vector3( 0.0f, 0.0f,-1.0f ), // floor
				new Vector3( 0.0f,-1.0f, 0.0f ), new Vector3(-1.0f, 0.0f, 0.0f ), new Vector3( 0.0f, 0.0f, 1.0f ), // ceiling
				new Vector3( 0.0f, 0.0f, 1.0f ), new Vector3( 1.0f, 0.0f, 0.0f ), new Vector3( 0.0f,-1.0f, 0.0f ), // north
				new Vector3( 0.0f, 0.0f,-1.0f ), new Vector3(-1.0f, 0.0f, 0.0f ), new Vector3( 0.0f,-1.0f, 0.0f ), // south
				new Vector3( 1.0f, 0.0f, 0.0f ), new Vector3( 0.0f, 0.0f, 1.0f ), new Vector3( 0.0f,-1.0f, 0.0f ), // west
				new Vector3(-1.0f, 0.0f, 0.0f ), new Vector3( 0.0f, 0.0f,-1.0f ), new Vector3( 0.0f,-1.0f, 0.0f )  // east
		};
		
		// 
		offset[0] = inOffsetU / 512.0f;
		offset[1] = inOffsetV / 512.0f;
		// 
		scale = new Vector2( 1.0f / inScaleU, 1.0f / inScaleV );
		// 
		rotation = inRotation;
		
		
		float bestDot = 0.0f;
		int bestAxis = 0;
		
		for( int i = 0; i < 6; i++ )
		{
			float dot = Vector3.Dot( inPlane.normal, baseAxis[i*3] );
			
			if( dot > bestDot )
			{
				bestDot = dot;
				bestAxis = i;
			}
		}
		
		// set base axis
		basis = new Vector3[2];
		
		// 
		basis[0] = new Vector3( baseAxis[bestAxis*3+1].x, baseAxis[bestAxis*3+1].y, baseAxis[bestAxis*3+1].z );
		basis[1] = new Vector3( baseAxis[bestAxis*3+2].x, baseAxis[bestAxis*3+2].y, baseAxis[bestAxis*3+2].z );
		
		// default texture scaling
		basis[0] /= 1.0f;
		basis[1] /= 1.0f;
	}
	
	private float Vector3Component( int component, Vector3 vec )
	{
		return (component == 0) ? vec.x : (component == 1) ? vec.y : vec.z;	
	}
	
	private void SetVector3Component( int component, Vector3 vec, float inValue )
	{
		switch( component )
		{
		case 0:
			vec.x = inValue;
			break;
		case 1:
			vec.y = inValue;
			break;
		case 2:
		default:
			vec.z = inValue;
			break;
		}
	}
	
	public Vector2 Project( Vector3 inPt )
	{
		Vector3[] rotatedBasis = new Vector3[]{
			basis[0], basis[1]	
		};
		
		float rotSin, rotCos;
		
		switch( (int)rotation )
		{
		case 0:
			rotSin = 0.0f;
			rotCos = 1.0f;
			break;
		case 90:
			rotSin = 1.0f;
			rotCos = 0.0f;
			break;
		case 180:
			rotSin = 0.0f;
			rotCos = -1.0f;
			break;
		case 270:
			rotSin = -1.0f;
			rotCos = 0.0f;
			break;		
		default:
			float radian = Mathf.Deg2Rad * rotation;
			rotSin = Mathf.Sin( radian );
			rotCos = Mathf.Cos( radian );
			break;
		}
		
		int baseEntryU = basis[0].x != 0.0f ? 0 : basis[0].y != 0.0f ? 1 : 2;
		int baseEntryV = basis[1].x != 0.0f ? 0 : basis[1].y != 0.0f ? 1 : 2;
		
		// rotate basis vectors
		for( int i = 0; i < 2; i++ )
		{
			float u = rotCos * Vector3Component( baseEntryU, basis[i] ) - rotSin * Vector3Component( baseEntryV, basis[i] );
			float v = rotCos * Vector3Component( baseEntryU, basis[i] ) + rotSin * Vector3Component( baseEntryV, basis[i] );
	//		rotatedBasis[0]	
			
			SetVector3Component( baseEntryU, rotatedBasis[i], u );
			SetVector3Component( baseEntryV, rotatedBasis[i], v );
		}
		
		Vector2 result = new Vector2();
		result.x = ( Vector3.Dot( inPt, rotatedBasis[0] ) / scale.x ) + offset[0];
		result.y = ( Vector3.Dot( inPt, rotatedBasis[1] ) / scale.y ) + offset[1];
		
		return result;
	}
	
}
