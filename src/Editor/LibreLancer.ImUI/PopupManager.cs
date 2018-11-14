// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using ImGuiNET;
namespace LibreLancer.ImUI
{
    public class PopupManager
    {
        List<PopupContext> popups = new List<PopupContext>();
        class PopupContext
        {
            public bool DoOpen = false;
            public Action<PopupData> DrawAction;
            public PopupData Data;
            public string Title;
            public ImGuiWindowFlags Flags;
        }
        public void AddPopup(string title, Action<PopupData> action, ImGuiWindowFlags flags = 0)
        {
            popups.Add(new PopupContext() { Title = title, DrawAction = action, Flags = flags, Data = new PopupData() });
        }
        public void OpenPopup(string title)
        {
            foreach (var p in popups)
            {
                if (p.Title == title)
                {
                    p.DoOpen = true;
                    break;
                }
            }
        }
        public void Run()
        {
            foreach(var p in popups) {
                if(p.DoOpen) {
                    p.Data.DoFocus = p.Data.First = true;
                    ImGui.OpenPopup(p.Title);
                    p.DoOpen = false;
                }
                bool open = true;
                if(ImGui.BeginPopupModal(p.Title, ref open,p.Flags)) {
                   
                    p.DrawAction(p.Data);
                    if (p.Data.First)
                        p.Data.First = false;
                    else
                        p.Data.DoFocus = false;
                    ImGui.EndPopup();
                }
            }
        }
    }
    public class PopupData
    {
        public bool DoFocus;
        public bool First;
    }
}
