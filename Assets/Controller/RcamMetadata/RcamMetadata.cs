
using UnityEngine;
using System;
using System.Runtime.InteropServices;

//
// Rcam2 Metadata struct
//
// This is a blittable struct that contains application-dependent data. The
// Rcam Controller serializes this struct using base64 and attaches it to NDI
// frames as metadata.
//
[StructLayout(LayoutKind.Sequential)]
public unsafe struct RcamMetadata
{
    #region Data members

    // Camera pose
    public Vector3 CameraPosition;
    public Quaternion CameraRotation;

    // Camera parameters
    public Matrix4x4 ProjectionMatrix;
    public Vector2 DepthRange;

    // Control input state
    public InputState InputState;

    // Initial data constructor
    public static RcamMetadata InitialData =>
        new RcamMetadata
        {
            CameraRotation = Quaternion.identity,
            ProjectionMatrix = Matrix4x4.identity,
            DepthRange = new Vector2(0, 1)
        };

    #endregion

    #region Serialization/deserialization

    public string Serialize()
    {
        ReadOnlySpan<RcamMetadata> data = stackalloc RcamMetadata[] { this };
        var bytes = MemoryMarshal.AsBytes(data).ToArray();
        return "<![CDATA[" + System.Convert.ToBase64String(bytes) + "]] >";
    }

    public static RcamMetadata Deserialize(string xml)
    {
        var base64 = xml.Substring(9, xml.Length - 9 - 4);
        var data = System.Convert.FromBase64String(base64);
        return MemoryMarshal.Read<RcamMetadata>(new Span<byte>(data));
    }

    #endregion
}