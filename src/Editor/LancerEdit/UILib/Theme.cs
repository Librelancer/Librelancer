using System;
using LibreLancer;
using ImGuiNET;
namespace LancerEdit
{
    public class Theme
    {
        public static unsafe void Apply()
        {
            var s = ImGui.GetStyle();
            //Settings
            s.FrameRounding = 2;
            s.ScrollbarSize = 12;
            s.ScrollbarRounding = 3;
            s.NativePtr->FrameBorderSize = 1f;
            //Colours
            s.SetColor(ColorTarget.WindowBg, RGBA(41, 42, 44, 255));
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
        }
        static Vector4 RGBA(int r, int g, int b, int a)
        {
            return new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
        }
    }
}
