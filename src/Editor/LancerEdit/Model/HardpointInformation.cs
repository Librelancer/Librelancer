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
namespace LancerEdit
{
    public enum HpNaming
    {
        None,
        Number,
        Letter
    }
    public static class HardpointInformation
    {
        public class HpEntry {
            public string Name;
            public HpNaming Autoname;
            public string Icon;
            public Color4 Color;
        }
        public static List<HpEntry> Fix = new List<HpEntry>();
        public static List<HpEntry> Rev = new List<HpEntry>();
        static void AddFix(string name, HpNaming n, string icon, Color4 c)
        {
            Fix.Add(new HpEntry() { Name = name, Autoname = n, Icon = icon, Color = c });
        }
        static void AddRev(string name, HpNaming n, string icon, Color4 c)
        {
            Rev.Add(new HpEntry() { Name = name, Autoname = n, Icon = icon, Color = c });
        }

        static HardpointInformation()
        {
            AddFix("HpBayDoor01", HpNaming.None, "fix", Color4.Purple);
            AddFix("HpBayDoor02", HpNaming.None, "fix", Color4.Purple);
            AddFix("HpCM", HpNaming.Number, "fix", Color4.LightGreen);
            AddFix("HpCockpit", HpNaming.None, "fix", Color4.Purple);
            AddFix("HpContrail", HpNaming.Number, "fix", Color4.LightSkyBlue);
            AddFix("HpDockCam", HpNaming.Letter, "fix",Color4.Yellow);
            AddFix("HpDockLight", HpNaming.Number, "fix", Color4.LightSkyBlue);
            AddFix("HpDockMount", HpNaming.Letter, "fix", Color4.Yellow);
            AddFix("HpDockPoint", HpNaming.Letter, "fix", Color4.Yellow);
            AddFix("HpEngine", HpNaming.Number, "fix", Color4.Purple);
            AddFix("HpFX", HpNaming.Number, "fix", Color4.LightSkyBlue);
            AddFix("HpHeadLight", HpNaming.Number, "fix", Color4.LightSkyBlue);
            AddFix("HpLaunchCam", HpNaming.Letter, "fix", Color4.Yellow);
            AddFix("HpMine", HpNaming.Number, "fix", Color4.LightGreen);
            AddFix("HpMount", HpNaming.None, "fix", Color4.Purple);
            AddFix("HpPilot", HpNaming.None, "fix", Color4.Purple);
            AddFix("HpRunningLight", HpNaming.Number, "fix", Color4.LightSkyBlue);
            AddFix("HpShield", HpNaming.Number, "fix", Color4.LightGreen);
            AddFix("HpSpecialEquipment", HpNaming.Number, "fix", Color4.LightGreen);
            AddFix("HpThruster", HpNaming.Number, "fix", Color4.LightGreen);
            AddFix("HpTractor_Source", HpNaming.None, "fix", Color4.Purple);
            AddFix("HpFire", HpNaming.Number, "fix", Color4.Coral);

            AddRev("HpTorpedo", HpNaming.Number, "rev", Color4.LightGreen);
            AddRev("HpTurret", HpNaming.Number, "rev", Color4.LightGreen);
            AddRev("HpWeapon", HpNaming.Number, "rev", Color4.LightGreen);
            AddRev("HpConnect", HpNaming.None, "rev", Color4.Coral);
        }
    }
}
