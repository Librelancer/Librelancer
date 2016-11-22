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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.Runtime.InteropServices;
namespace LibreLancer
{
	public delegate void ScreenshotSaveHandler(string filename, int width, int height, byte[] data);
	public class Game : IUIThread 
	{
		int width;
		int height;
		double totalTime;
		bool fullscreen;
		bool running = false;
		string title = "LibreLancer";
		IntPtr windowptr;
		double renderFrequency;
		public Mouse Mouse = new Mouse();
		public Keyboard Keyboard = new Keyboard();
		ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
		int mythread = -1;
		public ScreenshotSaveHandler ScreenshotSave;
		public RenderState RenderState;
		bool mouseVisible = true;
        bool forceANGLE = false;
        ANGLE angle;
        public string Renderer
        {
            get; private set;
        }
		public Game (int w, int h, bool fullscreen, bool forceAngle)
		{
			width = w;
			height = h;
			mythread = Thread.CurrentThread.ManagedThreadId;
            forceANGLE = forceAngle;
		}

		public int Width {
			get {
				return width;
			}
		}

		public bool MouseVisible
		{
			get
			{ 
				return mouseVisible;
			} 
			set
			{
				mouseVisible = value;
				SDL.SDL_ShowCursor(mouseVisible ? 1 : 0);
			}
		}
		public int Height {
			get {
				return height;
			}
		}

		public double TotalTime {
			get {
				return totalTime;
			}
		}

		public string Title {
			get {
				return title;
			} set {
				title = value;
				SDL.SDL_SetWindowTitle (windowptr, title);
			}
		}

		public double RenderFrequency {
			get {
				return renderFrequency;
			}
		}
        double frameTime;
        public double FrameTime
        {
            get {
                return frameTime;
            }
        }
		public IntPtr GetGLProcAddress(string name)
		{
			return SDL.SDL_GL_GetProcAddress(name);
		}

		public void UnbindAll()
		{
			GLBind.VertexArray(0);
			GLBind.VertexBuffer(0);
		}
		public void TrashGLState()
		{
			GLBind.Trash();
			RenderState.Instance.Trash();
		}

		public void QueueUIThread(Action work)
		{
			actions.Enqueue(work);
		}
		public void EnsureUIThread(Action work)
		{
			if (Thread.CurrentThread.ManagedThreadId == mythread)
				work();
			else
			{
				bool done = false;
				actions.Enqueue(() =>
				{
					work();
					done = true;
				});
				while (!done)
					Thread.Sleep(1);
			}
		}

		string _screenshotpath;
		bool _screenshot;
		public void Screenshot(string filename)
		{
			_screenshotpath = filename;
			_screenshot = true;
		}

		unsafe void TakeScreenshot()
		{
			if (ScreenshotSave != null)
			{
				GL.ReadBuffer(GL.GL_BACK);
				var colordata = new byte[width * height * 4];
				fixed (byte* ptr = colordata)
				{
					GL.ReadPixels(0, 0, width, height, GL.GL_RGBA, GL.GL_UNSIGNED_BYTE, (IntPtr)ptr);
				}
				var c = RenderState.ClearColor;
				for (int x = 0; x < width; x++)
					for (int y = 0; y < height; y++)
					{
						int offset = (y * height * 4) + (x * 4);
						colordata[offset + 3] = 0xFF;
					}
	
				ScreenshotSave(_screenshotpath, width, height, colordata);
			}

		}

        void LoadANGLE()
        {
            angle = new ANGLE();
            SDL.SDL_SysWMinfo wminfo = new SDL.SDL_SysWMinfo();
            SDL.SDL_GetWindowWMInfo(windowptr, ref wminfo);
            angle.CreateContext(wminfo.info.win.window);
            Renderer = "Direct3D9 (ANGLE)";
        }

		public void Run()
		{
            SSEMath.Load();
			if (SDL.SDL_Init (SDL.SDL_INIT_VIDEO) != 0) {
				FLLog.Error ("SDL", "SDL_Init failed, exiting.");
				return;
			}
			//Set GL states
			SDL.SDL_GL_SetAttribute (SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
			SDL.SDL_GL_SetAttribute (SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 2);
			SDL.SDL_GL_SetAttribute (SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
			SDL.SDL_GL_SetAttribute (SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
			//Create Window
			var flags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
			if (fullscreen)
				flags |= SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP;
			var sdlWin = SDL.SDL_CreateWindow (
				             "LibreLancer",
				             SDL.SDL_WINDOWPOS_CENTERED,
				             SDL.SDL_WINDOWPOS_CENTERED,
				             width,
				             height,
				             flags
			             );
			if (sdlWin == IntPtr.Zero) {
				FLLog.Error ("SDL", "Failed to create window, exiting.");
				return;
			}
			windowptr = sdlWin;
            if (forceANGLE)
            {
                LoadANGLE();
            }
            else
            {
                var glcontext = SDL.SDL_GL_CreateContext(sdlWin);
                if (glcontext == IntPtr.Zero)
                {
                    if (Platform.RunningOS == OS.Windows)
                    {
                        LoadANGLE();
                    }
                    else
                    {
                        FLLog.Error("OpenGL", "Failed to create OpenGL context, exiting.");
                        return;
                    }
                }
                GL.LoadSDL();
                Renderer = string.Format("{0} ({1})", GL.GetString(GL.GL_VERSION), GL.GetString(GL.GL_RENDERER));
            }
            //Init game state
            RenderState = new RenderState();
			Load();
			//Start game
			running = true;
			var timer = new Stopwatch ();
			timer.Start ();
			double last = 0;
			double elapsed = 0;
			SDL.SDL_Event e;
			SDL.SDL_StopTextInput();
            if (SDL.SDL_GL_SetSwapInterval(-1) < 0)
                SDL.SDL_GL_SetSwapInterval(1);
			while (running) {
				//Pump message queue
				while (SDL.SDL_PollEvent (out e) != 0) {
					switch (e.type) {
					case SDL.SDL_EventType.SDL_QUIT:
						{
							running = false; //TODO: Raise Event
							break;
						}
					case SDL.SDL_EventType.SDL_MOUSEMOTION:
						{
							Mouse.X = e.motion.x;
							Mouse.Y = e.motion.y;
							Mouse.OnMouseMove ();
							break;
						}
					case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
						{
							Mouse.X = e.button.x;
							Mouse.Y = e.button.y;
							var btn = GetMouseButton (e.button.button);
							Mouse.Buttons |= btn;
							Mouse.OnMouseDown (btn);
							break;
						}
					case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
						{
							Mouse.X = e.button.x;
							Mouse.Y = e.button.y;
							var btn = GetMouseButton (e.button.button);
							Mouse.Buttons &= ~btn;
							Mouse.OnMouseUp (btn);
							break;
						}
					case SDL.SDL_EventType.SDL_MOUSEWHEEL:
						{
							Mouse.OnMouseWheel (e.wheel.y);
							break;
						}
					case SDL.SDL_EventType.SDL_TEXTINPUT:
						{
							Keyboard.OnTextInput (GetEventText (ref e));
							break;
						}
					case SDL.SDL_EventType.SDL_KEYDOWN:
						{
							Keyboard.OnKeyDown ((Keys)e.key.keysym.sym, (KeyModifiers)e.key.keysym.mod);
							break;
						}
					case SDL.SDL_EventType.SDL_KEYUP:
						{
							Keyboard.OnKeyUp ((Keys)e.key.keysym.sym, (KeyModifiers)e.key.keysym.mod);
							break;
						}
					}
				}
				//Do game things
				if (!running)
					break;
				Action work;
				while (actions.TryDequeue(out work))
					work();
                totalTime = timer.Elapsed.TotalSeconds;
                Update(elapsed);
				if (!running)
					break;
				Draw (elapsed);
                //Frame time before, FPS after
                var tk = timer.Elapsed.TotalSeconds - totalTime;
                frameTime = CalcAverageTime(tk);
				if (_screenshot)
				{
					TakeScreenshot();
					_screenshot = false;
				}
                if (angle != null)
                    angle.SwapBuffers();
                else
				    SDL.SDL_GL_SwapWindow (sdlWin);
                elapsed = timer.Elapsed.TotalSeconds - last;
                renderFrequency = (1.0 / CalcAverageTick(elapsed));
                last = timer.Elapsed.TotalSeconds;
                totalTime = timer.Elapsed.TotalSeconds;
                if (elapsed < 0) {
					elapsed = 0;
					FLLog.Warning ("Timing", "Stopwatch returned negative time!");
				}
			}
			Cleanup ();
			SDL.SDL_Quit ();
		}
		const int FPS_MAXSAMPLES = 50;
		int tickindex = 0;
		double ticksum = 0;
		double[] ticklist = new double[FPS_MAXSAMPLES];

		double CalcAverageTick(double newtick)
		{
			ticksum -= ticklist[tickindex];
			ticksum += newtick;
			ticklist[tickindex] = newtick;
			if (++tickindex == FPS_MAXSAMPLES)
				tickindex=0;
			return ((double)ticksum / FPS_MAXSAMPLES);
		}

        int timeindex = 0;
        double timesum = 0;
        double[] timelist = new double[FPS_MAXSAMPLES];

        double CalcAverageTime(double newtick)
        {
            timesum -= timelist[tickindex];
            timesum += newtick;
            timelist[timeindex] = newtick;
            if (++timeindex == FPS_MAXSAMPLES)
                timeindex = 0;
            return ((double)timesum / FPS_MAXSAMPLES);
        }

        //Convert from SDL2 button to saner button
        MouseButtons GetMouseButton(byte b)
		{
			if (b == SDL.SDL_BUTTON_LEFT)
				return MouseButtons.Left;
			if (b == SDL.SDL_BUTTON_MIDDLE)
				return MouseButtons.Middle;
			if (b == SDL.SDL_BUTTON_RIGHT)
				return MouseButtons.Right;
			if (b == SDL.SDL_BUTTON_X1)
				return MouseButtons.X1;
			if (b == SDL.SDL_BUTTON_X2)
				return MouseButtons.X2;
			throw new Exception ("SDL2 gave undefined mouse button"); //should never happen
		}

		unsafe string GetEventText(ref SDL.SDL_Event e)
		{
			byte[] rawBytes = new byte[SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE];
			fixed (byte* txtPtr = e.text.text) {
				Marshal.Copy ((IntPtr)txtPtr, rawBytes, 0, SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE);
			}
			int nullIndex = Array.IndexOf (rawBytes, (byte)0);
			string text = Encoding.UTF8.GetString (rawBytes, 0, nullIndex);
			return text;
		}
		public void EnableTextInput()
		{
			SDL.SDL_StartTextInput();
		}
		public void DisableTextInput()
		{
			SDL.SDL_StopTextInput();
		}
		public void Exit()
		{
			running = false;
		}
		protected virtual void Load()
		{

		}
		protected virtual void Update(double elapsed)
		{

		}
		protected virtual void Draw(double elapsed)
		{

		}
		protected virtual void Cleanup()
		{

		}
	}
}

