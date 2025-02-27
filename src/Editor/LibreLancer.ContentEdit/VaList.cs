using System;
using System.Runtime.InteropServices;

namespace LibreLancer.ContentEdit;

// provides a method that native code can call with (const char*, va_list)
// which gives a string to C#
abstract class VaListCallback
{
    public static VaListCallback Create(Action<string> onCallback)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new VaListWindows(onCallback);
        }

        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86 when IntPtr.Size == 8:
            case Architecture.X64:
                return new VaListUnixX64(onCallback);
            case Architecture.X86:
            case Architecture.Arm64:
            case Architecture.Armv6:
            case Architecture.Arm:
                return new VaListUnixX86(onCallback);
            default:
                throw new PlatformNotSupportedException();
        }
    }

    protected abstract void Callback(string format, IntPtr args);

    private IntPtr ptr;
    private CallbackDelegate cb;


    delegate void CallbackDelegate(string format, IntPtr args);

    public IntPtr GetFunctionPointer()
    {
        if (cb == null)
        {
            cb = Callback;
            ptr = Marshal.GetFunctionPointerForDelegate(cb);
        }

        return ptr;
    }

    class libc
    {
        [DllImport("libc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int vsnprintf(
            IntPtr buffer,
            UIntPtr size,
            [In] [MarshalAs(UnmanagedType.LPStr)] string format,
            IntPtr args);
    }

    class msvcrt
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int _vscprintf(
            string format,
            IntPtr args);

        [DllImport("msvcrt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int vsprintf(
            IntPtr buffer,
            string format,
            IntPtr args);
    }

    class VaListWindows : VaListCallback
    {
        public Action<string> Target;
        public VaListWindows(Action<string> target) => Target = target;

        protected override void Callback(string format, IntPtr args)
        {
            var byteLength = msvcrt._vscprintf(format, args) + 1;
            using var utf8 = UnsafeHelpers.Allocate(byteLength);
            msvcrt.vsprintf((IntPtr)utf8, format, args);
            var str = Marshal.PtrToStringUTF8((IntPtr)utf8);
            Target(str);
        }
    }

    class VaListUnixX86 : VaListCallback
    {
        public Action<string> Target;

        public VaListUnixX86(Action<string> target) => Target = target;

        protected override void Callback(string format, IntPtr args)
        {
            int byteLength = libc.vsnprintf(IntPtr.Zero, UIntPtr.Zero, format, args) + 1;
            using var utf8 = UnsafeHelpers.Allocate(byteLength);
            libc.vsnprintf((IntPtr)utf8, (UIntPtr)byteLength, format, args);
            var str = Marshal.PtrToStringUTF8((IntPtr)utf8);
            Target(str);
        }
    }

    class VaListUnixX64 : VaListCallback
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct valist_x64
        {
            private uint gp_offset;
            private uint fp_offset;
            private IntPtr overflow_arg_area;
            private IntPtr reg_save_area;
        }

        public Action<string> Target;

        public VaListUnixX64(Action<string> target) => Target = target;

        protected override unsafe void Callback(string format, IntPtr args)
        {
            var list = Marshal.PtrToStructure<valist_x64>(args);
            var byteLength = libc.vsnprintf(IntPtr.Zero, UIntPtr.Zero, format, (IntPtr)(&list)) + 1;
            list = Marshal.PtrToStructure<valist_x64>(args);
            using var utf8 = UnsafeHelpers.Allocate(byteLength);
            libc.vsnprintf((IntPtr)utf8, (UIntPtr)byteLength, format, (IntPtr)(&list));
            var str = Marshal.PtrToStringUTF8((IntPtr)utf8);
            Target(str);
        }
    }
}
