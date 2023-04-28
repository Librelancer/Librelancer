// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer;
using ImGuiNET;

namespace LancerEdit
{
    public class UniverseMap
    {
        public static string Draw(int imageId, GameDataManager gameData, int width, int height, int offsetY)
        {
            var crmin = ImGui.GetWindowContentRegionMin() + new Vector2(0, offsetY);
            var wpos = ImGui.GetWindowPos() + new Vector2(0, offsetY);
            var a = wpos +  ImGui.GetWindowContentRegionMin();
            var b = wpos +  ImGui.GetWindowContentRegionMax();
            var drawList = ImGui.GetWindowDrawList();
            if (imageId != -1) {
                drawList.AddImage((IntPtr) imageId, a, b, new Vector2(0, 1), new Vector2(1, 0));
            }
            else {
                drawList.AddRectFilled(a, b, 0xFF000000);
            }

            float margin = 0.15f;
            var min = crmin + (new Vector2(width, height) * margin);
            var factor = (new Vector2(width, height) * (1 - 2 * margin)) / 16f;
            string retVal = null;
            foreach (var sys in gameData.AllSystems)
            {
                ImGui.SetCursorPos(min + (sys.UniversePosition * factor) - new Vector2(8,8));
                if (ImGui.Button($"x##{sys.Nickname}"))
                    retVal = sys.Nickname;
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip($"{gameData.GetString(sys.IdsName)} ({sys.Nickname})");
            }

            return retVal;
        }
    }
}