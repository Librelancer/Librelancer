/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
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
