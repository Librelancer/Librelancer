// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
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
		public static ImTextureRef CheckerboardId;
		Texture2D dot;
		Texture2D checkerboard;

        public static ImTextureRef FileId;
        private Texture2D file;
        public static ImTextureRef FolderId;
        private Texture2D folder;

		IntPtr ttfPtr;
        ImGuiContextPtr context;
		public static ImFontPtr Roboto;
        public static ImFontPtr SystemMonospace;

        public static float Scale { get; private set; } = 1;
        public static float UserScale { get; set; } = 1;

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

        static (Texture2D, ImTextureRef) LoadTexture(RenderContext context, string path)
        {
            using (var stream = typeof(ImGuiHelper).Assembly.GetManifestResourceStream($"LibreLancer.ImUI.{path}"))
            {
                var tex = (Texture2D)LibreLancer.ImageLib.Generic.TextureFromStream(context, stream);
                return (tex, RegisterTexture(tex));
            }
        }

        static (IntPtr Handle, int Length) GetManifestResource(string name) {
            using(var s = (UnmanagedMemoryStream)typeof(ImGuiHelper).Assembly.GetManifestResourceStream(name))
            {
                return new ( (IntPtr)s.PositionPointer, checked((int)s.Length) );
            }
        }

		public unsafe ImGuiHelper(Game game, float scale)
        {
            UserScale = scale;
            Scale = game.DpiScale * scale;
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
            igGuizmoSetImGuiContext(context.Handle);
            SetKeyMappings();
            var io = ImGui.GetIO();
            io.WantSaveIniSettings = false;
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            io.BackendFlags |= ImGuiBackendFlags.RendererHasTextures;
            io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
            io.IniFilename = (byte*)0; //disable ini!!
            //io.Fonts.Flags |= ImFontAtlasFlags.NoMouseCursors;

            Icons.Init();

            {
                var (robotoPtr, robotoLength) = GetManifestResource("LibreLancer.ImUI.Roboto-Regular.ttf");
                Roboto = io.Fonts.AddFontFromMemoryTTF(robotoPtr, robotoLength, 15);
                Roboto.AddRemapChar(ImGuiExt.ReplacementHash, '#');
            }

            {
                var (iconsPtr, iconsLength) = GetManifestResource("LibreLancer.ImUI.fa-solid-900.ttf");
                var fontConfigStruct = new ImFontConfig();
                var iconFontConfig = new ImFontConfigPtr(&fontConfigStruct);
                iconFontConfig.MergeMode = true;
                iconFontConfig.GlyphMinAdvanceX = iconFontConfig.GlyphMaxAdvanceX = 20;
                io.Fonts.AddFontFromMemoryTTF(iconsPtr, iconsLength, 15, iconFontConfig);
            }

            {
                var (emptyBulletConfig, emptyBulletLength) = GetManifestResource("LibreLancer.ImUI.empty-bullet.ttf");
                var fontConfigStruct = new ImFontConfig();
                var emptyBulletFontConfig = new ImFontConfigPtr(&fontConfigStruct);
                emptyBulletFontConfig.MergeMode = true;
                io.Fonts.AddFontFromMemoryTTF(emptyBulletConfig, emptyBulletLength, 15, emptyBulletFontConfig);
            }

            {
                var (fallbackPtr, fallbackLength) = GetManifestResource("LibreLancer.ImUI.DroidSansFallbackFull.ttf");
                var fontConfigStruct = new ImFontConfig();
                var fallbackFontConfig = new ImFontConfigPtr(&fontConfigStruct);
                fallbackFontConfig.MergeMode = true;
                io.Fonts.AddFontFromMemoryTTF(fallbackPtr, fallbackLength, 15, fallbackFontConfig);
            }

            (checkerboard, CheckerboardId) = LoadTexture(game.RenderContext, "checkerboard.png");
            (file, FileId) = LoadTexture(game.RenderContext, "file.png");
            (folder, FolderId) = LoadTexture(game.RenderContext, "folder.png");

            var monospace = Platform.GetMonospaceBytes();
            var monospacePtr = Marshal.AllocHGlobal(monospace.Length);
            Marshal.Copy(monospace, 0, monospacePtr, monospace.Length);
            SystemMonospace = io.Fonts.AddFontFromMemoryTTF(monospacePtr, monospace.Length, 15);
            SystemMonospace.AddRemapChar(ImGuiExt.ReplacementHash, '#');

            ImGuiShader.Compile(game.RenderContext);
			dot = new Texture2D(game.RenderContext, 1, 1, false, SurfaceFormat.Bgra8);
			var c = new Bgra8[] { Bgra8.White };
			dot.SetData(c);
            Theme.Apply(Scale);
            //Required for clipboard function on non-Windows platforms
            utf8buf = Marshal.AllocHGlobal(8192);
            instance = this;
            var platform = ImGui.GetPlatformIO();
            platform.Platform_GetClipboardTextFn = &GetClipboardText;
            platform.Platform_SetClipboardTextFn = &SetClipboardText;
            platform.Platform_OpenInShellFn = &OpenInShell;
            platform.Platform_SetImeDataFn = &SetImeData;
            platform.Renderer_TextureMaxWidth = 2048;
            platform.Renderer_TextureMaxHeight = 2048;
            ImGui.GetPlatformIO().Platform_LocaleDecimalPoint =
                (ushort)CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
		}

        [UnmanagedCallersOnly]
        static void SetImeData(IntPtr userData, ImGuiViewport* viewport, ImGuiPlatformImeData* data)
        {
            if (!instance.HandleKeyboard)
                return;
            instance.game.TextInputEnabled = (data->WantVisible || data->WantTextInput);
            if (data->WantVisible)
            {
                instance.game.TextInputRect =new Rectangle(
                    (int)data->InputPos.X,
                    (int)data->InputPos.Y,
                    1,
                    (int)data->InputLineHeight);
            }
            else
            {
                instance.game.TextInputRect = null;
            }
        }

        [UnmanagedCallersOnly]
        static byte OpenInShell(IntPtr ctx, byte* path)
        {
            var f = Marshal.PtrToStringUTF8((IntPtr)path);
            try
            {
                Shell.OpenCommand(f);
                return 1;
            }
            catch (Exception e)
            {
                FLLog.Error("Shell", e.ToString());
                return 0;
            }
        }

        [UnmanagedCallersOnly]
        static byte* GetClipboardText(IntPtr userdata)
        {
            var str = instance.game.GetClipboardText();
            var bytes = Encoding.UTF8.GetBytes(str);
            if (utf8bufsize < bytes.Length + 1)
            {
                Marshal.FreeHGlobal(utf8buf);
                utf8buf = Marshal.AllocHGlobal(bytes.Length + 1);
                utf8bufsize = bytes.Length + 1;
            }
            Marshal.Copy(bytes, 0, utf8buf, bytes.Length);
            Marshal.WriteByte(utf8buf, bytes.Length, 0);
            return (byte*)utf8buf;
        }

        [UnmanagedCallersOnly]
        static void SetClipboardText(IntPtr ctx, byte* text)
        {
            instance.game.SetClipboardText(UnsafeHelpers.PtrToStringUTF8((IntPtr)text));
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
        static int utf8bufsize = 8192;


		static Dictionary<ulong, Texture2D> textures = new Dictionary<ulong, Texture2D>();
		static Dictionary<Texture2D, ulong> textureIds = new Dictionary<Texture2D, ulong>();
		static ulong nextId = 32768;

		//Useful for crap like FBO resizing where textures will be thrown out a ton
		static Queue<ulong> freeIds = new Queue<ulong>();

		public static ImTextureRef RegisterTexture(Texture2D tex)
		{
			ulong id = 0;
			if (!textureIds.TryGetValue(tex, out id)) {
				if (freeIds.Count > 0)
					id = freeIds.Dequeue();
				else
					id = nextId++;
				textureIds.Add(tex, id);
				textures.Add(id, tex);
			}

            return new ImTextureRef() { _TexID = id };
        }

        public static ImTextureRef RenderGradient( Color4 top, Color4 bottom)
        {
            return instance.RenderGradientInternal(top, bottom);
        }

        ImTextureRef RenderGradientInternal(Color4 top, Color4 bottom)
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
                ImGui.GetIO().AddInputCharacterUTF16(c);
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
            io.AddKeyEvent(ImGuiKey.ImGuiMod_Ctrl, (e.Modifiers & KeyModifiers.Control) != 0);
            io.AddKeyEvent(ImGuiKey.ImGuiMod_Alt, (e.Modifiers & KeyModifiers.Alt) != 0);
            io.AddKeyEvent(ImGuiKey.ImGuiMod_Shift, (e.Modifiers & KeyModifiers.Shift) != 0);
            io.AddKeyEvent(ImGuiKey.ImGuiMod_Super, (e.Modifiers & KeyModifiers.GUI) != 0);
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
            Scale = game.DpiScale * UserScale;
            Theme.Apply(Scale);
			ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new Vector2(game.Width, game.Height);
			io.DisplayFramebufferScale = new Vector2(1, 1);
            const float MAX_DELTA = 0.1f;
			io.DeltaTime = elapsed > MAX_DELTA ? MAX_DELTA : (float)elapsed;
			//TODO: Mouse Wheel
			ImGui.NewFrame();
            ImGuizmo.BeginFrame();
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

        public static Rectangle GetClipRect(ImDrawCmd* pcmd)
        {
            return new Rectangle((int)pcmd->ClipRect.X, (int)pcmd->ClipRect.Y,
                (int)(pcmd->ClipRect.Z - pcmd->ClipRect.X),
                (int)(pcmd->ClipRect.W - pcmd->ClipRect.Y));
        }

        void UpdateTexture(ImTextureDataPtr texture, RenderContext rstate)
        {
            if (texture.Status == ImTextureStatus.WantCreate)
            {
                var gpuTex = new Texture2D(rstate, texture.Width, texture.Height);
                gpuTex.SetData(0, new Rectangle(0, 0, texture.Width, texture.Height), texture.GetPixels());
                texture.SetTexID(RegisterTexture(gpuTex)._TexID);
                texture.SetStatus(ImTextureStatus.OK);
            }
            else if (texture.Status == ImTextureStatus.WantUpdates)
            {
                for (int i = 0; i < texture.Updates.Size; i++)
                {
                    var r = texture.Updates[i];
                    var buf = new byte[r.h * r.w * 4];
                    for (int y = 0; y < r.h; y++)
                    {
                        var row = new Span<byte>((void*)texture.GetPixelsAt(r.x, r.y + y), r.w * 4);
                        row.CopyTo(buf.AsSpan(y * r.w * 4));
                    }
                    var gpuTex = textures[texture.TexID];
                    gpuTex.SetData(0, new Rectangle(r.x, r.y, r.w, r.h), buf, 0, buf.Length);
                }

                texture.SetStatus(ImTextureStatus.OK);
            }
            else if (texture.Status == ImTextureStatus.WantDestroy && texture.UnusedFrames > 0)
            {
                var gpuTex = textures[texture.TexID];
                DeregisterTexture(gpuTex);
                gpuTex.Dispose();
                texture.SetTexID(0);
                texture.SetStatus(ImTextureStatus.Destroyed);
            }
        }

		VertexBuffer vbo;
		ElementBuffer ibo;
		int vboSize = -1;
		int iboSize = -1;
		unsafe void RenderImDrawData(ImDrawDataPtr draw_data, RenderContext rstate)
		{
            if (draw_data.Textures != null)
            {
                ref ImPtrVector<ImTextureData> drawTextures = ref Unsafe.AsRef<ImPtrVector<ImTextureData>>(draw_data.Textures);
                for (int i = 0; i < drawTextures.Size; i++)
                {
                    if (drawTextures[i]->Status != ImTextureStatus.OK)
                    {
                        UpdateTexture(new(drawTextures[i]), rstate);
                    }
                }
            }
			var io = ImGui.GetIO();
            //Set cursor
            if (SetCursor)
            {
                switch (ImGui.GetMouseCursor())
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
            var uiShader = ImGuiShader.Shader.Get(0);
            uiShader.SetUniformBlock(2, ref mat);
            rstate.Shader = uiShader;
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
				var vtxCount = cmd_list->VtxBuffer.Size;
				var idxCount = cmd_list->IdxBuffer.Size;
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
                vbo.SetData(new ReadOnlySpan<DrawVert>((void*)cmd_list->VtxBuffer.Data, vtxCount));
                ibo.SetData(new ReadOnlySpan<ushort>((void*)cmd_list->IdxBuffer.Data, idxCount));
				for (int cmd_i = 0; cmd_i < cmd_list->CmdBuffer.Size; cmd_i++)
				{
                    ref var pcmd = ref cmd_list->CmdBuffer[cmd_i];
                    if (pcmd.UserCallback != null)
                    {
                        pcmd.UserCallback(cmd_list, (ImDrawCmd*)Unsafe.AsPointer(ref pcmd));
                        continue;
                    }
                    if (pcmd.ElemCount == 0)
                        continue;
                    rstate.Shader = uiShader;
                    var tid = pcmd.TexRef.GetTexID();
					if (textures.TryGetValue(tid, out var tex))
                    {
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
        }
    }
}
