// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LibreLancer;

namespace LancerEdit.GameContent
{
    public class UniverseMap
    {
        public static string Draw(int imageId, GameDataManager gameData, List<(Vector2, Vector2)> connections, int width, int height, int offsetY)
        {

            var minPos = ImGui.GetCursorScreenPos();
            var maxPos = minPos + new Vector2(width, height);
            
            var drawList = ImGui.GetWindowDrawList();

            float buttonSize = (int) ((width / 838.0f) * 16f);
            if (buttonSize < 2) buttonSize = 2;
            
            if (imageId != -1) {
                drawList.AddImage((IntPtr) imageId, minPos, maxPos, new Vector2(0, 1), new Vector2(1, 0));
            }
            else {
                drawList.AddRectFilled(minPos, maxPos, 0xFF000000);
            }
        
            float margin = 0.15f;
            var min = ImGui.GetCursorPos() + new Vector2(width, height) * margin;
            var factor = (new Vector2(width, height) * (1 - 2 * margin)) / 16f;
            var connectMin = minPos + new Vector2(width, height) * margin;
            foreach (var c in connections)
            {
                var a = connectMin + c.Item1 * factor;
                var b = connectMin + c.Item2 * factor;
                drawList.AddLine(a, b, (uint)Color4.CornflowerBlue.ToAbgr(), 2f);
            }
            
            string retVal = null;
            foreach (var sys in gameData.Systems)
            {
                ImGui.SetCursorPos(min + (sys.UniversePosition * factor) - new Vector2(0.5f * buttonSize));
                ImGui.PushStyleColor(ImGuiCol.Button, Color4.LightGray);
                if (ImGui.Button($"###{sys.Nickname}", new Vector2(buttonSize)))
                    retVal = sys.Nickname;
                ImGui.PopStyleColor();
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip($"{gameData.GetString(sys.IdsName)} ({sys.Nickname})");
            }

            return retVal;
        }
    }
}