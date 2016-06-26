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
namespace LibreLancer.Media
{
	unsafe class VideoPlayerMpv : VideoPlayerInternal
	{
		IntPtr mpvhandle;
		IntPtr mpvgl;
		Game game;
		RenderTarget2D framebuffer;
		public VideoPlayerMpv(Game game)
		{
			this.game = game;
			framebuffer = new RenderTarget2D(game.Width, game.Height);
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
		public override void Draw()
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
			game.UnbindAll();
			Mpv.mpv_opengl_cb_draw(mpvgl, (int)framebuffer.FBO, game.Width, game.Height);
			RenderTarget2D.ClearBinding();
			game.TrashGLState();
		}

		public IntPtr GetProcAddress(IntPtr fn_ctx, IntPtr address)
		{
			var str = Marshal.PtrToStringAnsi(address);
			return game.GetGLProcAddress(str);
		}

		public override void Dispose()
		{
			FLLog.Info("Video", "Closing mpv backend");
			Mpv.mpv_opengl_cb_uninit_gl(mpvgl);
			Mpv.mpv_terminate_destroy(mpvhandle);
			Playing = false;
		}
		const int LC_NUMERIC = 1;

		[DllImport("libc")]
		public static extern IntPtr setlocale (int category, [MarshalAs (UnmanagedType.LPStr)]string locale);

		public override bool Init()
		{
			FLLog.Info("Video", "Opening mpv backend");
			if (!Mpv.LoadLibrary())
				return false;
			try
			{
				//mpv will not run unless lc_numeric is "C"
				setlocale(LC_NUMERIC, "C");

				mpvhandle = Mpv.mpv_create();
				if (mpvhandle == IntPtr.Zero)
					throw new Exception("mpv_create failed");
				CheckError(Mpv.mpv_initialize(mpvhandle));
				mpvgl = Mpv.mpv_get_sub_api(mpvhandle, Mpv.mpv_sub_api.MPV_SUB_API_OPENGL_CB);
				CheckError(Mpv.mpv_opengl_cb_init_gl(mpvgl, IntPtr.Zero, GetProcAddress, IntPtr.Zero));
				CheckError(Mpv.mpv_set_option_string(mpvhandle, "vo", "opengl-cb"));
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

