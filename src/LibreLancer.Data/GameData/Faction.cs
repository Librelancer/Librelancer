using System;
using System.Collections.Generic;
using LibreLancer.Data.Schema.Missions;

namespace LibreLancer.Data.GameData;

public class Faction : NamedItem
{
    public bool Hidden; //Hidden from the player status list
    public int IdsShortName;

    public float ObjectDestroyRepChange;
    public float MissionSucceedRepChange;
    public float MissionFailRepChange;
    public float MissionAbortRepChange;

    public Empathy[] FactionEmpathy = [];

    public required Schema.Missions.FactionProps? Properties;
    public readonly Dictionary<Faction, float> Reputations = new();

    public const float FriendlyThreshold = 0.6f;
    public const float HostileThreshold = -0.6f;

    public override string? ToString() => $"Faction: {Nickname}";
    public float GetReputation(Faction f) => Reputations.GetValueOrDefault(f, 0.0f);

    public List<Voice> NpcVoices = [];
    public List<ShipArch> NpcShips = [];

    public Dictionary<string, List<ShipArch>> ShipsByClass = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, FormationDef> Formations = new(StringComparer.OrdinalIgnoreCase);
}
