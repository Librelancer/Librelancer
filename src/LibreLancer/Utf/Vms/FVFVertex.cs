using System;
using System.Collections.Generic;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;

namespace LibreLancer.Utf.Vms;

public struct FVFVertex : IVertexType
{
    private VertexDeclaration declaration;
    private int coordCount;
    D3DFVF fvf;

    public FVFVertex(D3DFVF fvf)
    {
        this.fvf = fvf;
        int stride = 12;
        var elems = new List<VertexElement>();
        elems.Add(new VertexElement(VertexSlots.Position, 3, VertexElementType.Float, false, 0));
        if ((FVF & D3DFVF.XYZ) != D3DFVF.XYZ)
            throw new InvalidOperationException("FVF must have XYZ set");
        if ((FVF & D3DFVF.NORMAL) == D3DFVF.NORMAL)
        {
            elems.Add(new VertexElement(VertexSlots.Normal, 3, VertexElementType.Float, false, 12));
            stride += 12;
        }
        if ((FVF & D3DFVF.DIFFUSE) == D3DFVF.DIFFUSE)
        {
            elems.Add(new VertexElement(VertexSlots.Color, 4, VertexElementType.UnsignedByte, true, stride));
            stride += 4;
        }
        if ((FVF & D3DFVF.TEX4) == D3DFVF.TEX4)
            coordCount = 4;
        else if ((FVF & D3DFVF.TEX3) == D3DFVF.TEX3)
            coordCount = 3;
        else if ((FVF & D3DFVF.TEX2) == D3DFVF.TEX2)
            coordCount = 2;
        else if ((FVF & D3DFVF.TEX1) == D3DFVF.TEX1)
            coordCount = 1;
        for (int i = 0; i < coordCount; i++) {
            elems.Add(new VertexElement(VertexSlots.Texture1 + i, 2, VertexElementType.Float, false,
                stride + i * 8));
        }
        stride += coordCount * 8;
        declaration = new VertexDeclaration(stride, elems.ToArray());
    }

    public D3DFVF FVF => fvf;
    public bool Normal => (FVF & D3DFVF.NORMAL) == D3DFVF.NORMAL;
    public bool Diffuse => (FVF & D3DFVF.DIFFUSE) == D3DFVF.DIFFUSE;
    public int TexCoords => coordCount;
    public int Stride => declaration.Stride;

    public VertexDeclaration GetVertexDeclaration() => declaration;

    public override string ToString() =>
            $"FVFPos{(Normal ? "Normal" : "")}{(Diffuse ? "Diffuse" : "")}{(TexCoords > 0 ? $"Tex{TexCoords}" : "")}";
}
