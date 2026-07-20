require 'childwindow.lua'

local preview_names = {
    "destroy",
    "destroyinstallation",
    "destroycontraband",
    "bountyoffer",
    "assassinate",
    "retrievemissingitem",
}

local mission_icon_paths = {
    DestroyMission = "INTERFACE/NEURONET/MISSIONVENDOR/mv_missiondestroy.3db",
    DestroyInstallationMission = "INTERFACE/NEURONET/MISSIONVENDOR/mv_missiondestroyinstall.3db",
    DestroyContrabandMission = "INTERFACE/NEURONET/MISSIONVENDOR/mv_missiondestroycontra.3db",
    AssassinationMission = "INTERFACE/NEURONET/MISSIONVENDOR/mv_missionassassinate.3db",
    AssassinateMission = "INTERFACE/NEURONET/MISSIONVENDOR/mv_missionassassinate.3db",
    BountyMission = "INTERFACE/NEURONET/MISSIONVENDOR/mv_missionbounty.3db",
    RetrieveMission = "INTERFACE/NEURONET/MISSIONVENDOR/mv_missionretrieve.3db",
}

local mission_preview_textures = {
    DestroyMission = "destroy",
    DestroyInstallationMission = "destroyinstallation",
    DestroyContrabandMission = "destroycontraband",
    AssassinationMission = "assassinate",
    AssassinateMission = "assassinate",
    BountyMission = "bountyoffer",
    RetrieveMission = "retrievemissingitem",
}

local function mission_type_key(missionType)
{
    if (missionType == nil)
        return "";
    return string.lower(missionType);
}

local function mission_icon_path(missionType)
{
    local exact = mission_icon_paths[missionType];
    if (exact != nil)
        return exact;

    local key = mission_type_key(missionType);
    if (string.find(key, "assassin") != nil)
        return mission_icon_paths.AssassinateMission;
    if (string.find(key, "bounty") != nil)
        return mission_icon_paths.BountyMission;
    if (string.find(key, "retrieve") != nil || string.find(key, "missing") != nil)
        return mission_icon_paths.RetrieveMission;
    if (string.find(key, "contraband") != nil)
        return mission_icon_paths.DestroyContrabandMission;
    if (string.find(key, "installation") != nil || string.find(key, "solar") != nil)
        return mission_icon_paths.DestroyInstallationMission;
    if (string.find(key, "destroy") != nil)
        return mission_icon_paths.DestroyMission;
    return mission_icon_paths.DestroyMission;
}

local function mission_preview_name(missionType)
{
    local exact = mission_preview_textures[missionType];
    if (exact != nil)
        return exact;

    local key = mission_type_key(missionType);
    if (string.find(key, "assassin") != nil)
        return "assassinate";
    if (string.find(key, "bounty") != nil)
        return "bountyoffer";
    if (string.find(key, "retrieve") != nil || string.find(key, "missing") != nil)
        return "retrievemissingitem";
    if (string.find(key, "contraband") != nil)
        return "destroycontraband";
    if (string.find(key, "installation") != nil || string.find(key, "solar") != nil)
        return "destroyinstallation";
    if (string.find(key, "destroy") != nil)
        return "destroy";
    return "destroy";
}

local function format_reward(offer)
{
    local sign = StringFromID(STRID_CREDIT_SIGN)
    return offer.Reward > 0
        ? sign + NumberToStringCS(offer.Reward, "N0")
        : sign + "---"
}

local function format_system_name(offer)
{
    local name = StringFromID(offer.SystemIdsName)
    if (name == nil || name == "")
        return "Unknown System";
    if (string.sub(name, -7) == " System")
        return name;
    return name + " System";
}

local function format_faction_name(offer)
{
    local name = StringFromID(offer.FactionIdsName)
    return (name == nil || name == "") ? "Unknown Faction" : name;
}

local function control_icon_3db(path, control)
{
    control.Background = NewObject("UiRenderable")
    local colorElem = NewObject("DisplayColor")
    colorElem.Color = GetColor("black")
    control.Background.AddElement(colorElem)
    if (path == nil)
        return;
    local elem = NewObject("DisplayModel")
    elem.Tint = GetColor("text")
    local model = NewObject("InterfaceModel")
    model.Path = path
    model.XScale = 32
    model.YScale = 34
    elem.Model = model
    control.Background.AddElement(elem)
}

local function set_preview(e, preview)
{
    for (name in preview_names)
        e["preview_" + name].Visible = false;
    if (preview != nil)
        e["preview_" + preview].Visible = true;
}

local function mission_list_item(offer)
{
    local li = NewObject("ListItem")
    li.Border = NewObject("UiRenderable")
    local wire = NewObject("DisplayWireBorder")
    wire.Color = GetColor("text")
    li.Border.AddElement(wire)
    li.HoverBorder = NewObject("UiRenderable")
    wire = NewObject("DisplayWireBorder")
    wire.Color = GetColor("slow_blue_yellow")
    li.HoverBorder.AddElement(wire)
    li.SelectedBorder = NewObject("UiRenderable")
    wire = NewObject("DisplayWireBorder")
    wire.Color = GetColor("yellow")
    li.SelectedBorder.AddElement(wire)
    li.ItemMarginX = 10
    li.ItemA = NewObject("Panel")
    li.ItemA.Width = 26
    control_icon_3db(mission_icon_path(offer.MissionType), li.ItemA)
    li.ItemB = NewObject("Panel")
    local systemText = NewObject("TextBlock")
    systemText.Text = format_system_name(offer)
    systemText.TextColor = GetColor("text")
    systemText.TextShadow = GetColor("black")
    systemText.TextSize = 10
    systemText.HorizontalAlignment = HorizontalAlignment.Left
    systemText.X = 3
    systemText.Y = 2
    systemText.Width = 155
    systemText.Height = 14
    li.ItemB.Children.Add(systemText)
    local factionText = NewObject("TextBlock")
    factionText.Text = format_faction_name(offer)
    factionText.TextColor = GetColor("text")
    factionText.TextShadow = GetColor("black")
    factionText.TextSize = 10
    factionText.HorizontalAlignment = HorizontalAlignment.Left
    factionText.X = 3
    factionText.Y = 16
    factionText.Width = 155
    factionText.Height = 14
    li.ItemB.Children.Add(factionText)
    local rewardText = NewObject("TextBlock")
    rewardText.Text = format_reward(offer)
    rewardText.TextColor = GetColor("text")
    rewardText.TextShadow = GetColor("black")
    rewardText.TextSize = 10
    rewardText.HorizontalAlignment = HorizontalAlignment.Right
    rewardText.X = 140
    rewardText.Y = 9
    rewardText.Width = 90
    rewardText.Height = 14
    li.ItemB.Children.Add(rewardText)
    return li
}

class jobboard : jobboard_Designer with ChildWindow
{
    jobboard()
    {
        base();
        this.ChildWindowInit();
        local e = this.Elements;
        e.close.OnClick(() => this.Close());
        e.accept.OnClick(() => {
            if (this.SelectedOffer != nil)
                Game.AcceptMissionOffer(this.SelectedOffer.Id);
            this.Close();
        });
        this.Offers = Game.GetMissionOffers();
        for (offer in this.Offers) {
            e.mission_list.Children.Add(mission_list_item(offer));
        }
        if (this.Offers.length > 0) {
            e.mission_list.SelectedIndex = 0;
            this.SetMission(this.Offers[1]);
        }
        e.mission_list.OnSelectedIndexChanged(() => this.SetMission(this.Offers[e.mission_list.SelectedIndex + 1]));
    }

    SetMission(offer)
    {
        if (offer == nil) return;
        this.SelectedOffer = offer;
        local e = this.Elements;
        e.detail_system.Text = format_system_name(offer);
        e.detail_faction.Text = format_faction_name(offer);
        e.detail_reward.Text = format_reward(offer);
        control_icon_3db(mission_icon_path(offer.MissionType), e.detail_icon);
        set_preview(e, mission_preview_name(offer.MissionType));
        e.detail_description.SetString(offer.OfferText, "$ListText", 22);
    }
}
