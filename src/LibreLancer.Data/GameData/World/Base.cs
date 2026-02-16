// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData.Market;
using LibreLancer.Data.Schema.MBases;

namespace LibreLancer.Data.GameData.World;

public class Base : NamedItem
{
    //Populated from universe
    public string? System = null;
    public string? TerrainTiny;
    public string? TerrainSml;
    public string? TerrainMdm;
    public string? TerrainLrg;
    public string? TerrainDyna1;
    public string? TerrainDyna2;
    public string? BaseRunBy = null!;
    public bool AutosaveForbidden;

    //Populated from base ini
    public string SourceFile = null!;
    public BaseRoom StartRoom = null!;
    public GameItemCollection<BaseRoom> Rooms = [];

    //Populated from markets
    public List<SoldShip> SoldShips = [];
    public List<BaseSoldGood> SoldGoods = [];

    //Populated from mbases
    public Faction? LocalFaction;
    public int Diff;
    public string? MsgIdPrefix;
    public int MinMissionOffers;
    public int MaxMissionOffers; //not respected by vanilla (?)
    public List<BaseNpc> Npcs = [];

    public ulong GetUnitPrice(Items.Equipment eq)
    {
        var g = SoldGoods.FirstOrDefault(x => x.Good.Equipment.Nickname.Equals(eq.Nickname, StringComparison.OrdinalIgnoreCase));
        return g.Good is null
            ? (ulong) (eq.Good?.Ini?.Price ?? 0)
            : g.Price;
    }
}

public class BaseNpc
{
    public required string Nickname;
    public required string? BaseAppr;
    public required string? Body;
    public required string? Head;
    public required string? LeftHand;
    public required string? RightHand;
    public required string? Accessory;
    public required int IndividualName;
    public required Faction? Affiliation;
    public required string? Voice;
    public required string? Room;

    public List<NpcKnow> Know = [];
    public List<NpcRumor> Rumors = [];
    public List<NpcBribe> Bribes = [];
    public required NpcMission? Mission;
}
