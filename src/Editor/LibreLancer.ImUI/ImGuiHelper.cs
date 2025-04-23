// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using LibreLancer.Dialogs;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.ImUI.Shaders;

namespace LibreLancer.ImUI
{
    public enum ImGuiProcessing
    {
        Sleep,
        Slow,
        Full
    }
	public unsafe partial class ImGuiHelper
	{
		Game game;
		//TODO: This is duplicated from Renderer2D
		[StructLayout(LayoutKind.Sequential)]
		struct Vertex2D : IVertexType
		{
			public Vector2 Position;
			public Vector2 TexCoord;
			public Color4 Color;

			public Vertex2D(Vector2 position, Vector2 texcoord, Color4 color)
			{
				Position = position;
				TexCoord = texcoord;
				Color = color;
			}

			public VertexDeclaration GetVertexDeclaration()
			{
				return new VertexDeclaration(
					sizeof(float) * 2 + sizeof(float) * 2 + sizeof(float) * 4,
					new VertexElement(VertexSlots.Position, 2, VertexElementType.Float, false, 0),
					new VertexElement(VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 2),
					new VertexElement(VertexSlots.Color, 4, VertexElementType.Float, false, sizeof(float) * 4)
				);
			}
		}

		Texture2D fontTexture;
		const int FONT_TEXTURE_ID = 8;
		public static int CheckerboardId;
		Texture2D dot;
		Texture2D checkerboard;

        public static int FileId;
        private Texture2D file;
        public static int FolderId;
        private Texture2D folder;

		IntPtr ttfPtr;
        IntPtr context;
		public static ImFontPtr Noto;
		public static ImFontPtr Default;
        public static ImFontPtr SystemMonospace;

        //Not shown in current version of ImGui.NET,
        //can probably remove when I update the dependencies
        [DllImport("cimgui")]
        static extern IntPtr ImFontConfig_ImFontConfig();

        public static float Scale { get; private set; } = 1;

        public static bool DialogOpen = false;

        public static IUIThread UiThread => instance.game;

        [DllImport("cimgui")]
        static extern void igInstallAssertHandler(delegate* unmanaged<IntPtr, IntPtr, int, void> handler);

        [UnmanagedCallersOnly]
        static void AssertionFailure(IntPtr expr, IntPtr file, int line)
        {
            var msg =
                $"imgui assert failed at {Marshal.PtrToStringUTF8(file)}:{line}: {Marshal.PtrToStringUTF8(expr)}";
            var st = new System.Diagnostics.StackTrace(true);
            CrashWindow.Run("ImGui Error", msg, st.ToString());
            Environment.Exit(255);
        }
        static ImGuiHelper()
        {
            igInstallAssertHandler(&AssertionFailure);
        }

        [DllImport("cimgui")]
        static extern void igGuizmoSetImGuiContext(IntPtr ctx);

        static (Texture2D, int) LoadTexture(RenderContext context, string path)
        {
            using (var stream = typeof(ImGuiHelper).Assembly.GetManifestResourceStream($"LibreLancer.ImUI.{path}"))
            {
                var tex = (Texture2D)LibreLancer.ImageLib.Generic.TextureFromStream(context, stream);
                return (tex, RegisterTexture(tex));
            }
        }

		public unsafe ImGuiHelper(Game game, float scale)
        {
            ImGuiExt.igFtLoad();
            Scale = scale;
			this.game = game;
			game.Keyboard.KeyDown += Keyboard_KeyDown;
			game.Keyboard.KeyUp += Keyboard_KeyUp;
			game.Keyboard.TextInput += Keyboard_TextInput;
            game.Mouse.MouseDown += MouseOnMouseDown;
            game.Mouse.MouseUp += MouseOnMouseUp;
            game.Mouse.MouseMove += MouseOnMouseMove;
            game.Mouse.MouseWheel += MouseOnMouseWheel;

            context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);
            igGuizmoSetImGuiContext(context);
            SetKeyMappings();
            var io = ImGui.GetIO();
            io.WantSaveIniSettings = false;
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            io.NativePtr->IniFilename = (byte*)0; //disable ini!!
            io.Fonts.Flags |= ImFontAtlasFlags.NoMouseCursors;
            var fontConfigA = new ImFontConfigPtr(ImFontConfig_ImFontConfig());
            var fontConfigB = new ImFontConfigPtr(ImFontConfig_ImFontConfig());
            var fontConfigC = new ImFontConfigPtr(ImFontConfig_ImFontConfig());
            ushort[] glyphRangesFull = new ushort[]
            {
                0x0020, 0x00FF, //Basic Latin + Latin Supplement,
                0x0400, 0x052F, //Cyrillic + Cyrillic Supplement
                0x2DE0, 0x2DFF, //Cyrillic Extended-A
                0xA640, 0xA69F, //Cyrillic Extended-B
                ImGuiExt.ReplacementHash, ImGuiExt.ReplacementHash,
                0
            };
            var rangesPtrFull = Marshal.AllocHGlobal(sizeof(short) * glyphRangesFull.Length);
            for (int i = 0; i < glyphRangesFull.Length; i++) ((ushort*)rangesPtrFull)[i] = glyphRangesFull[i];
            ushort[] glyphRangesLatin = new ushort[]
            {
                0x0020, 0x00FF, //Basic Latin + Latin Supplement
                ImGuiExt.ReplacementHash, ImGuiExt.ReplacementHash,
                0
            };
            var rangesPtrLatin = Marshal.AllocHGlobal(sizeof(short) * glyphRangesLatin.Length);
            for (int i = 0; i < glyphRangesLatin.Length; i++) ((ushort*)rangesPtrLatin)[i] = glyphRangesLatin[i];
            fontConfigA.GlyphRanges = rangesPtrLatin;
            fontConfigB.GlyphRanges = rangesPtrFull;
            fontConfigC.GlyphRanges = rangesPtrFull;

            Default = io.Fonts.AddFontDefault(fontConfigA);
			using (var stream = typeof(ImGuiHelper).Assembly.GetManifestResourceStream("LibreLancer.ImUI.Roboto-Regular.ttf"))
			{
				var ttf = new byte[stream.Length];
				stream.Read(ttf, 0, ttf.Length);
				ttfPtr = Marshal.AllocHGlobal(ttf.Length);
				Marshal.Copy(ttf, 0, ttfPtr, ttf.Length);
                Noto = io.Fonts.AddFontFromMemoryTTF(ttfPtr, ttf.Length, (int)(15 * Scale), fontConfigB);
			}

            using (var stream =
                   typeof(ImGuiHelper).Assembly.GetManifestResourceStream("LibreLancer.ImUI.fa-solid-900.ttf"))
            {
                var iconFontConfig = new ImFontConfigPtr(ImFontConfig_ImFontConfig());
                iconFontConfig.MergeMode = true;
                iconFontConfig.GlyphMinAdvanceX = iconFontConfig.GlyphMaxAdvanceX = (int) (20 * Scale);
                var glyphs = new List<ushort>();
                foreach (var chars in Icons.GetChars())
                {
                    glyphs.Add(chars);
                    glyphs.Add(chars);
                }
                glyphs.Add(0);
                var rangesPtrIcon = Marshal.AllocHGlobal(sizeof(short) * glyphs.Count);
                for (int i = 0; i < glyphs.Count; i++) ((ushort*)rangesPtrIcon)[i] = glyphs[i];
                iconFontConfig.GlyphRanges = rangesPtrIcon;
                var ttf = new byte[stream.Length];
                stream.Read(ttf, 0, ttf.Length);
                ttfPtr = Marshal.AllocHGlobal(ttf.Length);
                Marshal.Copy(ttf, 0, ttfPtr, ttf.Length);
                io.Fonts.AddFontFromMemoryTTF(ttfPtr, ttf.Length, (int)(15 * Scale), iconFontConfig);
            }

            using (var stream =
                   typeof(ImGuiHelper).Assembly.GetManifestResourceStream("LibreLancer.ImUI.empty-bullet.ttf"))
            {
                var iconFontConfig = new ImFontConfigPtr(ImFontConfig_ImFontConfig());
                iconFontConfig.MergeMode = true;
                var glyphs = new ushort[] {Icons.BulletEmpty, Icons.BulletEmpty, 0};
                var rangesPtrIcon = Marshal.AllocHGlobal(sizeof(short) * glyphs.Length);
                for (int i = 0; i < glyphs.Length; i++) ((ushort*)rangesPtrIcon)[i] = glyphs[i];
                iconFontConfig.GlyphRanges = rangesPtrIcon;
                var ttf = new byte[stream.Length];
                stream.Read(ttf, 0, ttf.Length);
                ttfPtr = Marshal.AllocHGlobal(ttf.Length);
                Marshal.Copy(ttf, 0, ttfPtr, ttf.Length);
                io.Fonts.AddFontFromMemoryTTF(ttfPtr, ttf.Length, (int)(15 * Scale), iconFontConfig);
            }

            (checkerboard, CheckerboardId) = LoadTexture(game.RenderContext, "checkerboard.png");
            (file, FileId) = LoadTexture(game.RenderContext, "file.png");
            (folder, FolderId) = LoadTexture(game.RenderContext, "folder.png");

            var monospace = Platform.GetMonospaceBytes();
            fixed (byte* mmPtr = monospace)
            {
                SystemMonospace = io.Fonts.AddFontFromMemoryTTF((IntPtr) mmPtr, monospace.Length, (int)(16 * Scale), fontConfigC);
            }

            io.Fonts.Build();
            byte* fontBytes;
            int fontWidth, fontHeight;
            io.Fonts.GetTexDataAsRGBA32(out fontBytes, out fontWidth, out fontHeight);
            io.Fonts.TexUvWhitePixel = new Vector2(10, 10);
            Icons.TintGlyphs(fontBytes, fontWidth, fontHeight, Noto);
            fontTexture = new Texture2D(game.RenderContext, fontWidth,fontHeight, false, SurfaceFormat.Bgra8);
			var bytes = new byte[fontWidth * fontHeight * 4];
            Marshal.Copy((IntPtr)fontBytes, bytes, 0, fontWidth * fontHeight * 4);
			fontTexture.SetData(bytes);
			fontTexture.SetFiltering(TextureFiltering.Linear);
			io.Fonts.SetTexID((IntPtr)FONT_TEXTURE_ID);
			io.Fonts.ClearTexData();
            ImGuiShader.Compile(game.RenderContext);
			dot = new Texture2D(game.RenderContext, 1, 1, false, SurfaceFormat.Bgra8);
			var c = new Bgra8[] { Bgra8.White };
			dot.SetData(c);
            Theme.Apply(scale);
            //Required for clipboard function on non-Windows platforms
            utf8buf = Marshal.AllocHGlobal(8192);
            instance = this;
            setTextDel = SetClipboardText;
            getTextDel = GetClipboardText;

            io.GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(getTextDel);
            io.SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(setTextDel);

            io.PlatformLocaleDecimalPoint = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
		}

        private void MouseOnMouseWheel(float amountx, float amounty)
        {
            ImGui.GetIO().AddMouseWheelEvent(amountx, amounty);
        }

        private void MouseOnMouseMove(MouseEventArgs e)
        {
            ImGui.GetIO().AddMousePosEvent(e.X, e.Y);
        }

        static bool Translate(MouseButtons buttons, out ImGuiMouseButton translated)
        {
            switch (buttons)
            {
                case MouseButtons.Left:
                    translated = ImGuiMouseButton.Left;
                    return true;
                case MouseButtons.Middle:
                    translated = ImGuiMouseButton.Middle;
                    return true;
                case MouseButtons.Right:
                    translated = ImGuiMouseButton.Right;
                    return true;
                default:
                    translated = ImGuiMouseButton.COUNT;
                    return false;
            }
        }
        private void MouseOnMouseDown(MouseEventArgs e)
        {
            var io = ImGui.GetIO();
            if (Translate(e.Buttons, out var mb))
                io.AddMouseButtonEvent((int)mb, true);
        }

        void MouseOnMouseUp(MouseEventArgs e)
        {
            var io = ImGui.GetIO();
            if (Translate(e.Buttons, out var mb))
                io.AddMouseButtonEvent((int)mb, false);
        }

        static ImGuiHelper instance;
        static IntPtr utf8buf;
        static GetClipboardTextType getTextDel;
        static SetClipboardTextType setTextDel;
        delegate IntPtr GetClipboardTextType(IntPtr userdata);
        delegate void SetClipboardTextType(IntPtr userdata, IntPtr text);
        static IntPtr GetClipboardText(IntPtr userdata)
        {
            var str = instance.game.GetClipboardText();
            var bytes = Encoding.UTF8.GetBytes(str);
            Marshal.Copy(bytes, 0, utf8buf, bytes.Length);
            Marshal.WriteByte(utf8buf, bytes.Length, 0);
            return utf8buf;
        }
        static unsafe void SetClipboardText(IntPtr userdata, IntPtr text)
        {
            instance.game.SetClipboardText(UnsafeHelpers.PtrToStringUTF8(text));
        }
		static Dictionary<int, Texture2D> textures = new Dictionary<int, Texture2D>();
		static Dictionary<Texture2D, int> textureIds = new Dictionary<Texture2D, int>();
		static int nextId = 8192;

		//Useful for crap like FBO resizing where textures will be thrown out a ton
		static Queue<int> freeIds = new Queue<int>();

		public static int RegisterTexture(Texture2D tex)
		{
			int id = 0;
			if (!textureIds.TryGetValue(tex, out id)) {
				if (freeIds.Count > 0)
					id = freeIds.Dequeue();
				else
					id = nextId++;
				textureIds.Add(tex, id);
				textures.Add(id, tex);
			}
			return id;
		}

        public static int RenderGradient( Color4 top, Color4 bottom)
        {
            return instance.RenderGradientInternal(top, bottom);
        }

        int RenderGradientInternal(Color4 top, Color4 bottom)
        {
            var target = new RenderTarget2D(game.RenderContext, 128,128);
            var r2d = game.RenderContext.Renderer2D;
            game.RenderContext.RenderTarget = target;
            game.RenderContext.PushViewport(0, 0, 128, 128);
            r2d.DrawVerticalGradient(new Rectangle(0,0,128,128), top, bottom);
            game.RenderContext.PopViewport();
            game.RenderContext.RenderTarget = null;
            toFree.Add(target);
            return RegisterTexture(target.Texture);
        }

        public bool PauseWhenUnfocused = false;
        private double renderTimer = 0.47;
        private const double RENDER_TIME = 0.47;
        private bool lastWantedKeyboard = false;
        public ImGuiProcessing DoRender(double elapsed)
        {
            if (elapsed > 0.05) elapsed = 0;
            if (game.EventsThisFrame ||
                (animating && !(PauseWhenUnfocused && !game.Focused))
                || game.Keyboard.AnyKeyDown())
                renderTimer = RENDER_TIME;
            animating = false;
            renderTimer -= elapsed;
            if (renderTimer <= 0) renderTimer = 0;
            if (renderTimer > 0)
                return ImGuiProcessing.Full;
            else if (lastWantedKeyboard)
                return ImGuiProcessing.Slow;
            else
                return ImGuiProcessing.Sleep;
        }

        public void ResetRenderTimer()
        {
            renderTimer = RENDER_TIME;
        }
        public bool DoUpdate()
        {
            return renderTimer != 0 || lastWantedKeyboard;
        }

        private static bool animating = false;
        public static void AnimatingElement() => animating = true;
        public static void DeregisterTexture(Texture2D tex)
		{
			var id = textureIds[tex];
			textureIds.Remove(tex);
			textures.Remove(id);
			freeIds.Enqueue(id);
		}

		void Keyboard_TextInput(string text)
		{
            foreach (var c in text)
                ImGui.GetIO().AddInputCharacter(c);
		}

		void Keyboard_KeyDown(KeyEventArgs e)
		{
			var io = ImGui.GetIO();
            UpdateKeyMods(e);
            if (keyMapping.TryGetValue(e.Key, out var imk))
                io.AddKeyEvent(imk, true);
        }

        void UpdateKeyMods(KeyEventArgs e)
        {
            var io = ImGui.GetIO();
            io.AddKeyEvent(ImGuiKey.ModCtrl, (e.Modifiers & KeyModifiers.Control) != 0);
            io.AddKeyEvent(ImGuiKey.ModAlt, (e.Modifiers & KeyModifiers.Alt) != 0);
            io.AddKeyEvent(ImGuiKey.ModShift, (e.Modifiers & KeyModifiers.Shift) != 0);
            io.AddKeyEvent(ImGuiKey.ModSuper, (e.Modifiers & KeyModifiers.GUI) != 0);
        }

		void Keyboard_KeyUp(KeyEventArgs e)
		{
			var io = ImGui.GetIO();
            UpdateKeyMods(e);
            if (keyMapping.TryGetValue(e.Key, out var imk))
                io.AddKeyEvent(imk, false);
        }


		public void NewFrame(double elapsed)
		{
			ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new Vector2(game.Width, game.Height);
			io.DisplayFramebufferScale = new Vector2(1, 1);
            const float MAX_DELTA = 0.1f;
			io.DeltaTime = elapsed > MAX_DELTA ? MAX_DELTA : (float)elapsed;
			//Update input
            if(HandleKeyboard)
                game.TextInputEnabled = io.WantCaptureKeyboard;
			//TODO: Mouse Wheel
			ImGui.NewFrame();
            ImGuizmo.BeginFrame();
        }
        //These are required as FBOs with 1 - SrcAlpha will end up with alpha != 1
        public static void DisableAlpha()
        {
            ImGui.GetWindowDrawList().AddCallback((IntPtr)1, (IntPtr)BlendMode.Opaque);
        }
        public static void EnableAlpha()
        {
            ImGui.GetWindowDrawList().AddCallback((IntPtr) 1, (IntPtr) BlendMode.Normal);
        }

        private static Action<Rectangle>[] callbacks = new Action<Rectangle>[4096];
        private static int cbIndex = 0;
        public static IntPtr Callback(Action<Rectangle> callback)
        {
            var retval = cbIndex;
            callbacks[cbIndex++] = callback;
            return (IntPtr)retval;
        }


        // Draw over the the top to block input while a file dialog is showing
        // Needed as a separate method as stacked modals require you to call within the parent modal
        private static bool _modalDrawn = false;
        public static void FileModal()
        {
            if (!_modalDrawn && DialogOpen)
            {
                bool open = true;
                ImGui.OpenPopup("##blockwindow");
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.1f);
                ImGui.BeginPopupModal("##blockwindow", ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize);
                ImGui.Dummy(new Vector2(instance.game.Width, instance.game.Height));
                ImGui.EndPopup();
                ImGui.PopStyleVar();
            }
            _modalDrawn = true;
        }

        List<RenderTarget2D> toFree = new List<RenderTarget2D>();
		public void Render(RenderContext rstate)
		{
            lastWantedKeyboard = ImGui.GetIO().WantCaptureKeyboard;
            FileModal();
            _modalDrawn = false;
			ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData(), rstate);
            foreach (var tex in toFree) {
                DeregisterTexture(tex.Texture);
                tex.Dispose();
            }
            toFree = new List<RenderTarget2D>();
		}

        [StructLayout(LayoutKind.Sequential)]
        struct DrawVert : IVertexType
        {
            public Vector2 pos;
            public Vector2 uv;
            public uint col;

            public VertexDeclaration GetVertexDeclaration() => new(
                sizeof(float) * 5,
                new VertexElement(VertexSlots.Position, 2, VertexElementType.Float, false, 0),
                new VertexElement(VertexSlots.Texture1, 2, VertexElementType.Float, false, 2 * sizeof(float)),
                new VertexElement(VertexSlots.Color, 4, VertexElementType.UnsignedByte, true, 4 * sizeof(float))
            );
        }

        public bool SetCursor = true;
        public bool HandleKeyboard = true;

		VertexBuffer vbo;
		ElementBuffer ibo;
		int vboSize = -1;
		int iboSize = -1;
		unsafe void RenderImDrawData(ImDrawDataPtr draw_data, RenderContext rstate)
		{
			var io = ImGui.GetIO();
            //Set cursor
            if (SetCursor)
            {
                var cur = ImGuiNative.igGetMouseCursor();
                switch (cur)
                {
                    case ImGuiMouseCursor.Arrow:
                        game.CursorKind = CursorKind.Arrow;
                        break;
                    case ImGuiMouseCursor.ResizeAll:
                        game.CursorKind = CursorKind.Move;
                        break;
                    case ImGuiMouseCursor.TextInput:
                        game.CursorKind = CursorKind.TextInput;
                        break;
                    case ImGuiMouseCursor.ResizeNS:
                        game.CursorKind = CursorKind.ResizeNS;
                        break;
                    case ImGuiMouseCursor.ResizeEW:
                        game.CursorKind = CursorKind.ResizeEW;
                        break;
                    case ImGuiMouseCursor.ResizeNESW:
                        game.CursorKind = CursorKind.ResizeNESW;
                        break;
                    case ImGuiMouseCursor.ResizeNWSE:
                        game.CursorKind = CursorKind.ResizeNWSE;
                        break;
                    case ImGuiMouseCursor.NotAllowed:
                        game.CursorKind = CursorKind.NotAllowed;
                        break;
                }
            }
            //Render
            draw_data.ScaleClipRects(io.DisplayFramebufferScale);

			var mat = Matrix4x4.CreateOrthographicOffCenter(0, game.Width, game.Height, 0, 0, 1);
            var textShader = ImGuiShader.Shader.Get(0);
            var colorShader = ImGuiShader.Shader.Get(0);
            textShader.SetUniformBlock(2, ref mat);
            colorShader.SetUniformBlock(2, ref mat);
            rstate.Shader = textShader;
			rstate.Cull = false;
			rstate.BlendMode = BlendMode.Normal;
			rstate.DepthEnabled = false;
            //
            Rectangle currScissor = new Rectangle(0, 0, game.Width, game.Height);
            rstate.PushScissor(currScissor);
            //
			for (int n = 0; n < draw_data.CmdListsCount; n++)
			{
                var cmd_list = draw_data.CmdLists[n];
				var vtxCount = cmd_list.VtxBuffer.Size;
				var idxCount = cmd_list.IdxBuffer.Size;
                if (vboSize < vtxCount || iboSize < idxCount)
				{
					if (vbo != null) vbo.Dispose();
					if (ibo != null) ibo.Dispose();
					vboSize = Math.Max(vboSize, vtxCount);
					iboSize = Math.Max(iboSize, idxCount);
					vbo = new VertexBuffer(game.RenderContext, typeof(DrawVert), vboSize, true);
					ibo = new ElementBuffer(game.RenderContext, iboSize, true);
					vbo.SetElementBuffer(ibo);
				}
                vbo.SetData(new ReadOnlySpan<DrawVert>((void*)cmd_list.VtxBuffer.Data, vtxCount));
                ibo.SetData(new ReadOnlySpan<ushort>((void*)cmd_list.IdxBuffer.Data, idxCount));
				for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
				{
                    var pcmd = cmd_list.CmdBuffer[cmd_i];
                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                        if (pcmd.UserCallback == 2)
                        {
                            rstate.BlendMode = (ushort)pcmd.UserCallbackData;
                        }
                        else if (pcmd.UserCallback == IntPtr.MaxValue)
                        {
                            callbacks[(int)pcmd.UserCallbackData](new Rectangle((int) pcmd.ClipRect.X, (int) pcmd.ClipRect.Y,
                                (int) (pcmd.ClipRect.Z - pcmd.ClipRect.X),
                                (int) (pcmd.ClipRect.W - pcmd.ClipRect.Y)));
                        }
                        else if (pcmd.UserCallback > 8)
                        {
                            var cb = (delegate* unmanaged<IntPtr,IntPtr, void>)pcmd.UserCallback;
                            cb((IntPtr)cmd_list.NativePtr, (IntPtr)pcmd.NativePtr);
                        }
                        continue;
                    }

                    if (pcmd.ElemCount == 0)
                        continue;
					var tid = pcmd.TextureId.ToInt32();
					Texture2D tex;
					if (tid == FONT_TEXTURE_ID)
                    {
                        rstate.Shader = textShader;
						fontTexture.BindTo(0);
                    }
					else if (textures.TryGetValue(tid, out tex))
                    {
                        rstate.Shader = colorShader;
						tex.BindTo(0);
					}
					else
					{
						dot.BindTo(0);
					}

                    var newScissor = new Rectangle((int)pcmd.ClipRect.X, (int)pcmd.ClipRect.Y,
                        (int)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                        (int)(pcmd.ClipRect.W - pcmd.ClipRect.Y));
                    if (currScissor != newScissor) {
                        rstate.ReplaceScissor(newScissor);
                        currScissor = newScissor;
                    }
                    vbo.Draw(PrimitiveTypes.TriangleList, (int)pcmd.VtxOffset, (int)pcmd.IdxOffset, (int)pcmd.ElemCount / 3);
				}
            }
            rstate.PopScissor();

            for (int i = 0; i < cbIndex; i++)
                callbacks[i] = null;
            cbIndex = 0;
        }
	}
}
