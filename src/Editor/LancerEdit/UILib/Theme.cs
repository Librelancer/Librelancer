using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibreLancer.ImageLib;
using LibreLancer;
using ImGuiNET;
namespace LancerEdit
{
    public class Theme
    {
        static Texture2D iconTexture;
        static int iconId;
        class TCoordinates
        {
            public Vector2 UV0;
            public Vector2 UV1;
            public Vector2 Size;
        }
        static Dictionary<string, TCoordinates> icons = new Dictionary<string, TCoordinates>();
        public static unsafe void Apply()
        {
            var s = ImGui.GetStyle();
            //Settings
            s.FrameRounding = 2;
            s.ScrollbarSize = 12;
            s.ScrollbarRounding = 3;
            s.NativePtr->FrameBorderSize = 1f;
            //Colours
            s.SetColor(ColorTarget.WindowBg, RGBA(21, 21, 22, 128));
            s.SetColor(ColorTarget.Border, RGBA(83, 83, 83, 255));
            s.SetColor(ColorTarget.FrameBg, RGBA(56, 57, 58, 255));
            s.SetColor(ColorTarget.PopupBg, RGBA(56, 57, 58, 255));
            s.SetColor(ColorTarget.FrameBgHovered, RGBA(66, 133, 190, 255));
            s.SetColor(ColorTarget.Header, RGBA(88, 178, 255, 132));
            s.SetColor(ColorTarget.HeaderActive, RGBA(88, 178, 255, 164));
            s.SetColor(ColorTarget.FrameBgActive, RGBA(95,97, 98, 255));
            s.SetColor(ColorTarget.MenuBarBg, RGBA(66, 67, 69, 255));
            s.SetColor(ColorTarget.ScrollbarBg, RGBA(51, 64, 77, 153));
            s.SetColor(ColorTarget.Button, RGBA(128, 128, 128, 88));

            using(var stream = typeof(Theme).Assembly.GetManifestResourceStream("LancerEdit.UILib.icons.png")) {
                iconTexture = Generic.FromStream(stream);
                iconId = ImGuiHelper.RegisterTexture(iconTexture);
            }
            using(var reader = new StreamReader(typeof(Theme).Assembly.GetManifestResourceStream("LancerEdit.UILib.icons.txt"))) {
                while(!reader.EndOfStream) {
                    var ln = reader.ReadLine().Trim();
                    if (string.IsNullOrEmpty(ln)) continue;
                    var sp = ln.Split('=');
                    var n = sp[0].Trim();
                    var vals = sp[1].Trim().Split(' ').Select(float.Parse).ToArray();
                    var uv0 = new Vector2(vals[0] / iconTexture.Width,
                                          1 - (vals[1] / iconTexture.Height));
                    var uv1 = new Vector2((vals[0] + vals[2]) / iconTexture.Width,
                                          1 - (vals[1] + vals[3]) / iconTexture.Height);
                    icons.Add(n, new TCoordinates() { UV0 = uv0, UV1 = uv1, Size = new Vector2(vals[2],vals[3]) });
                }   
            }
        }

        public static void RenderTreeIcon(string text, string icon, Color4 tint)
        {
            ImGui.SameLine();
            var w = ImGui.GetTextSize(text).X;
            ImGuiNative.igSetCursorPosX(ImGuiNative.igGetCursorPosX() - w - 27);
            var uvs = icons[icon];
            ImGui.Image((IntPtr)iconId,
                       uvs.Size, uvs.UV0, uvs.UV1,
                        new Vector4(tint.R, tint.G, tint.B, tint.A), Vector4.Zero);
        }

        public static void Icon(string icon, Color4 tint)
        {
            var uvs = icons[icon];
            ImGui.Image((IntPtr)iconId, uvs.Size, uvs.UV0, uvs.UV1,
                        new Vector4(tint.R, tint.G, tint.B, tint.A), Vector4.Zero);
        }
        public static bool IconButton(string id, string icon, Color4 tint)
        {
            ImGui.PushID(id);
            var uvs = icons[icon];
            var ret = ImGui.ImageButton((IntPtr)iconId, uvs.Size, uvs.UV0, uvs.UV1, 1,
                              Vector4.Zero, new Vector4(tint.R, tint.G, tint.B, tint.A));
            ImGui.PopID();
            return ret;
        }
        const string MENU_PADDING = "        ";
        public static bool IconMenuItem(string text, string icon, Color4 tint, bool enabled)
        {
            bool ret = false;
            if (enabled) ret = ImGui.Selectable(MENU_PADDING + text);
            else
            {
                ImGui.PushStyleColor(ColorTarget.Text, ImGui.GetStyle().GetColor(ColorTarget.TextDisabled));
                ImGui.Text(MENU_PADDING + text);
                ImGui.PopStyleColor();
            }
            ImGui.SameLine();
            var w = ImGui.GetTextSize(text).X;
            ImGuiNative.igSetCursorPosX(ImGuiNative.igGetCursorPosX() - w - 32);
            Icon(icon, tint);
            return ret;
        }
        static Vector4 RGBA(int r, int g, int b, int a)
        {
            return new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
        }
    }
}
