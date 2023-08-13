using System;
using System.Numerics;
using System.Text;

namespace ImGuiNET;

public static partial class ImGui
{
    public static unsafe bool Button(char icon)
    {
        Span<byte> str = stackalloc byte[5];
        Span<char> c = stackalloc char[1];
        c[0] = icon;
        int l = Encoding.UTF8.GetBytes(c, str);
        str[l] = 0;
        fixed(byte* b = str)
            return ImGuiNative.igButton(b, new Vector2()) != 0;
    }
}