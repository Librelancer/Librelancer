// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
namespace LibreLancer.Media
{
	unsafe class VideoPlayerMpv : VideoPlayerInternal
	{
		IntPtr mpvhandle;
		IntPtr mpvgl;
		Game game;
		RenderTarget2D framebuffer;
		string mpvo;
		bool doDraw = false;
		public VideoPlayerMpv(Game game, string mpvoverride)
		{
			this.game = game;
			framebuffer = new RenderTarget2D(game.Width, game.Height);
			mpvo = mpvoverride;
		}
		public override void PlayFile(string filename)
		{
			Playing = true;
			CheckError(Mpv.mpv_command(mpvhandle, "loadfile", filename));

		}
		public override Texture2D GetTexture()
		{
			return framebuffer;
		}
		bool firstDraw = true;
		public override void Draw(RenderState rstate)
		{
			Mpv.mpv_event* ev = Mpv.mpv_wait_event(mpvhandle, 0);
			while (ev->event_id != Mpv.mpv_event_id.MPV_EVENT_NONE)
			{
				if (ev->event_id == Mpv.mpv_event_id.MPV_EVENT_END_FILE)
				{
					Playing = false;
				}
				ev = Mpv.mpv_wait_event(mpvhandle, 0);
			}
			if (disposed)
				return;
			game.UnbindAll();
			RenderTarget2D.ClearBinding();
			game.TrashGLState();
			if (firstDraw)
			{
				firstDraw = false;
				framebuffer.BindFramebuffer();
				rstate.ClearColor = Color4.Black;
				rstate.ClearAll();
				RenderTarget2D.ClearBinding();
			}
			if (doDraw)
			{
				rstate.Cull = false;
				rstate.Apply();
				Mpv.mpv_opengl_cb_draw(mpvgl, (int)framebuffer.FBO, game.Width, game.Height);
				doDraw = false;
				rstate.Cull = true;
			}
		}

		public IntPtr GetProcAddress(IntPtr fn_ctx, IntPtr address)
		{
			var str = Marshal.PtrToStringAnsi(address);
			return game.GetGLProcAddress(str);
		}
		bool disposed = false;
		public override void Dispose()
		{
			disposed = true;
			if (mpvhandle != IntPtr.Zero)
			{
				FLLog.Info("Video", "Closing mpv backend");
				Mpv.mpv_opengl_cb_uninit_gl(mpvgl);
				Mpv.mpv_terminate_destroy(mpvhandle);
				framebuffer.Dispose();
				Playing = false;
			}
		}
		static readonly int LC_NUMERIC = Platform.RunningOS == OS.Linux ? 1 : 4;

		[DllImport("libc")]
		public static extern IntPtr setlocale (int category, [MarshalAs (UnmanagedType.LPStr)]string locale);
		Mpv.GLUpdateDelegate update;
		public override bool Init()
		{
			FLLog.Info("Video", "Opening mpv backend");
			if (!Mpv.LoadLibrary(mpvo))
				return false;
			try
			{
				//mpv will not run unless lc_numeric is "C"
				IntPtr locale;
				if ((locale = setlocale(LC_NUMERIC, "C")) == IntPtr.Zero)
					throw new Exception("setlocale(LC_NUMERIC, \"C\") failed");
				mpvhandle = Mpv.mpv_create();
				if (mpvhandle == IntPtr.Zero)
					throw new Exception("mpv_create failed");
				CheckError(Mpv.mpv_initialize(mpvhandle));
				mpvgl = Mpv.mpv_get_sub_api(mpvhandle, Mpv.mpv_sub_api.MPV_SUB_API_OPENGL_CB);
				CheckError(Mpv.mpv_opengl_cb_init_gl(mpvgl, IntPtr.Zero, GetProcAddress, IntPtr.Zero));
				CheckError(Mpv.mpv_set_option_string(mpvhandle, "vo", "opengl-cb"));
				update = ctx => game.QueueUIThread(() => doDraw = true);
				Mpv.mpv_opengl_cb_set_update_callback(mpvgl, update, IntPtr.Zero);
			}
			catch (Exception ex)
			{
				FLLog.Info("mpv", "Failed to initialize: " + ex.Message);
				return false;
			}
			return true;
		}
		static void CheckError(int status)
		{
			if (status < 0)
			{
				throw new Exception(Mpv.mpv_error_string(status));
			}
		}
	}
}

