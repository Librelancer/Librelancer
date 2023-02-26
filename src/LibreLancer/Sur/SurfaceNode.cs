using System.IO;
using System.Numerics;

namespace LibreLancer.Sur;

public class SurfaceNode
{
    public Vector3 Center;
    public float Radius;
    public Vector3 Scale;
    public byte Unknown;

    public SurfaceHull Hull;
    public SurfaceNode Left;
    public SurfaceNode Right;

    public static SurfaceNode Read(BinaryReader reader)
    {
        return new SurfaceNode()
        {
            Center = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
            Radius = reader.ReadSingle(),
            Scale = new Vector3(reader.ReadByte(), reader.ReadByte(), reader.ReadByte()) / 0xFA,
            Unknown = reader.ReadByte()
        };
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Center.X);
        writer.Write(Center.Y);
        writer.Write(Center.Z);
        writer.Write(Radius);
        writer.Write((byte)(Scale.X * 0xFA));
        writer.Write((byte)(Scale.Y * 0xFA));
        writer.Write((byte)(Scale.Z * 0xFA));
        writer.Write(Unknown);
    }
}