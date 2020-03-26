// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace LancerEdit
{
    public static class ViewerControls
    {
        public static bool GradientButton(string id, Color4 colA, Color4 colB, Vector2 size, ViewportManager vps, bool gradient)
        {
            if (!gradient)
                return ImGui.ColorButton(id, new Vector4(colA.R, colA.G, colA.B, 1), ImGuiColorEditFlags.NoAlpha, size);
            ImGui.PushID(id);
            var img = ImGuiHelper.RenderGradient(vps, colA, colB);
            var retval = ImGui.ImageButton((IntPtr) img, size, new Vector2(0, 1), new Vector2(0, 0), 0);
            ImGui.PopID();
            return retval;
        }
    }
}