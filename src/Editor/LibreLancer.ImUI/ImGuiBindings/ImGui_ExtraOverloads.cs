using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using LibreLancer;

namespace ImGuiNET;

public static unsafe partial class ImGui
{
    public static unsafe bool Button(char icon)
    {
        Span<byte> str = stackalloc byte[5];
        Span<char> c = stackalloc char[1];
        c[0] = icon;
        int l = Encoding.UTF8.GetBytes(c, str);
        str[l] = 0;
        fixed(byte* b = str)
            return ImGuiNative.ImGui_Button(b, new Vector2()) != 0;
    }

    public static bool MenuItem(string label, bool enabled)
        => MenuItem(label, null, false, enabled);

    static bool UTF8ZEqual(byte* string0, byte* string1, int max)
    {
        for (int i = 0; i < max; i++)
        {
            // Null terminator
            if (string0[i] == 0 &&
                string1[i] == 0)
            {
                return true;
            }
            if (string0[i] != string1[i])
                return false;
        }
        return true;
    }

    public static bool InputText(string label, byte[] buf, uint buf_size,
        ImGuiInputTextFlags flags = (ImGuiInputTextFlags)0, ImGuiInputTextCallback callback = null,
        IntPtr user_data = 0)
    {
        fixed (byte* ptr = buf)
        {
            return InputText(label, (IntPtr)ptr, (IntPtr)buf_size, flags, callback, user_data);
        }
    }

    public static bool InputText(string label, ref string buf, uint buf_size,
        ImGuiInputTextFlags flags = (ImGuiInputTextFlags)0, ImGuiInputTextCallback callback = null,
        IntPtr user_data = 0)
    {
        byte* originalBuf = stackalloc byte[1024];
        byte* editBuf = stackalloc byte[1024];
        if ((flags & ImGuiInputTextFlags.ReadOnly) == ImGuiInputTextFlags.ReadOnly)
        {
            using var utf8z = new UTF8ZHelper(originalBuf, 1024, buf ?? "");
            return InputText(label, (IntPtr)utf8z.Pointer, (IntPtr)buf_size, flags, callback, user_data);
        }
        buf ??= "";
        using var utf8z_original = new UTF8ZHelper(originalBuf, 1024, buf);
        int inputBufSize = Math.Max((int)buf_size + 1, utf8z_original.ByteCount + 1);
        var alloc = IntPtr.Zero;
        if (inputBufSize > 1024)
        {
            alloc = Marshal.AllocHGlobal(inputBufSize);
            editBuf = (byte*)alloc;
        }
        Buffer.MemoryCopy(utf8z_original.Pointer, editBuf, inputBufSize, utf8z_original.ByteCount);
        editBuf![utf8z_original.ByteCount] = 0;
        var result = InputText(label, (IntPtr)editBuf, (IntPtr)buf_size, flags, callback, user_data);
        if (!UTF8ZEqual(utf8z_original.Pointer, editBuf, (int)buf_size))
        {
            buf = Marshal.PtrToStringUTF8((IntPtr)editBuf);
        }
        if (alloc != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(alloc);
        }
        return result;
    }

    public static bool InputTextMultiline(string label, ref string buf, uint buf_size,
        System.Numerics.Vector2 size = default, ImGuiInputTextFlags flags = (ImGuiInputTextFlags)0,
        ImGuiInputTextCallback callback = null, IntPtr user_data = 0)
    {
        byte* originalBuf = stackalloc byte[1024];
        byte* editBuf = stackalloc byte[1024];
        if ((flags & ImGuiInputTextFlags.ReadOnly) == ImGuiInputTextFlags.ReadOnly)
        {
            using var utf8z = new UTF8ZHelper(originalBuf, 1024, buf ?? "");
            return InputTextMultiline(label, (IntPtr)utf8z.Pointer, (IntPtr)buf_size, size, flags, callback, user_data);
        }

        buf ??= "";
        using var utf8z_original = new UTF8ZHelper(originalBuf, 1024, buf);
        int inputBufSize = Math.Max((int)buf_size + 1, utf8z_original.ByteCount + 1);
        var alloc = IntPtr.Zero;
        if (inputBufSize > 1024)
        {
            alloc = Marshal.AllocHGlobal(inputBufSize);
            editBuf = (byte*)alloc;
        }
        Buffer.MemoryCopy(utf8z_original.Pointer, editBuf, inputBufSize, utf8z_original.ByteCount);
        editBuf![utf8z_original.ByteCount] = 0;
        var result = InputTextMultiline(label, (IntPtr)editBuf, (IntPtr)buf_size,  size, flags, callback, user_data);
        if (!UTF8ZEqual(utf8z_original.Pointer, editBuf, (int)buf_size))
        {
            buf = Marshal.PtrToStringUTF8((IntPtr)editBuf);
        }
        if (alloc != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(alloc);
        }
        return result;
    }

    public static bool InputTextWithHint(string label, string hint, ref string buf, uint buf_size,
        ImGuiInputTextFlags flags = (ImGuiInputTextFlags)0, ImGuiInputTextCallback callback = null,
        IntPtr user_data = 0)
    {
        byte* originalBuf = stackalloc byte[1024];
        byte* editBuf = stackalloc byte[1024];
        buf ??= "";
        using var utf8z_original = new UTF8ZHelper(originalBuf, 1024, buf);
        int inputBufSize = Math.Max((int)buf_size + 1, utf8z_original.ByteCount + 1);
        var alloc = IntPtr.Zero;
        if (inputBufSize > 1024)
        {
            alloc = Marshal.AllocHGlobal(inputBufSize);
            editBuf = (byte*)alloc;
        }
        Buffer.MemoryCopy(utf8z_original.Pointer, editBuf, inputBufSize, utf8z_original.ByteCount);
        editBuf![utf8z_original.ByteCount] = 0;
        var result = InputTextWithHint(label, hint, (IntPtr)editBuf, (IntPtr)buf_size, flags, callback, user_data);
        if (!UTF8ZEqual(utf8z_original.Pointer, editBuf, (int)buf_size))
        {
            buf = Marshal.PtrToStringUTF8((IntPtr)editBuf);
        }
        if (alloc != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(alloc);
        }
        return result;
    }

    public static bool Begin(string name, ImGuiWindowFlags flags)
    {
        byte* __bytes_name = stackalloc byte[128];
        using var __utf8z_name = new UTF8ZHelper(__bytes_name, 128, name);
        return ImGuiNative.ImGui_Begin(__utf8z_name.Pointer, null, flags) != 0;
    }

    public static void InputInt2(string label, ref Point v, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        fixed (Point* p = &v)
            InputInt2(label, (int*)p, flags);
    }
}
