using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using LibreLancer.Utf.Anm;

namespace LibreLancer.ContentEdit.Model;

public class EditableAnmBuffer : AnmBuffer
{
    public EditableAnmBuffer(ref Channel source)
    {
        Buffer = source.GetDataCopy();
    }

    public EditableAnmBuffer(int initial = 2048)
    {
        Buffer = new byte[initial];
    }

    public void EnsureSize(int size)
    {
        if (Buffer.Length < size)
        {
            int newSize = Math.Max(Buffer.Length * 2, size);
            Array.Resize(ref Buffer, newSize);
        }
    }

    public void SetTime(ref Channel c, int index, float time)
    {
        if (c.Interval > 0)
            throw new InvalidOperationException("Channel type not valid");
        Unsafe.WriteUnaligned(ref Buffer[c.Stride * index], time);
    }

    public void SetAngle(ref Channel c, int index, float angle)
    {
        if (!c.HasAngle)
            throw new InvalidOperationException("Channel type not valid");
        Unsafe.WriteUnaligned(ref Buffer[c.Stride * index + (c.Interval <= 0 ? 4 : 0)], angle);
    }

    public void SetVector(ref Channel c, int index, Vector3 vector)
    {
        if ((c.ChannelType & 0x2) != 0x2)
            throw new InvalidOperationException("Channel type not valid");
        Unsafe.WriteUnaligned(ref Buffer[c.Stride * index + (c.Interval <= 0 ? 4 : 0)], vector);
    }

    public void SetQuaternion(ref Channel c, int index, Quaternion quaternion)
    {
        var field = (c.Interval <= 0 ? 4 : 0) + ((c.ChannelType & 0x2) == 0x2 ? 12 : 0);
        var off = c.Stride * index + field;
        switch (c.QuaternionMethod)
        {
            case QuaternionMethod.Full:
                Unsafe.WriteUnaligned(ref Buffer[off],
                    new Vector4(quaternion.W, quaternion.X, quaternion.Y, quaternion.Z));
                break;
            default:
                throw new InvalidOperationException("Channel type not valid");
        }
    }

    public override int Take(int size)
    {
        throw new InvalidOperationException("Should never be called");
    }
}
