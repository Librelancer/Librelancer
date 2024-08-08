// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;
using LibreLancer.Render;
using LibreLancer.World;

namespace LibreLancer.Interface
{
    public class DebugView
    {
        private FreelancerGame game;
        private ImGuiHelper igrender;

        public DebugView(FreelancerGame game)
        {
            this.game = game;
            igrender = new ImGuiHelper(game, 1f);
            igrender.SetCursor = false;
            igrender.HandleKeyboard = false;
        }

        public bool Enabled = false;
        public bool CaptureMouse = false;

        public void MissionWindow(MissionRuntime.TriggerInfo[] triggers)
        {
            if (triggers != null)
            {
                ImGui.SetNextWindowSize(new Vector2(600, 500), ImGuiCond.FirstUseEver);
                ImGui.Begin("Triggers");
                int i = 0;
                foreach (var t in triggers)
                {
                    ImGui.BeginChild($"{t.Name};{i++}", new Vector2(-1, 250), ImGuiChildFlags.Border);
                    ImGui.PushFont(ImGuiHelper.SystemMonospace);
                    ImGui.Text(t.Name);
                    ImGui.PopFont();
                    ImGui.Text("Conditions");
                    ImGui.Separator();
                    foreach(var c in t.Conditions)
                        ImGui.Text(c);
                    ImGui.Separator();
                    ImGui.Text("Actions");
                    ImGui.Separator();
                    foreach (var a in t.Actions)
                        ImGui.Text(a);
                    ImGui.EndChild();
                }
                ImGui.End();
            }
        }

        private ObjectRenderer lastHighlight = null;
        private bool lastAmbient;
        private bool lastDynamic;

        public void ObjectsWindow(IReadOnlyCollection<GameObject> objects)
        {
            ImGui.SetNextWindowSize(new Vector2(300, 500), ImGuiCond.FirstUseEver);
            ImGui.Begin("Objects");
            ObjectRenderer toHighlight = null;
            foreach (var obj in objects)
            {
                ImGui.Selectable(obj.ToString());
                if (ImGui.IsItemHovered() && obj.RenderComponent != null)
                {
                    toHighlight = obj.RenderComponent;
                }
            }

            if (toHighlight != lastHighlight)
            {
                if (lastHighlight != null)
                {
                    lastHighlight.LitAmbient = lastAmbient;
                    lastHighlight.LitDynamic = lastDynamic;
                }

                if (toHighlight != null)
                {
                    lastAmbient = toHighlight.LitAmbient;
                    lastDynamic = toHighlight.LitDynamic;
                    toHighlight.LitAmbient = false;
                    toHighlight.LitDynamic = false;
                }

                lastHighlight = toHighlight;
            }

            ImGui.End();
        }

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
