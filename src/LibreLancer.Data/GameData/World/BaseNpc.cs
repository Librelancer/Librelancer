// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.Data.Schema.MBases;

namespace LibreLancer.Data.GameData.World;

public class BaseNpc : NicknameItem
{
    public BaseNpc(string nickname)
    {
        Nickname = nickname;
    }

    public BaseNpcPlacement? Placement;
    public Costume? BaseAppr;
    public Bodypart? Body;
    public Bodypart? Head;
    public Bodypart? LeftHand;
    public Bodypart? RightHand;
    public List<Accessory> Accessories = [];
    public Accessory? Accessory => Accessories.Count > 0 ? Accessories[0] : null;
    public int IndividualName;
    public Faction? Affiliation;
    public string? Voice;

    public List<NpcKnow> Know = [];
    public List<BaseNpcRumor> Rumors = [];
    public List<BaseNpcBribe> Bribes = [];
    public NpcMission? Mission;
}

public record BaseNpcPlacement(string Spot, ResolvedThn FidgetScript, string Action)
{
    public override string ToString() => $"{Spot}, {(FidgetScript.SourcePath ?? "error")}, {Action}";
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
