// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Text;
using System.Runtime.InteropServices;
namespace LibreLancer.Media
{
	public unsafe class Mpv
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct mpv_event
		{
			public mpv_event_id event_id;
			public int error;
		}
		public enum mpv_event_id
		{
			MPV_EVENT_NONE = 0,
			MPV_EVENT_SHUTDOWN = 1,
			MPV_EVENT_LOG_MESSAGE = 2,
			MPV_EVENT_GET_PROPERTY_REPLY = 3,
			MPV_EVENT_SET_PROPERTY_REPLY = 4,
			MPV_EVENT_COMMAND_REPLY = 5,
			MPV_EVENT_START_FILE = 6,
			MPV_EVENT_END_FILE = 7,
			MPV_EVENT_FILE_LOADED = 8,
			MPV_EVENT_TRACKS_CHANGED = 9,
			MPV_EVENT_TRACK_SWITCHED = 10,
			MPV_EVENT_IDLE = 11,
			MPV_EVENT_PAUSE = 12,
			MPV_EVENT_UNPAUSE = 13,
			MPV_EVENT_TICK = 14,
			MPV_EVENT_SCRIPT_INPUT_DISPATCH = 15,
			MPV_EVENT_CLIENT_MESSAGE = 16,
			MPV_EVENT_VIDEO_RECONFIG = 17,
			MPV_EVENT_AUDIO_RECONFIG = 18,
			MPV_EVENT_METADATA_UPDATE = 19,
			MPV_EVENT_SEEK = 20,
			MPV_EVENT_PLAYBACK_RESTART = 21,
			MPV_EVENT_PROPERTY_CHANGE = 22,
			MPV_EVENT_CHAPTER_CHANGE = 23,
			MPV_EVENT_QUEUE_OVERFLOW = 24
		}
		public enum mpv_sub_api
		{
			MPV_SUB_API_OPENGL_CB = 1
		}
		public const int MPV_FORMAT_FLAG = 3;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr mpvCreateDel();
		public static mpvCreateDel mpv_create;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int mpvInitDel(IntPtr mpvHandle);
		public static mpvInitDel mpv_initialize;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate int mpvCommandDel(IntPtr mpvHandle, IntPtr strings);
		static mpvCommandDel _mpv_command;


		public static int mpv_command(IntPtr mpvHandle, params string[] args)
		{
			IntPtr[] byteArrayPointers;
			var mainPtr = AllocateUtf8IntPtrArrayWithSentinel(args, out byteArrayPointers);
			var result = _mpv_command(mpvHandle, mainPtr);
			foreach (var ptr in byteArrayPointers)
			{
				Marshal.FreeHGlobal(ptr);
			}
			Marshal.FreeHGlobal(mainPtr);
			return result;
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int setOptionStringDel(IntPtr mpvHandle, byte[] name, byte[] value);
		static setOptionStringDel _mpv_set_option_string;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate IntPtr errStringDel(int error);
		static errStringDel _mpv_error_string;

		public static string mpv_error_string(int error)
		{
			return Marshal.PtrToStringAnsi(_mpv_error_string(error));
		}

		public static int mpv_set_option_string(IntPtr mpvHandle, string name, string value)
		{
			return _mpv_set_option_string(mpvHandle, GetUtf8Bytes(name), GetUtf8Bytes(value));
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate int setOptionDel(IntPtr mpvHandle, byte[] name, int format, ref int data);
		static setOptionDel _mpv_set_option;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate mpv_event* waitEventDel(IntPtr ctx, double timeout);
		public static waitEventDel mpv_wait_event;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr openApiDel(IntPtr ctx, mpv_sub_api api);
		public static openApiDel mpv_get_sub_api;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr GetProcAddressDelegate(IntPtr fn_ctx, IntPtr name);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void GLUpdateDelegate(IntPtr fn_ctx);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int initgldel(IntPtr ctx, IntPtr exts, GetProcAddressDelegate get_proc_address, IntPtr get_proc_address_ctx);
		public static initgldel mpv_opengl_cb_init_gl;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void glCallbackDel(IntPtr ctx, GLUpdateDelegate fn, IntPtr callback_ctx);
		public static glCallbackDel mpv_opengl_cb_set_update_callback;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void drawgldel(IntPtr ctx, int fbo, int w, int h);
		public static drawgldel mpv_opengl_cb_draw;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void terminatedel(IntPtr ctx);
		public static terminatedel mpv_terminate_destroy;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int uninitgldel(IntPtr ctx);
		public static uninitgldel mpv_opengl_cb_uninit_gl;

		public static int mpv_set_option(IntPtr handle, string name, int format, ref int data)
		{
			return _mpv_set_option(handle, Encoding.UTF8.GetBytes(name), format, ref data);
		}
		static bool loaded = false;
		public static bool LoadLibrary(string mpvo)
		{
			if (loaded == true)
				return true;
			try
			{
				//Open library
				var lib = new SharedLib(
					mpvo,
					"mpv",
					"libmpv.dylib",
					"libmpv.so",
					"/usr/bin/mpv",
					"/usr/local/bin/mpv"
				);
				//Load functions
				mpv_create = lib.GetFunction<mpvCreateDel>("mpv_create");
				mpv_initialize = lib.GetFunction<mpvInitDel>("mpv_initialize");
				_mpv_command = lib.GetFunction<mpvCommandDel>("mpv_command");
				_mpv_set_option_string = lib.GetFunction<setOptionStringDel>("mpv_set_option_string");
				_mpv_error_string = lib.GetFunction<errStringDel>("mpv_error_string");
				_mpv_set_option = lib.GetFunction<setOptionDel>("mpv_set_option");
				mpv_wait_event = lib.GetFunction<waitEventDel>("mpv_wait_event");
				mpv_get_sub_api = lib.GetFunction<openApiDel>("mpv_get_sub_api");
				mpv_opengl_cb_init_gl = lib.GetFunction<initgldel>("mpv_opengl_cb_init_gl");
				mpv_opengl_cb_set_update_callback = lib.GetFunction<glCallbackDel>("mpv_opengl_cb_set_update_callback");
				mpv_opengl_cb_draw = lib.GetFunction<drawgldel>("mpv_opengl_cb_draw");
				mpv_opengl_cb_uninit_gl = lib.GetFunction<uninitgldel>("mpv_opengl_cb_uninit_gl");
				mpv_terminate_destroy = lib.GetFunction<terminatedel>("mpv_terminate_destroy");
				loaded = true;
				return true;
			}
			catch (Exception ex)
			{
				FLLog.Error("mpv", "Failed to open mpv. " + ex.Message);
				return false;
			}
		}
		static IntPtr AllocateUtf8IntPtrArrayWithSentinel(string[] arr, out IntPtr[] byteArrayPointers)
		{
			int numberOfStrings = arr.Length + 1; // add extra element for extra null pointer last (sentinel)
			byteArrayPointers = new IntPtr[numberOfStrings];
			IntPtr rootPointer = Marshal.AllocCoTaskMem(IntPtr.Size * numberOfStrings);
			for (int index = 0; index < arr.Length; index++)
			{
				var bytes = GetUtf8Bytes(arr[index]);
				IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
				Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
				byteArrayPointers[index] = unmanagedPointer;
			}
			Marshal.Copy(byteArrayPointers, 0, rootPointer, numberOfStrings);
			return rootPointer;
		}

		private static byte[] GetUtf8Bytes(string s)
		{
			return Encoding.UTF8.GetBytes(s + "\0");
		}
	}
}

