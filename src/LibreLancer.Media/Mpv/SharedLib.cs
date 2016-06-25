/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
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
			return Marshal.GetDelegateForFunctionPointer<T>(GetProcAddress(name));
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

