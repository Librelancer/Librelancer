using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImGuiNET;
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


    private string? text = null;
    private string? jsonText = null;
    private int seed = 4819;


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


    private float difficulty = 0;


    public override void Draw(double elapsed)
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
        ImGui.InputFloat("Difficulty", ref difficulty);
        ImGui.InputInt("Seed", ref seed);


        if (ImGuiExt.Button("Go", offerFaction != null && hostileFaction != null))
        {
            var random = new VC6Random(seed);

            var leaves = VignetteWalker.Enumerate(data.GameData.Items.VignetteTree,
                new(offerFaction!, hostileFaction!, difficulty, AllowedZoneType.Open));

            if (leaves.Count == 0)
            {
                text = "No missions generated for parameters.";
            }
            else
            {
                var selectedLeaf = random.Select(leaves, x => x.EndNode.Weight);
                var selectedPath = random.Select(selectedLeaf.Paths, x => (float)x.Probability);

                // Assume assassinate mission.
                random.Next(); //unknown
                random.Next(); //unknown

                var firstNameMale = random.NextInt(hostileFaction.Properties!.FirstNameMale.Value.Min,
                    hostileFaction.Properties.FirstNameMale.Value.Max);
                var lastName = random.NextInt(hostileFaction.Properties.LastName.Min,
                    hostileFaction.Properties.LastName.Max);

                var info = selectedPath.GetStrings(random);

                jsonText = JSON.Serialize(selectedPath.Decisions);

                Dictionary<string, object> items = new(StringComparer.OrdinalIgnoreCase);
                items["MISSION_DIFFICULTY"] = 5;
                items["REWARD_MONEY"] = 20;
                items["Offer_group"] = offerFaction;
                items["Hostile_group"] = hostileFaction;
                items["TARGET_ZONE"] =
                    data.GameData.Items.Systems.Get("li01")!.ZoneDict["Zone_Li01_Detroit_debris_001"];
                items["TARGET_FULL_NAME"] =
                    $"{data.GameData.GetString(firstNameMale)} {data.GameData.GetString(lastName)}";
                var sb = new StringBuilder();
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
                text = sb.ToString();
            }
        }

        if (!string.IsNullOrEmpty(text))
        {
            if (ImGui.Button("Copy Resolved Text"))
                win.SetClipboardText(text);
            ImGui.TextWrapped(text);
        }

        if (!string.IsNullOrWhiteSpace(jsonText))
        {
            if (ImGui.Button("Copy JSON"))
                win.SetClipboardText(jsonText);
            ImGui.Text(jsonText);
        }
    }
}
