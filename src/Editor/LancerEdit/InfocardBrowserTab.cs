// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.ImUI;
using LibreLancer;
using LibreLancer.Infocards;
using ImGuiNET;
    
namespace LancerEdit
{
    public class InfocardBrowserTab : EditorTab
    {
        int currentString = -1;
        int currentInfocard = -1;

        bool showStrings = true;
        InfocardManager manager;
        InfocardControl display;
        int[] stringsIds;
        int[] infocardsIds;

        TextBuffer txt;

        ListClipper stringClipper;
        ListClipper infocardClipper;

        MainWindow win;

        public InfocardBrowserTab(string flini, MainWindow win)
        {
            this.win = win;
            var ini = new FreelancerIni(flini);
            if (ini.JsonResources != null)
                manager = new InfocardManager(File.ReadAllText(ini.JsonResources.Item1), File.ReadAllText(ini.JsonResources.Item2));
            else
                manager = new InfocardManager(ini.Resources);
            stringsIds = manager.StringIds.ToArray();
            infocardsIds = manager.InfocardIds.ToArray();
            txt = new TextBuffer(8192);

            stringClipper = new ListClipper(stringsIds.Length);
            infocardClipper = new ListClipper(infocardsIds.Length);
            Title = "Infocard Browser##" + Unique;
        }


        public override void Draw()
        {
            ImGui.Columns(2, "cols", true);
            //strings vs infocards
            if (ImGuiExt.ToggleButton("Strings", showStrings)) showStrings = true;
            ImGui.SameLine();
            if (ImGuiExt.ToggleButton("Infocards", !showStrings)) showStrings = false;
            ImGui.Separator();
            //list
            ImGui.BeginChild("##list");
            if(showStrings)
            {
                stringClipper.Begin(stringsIds.Length);
                while (stringClipper.Step())
                {
                    for (int i = stringClipper.DisplayStart; i < stringClipper.DisplayEnd; i++)
                    {
                        if (ImGui.Selectable(stringsIds[i] + "##" + i, currentString == i))
                        {
                            currentString = i;
                            txt.SetText(manager.GetStringResource(stringsIds[i]));
                        }
                    }
                }
                stringClipper.End();
            } else {
                infocardClipper.Begin(infocardsIds.Length);
                while (infocardClipper.Step())
                {
                    for (int i = infocardClipper.DisplayStart; i < infocardClipper.DisplayEnd; i++)
                    {
                        if (ImGui.Selectable(infocardsIds[i] + "##" + i, currentInfocard == i))
                        {
                            currentInfocard = i;
                            if (display == null)
                            {
                                display = new InfocardControl(win, RDLParse.Parse(manager.GetXmlResource(infocardsIds[currentInfocard])), 100);
                            }
                            else
                            {
                                display.SetInfocard(RDLParse.Parse(manager.GetXmlResource(infocardsIds[currentInfocard])));
                            }
                        }
                    }
                }
                infocardClipper.End();
            }
            ImGui.EndChild();
            ImGui.NextColumn();
            //Display
            if (showStrings)
            {
                if (currentString != -1)
                {
                    ImGui.Text(stringsIds[currentString].ToString());
                    txt.InputTextMultiline("##txt", new Vector2(-1, ImGui.GetWindowHeight() - 70), ImGuiInputTextFlags.ReadOnly);
                }
            }
            else
            {
                if(currentInfocard != -1)
                {
                    ImGui.Text(infocardsIds[currentInfocard].ToString());
                    ImGui.BeginChild("##display");
                    display.Draw(ImGui.GetWindowWidth() - 5);
                    ImGui.EndChild();
                }
            }
        }
        public override void Dispose()
        {
            stringClipper.Dispose();
            infocardClipper.Dispose();
        }
    }
}
