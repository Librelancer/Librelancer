// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Graphics;

//D3DBLEND
public enum BlendOp : byte
{
    Invalid = 0,
    Zero = 1,
    One = 2,
    SrcColor = 3,
    InvSrcColor = 4,
    SrcAlpha = 5,
    InvSrcAlpha = 6,
    DstAlpha = 7,
    InvDstAlpha = 8,
    DstColor = 9,
    InvDstColor = 10,
    SrcAlphaSat = 11
}

public static class BlendMode
{
    public const ushort Opaque = 0;
    public const ushort Normal = ((ushort)BlendOp.SrcAlpha << 8) | (ushort)BlendOp.InvSrcAlpha;
    public const ushort Additive = ((ushort)BlendOp.SrcAlpha << 8) | (ushort)BlendOp.One;
    public const ushort OneInvSrcColor = ((ushort)BlendOp.One << 8) | (ushort)BlendOp.InvSrcColor;

    internal static void Validate(ushort blend)
    {
        Validate((ushort)((blend >> 8) & 0xFF), (ushort)(blend & 0xFF));
    }

    private static void Validate(ushort src, ushort dst)
    {
        if (src > 11) throw new ArgumentOutOfRangeException(nameof(src));
        if (dst > 11) throw new ArgumentOutOfRangeException(nameof(dst));
        if(src == 0 && dst != 0) throw new ArgumentOutOfRangeException(nameof(dst));
        if(dst == 0 && src != 0) throw new ArgumentOutOfRangeException(nameof(src));
    }

    public static ushort Create(ushort src, ushort dst)
    {
        Validate(src, dst);
        return (ushort)((src << 8) | dst);
    }

    public static ushort Create(BlendOp src, BlendOp dst) => Create((ushort)src, (ushort)dst);

    public static (BlendOp, BlendOp) Deconstruct(ushort blendMode)
    {
        var src = (blendMode >> 8) & 0xFF;
        var dst = blendMode & 0xFF;
        return ((BlendOp)src, (BlendOp)dst);
    }
}
