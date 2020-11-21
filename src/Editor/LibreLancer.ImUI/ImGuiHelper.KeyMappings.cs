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

        static void SetKeyMappings()
		{
			var io = ImGui.GetIO();
			io.KeyMap[(int)ImGuiKey.Tab] = (int)Keys.Tab;
            io.KeyMap[(int) ImGuiKey.LeftArrow] = (int) Keys.Left;
            io.KeyMap[(int) ImGuiKey.RightArrow] = (int) Keys.Right;
            io.KeyMap[(int) ImGuiKey.UpArrow] = (int) Keys.Up;
            io.KeyMap[(int) ImGuiKey.DownArrow] = (int) Keys.Down;
            io.KeyMap[(int) ImGuiKey.PageUp] = (int) Keys.NavPageUp;
            io.KeyMap[(int) ImGuiKey.PageDown] = (int) Keys.NavPageDown;
            io.KeyMap[(int) ImGuiKey.Home] = (int) Keys.NavHome;
            io.KeyMap[(int) ImGuiKey.End] = (int) Keys.NavEnd;
            io.KeyMap[(int) ImGuiKey.Delete] = (int) Keys.Delete;
            io.KeyMap[(int) ImGuiKey.Backspace] = (int) Keys.Backspace;
            io.KeyMap[(int) ImGuiKey.Enter] = (int) Keys.Enter;
            io.KeyMap[(int) ImGuiKey.Escape] = (int) Keys.Escape;
            io.KeyMap[(int) ImGuiKey.A] = (int) Keys.A.Map();
            io.KeyMap[(int) ImGuiKey.C] = (int) Keys.C.Map();
            io.KeyMap[(int) ImGuiKey.V] = (int) Keys.V.Map();
            io.KeyMap[(int) ImGuiKey.X] = (int) Keys.X.Map();
            io.KeyMap[(int) ImGuiKey.Y] = (int) Keys.Y.Map();
            io.KeyMap[(int) ImGuiKey.Z] = (int) Keys.Z.Map();
        }
	}
}
