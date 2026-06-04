// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;

namespace LibreLancer.Data.GameData.Market;

public class ShipPackage : IdentifiableItem
{
    public Ship Ship;
    public long BasePrice;
    public List<PackageAddon> Addons = [];

    public ShipPackage(string nickname, Ship ship)
    {
        Nickname = nickname;
        CRC = FLHash.CreateID(nickname);
        Ship = ship;
    }
}

public record PackageAddon(Items.Equipment Equipment, string? Hardpoint, int Amount);
