// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.Data.Schema.MBases;

namespace LibreLancer.Data.GameData.World;

public class BaseNpc
{
    public required string Nickname;
    public required string? BaseAppr;
    public required Bodypart? Body;
    public required Bodypart? Head;
    public required Bodypart? LeftHand;
    public required Bodypart? RightHand;
    public List<Accessory> Accessories = [];
    public Accessory? Accessory => Accessories.Count > 0 ? Accessories[0] : null;
    public required int IndividualName;
    public required Faction? Affiliation;
    public required string? Voice;
    public required string? Room;

    public List<NpcKnow> Know = [];
    public List<BaseNpcRumor> Rumors = [];
    public List<BaseNpcBribe> Bribes = [];
    public required NpcMission? Mission;
}

public class BaseNpcRumor : RepInfo
{
    public StoryIndex? Start;
    public StoryIndex? End;
    public int Ids;
    public bool Type2;
    public string[]? Objects;
}

public class BaseNpcBribe
{
    public Faction? Faction;
    public int Price;
    public int Ids;
}
