// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer;
using ImGuiNET;
namespace LibreLancer.ImUI
{
	public partial class ImGuiHelper
    {
        private static Dictionary<Keys, ImGuiKey> keyMapping = new Dictionary<Keys, ImGuiKey>();

        static void SetKeyMappings()
		{
			var io = ImGui.GetIO();
            keyMapping = new Dictionary<Keys, ImGuiKey>();
            keyMapping[Keys.Tab] = ImGuiKey.Tab;
            keyMapping[Keys.Left] = ImGuiKey.LeftArrow;
            keyMapping[Keys.Right] = ImGuiKey.RightArrow;
            keyMapping[Keys.Up] = ImGuiKey.UpArrow;
            keyMapping[Keys.Down] = ImGuiKey.DownArrow;
            keyMapping[Keys.NavPageUp] = ImGuiKey.PageUp;
            keyMapping[Keys.NavPageDown] = ImGuiKey.PageDown;
            keyMapping[Keys.NavHome] = ImGuiKey.Home;
            keyMapping[Keys.NavEnd] = ImGuiKey.End;
            keyMapping[Keys.Delete] = ImGuiKey.Delete;
            keyMapping[Keys.Backspace] = ImGuiKey.Backspace;
            keyMapping[Keys.Enter] = ImGuiKey.Enter;
            keyMapping[Keys.Escape] = ImGuiKey.Escape;
            keyMapping[Keys.A.Map()] = ImGuiKey.A;
            keyMapping[Keys.C.Map()] = ImGuiKey.C;
            keyMapping[Keys.V.Map()] = ImGuiKey.V;
            keyMapping[Keys.X.Map()] = ImGuiKey.X;
            keyMapping[Keys.Y.Map()] = ImGuiKey.Y;
            keyMapping[Keys.Z.Map()] = ImGuiKey.Z;
        }
	}
}
