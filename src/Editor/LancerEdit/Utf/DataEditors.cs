// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer.ContentEdit;
using LibreLancer.ImUI;

namespace LancerEdit
{
	public unsafe class DataEditors
	{
		public static void IntEditor(string title, ref int[] ints, ref bool intHex, LUtfNode selectedNode)
		{
            ImGui.SetNextWindowSize(new Vector2(300,200) * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
			if (ImGui.BeginPopupModal(title))
			{
				bool remove = false;
				bool add = false;
				ImGui.Text(string.Format("Count: {0} ({1} bytes)", ints.Length, ints.Length * 4));
				ImGui.SameLine();
				add = ImGui.Button("+");
				ImGui.SameLine();
				remove = ImGui.Button("-");
				ImGui.SameLine();
				ImGui.Checkbox("Hex", ref intHex);
				ImGui.Separator();
				//Magic number 94px seems to fix the scrollbar thingy
				var h = ImGui.GetWindowHeight();
				ImGui.BeginChild("##scroll", new Vector2(0, h - 94 * ImGuiHelper.Scale), ImGuiChildFlags.Border);
				ImGui.Columns(4, "##columns", true);
					for (int i = 0; i < ints.Length; i++)
					{
                        ImGui.InputInt("##" + i.ToString(), ref ints[i], 0, 0, intHex ? ImGuiInputTextFlags.CharsHexadecimal : ImGuiInputTextFlags.CharsDecimal);
						ImGui.NextColumn();
						if (i % 4 == 0 && i != 0) ImGui.Separator();
                        }
				ImGui.EndChild();
				if (ImGui.Button("Ok"))
				{
					var bytes = new byte[ints.Length * 4];
					fixed (byte* ptr = bytes)
					{
						var f = (int*)ptr;
						for (int i = 0; i < ints.Length; i++) f[i] = ints[i];
					}
					selectedNode.Data = bytes;
					ints = null;
					ImGui.CloseCurrentPopup();
				}
				ImGui.SameLine();
				if (ImGui.Button("Cancel")) { ints = null; ImGui.CloseCurrentPopup(); }
				ImGui.EndPopup();
				if (add) Array.Resize(ref ints, ints.Length + 1);
				if (remove && ints.Length > 1) Array.Resize(ref ints, ints.Length - 1);
			}
		}
		public static void FloatEditor(string title, ref float[] floats, LUtfNode selectedNode)
		{
            ImGui.SetNextWindowSize(new Vector2(300,200) * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
			if (ImGui.BeginPopupModal(title))
			{
				bool remove = false;
				bool add = false;
				ImGui.Text(string.Format("Count: {0} ({1} bytes)", floats.Length, floats.Length * 4));
				ImGui.SameLine();
				add = ImGui.Button("+");
				ImGui.SameLine();
				remove = ImGui.Button("-");
				ImGui.Separator();
				//Magic number 94px seems to fix the scrollbar thingy
				var h = ImGui.GetWindowHeight();
				ImGui.BeginChild("##scroll", new Vector2(0, h - 94 * ImGuiHelper.Scale));
				ImGui.Columns(4, "##columns", true);
                for (int i = 0; i < floats.Length; i++)
                {
                    ImGui.InputFloat("##" + i, ref floats[i], 0, 0);
                    ImGui.NextColumn();
                    if (i % 4 == 0 && i != 0) ImGui.Separator();
                }
				ImGui.EndChild();
				if (ImGui.Button("Ok"))
				{
					var bytes = new byte[floats.Length * 4];
					fixed (byte* ptr = bytes)
					{
						var f = (float*)ptr;
						for (int i = 0; i < floats.Length; i++) f[i] = floats[i];
					}
					selectedNode.Data = bytes;
					floats = null;
					ImGui.CloseCurrentPopup();
				}
				ImGui.SameLine();
				if (ImGui.Button("Cancel")) { floats = null; ImGui.CloseCurrentPopup(); }
				ImGui.EndPopup();
				if (add) Array.Resize(ref floats, floats.Length + 1);
				if (remove && floats.Length > 1) Array.Resize(ref floats, floats.Length - 1);
			}
		}
	}
}
