// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer;
using LibreLancer.ImUI;

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
            public char Icon;
        }
        public static List<HpEntry> Fix = new List<HpEntry>();
        public static List<HpEntry> Rev = new List<HpEntry>();

        public static IEnumerable<HpEntry> All()
        {
            foreach (var e in Fix) yield return e;
            foreach (var e in Rev) yield return e;
        }

        static void AddFix(string name, HpNaming n, char icon)
        {
            Fix.Add(new HpEntry() { Name = name, Autoname = n, Icon = icon });
        }
        static void AddRev(string name, HpNaming n, char icon)
        {
            Rev.Add(new HpEntry() { Name = name, Autoname = n, Icon = icon });
        }

        static HardpointInformation()
        {
            AddFix("HpBayDoor01", HpNaming.None, Icons.Cube_Purple);
            AddFix("HpBayDoor02", HpNaming.None, Icons.Cube_Purple);
            AddFix("HpCM", HpNaming.Number, Icons.Cube_LightGreen);
            AddFix("HpCockpit", HpNaming.None, Icons.Cube_Purple);
            AddFix("HpContrail", HpNaming.Number, Icons.Cube_LightSkyBlue);
            AddFix("HpDockCam", HpNaming.Letter, Icons.Cube_LightYellow);
            AddFix("HpDockLight", HpNaming.Number, Icons.Cube_LightSkyBlue);
            AddFix("HpDockMount", HpNaming.Letter, Icons.Cube_LightYellow);
            AddFix("HpDockPoint", HpNaming.Letter, Icons.Cube_LightYellow);
            AddFix("HpEngine", HpNaming.Number, Icons.Cube_Purple);
            AddFix("HpFX", HpNaming.Number, Icons.Cube_LightSkyBlue);
            AddFix("HpHeadLight", HpNaming.Number, Icons.Cube_LightSkyBlue);
            AddFix("HpLaunchCam", HpNaming.Letter, Icons.Cube_LightYellow);
            AddFix("HpMine", HpNaming.Number, Icons.Cube_LightGreen);
            AddFix("HpMount", HpNaming.None, Icons.Cube_Purple);
            AddFix("HpPilot", HpNaming.None, Icons.Cube_Purple);
            AddFix("HpRunningLight", HpNaming.Number, Icons.Cube_LightSkyBlue);
            AddFix("HpShield", HpNaming.Number, Icons.Cube_LightGreen);
            AddFix("HpSpecialEquipment", HpNaming.Number, Icons.Cube_LightGreen);
            AddFix("HpThruster", HpNaming.Number, Icons.Cube_LightGreen);
            AddFix("HpTractor_Source", HpNaming.None, Icons.Cube_Purple);
            AddFix("HpFire", HpNaming.Number, Icons.Cube_Coral);
            AddFix("HpConnect", HpNaming.None, Icons.Cube_Coral);

            AddRev("HpTorpedo", HpNaming.Number, Icons.Rev_LightGreen);
            AddRev("HpTurret", HpNaming.Number, Icons.Rev_LightGreen);
            AddRev("HpWeapon", HpNaming.Number, Icons.Rev_LightGreen);
            AddRev("HpConnect", HpNaming.None, Icons.Rev_Coral);
        }
    }
}
