using System;
using System.Numerics;
using LibreLancer;
using LibreLancer.Render;

namespace LancerEdit;

public class EditorPrimitives
{
    static Vector3[] GetBoxMesh(BoundingBox box)
    {
        var a = new Vector3(box.Min.X, box.Max.Y, box.Max.Z); 
        var b = new Vector3(box.Max.X, box.Max.Y, box.Max.Z); 
        var c = new Vector3(box.Max.X, box.Min.Y, box.Max.Z);
        var d = new Vector3(box.Min.X, box.Min.Y, box.Max.Z); 
        var e = new Vector3(box.Min.X, box.Max.Y, box.Min.Z); 
        var f = new Vector3(box.Max.X, box.Max.Y, box.Min.Z); 
        var g = new Vector3(box.Max.X, box.Min.Y, box.Min.Z); 
        var h = new Vector3(box.Min.X, box.Min.Y, box.Min.Z); 
        return new[]
        {
            a,b,
            c,d,
            e,f,
            g,h,
            a,e,
            b,f,
            c,g,
            d,h,
            a,d,
            b,c,
            e,h,
            f,g
        };
    }
    
    public static void DrawBox(LineRenderer renderer, BoundingBox box, Matrix4x4 mat, Color4 color)
    {
        var lines = GetBoxMesh(box);
        for (int i = 0; i < lines.Length / 2; i++)
        {
            renderer.DrawLine(
                Vector3.Transform(lines[i * 2],mat),
                Vector3.Transform(lines[i * 2 + 1],mat),
                color
            );
        }
    }
    
    
}