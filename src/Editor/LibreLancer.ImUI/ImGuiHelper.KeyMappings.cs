// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer;
using ImGuiNET;
namespace LibreLancer.ImUI
{
	public partial class ImGuiHelper
	{
		static readonly Keys[] _mappedKeys = new Keys[] {
			Keys.Tab,
			Keys.Left,
			Keys.Right,
			Keys.Up,
			Keys.Down,
			Keys.NavPageUp,
			Keys.NavPageDown,
			Keys.NavHome,
			Keys.NavEnd,
			Keys.Delete,
			Keys.Backspace,
			Keys.Enter,
			Keys.Escape,
			Keys.A,
			Keys.C,
			Keys.V,
			Keys.X,
			Keys.Y,
			Keys.Z
		};
		static List<Keys> mappedKeys;
		static ImGuiHelper()
		{
			mappedKeys = new List<Keys>(_mappedKeys);
		}
		static void SetKeyMappings()
		{
			var io = ImGui.GetIO();
			io.KeyMap[(int)ImGuiKey.Tab] = 0;
			io.KeyMap[(int)ImGuiKey.LeftArrow] = 1;
			io.KeyMap[(int)ImGuiKey.RightArrow] = 2;
			io.KeyMap[(int)ImGuiKey.UpArrow] = 3;
			io.KeyMap[(int)ImGuiKey.DownArrow] = 4;
			io.KeyMap[(int)ImGuiKey.PageUp] = 5;
			io.KeyMap[(int)ImGuiKey.PageDown] = 6;
			io.KeyMap[(int)ImGuiKey.Home] = 7;
			io.KeyMap[(int)ImGuiKey.End] = 8;
			io.KeyMap[(int)ImGuiKey.Delete] = 9;
			io.KeyMap[(int)ImGuiKey.Backspace] = 10;
			io.KeyMap[(int)ImGuiKey.Enter] = 11;
			io.KeyMap[(int)ImGuiKey.Escape] = 12;
			io.KeyMap[(int)ImGuiKey.A] = 13;
			io.KeyMap[(int)ImGuiKey.C] = 14;
			io.KeyMap[(int)ImGuiKey.V] = 15;
			io.KeyMap[(int)ImGuiKey.X] = 16;
			io.KeyMap[(int)ImGuiKey.Y] = 17;
			io.KeyMap[(int)ImGuiKey.Z] = 18;
		}
	}
}
