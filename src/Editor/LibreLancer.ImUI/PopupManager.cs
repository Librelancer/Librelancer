// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using ImGuiNET;

namespace LibreLancer.ImUI
{
    public class PopupManager
    {
        private List<MessageBoxData> messageBoxes = new List<MessageBoxData>();

        private List<string> toOpens = new List<string>();
        private Dictionary<string, PopupContext> actionPopups = new Dictionary<string, PopupContext>();

        private List<PopupWindow> openPopups = new List<PopupWindow>();

        class PopupContext : PopupWindow
        {
            public override string Title { get; set; }
            public Action<PopupData> DrawAction;
            public PopupData Data;
            public ImGuiWindowFlags Flags;

            public override Vector2 InitSize => Size;

            public Vector2 Size;
            public override bool NoClose => Data.NoClose;

            public override ImGuiWindowFlags WindowFlags => Flags;
            public override void Draw()
            {
                DrawAction(Data);
                Data.First = false;
                Data.DoFocus = false;
            }
        }

        private int unique = 0;

        class MessageBoxData
        {
            public bool DoOpen = true;
            public string Title;
            public string Text;
            public bool Multiline;
        }

        public void OpenPopup<T>(T popup) where T : PopupWindow
        {
            toOpens.Add(popup.Title);
            openPopups.Add(popup);
        }

        public void AddPopup<T>(string title, Action<PopupData, T> action, ImGuiWindowFlags flags = 0)
        {
            actionPopups.Add(title,
                new PopupContext()
                {
                    Title = title,
                    DrawAction = d => action(d, (T) d.Arguments),
                    Flags = flags,
                    Data = new PopupData()
                }
            );
        }

        public void AddPopup(string title, Action<PopupData> action, ImGuiWindowFlags flags = 0, bool noClose = false, Vector2? initSize = null)
        {
            actionPopups.Add(title,
                new PopupContext()
                {
                    Title = title,
                    DrawAction = action,
                    Flags = flags,
                    Data = new PopupData() { NoClose =  noClose },
                    Size = initSize ?? Vector2.Zero,
                }
            );
        }

        public void OpenPopup(string title)
        {
            actionPopups[title].Data.First = true;
            actionPopups[title].Data.DoFocus = true;
            toOpens.Add(title);
            openPopups.Add(actionPopups[title]);
        }

        public void OpenPopup<T>(string title, T args)
        {
            actionPopups[title].Data.Arguments = args;
            actionPopups[title].Data.First = true;
            actionPopups[title].Data.DoFocus = true;
            toOpens.Add(title);
            openPopups.Add(actionPopups[title]);
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
            foreach(var o in toOpens)
                ImGui.OpenPopup(o);
            toOpens.Clear();
            for (int i = 0; i < openPopups.Count; i++)
            {
                var p = openPopups[i];
                bool open = true;
                bool beginval;
                if(p.InitSize != Vector2.Zero)
                    ImGui.SetNextWindowSize(p.InitSize * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
                if (!p.NoClose)
                    beginval = ImGui.BeginPopupModal(p.Title, ref open, p.WindowFlags);
                else
                    beginval = ImGuiExt.BeginModalNoClose(p.Title, p.WindowFlags);
                if (beginval)
                {
                    p.Draw();
                    ImGui.EndPopup();
                }
                else
                {
                    openPopups.RemoveAt(i);
                    i--;
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
