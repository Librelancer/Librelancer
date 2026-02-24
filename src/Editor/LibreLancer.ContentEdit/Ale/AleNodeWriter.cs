using System;
using System.Globalization;
using System.IO;
using System.Text;
using LibreLancer.Utf.Ale;

namespace LibreLancer.ContentEdit.Ale;

public static class AleNodeWriter
{
    public static byte[] WriteALEffectLib(ALEffectLib fxlib)
    {
        var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);

        bw.Write(1.0f); // No extra floats (1.1f == 16 unused bytes per fx)
        bw.Write(fxlib.Effects.Count);
        foreach (var fx in fxlib.Effects)
        {
            WriteString(bw, fx.Name);
            bw.Write(fx.Fx.Count);
            foreach(var fxref in fx.Fx)
            {
                bw.Write(fxref.Flag);
                bw.Write(fxref.CRC);
                bw.Write(fxref.Parent);
                bw.Write(fxref.Index);
            }
            bw.Write(fx.Pairs.Count);
            foreach (var p in fx.Pairs)
            {
                bw.Write(p.Source);
                bw.Write(p.Target);
            }
        }
        return ms.ToArray();
    }

    public static byte[] WriteAlchemyNodeLibrary(AlchemyNodeLibrary nodelib)
    {
        var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);
        bw.Write(1.1f); // Match FL
        bw.Write(nodelib.Nodes.Count);
        foreach (var n in nodelib.Nodes)
        {
            WriteString(bw, n.ClassName);
            foreach (var p in n.Parameters)
            {
                WriteParameter(bw, p);
            }
            bw.Write((ushort)0);
        }
        return ms.ToArray();
    }

    static void WriteString(BinaryWriter bw, string str)
    {
        var bytes = Encoding.ASCII.GetBytes(str);
        bw.Write((ushort)(bytes.Length + 1));
        bw.Write(bytes);
        bw.Write((byte)0);
        if (((bytes.Length + 1) & 1) != 0)
        {
            bw.Write((byte)0); // Alignment
        }
    }

    public static void WriteParameter(BinaryWriter bw, AleParameter param)
    {
        if (param.Value == null)
        {
            throw new InvalidOperationException("Cannot write AleParameter of type null");
        }
        if (param.Value is bool b)
        {
            bw.Write((ushort)(b ? 0x8001 : 0x1));
        }
        else
        {
            bw.Write((ushort)(param.Value switch
            {
                uint => AleTypes.Integer,
                float => AleTypes.Float,
                string => AleTypes.Name,
                Tuple<uint,uint> => AleTypes.IPair,
                AlchemyTransform => AleTypes.Transform,
                AlchemyFloatAnimation => AleTypes.FloatAnimation,
                AlchemyColorAnimation => AleTypes.ColorAnimation,
                AlchemyCurveAnimation => AleTypes.CurveAnimation,
                _ => throw new InvalidOperationException($"Cannot write AleParameter of type {(param.Value.GetType())}"),
            }));
        }
        bw.Write((uint)param.Name);
        switch (param.Value)
        {
            case uint u:
                bw.Write(u);
                break;
            case float f:
                bw.Write(f);
                break;
            case string s:
                WriteString(bw,s);
                break;
            case Tuple<uint, uint> tu:
                bw.Write(tu.Item1);
                bw.Write(tu.Item2);
                break;
            case AlchemyTransform tr:
                WriteTransform(bw, tr);
                break;
            case AlchemyCurveAnimation cr:
                WriteCurveAnimation(bw, cr);
                break;
            case AlchemyFloatAnimation fl:
                WriteFloatAnimation(bw, fl);
                break;
            case AlchemyColorAnimation cl:
                WriteColorAnimation(bw, cl);
                break;
        }
    }

    static void WriteColorAnimation(BinaryWriter bw, AlchemyColorAnimation cl)
    {
        bw.Write((byte)cl.Type);
        bw.Write((byte)cl.Items.Count);
        foreach (var item in cl.Items)
        {
            bw.Write(item.SParam);
            bw.Write((byte)item.Type);
            bw.Write((byte)item.Keyframes.Count);
            foreach (var kf in item.Keyframes)
            {
                bw.Write(kf.Time);
                bw.Write(kf.Value.R);
                bw.Write(kf.Value.G);
                bw.Write(kf.Value.B);
            }
        }
    }

    static void WriteFloatAnimation(BinaryWriter bw, AlchemyFloatAnimation fl)
    {
        bw.Write((byte)fl.Type);
        bw.Write((byte)fl.Items.Count);
        foreach (var item in fl.Items)
        {
            bw.Write(item.SParam);
            bw.Write((byte)item.Type);
            bw.Write((byte)item.Keyframes.Count);
            foreach (var kf in item.Keyframes)
            {
                bw.Write(kf.Time);
                bw.Write(kf.Value);
            }
        }
    }

    static void WriteCurveAnimation(BinaryWriter bw, AlchemyCurveAnimation ca)
    {
        bw.Write((byte)ca.Type);
        bw.Write((byte)ca.Items.Count);
        foreach (var item in ca.Items)
        {
            bw.Write(item.SParam);
            bw.Write(item.Value);
            bw.Write((ushort)item.Flags);
            if (item.Keyframes is { Count: > 0 })
            {
                bw.Write((ushort)item.Keyframes.Count);
                foreach (var k in item.Keyframes)
                {
                    bw.Write(k.Time);
                    bw.Write(k.Value);
                    bw.Write(k.End);
                    bw.Write(k.Start);
                }
            }
            else
            {
                bw.Write((ushort)0);
            }
        }
    }

    static void WriteTransform(BinaryWriter bw, AlchemyTransform tr)
    {
        //Xform 0x4 0x3 0x5 (doesn't have another value in vanilla)
        bw.Write((byte)0x4);
        bw.Write((byte)0x3);
        bw.Write((byte)0x5);
        // Write curves if present
        bw.Write(tr.HasTransform ? (byte)1 : (byte)0);
        if (!tr.HasTransform)
            return;
        WriteCurveAnimation(bw, tr.TranslateX);
        WriteCurveAnimation(bw, tr.TranslateY);
        WriteCurveAnimation(bw, tr.TranslateZ);
        WriteCurveAnimation(bw, tr.RotatePitch);
        WriteCurveAnimation(bw, tr.RotateYaw);
        WriteCurveAnimation(bw, tr.RotateRoll);
        WriteCurveAnimation(bw, tr.ScaleX);
        WriteCurveAnimation(bw, tr.ScaleY);
        WriteCurveAnimation(bw, tr.ScaleZ);
    }
}
