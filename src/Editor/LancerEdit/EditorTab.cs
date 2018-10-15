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
using LibreLancer.ImUI;
namespace LancerEdit
{
    public enum Hotkeys
    {
        Deselect,
        ResetViewport
    }
    public abstract class EditorTab : DockTab
    {
        public virtual void DetectResources(List<MissingReference> missing, List<uint> matrefs, List<string> texrefs)
        {
        }
        public virtual void SetActiveTab(MainWindow win)
        {
            win.ActiveTab = null;
        }
        public virtual void OnHotkey(Hotkeys hk) {}
    }
}
