using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.RandomMissions;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Data.Schema.RandomMissions;
using LibreLancer.ImUI;
using LibreLancer.Interface;
using LibreLancer.Server.RandomMissions;


namespace LancerEdit.GameContent;

public class VignetteTester : GameContentTab
{
    private GameDataContext data;
    private MainWindow win;

    private Faction? offerFaction = null;
    private Faction? hostileFaction = null;

    private int seed = 9827;


    public VignetteTester(GameDataContext data, MainWindow win)
    {
        this.data = data;
        this.win = win;
        Title = "Vignette Tester";
    }

    string Format(int ids, string[] args, Dictionary<string, object> items)
    {
        List<IdsFormatItem> resolvedArgs = new();
        int indexS = 0;
        int indexD = 0;
        int indexF = 0;
        int indexZ = 0;
        int indexI = 0;
        foreach (var a in args)
        {
            if (!items.TryGetValue(a, out var value))
                value = "";
            switch (value)
            {
                //todo: add case for 'I' (used in OTHER_SOLAR, not sure what sort of object)
                case string s:
                    resolvedArgs.Add(new('s', indexS++, s));
                    break;
                case int d:
                    resolvedArgs.Add(new('d', indexD++, d.ToString()));
                    break;
                case Faction f:
                    resolvedArgs.Add(new('F', indexF++, f.IdsName));
                    break;
                case Zone z:
                    resolvedArgs.Add(new('Z', indexZ++, z.IdsName));
                    break;
            }
        }

        return IdsFormatting.Format(data.GameData.GetString(ids),
            data.GameData.Items.Ini.Infocards, resolvedArgs.ToArray());
    }


    private float difficulty = 0.0810805f;

    private string path = "";
    private AllowedZoneType zoneType = AllowedZoneType.Open;


    private static char[] pathChars = ['t', 'T', 'f', 'F'];
    static char? PathFilter(char ch) => pathChars.Contains(ch) ? ch : null;

    public override unsafe void Draw(double elapsed)
    {
        ImGui.Text("Vignette Tester");
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Offer Group: ");
        ImGui.SameLine();
        data.Factions.Draw("##offergroup", ref offerFaction);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Hostile Group: ");
        ImGui.SameLine();
        data.Factions.Draw("##hostilegroup", ref hostileFaction);
        ImGui.InputFloat("Difficulty", ref difficulty, 0, 0, "%.7f");
        ImGui.InputInt("Seed", ref seed);
        if (ImGui.BeginCombo("ZoneType", zoneType.ToString()))
        {
            if (ImGui.Selectable("Open", zoneType == AllowedZoneType.Open))
                zoneType = AllowedZoneType.Open;
            if (ImGui.Selectable("Field", zoneType == AllowedZoneType.Field))
                zoneType = AllowedZoneType.Field;
            if (ImGui.Selectable("Exclusion", zoneType == AllowedZoneType.Exclusion))
                zoneType = AllowedZoneType.Exclusion;
            ImGui.EndCombo();
        }

        if (ImGuiExt.Button("Generate Mission", offerFaction != null && hostileFaction != null))
        {
            var random = new VC6Random(seed);

            var vignetteParameters = new VignetteGraphParameters(offerFaction!, hostileFaction!, difficulty, zoneType);
            var leaves = VignetteWalker.Enumerate(data.GameData.Items.VignetteTree,vignetteParameters);

            var sb = new StringBuilder();
            if (leaves.Count == 0)
            {
                win.Popups.MessageBox("Error", "No possible missions for config.");
            }
            else
            {
                sb.AppendLine($"Generation for: {vignetteParameters}");
                foreach (var l in leaves)
                {
                    sb.AppendLine($"leaf: {l.EndNode.Id} {l.EndNode.Weight}");
                }

                var selectedLeaf = random.Select(leaves, x => x.EndNode.Weight);
                sb.AppendLine($"selected leaf: {selectedLeaf.EndNode.Id}");
                foreach (var path in selectedLeaf.Paths)
                {
                    sb.Append($"P: {path.Probability:F7}, ");
                    sb.AppendLine(string.Join("", path.Branches.Select(x => x ? 'T' : 'F')));
                }

                var selectedPath = random.Select(selectedLeaf.Paths, x => (float)x.Probability);
                sb.AppendLine();
                sb.AppendLine($"selected path: {string.Join("", selectedPath.Branches.Select(x => x ? 'T' : 'F'))}");
                // Assume assassinate mission.
                random.Next(); //unknown
                random.Next(); //unknown

                var firstNameMale = random.NextInt(hostileFaction.Properties!.FirstNameMale.Value.Min,
                    hostileFaction.Properties.FirstNameMale.Value.Max);
                var lastName = random.NextInt(hostileFaction.Properties.LastName.Min,
                    hostileFaction.Properties.LastName.Max);

                var info = selectedPath.GetStrings(data.GameData.Items.VignetteTree, random);

                var jsonText = JSON.Serialize(selectedPath.Decisions);

                Dictionary<string, object> items = new(StringComparer.OrdinalIgnoreCase);
                items["MISSION_DIFFICULTY"] = 5;
                items["REWARD_MONEY"] = 20;
                items["Offer_group"] = offerFaction;
                items["Hostile_group"] = hostileFaction;
                items["TARGET_ZONE"] =
                    data.GameData.Items.Systems.Get("li01")!.ZoneDict["Zone_Li01_Detroit_debris_001"];
                items["TARGET_FULL_NAME"] =
                    $"{data.GameData.GetString(firstNameMale)} {data.GameData.GetString(lastName)}";
                sb.AppendLine("Reward Text:");
                sb.AppendLine(Format(info.RewardText.Ids, info.RewardText.Arguments, items));
                sb.AppendLine("Failure Text:");
                sb.AppendLine(Format(info.FailureText.Ids, info.FailureText.Arguments, items));
                sb.AppendLine("Offer Text:");
                foreach (var offerText in info.OfferText)
                {
                    if (offerText.Type == OfferTextType.singular)
                    {
                        if (items[offerText.Args[0]]
                                is Faction t && t.Properties?.NicknamePlurality == NicknamePlurality.Singular)
                            sb.Append(Format(offerText.Ids, offerText.Args, items));
                    }
                    else if (offerText.Type == OfferTextType.plural)
                    {
                        if (items[offerText.Args[0]]
                                is Faction t && t.Properties?.NicknamePlurality == NicknamePlurality.Plural)
                            sb.Append(Format(offerText.Ids, offerText.Args, items));
                    }
                    else
                    {
                        sb.Append(Format(offerText.Ids, offerText.Args, items));
                    }
                }

                sb.AppendLine();
                sb.AppendLine("decisions:");
                sb.Append(jsonText);
                win.TextWindows.Add(new TextDisplayWindow(sb.ToString(), "vignette.txt", win));
            }
        }

        ImGui.Separator();

        Controls.InputTextFilter("Path (T/F)", ref path, PathFilter);

        if (ImGui.Button("Walk Path"))
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Path Walked: {path.ToUpper()}");
            var bools = path.Select(x => x == 't' || x == 'T').ToArray();
            VignetteTreeNode tN = data.GameData.Items.VignetteTree.StartNode;
            sb.AppendLine(tN.ToString());
            foreach (var b in bools)
            {
                tN = b ? tN.Left : tN.Right;
                sb.AppendLine($"{(b ? 'T' : 'F')} -> {tN}");
            }
            win.TextWindows.Add(new TextDisplayWindow(sb.ToString(), "vignette-path.txt", win));
        }
    }
}
