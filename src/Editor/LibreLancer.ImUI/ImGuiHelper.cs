// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using LibreLancer.Vertices;
using ImGuiNET;
namespace LibreLancer.ImUI
{
	public partial class ImGuiHelper
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
		const string vertex_source = @"
		#version 140
		in vec2 vertex_position;
		in vec2 vertex_texture1;
		in vec4 vertex_color;
		out vec2 out_texcoord;
		out vec4 blendColor;
		uniform mat4 modelviewproj;
		void main()
		{
    		gl_Position = modelviewproj * vec4(vertex_position, 0.0, 1.0);
    		blendColor = vertex_color;
    		out_texcoord = vertex_texture1;
		}
		";

		const string text_fragment_source = @"
		#version 140
		in vec2 out_texcoord;
		in vec4 blendColor;
		out vec4 out_color;
		uniform sampler2D tex;
		void main()
		{
			float alpha = texture(tex, out_texcoord).r;
            if(out_texcoord.x > 3) alpha = 1;
			out_color = vec4(blendColor.xyz, blendColor.a * alpha);
		}
		";

		const string color_fragment_source = @"
		#version 140
		in vec2 out_texcoord;
		in vec4 blendColor;
		out vec4 out_color;
		uniform sampler2D tex;
		void main()
		{
			vec4 texsample = texture(tex, out_texcoord);
			out_color = blendColor * texsample;
		}
		";
		
		Shader textShader;
		Shader colorShader;
		Texture2D fontTexture;
		const int FONT_TEXTURE_ID = 8;
		public static int CheckerboardId;
		Texture2D dot;
		Texture2D checkerboard;
		IntPtr ttfPtr;
        IntPtr context;
		public static ImFontPtr Noto;
		public static ImFontPtr Default;
		public unsafe ImGuiHelper(Game game)
		{
			this.game = game;
			game.Keyboard.KeyDown += Keyboard_KeyDown;
			game.Keyboard.KeyUp += Keyboard_KeyUp;
			game.Keyboard.TextInput += Keyboard_TextInput;
            context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);
            SetKeyMappings();

            var io = ImGui.GetIO();
            //IniFilename?
            io.WantSaveIniSettings = false;
            Default = io.Fonts.AddFontDefault();
			using (var stream = typeof(ImGuiHelper).Assembly.GetManifestResourceStream("LibreLancer.ImUI.Roboto-Medium.ttf"))
			{
				var ttf = new byte[stream.Length];
				stream.Read(ttf, 0, ttf.Length);
				ttfPtr = Marshal.AllocHGlobal(ttf.Length);
				Marshal.Copy(ttf, 0, ttfPtr, ttf.Length);
				Noto = io.Fonts.AddFontFromMemoryTTF(ttfPtr, ttf.Length, 15);
			}
			using (var stream = typeof(ImGuiHelper).Assembly.GetManifestResourceStream("LibreLancer.ImUI.checkerboard.png"))
			{
				checkerboard = LibreLancer.ImageLib.Generic.FromStream(stream);
				CheckerboardId = RegisterTexture(checkerboard);
			}
            ImGuiExt.BuildFontAtlas((IntPtr)io.Fonts.NativePtr);
            byte* fontBytes;
            int fontWidth, fontHeight;
            io.Fonts.GetTexDataAsAlpha8(out fontBytes, out fontWidth, out fontHeight);
            io.Fonts.TexUvWhitePixel = new Vector2(10, 10);
            fontTexture = new Texture2D(fontWidth,fontHeight, false, SurfaceFormat.R8);
			var bytes = new byte[fontWidth * fontHeight];
            Marshal.Copy((IntPtr)fontBytes, bytes, 0, fontWidth * fontHeight);
			fontTexture.SetData(bytes);
			fontTexture.SetFiltering(TextureFiltering.Linear);
			io.Fonts.SetTexID((IntPtr)FONT_TEXTURE_ID);
			io.Fonts.ClearTexData();
			textShader = new Shader(vertex_source, text_fragment_source);
			colorShader = new Shader(vertex_source, color_fragment_source);
			dot = new Texture2D(1, 1, false, SurfaceFormat.Color);
			var c = new Color4b[] { Color4b.White };
			dot.SetData(c);
            Theme.Apply();
            //Required for clipboard function on non-Windows platforms
            utf8buf = Marshal.AllocHGlobal(8192);
            instance = this;
            setTextDel = SetClipboardText;
            getTextDel = GetClipboardText;
            
            io.GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(getTextDel);
            io.SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(setTextDel);
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
		static int nextId = 2;

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
			if (mappedKeys.Contains(e.Key)) io.KeysDown[(int)mappedKeys.IndexOf(e.Key)] = true;
			io.KeyAlt = ((e.Modifiers & KeyModifiers.LeftAlt) == KeyModifiers.LeftAlt);
			io.KeyCtrl = ((e.Modifiers & KeyModifiers.LeftControl) == KeyModifiers.LeftControl);
			io.KeyShift = ((e.Modifiers & KeyModifiers.LeftShift) == KeyModifiers.LeftShift);
		}

		void Keyboard_KeyUp(KeyEventArgs e)
		{
			var io = ImGui.GetIO();
			if (mappedKeys.Contains(e.Key)) io.KeysDown[(int)mappedKeys.IndexOf(e.Key)] = false;
			io.KeyAlt = ((e.Modifiers & KeyModifiers.LeftAlt) == KeyModifiers.LeftAlt);
			io.KeyCtrl = ((e.Modifiers & KeyModifiers.LeftControl) == KeyModifiers.LeftControl);
			io.KeyShift = ((e.Modifiers & KeyModifiers.LeftShift) == KeyModifiers.LeftShift);
		}


		public void NewFrame(double elapsed)
		{
			ImGuiIOPtr io = ImGui.GetIO();
			io.DisplaySize = new Vector2(game.Width, game.Height);
			io.DisplayFramebufferScale = new Vector2(1, 1);
			io.DeltaTime = (float)elapsed;
			//Update input
			io.MousePos = new Vector2(game.Mouse.X, game.Mouse.Y);
			io.MouseDown[0] = game.Mouse.IsButtonDown(MouseButtons.Left);
			io.MouseDown[1] = game.Mouse.IsButtonDown(MouseButtons.Right);
			io.MouseDown[2] = game.Mouse.IsButtonDown(MouseButtons.Middle);
			io.MouseWheel = game.Mouse.MouseDelta / 2.5f;
			game.Mouse.MouseDelta = 0;
            game.TextInputEnabled = io.WantCaptureKeyboard;
			//TODO: Mouse Wheel
			ImGui.NewFrame();
		}

		public void Render(RenderState rstate)
		{
			ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData(), rstate);
		}

		VertexBuffer vbo;
		ElementBuffer ibo;
		ushort[] ibuffer;
		Vertex2D[] vbuffer;
		int vboSize = -1;
		int iboSize = -1;
		unsafe void RenderImDrawData(ImDrawDataPtr draw_data, RenderState rstate)
		{
			var io = ImGui.GetIO();
            //Set cursor
            var cur = ImGuiNative.igGetMouseCursor();
            switch(cur) {
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
                case ImGuiMouseCursor.ResizeNESW:
                    game.CursorKind = CursorKind.ResizeNESW;
                    break;
                case ImGuiMouseCursor.ResizeNWSE:
                    game.CursorKind = CursorKind.ResizeNWSE;
                    break;
            }
            //Render
            draw_data.ScaleClipRects(io.DisplayFramebufferScale);

			var mat = Matrix4.CreateOrthographicOffCenter(0, game.Width, game.Height, 0, 0, 1);
			Shader lastShader = textShader;
			textShader.SetMatrix(textShader.GetLocation("modelviewproj"), ref mat);
			textShader.SetInteger(textShader.GetLocation("tex"), 0);
			colorShader.SetMatrix(textShader.GetLocation("modelviewproj"), ref mat);
			colorShader.SetInteger(textShader.GetLocation("tex"), 0);
			textShader.UseProgram();
			rstate.Cull = false;
			rstate.BlendMode = BlendMode.Normal;
			rstate.DepthEnabled = false;
			for (int n = 0; n < draw_data.CmdListsCount; n++)
			{
                var cmd_list = draw_data.CmdListsRange[n];
                byte* vtx_buffer = (byte*)cmd_list.VtxBuffer.Data;
				ushort* idx_buffer = (ushort*)cmd_list.IdxBuffer.Data;
				var vtxCount = cmd_list.VtxBuffer.Size;
				var idxCount = cmd_list.IdxBuffer.Size;
				if (vboSize < vtxCount || iboSize < idxCount)
				{
					if (vbo != null) vbo.Dispose();
					if (ibo != null) ibo.Dispose();
					vboSize = Math.Max(vboSize, vtxCount);
					iboSize = Math.Max(iboSize, idxCount);
					vbo = new VertexBuffer(typeof(Vertex2D), vboSize, true);
					ibo = new ElementBuffer(iboSize, true);
					vbo.SetElementBuffer(ibo);
					vbuffer = new Vertex2D[vboSize];
					ibuffer = new ushort[iboSize];
				}
				for (int i = 0; i < cmd_list.IdxBuffer.Size; i++)
				{
					ibuffer[i] = idx_buffer[i];
				}
				for (int i = 0; i < cmd_list.VtxBuffer.Size; i++)
				{
					var ptr = (ImDrawVert*)vtx_buffer;
					var unint = ptr[i].col;
					var a = unint >> 24 & 0xFF;
					var b = unint >> 16 & 0xFF;
					var g = unint >> 8 & 0xFF;
					var r = unint & 0xFF;
					vbuffer[i] = new Vertex2D(ptr[i].pos, ptr[i].uv, new Color4(r / 255f, g / 255f, b / 255f, a / 255f));
				}
				vbo.SetData(vbuffer, cmd_list.VtxBuffer.Size);
				ibo.SetData(ibuffer, cmd_list.IdxBuffer.Size);
				int startIndex = 0;
				for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
				{
                    var pcmd = cmd_list.CmdBuffer[cmd_i];
					//TODO: Do something with pcmd->UserCallback ??
					var tid = pcmd.TextureId.ToInt32();
					Texture2D tex;
					if (tid == FONT_TEXTURE_ID)
					{
						if (lastShader != textShader)
						{
							textShader.UseProgram();
							lastShader = textShader;
						}
						fontTexture.BindTo(0);
					}
					else if (textures.TryGetValue(tid, out tex))
					{
						if (lastShader != colorShader)
						{
							colorShader.UseProgram();
							lastShader = colorShader;
						}
						tex.BindTo(0);
					}
					else
					{
						dot.BindTo(0);
					}

					GL.Enable(GL.GL_SCISSOR_TEST);
					GL.Scissor(
					(int)pcmd.ClipRect.X,
					(int)(io.DisplaySize.Y - pcmd.ClipRect.W),
					(int)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
					(int)(pcmd.ClipRect.W - pcmd.ClipRect.Y));
					vbo.Draw(PrimitiveTypes.TriangleList, 0, startIndex, (int)pcmd.ElemCount / 3);
					GL.Disable(GL.GL_SCISSOR_TEST);
					startIndex += (int)pcmd.ElemCount;
				}
			}
		}
	}
}
