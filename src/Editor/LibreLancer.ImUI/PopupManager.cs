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
            public WindowFlags Flags;
        }
        public void AddPopup(string title, Action<PopupData> action, WindowFlags flags = 0)
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
                if(ImGui.BeginPopupModal(p.Title, p.Flags)) {
                   
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
