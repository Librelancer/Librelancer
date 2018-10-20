// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
using System.Reflection;

namespace LibreLancer
{
    public static partial class SSEMath
    {
        public static bool IsAccelerated = false;
        public static void Load()
        {
            //Eliminate ARM
            PortableExecutableKinds peKind;
            ImageFileMachine machine;
            typeof(object).Module.GetPEKind(out peKind, out machine);
            if (machine != ImageFileMachine.AMD64 && machine != ImageFileMachine.I386)
            {
                FLLog.Info("SSE", "SSE Math Disabled: Reason - Unsupported Architecture");
                return;
            }
            var mytype = typeof(SSEMath);
            foreach (var field in mytype.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                AsmMethodAttribute a = null;
                foreach (var attr in field.GetCustomAttributes())
                {
                    if (attr is AsmMethodAttribute)
                        a = (AsmMethodAttribute)attr;
                }
                if (a != null)
                {
                    Delegate func = null;
					if (IntPtr.Size == 4)
					{
						func = GetFunction(field.FieldType, a.X86Name);
					}
					else
					{
						if (IsUnix)
							func = GetFunction(field.FieldType, a.UnixName);
						else
							func = GetFunction(field.FieldType, a.WindowsName);
					}
                    field.SetValue(null, func);
                }
            }
            FLLog.Info("SSE", "SSE Math Enabled");
            IsAccelerated = true;
        }

        static Delegate GetFunction(Type t, string name)
        {
            var mytype = typeof(SSEMath);
            var field = mytype.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
                throw new Exception("Implementation missing: " + name);
            else
            {
                var bytes = (byte[])field.GetValue(null);
                if (IsUnix)
                    return GetFunctionUnix(bytes, t);
                else
                    return GetFunctionWindows(bytes, t);
            }
        }
        [DllImport("libc", SetLastError = true)]
        static extern IntPtr mmap(IntPtr addr, IntPtr length, int prot, int flags, int fd, int offset);
		[DllImport("libc")]
		static extern int mprotect (IntPtr addr, IntPtr len, int prot);

        const int MAP_SHARED = 0x01;
        const int MAP_ANONYMOUS = 0x20;
		const int MAP_ANONYMOUS_MAC = 0x1000;
        const int PROT_READ = 0x1;
        const int PROT_WRITE = 0x2;
        const int PROT_EXEC = 0x4;

        static Delegate GetFunctionUnix(byte[] code, Type type)
        {
			//Make W^X distros happy
			int map_anonymous = Platform.RunningOS == OS.Mac ? MAP_ANONYMOUS_MAC : MAP_ANONYMOUS;
			IntPtr func = mmap(IntPtr.Zero, (IntPtr)code.Length, PROT_READ | PROT_WRITE, MAP_SHARED | map_anonymous, -1, 0);
            Marshal.Copy(code, 0, func, code.Length);
			mprotect (func, (IntPtr)code.Length, PROT_READ | PROT_EXEC);
            var del = (Delegate)(object)Marshal.GetDelegateForFunctionPointer(func, type);
            return del;
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr VirtualAlloc(IntPtr lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);
        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_EXECUTE_READWRITE = 0x40;
        static Delegate GetFunctionWindows(byte[] code, Type type)
        {
            IntPtr func = VirtualAlloc(IntPtr.Zero, (IntPtr)code.Length, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
            Marshal.Copy(code, 0, func, code.Length);
            var del = (Delegate)(object)Marshal.GetDelegateForFunctionPointer(func, type);
            return del;
        }
        static bool IsUnix
        {
            get
            {
                return Environment.OSVersion.Platform == PlatformID.Unix;
            }
        }
    }
}
