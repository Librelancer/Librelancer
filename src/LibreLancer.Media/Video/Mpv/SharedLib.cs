// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
using System.Text;
namespace LibreLancer.Media
{
	class SharedLib : IDisposable
	{
		const int RTLD_NOW = 2;
		IntPtr handle;

		public SharedLib(params string[] libraries)
		{
			var errbuilder = new StringBuilder();
			errbuilder.AppendLine("Failed loading library:");
			IntPtr ptr;
			foreach (string lib in libraries)
			{
				if (Load(lib, errbuilder, out ptr))
				{
					FLLog.Info("dlopen", "opened " + lib);
					handle = ptr;
					return;
				}
			}
			throw new Exception(errbuilder.ToString());
		}

		bool Load(string library, StringBuilder output, out IntPtr ptr)
		{
			ptr = dlopen(library, RTLD_NOW);
			var errPtr = dlerror();
			if (errPtr != IntPtr.Zero)
			{
				output.AppendLine("dlopen: " + Marshal.PtrToStringAnsi(errPtr));
				return false;
			}
			if (ptr == IntPtr.Zero)
			{
				output.AppendLine("dlopen: generic failure for " + library);
				return false;
			}
			return true;
		}

		public T GetFunction<T>(string name)
		{
            return (T)(object)Marshal.GetDelegateForFunctionPointer(GetProcAddress(name), typeof(T));
		}

		public void Dispose()
		{
			dlclose(handle);
		}


		IntPtr GetProcAddress(string name)
		{
			// clear previous errors if any
			dlerror();
			var res = dlsym(handle, name);
			var errPtr = dlerror();
			if (errPtr != IntPtr.Zero)
			{
				throw new Exception("dlsym: " + Marshal.PtrToStringAnsi(errPtr));
			}
			return res;
		}

		static IntPtr dlopen(string filename, int flags)
		{
			if (Platform.RunningOS == OS.Linux)
				return LinuxInterop.dlopen(filename, flags);
			else
				return MacInterop.dlopen(filename, flags);
		}

		static IntPtr dlsym(IntPtr handle, string symbol)
		{
			if (Platform.RunningOS == OS.Linux)
				return LinuxInterop.dlsym(handle, symbol);
			else
				return MacInterop.dlsym(handle, symbol);
		}

		static void dlclose(IntPtr handle)
		{
			if (Platform.RunningOS == OS.Linux)
				LinuxInterop.dlclose(handle);
			else
				MacInterop.dlclose(handle);
		}

		static IntPtr dlerror()
		{
			if (Platform.RunningOS == OS.Linux)
				return LinuxInterop.dlerror();
			else
				return MacInterop.dlerror();
		}

		static class LinuxInterop
		{
			[DllImport("libdl.so")]
			public static extern IntPtr dlopen(string filename, int flags);

			[DllImport("libdl.so")]
			public static extern IntPtr dlsym(IntPtr handle, string symbol);

			[DllImport("libdl.so")]
			public static extern IntPtr dlclose(IntPtr handle);

			[DllImport("libdl.so")]
			public static extern IntPtr dlerror();
		}
		static class MacInterop
		{
			[DllImport("libSystem.B.dylib")]
			public static extern IntPtr dlopen(string filename, int flags);

			[DllImport("libSystem.B.dylib")]
			public static extern IntPtr dlsym(IntPtr handle, string symbol);

			[DllImport("libSystem.B.dylib")]
			public static extern IntPtr dlclose(IntPtr handle);

			[DllImport("libSystem.B.dylib")]
			public static extern IntPtr dlerror();
		}
	}
}

