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
        List<PopupContext> popups = new List<PopupContext>();
        private List<MessageBoxData> messageBoxes = new List<MessageBoxData>();

        class PopupContext
        {
            public bool DoOpen = false;
            public Action<PopupData> DrawAction;
            public PopupData Data;
            public string Title;
            public ImGuiWindowFlags Flags;
        }

        private int unique = 0;

        class MessageBoxData
        {
            public bool DoOpen = true;
            public string Title;
            public string Text;
            public bool Multiline;
        }

        public void AddPopup<T>(string title, Action<PopupData, T> action, ImGuiWindowFlags flags = 0)
        {
            popups.Add(
                new PopupContext()
                {
                    Title = title,
                    DrawAction = d => action(d, (T) d.Arguments),
                    Flags = flags,
                    Data = new PopupData()
                }
            );
        }

        public void AddPopup(string title, Action<PopupData> action, ImGuiWindowFlags flags = 0)
        {
            popups.Add(
                new PopupContext()
                {
                    Title = title,
                    DrawAction = action,
                    Flags = flags,
                    Data = new PopupData()
                }
            );
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

        public void OpenPopup<T>(string title, T args)
        {
            foreach (var p in popups)
            {
                if (p.Title == title)
                {
                    p.DoOpen = true;
                    p.Data.Arguments = args;
                    break;
                }
            }
        }

        public void MessageBox(string title, string message, bool selectable = true)
        {
            var msg = new MessageBoxData();
            msg.Title = ImGuiExt.IDWithExtra(title, "msgbox" + unique++);
            msg.Text = message;
            msg.Multiline = selectable;
            messageBoxes.Add(msg);
        }

        public void Run()
        {
            foreach (var p in popups)
            {
                if (p.DoOpen)
                {
                    p.Data.DoFocus = p.Data.First = true;
                    ImGui.OpenPopup(p.Title);
                    p.DoOpen = false;
                }

                bool open = true;
                bool beginval;
                if (!p.Data.NoClose)
                    beginval = ImGui.BeginPopupModal(p.Title, ref open, p.Flags);
                else
                    beginval = ImGuiExt.BeginModalNoClose(p.Title, p.Flags);
                if (beginval)
                {
                    p.DrawAction(p.Data);
                    if (p.Data.First)
                        p.Data.First = false;
                    else
                        p.Data.DoFocus = false;
                    ImGui.EndPopup();
                }
            }

            for (int i = 0; i < messageBoxes.Count; i++)
            {
                var p = messageBoxes[i];
                if (p.DoOpen)
                {
                    ImGui.OpenPopup(p.Title);
                    p.DoOpen = false;
                }

                bool open = true;
                if (ImGui.BeginPopupModal(p.Title, ref open, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    if (p.Multiline)
                    {
                        ImGui.InputTextMultiline(
                            "##label",
                            ref p.Text,
                            UInt32.MaxValue,
                            new Vector2(350, 150) * ImGuiHelper.Scale,
                            ImGuiInputTextFlags.ReadOnly
                        );
                    }
                    else
                    {
                        ImGui.Text(p.Text);
                    }

                    if (ImGui.Button("Ok")) open = false;
                    ImGui.EndPopup();
                }

                if (!open)
                {
                    messageBoxes.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    public class PopupData
    {
        public bool DoFocus;
        public bool First;
        public bool NoClose;
        public object Arguments;
    }
}