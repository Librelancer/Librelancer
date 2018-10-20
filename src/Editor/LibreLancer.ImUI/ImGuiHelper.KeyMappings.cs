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
			IO io = ImGui.GetIO();
			io.KeyMap[GuiKey.Tab] = 0;
			io.KeyMap[GuiKey.LeftArrow] = 1;
			io.KeyMap[GuiKey.RightArrow] = 2;
			io.KeyMap[GuiKey.UpArrow] = 3;
			io.KeyMap[GuiKey.DownArrow] = 4;
			io.KeyMap[GuiKey.PageUp] = 5;
			io.KeyMap[GuiKey.PageDown] = 6;
			io.KeyMap[GuiKey.Home] = 7;
			io.KeyMap[GuiKey.End] = 8;
			io.KeyMap[GuiKey.Delete] = 9;
			io.KeyMap[GuiKey.Backspace] = 10;
			io.KeyMap[GuiKey.Enter] = 11;
			io.KeyMap[GuiKey.Escape] = 12;
			io.KeyMap[GuiKey.A] = 13;
			io.KeyMap[GuiKey.C] = 14;
			io.KeyMap[GuiKey.V] = 15;
			io.KeyMap[GuiKey.X] = 16;
			io.KeyMap[GuiKey.Y] = 17;
			io.KeyMap[GuiKey.Z] = 18;
		}
	}
}
