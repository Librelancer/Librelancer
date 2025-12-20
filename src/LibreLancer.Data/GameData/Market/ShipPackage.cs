// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;

namespace LibreLancer.Data.GameData.Market
{
    public class ShipPackage
    {
        public string Nickname;
        public uint CRC;
        public long BasePrice;
        public string Ship;
        public List<PackageAddon> Addons = new List<PackageAddon>();
    }

    public class PackageAddon
    {
        public Items.Equipment Equipment;
        public string Hardpoint;
        public int Amount;
    }
}
