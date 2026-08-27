using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.GCS;
using LibreLancer.Data.Schema;
using LibreLancer.Utf.Dfm;
using LibreLancer.World;

namespace LibreLancer.Client;

public readonly record struct BaseNpcAppearance(
    Bodypart? Body,
    Bodypart? Head,
    Bodypart? LeftHand,
    Bodypart? RightHand,
    Accessory? Accessory)
{
    public bool HasBody => Body != null;
}

public readonly record struct BaseNpcAssignment(
    BaseNpc Npc,
    RoomNpcSpot Spot,
    ResolvedThn? FidgetScript,
    bool Fixed,
    BaseNpcRumor? SelectedRumor)
{
    public string SceneSpot => Spot.Nickname;
}

public static class BaseNpcPopulation
{
    public static string? GetServiceAction(BaseNpc npc)
    {
        var action = npc.Placement?.Action;
        if (action?.Equals("trader", StringComparison.OrdinalIgnoreCase) == true)
            return "trader";
        if (action?.Equals("Equipment", StringComparison.OrdinalIgnoreCase) == true)
            return "Equipment";
        if (action?.Equals("ShipDealer", StringComparison.OrdinalIgnoreCase) == true)
            return "ShipDealer";
        return null;
    }

    public static string GetInteractionCursor(BaseNpc npc, bool hasMissionOffer)
    {
        return GetInteractionCursor(
            npc,
            hasMissionOffer,
            npc.Rumors.FirstOrDefault(x => x.Ids != 0),
            GetBribe(npc));
    }

    public static BaseNpcBribe? GetBribe(BaseNpc npc)
    {
        return npc.Bribes.FirstOrDefault(x => x.Faction != null && x.Ids != 0);
    }

    public static string GetInteractionCursor(
        BaseNpc npc,
        bool hasMissionOffer,
        BaseNpcRumor? selectedRumor)
    {
        return GetInteractionCursor(
            npc,
            hasMissionOffer,
            selectedRumor,
            GetBribe(npc));
    }

    public static string GetInteractionCursor(
        BaseNpc npc,
        bool hasMissionOffer,
        BaseNpcRumor? selectedRumor,
        BaseNpcBribe? selectedBribe)
    {
        return GetInteractionCursor(
            npc,
            hasMissionOffer,
            selectedRumor,
            selectedBribe,
            float.MaxValue);
    }

    public static string GetInteractionCursor(
        BaseNpc npc,
        bool hasMissionOffer,
        BaseNpcRumor? selectedRumor,
        BaseNpcBribe? selectedBribe,
        float reputation)
    {
        switch (GetServiceAction(npc))
        {
            case "trader":
                return "talk_commodity_dealer";
            case "Equipment":
                return "talk_equip_dealer";
            case "ShipDealer":
                return "talk_ship_dealer";
        }

        if (hasMissionOffer && npc.Mission != null)
            return "talk_mission";
        if (selectedBribe != null)
            return "talk_bribe";
        if (npc.Know.Any(x =>
                x.Ids1 != 0 &&
                x.Ids2 != 0 &&
                reputation >= x.RepThreshold))
            return "talk_info";
        if (selectedRumor != null)
            return "talk_rumor";

        return "talk_blowoff";
    }

    public static Dictionary<string, BaseNpcBribe> SelectBribes(
        IReadOnlyList<BaseNpcAssignment> assignments,
        ReputationCollection playerReputations,
        Random random)
    {
        var candidates = new List<(BaseNpc Npc, BaseNpcBribe Bribe)>();
        foreach (var assignment in assignments)
        {
            var bribe = assignment.Npc.Bribes.FirstOrDefault(x =>
                BaseNpcRules.IsBribeAvailable(x, playerReputations));
            if (bribe != null)
                candidates.Add((assignment.Npc, bribe));
        }

        Shuffle(candidates, random);

        var selected = new Dictionary<string, BaseNpcBribe>(StringComparer.OrdinalIgnoreCase);
        var factions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            var faction = candidate.Bribe.Faction!;
            if (!factions.Add(faction.Nickname))
                continue;

            selected[candidate.Npc.Nickname] = candidate.Bribe;
        }

        return selected;
    }

    public static BaseNpcRumor? SelectRandomRumor(BaseNpc npc, Random random)
    {
        var rumors = npc.Rumors
            .Where(x => x.Ids != 0)
            .ToArray();
        return rumors.Length == 0 ? null : rumors[random.Next(rumors.Length)];
    }

    public static BaseNpcAppearance ResolveAppearance(BaseNpc npc)
    {
        return new BaseNpcAppearance(
            npc.Body ?? npc.BaseAppr?.Body,
            npc.Head ?? npc.BaseAppr?.Head,
            npc.LeftHand ?? npc.BaseAppr?.LeftHand,
            npc.RightHand ?? npc.BaseAppr?.RightHand,
            npc.Accessory ?? npc.BaseAppr?.Accessory);
    }

    public static BaseNpcAssignment[] Select(
        BaseRoom room,
        IReadOnlyList<RoomNpcSpot> spots,
        GameItemDb items,
        Random random)
    {
        return Select(
            room,
            spots,
            (gender, posture) => items.GetGCSScripts("fidget", gender, posture).FirstOrDefault(),
            random);
    }

    public static BaseNpcAssignment[] Select(
        BaseRoom room,
        IReadOnlyList<RoomNpcSpot> spots,
        Func<FLGender, Posture, ResolvedThn?> fidgetResolver,
        Random random)
    {
        var assignments = new List<BaseNpcAssignment>();
        var fixedNpcs = new Dictionary<string, BaseNpc>(StringComparer.OrdinalIgnoreCase);
        var usedSpots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var npc in room.Npcs)
        {
            if (npc.Placement == null)
                continue;

            fixedNpcs[npc.Placement.Spot] = npc;
            usedSpots.Add(npc.Placement.Spot);
        }

        foreach (var spot in spots)
        {
            if (!fixedNpcs.TryGetValue(spot.Nickname, out var npc) || npc.Placement == null)
                continue;

            assignments.Add(new BaseNpcAssignment(
                npc,
                spot,
                npc.Placement.FidgetScript,
                true,
                SelectRandomRumor(npc, random)));
        }

        var dynamicSpots = spots
            .Where(x => x.Dynamic && !usedSpots.Contains(x.Nickname))
            .ToArray();
        var ambientNpcs = room.Npcs
            .Where(x => x.Placement == null)
            .ToArray();

        Shuffle(dynamicSpots, random);
        Shuffle(ambientNpcs, random);

        var maxCount = Math.Min(Math.Max(room.MaxCharacters, 0),
            Math.Min(dynamicSpots.Length, ambientNpcs.Length));
        // character_density is the maximum number of ambient NPCs in the
        // room. Fixtures remain fixed, while the ambient part may leave
        // dynamic spots empty on each room entry.
        var count = maxCount == 0 ? 0 : random.Next(maxCount + 1);

        for (var i = 0; i < count; i++)
        {
            var npc = ambientNpcs[i];
            var spot = dynamicSpots[i];
            var appearance = ResolveAppearance(npc);
            var gender = appearance.Body?.Sex ?? FLGender.male;
            var fidget = fidgetResolver(gender, spot.Posture);

            assignments.Add(new BaseNpcAssignment(
                npc,
                spot,
                fidget,
                false,
                SelectRandomRumor(npc, random)));
        }

        return assignments.ToArray();
    }

    private static void Shuffle<T>(T[] values, Random random)
    {
        for (var i = values.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    private static void Shuffle<T>(List<T> values, Random random)
    {
        for (var i = values.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }
}
