// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer;
using ImGuiNET;

namespace SystemViewer
{
    public class UniverseMap
    {
        public static string Draw(int imageId, GameDataManager gameData, int width, int height)
        {
            var crmin = (Vector2) ImGui.GetWindowContentRegionMin();
            if (imageId != -1)
            {
                var wpos = (Vector2)ImGui.GetWindowPos();
                var a = wpos +  (Vector2)ImGui.GetWindowContentRegionMin();
                var b = wpos +  (Vector2)ImGui.GetWindowContentRegionMax();
                var drawList = ImGui.GetWindowDrawList();
                drawList.AddImage((IntPtr) imageId, a, b, new Vector2(0, 1), new Vector2(1, 0));
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
                    ImGui.SetTooltip($"{sys.Name} ({sys.Nickname})");
            }

            return retVal;
        }
    }
}