using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

/// <summary>
/// Reference Article http://www.codeproject.com/KB/tips/SerializedObjectCloner.aspx
/// Provides a method for performing a deep copy of an object.
/// Binary Serialization is used to perform the copy.
/// </summary>
public static class ObjectCloner
{
    /// <summary>
    /// Perform a deep Copy of the object.
    /// </summary>
    /// <typeparam name="T">The type of object being copied.</typeparam>
    /// <param name="source">The object instance to copy.</param>
    /// <returns>The copied object.</returns>
    public static T Clone<T>(T source)
    {
        if (!typeof(T).IsSerializable)
        {
//            throw new ArgumentException("The type must be serializable.", "source");
        }

        // Don't serialize a null object, simply return the default for that object
        if (Object.ReferenceEquals(source, null))
        {
            return default(T);
        }

        IFormatter formatter = new BinaryFormatter();
        Stream stream = new MemoryStream();
        using (stream)
        {
            formatter.Serialize(stream, source);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)formatter.Deserialize(stream);
        }
    }
	
	
	
	public static Mesh CloneMesh( Mesh inMesh )
	{
		Mesh newMesh = new Mesh();
		
		// this should work as the docs explain that you get a copy
		newMesh.vertices = inMesh.vertices;
		
		newMesh.uv = (Vector2[])inMesh.uv.Clone();
		newMesh.triangles = (int[])inMesh.triangles.Clone();
		newMesh.normals = (Vector3[])inMesh.normals.Clone();
		newMesh.tangents = (Vector4[])inMesh.tangents.Clone();
		
		newMesh.RecalculateBounds();
		newMesh.Optimize();
		
		
		return newMesh;
	}
	
	
}    