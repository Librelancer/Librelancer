// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using ImGuiNET;
using LibreLancer.ImUI;
namespace LibreLancer.Interface
{
    public class DebugView
    {
        private FreelancerGame game;
        private ImGuiHelper igrender;
        
        public DebugView(FreelancerGame game)
        {
            this.game = game;
            igrender = new ImGuiHelper(game);
            igrender.SetCursor = false;
            igrender.HandleKeyboard = false;
        }

        public bool Enabled = false;
        public bool CaptureMouse = false;

        public void Draw(double elapsed, Action debugWindow = null, Action otherWindows = null)
        {
            if (Enabled)
            {
                igrender.NewFrame(elapsed);
                ImGui.PushFont(ImGuiHelper.Noto);
                ImGui.Begin("Debug");
                ImGui.Text($"FPS: {game.RenderFrequency:F2}");
                debugWindow?.Invoke();
                CaptureMouse = ImGui.IsWindowHovered();
                ImGui.End();
                otherWindows?.Invoke();
                ImGui.PopFont();
                igrender.Render(game.RenderContext);
            }
            else
            {
                CaptureMouse = false;
            }
        }
    }
}