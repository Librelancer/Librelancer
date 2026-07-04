using System;
using System.Collections.Generic;
using System.Text;
using ImGuiNET;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Data.Schema.RandomMissions;
using LibreLancer.ImUI;
using LibreLancer.Interface;
using LibreLancer.Server;


namespace LancerEdit.GameContent;

public class VignetteTester : GameContentTab
{
    private GameDataContext data;
    private MainWindow win;

    private Faction? offerFaction = null;
    private Faction? hostileFaction = null;


    private string? text = null;
    private string? jsonText = null;

    private VignetteParameters parameters = new() { AssassinateMission = true };



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
        ImGui.Checkbox("DefensiveSolarsAtMainBattle", ref parameters.DefensiveSolarsAtMainBattle);
        ImGui.Checkbox("FriendlyShips", ref parameters.FriendlyShips);
        parameters.FriendlyShipsAfter30S = parameters.FriendlyShips;
        if (ImGuiExt.Button("Go", offerFaction != null && hostileFaction != null))
        {
            parameters.Seed = new Random().Next();
            parameters.OfferGroup = offerFaction!.Nickname;
            var info = VignetteBuilder.Run(data.GameData.Items.VignetteTree, parameters);

            if (info.IsError)
            {
                text = $"Error: {info.ErrorReason}";
                jsonText = null;
                return;
            }
            jsonText = JSON.Serialize(info);

            Dictionary<string, object> items = new(StringComparer.OrdinalIgnoreCase);
            items["MISSION_DIFFICULTY"] = 5;
            items["REWARD_MONEY"] = 20;
            items["Offer_group"] = offerFaction;
            items["Hostile_group"] = hostileFaction;
            items["TARGET_ZONE"] = data.GameData.Items.Systems.Get("li01")!.ZoneDict["Zone_Li01_Detroit_debris_001"];
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
                    if(items[offerText.Args[0]]
                           is Faction t && t.Properties?.NicknamePlurality == NicknamePlurality.Singular)
                        sb.Append(Format(offerText.Ids, offerText.Args, items));
                }
                else if (offerText.Type == OfferTextType.plural)
                {
                    if(items[offerText.Args[0]]
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

        if (!string.IsNullOrEmpty(text))
        {
            if(ImGui.Button("Copy Resolved Text"))
                win.SetClipboardText(text);
            ImGui.TextWrapped(text);
        }

        if (!string.IsNullOrWhiteSpace(jsonText))
        {
            if(ImGui.Button("Copy JSON"))
                win.SetClipboardText(jsonText);
            ImGui.Text(jsonText);
        }
    }
}
