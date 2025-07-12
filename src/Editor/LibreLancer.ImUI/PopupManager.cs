// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace LibreLancer.ImUI
{
    public class PopupManager
    {
        private List<string> toOpens = new List<string>();
        private RefList<(PopupWindow Window, bool Appearing)> openPopups = new();

        public void OpenPopup<T>(T popup) where T : PopupWindow
        {
            toOpens.Add(popup.Title);
            openPopups.Add((popup, true));
        }

        public void MessageBox(string title, string message,
            bool multiline = false,
            MessageBoxButtons buttons = MessageBoxButtons.Ok,
            Action<MessageBoxResponse> callback = null)
        {
            OpenPopup(new MessageBoxPopup(title, message, multiline, buttons, callback));
        }

        public void Run()
        {
            foreach(var o in toOpens)
                ImGui.OpenPopup(o);
            toOpens.Clear();
            for (int i = 0; i < openPopups.Count; i++)
            {
                var p = openPopups[i].Window;
                bool open = true;
                bool beginval;
                if(p.InitSize != Vector2.Zero)
                    ImGui.SetNextWindowSize(p.InitSize * ImGuiHelper.Scale, ImGuiCond.Appearing);
                if (!p.NoClose)
                    beginval = ImGui.BeginPopupModal(p.Title, ref open, p.WindowFlags);
                else
                    beginval = ImGuiExt.BeginModalNoClose(p.Title, p.WindowFlags);
                if (beginval)
                {
                    p.Draw(openPopups[i].Appearing);
                    openPopups[i].Appearing = false;
                    ImGui.EndPopup();
                }
                else
                {
                    p.OnClosed();
                    openPopups.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
