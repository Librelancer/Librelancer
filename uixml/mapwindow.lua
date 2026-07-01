require 'childwindow.lua'

local function _Lerp(x, y, t) {
    if (t >= 1) return y;
    if (t <= 0) return x;
    return x + (y - x) * t;
}

local function waypoint_list_item(text)
{
    local li = NewObject("ListItem");
    li.Border = NewObject("UiRenderable");
    local wire = NewObject("DisplayWireBorder");
    wire.Color = GetColor("text");
    wire.Width = 1;
    li.Border.AddElement(wire);

    li.ItemA = NewObject("Panel");
    local tb = NewObject("TextBlock");
    tb.HorizontalAlignment = HorizontalAlignment.Left;
    tb.VerticalAlignment = VerticalAlignment.Top;
    tb.TextSize = 8;
    tb.Font = "$ListText";
    tb.TextColor = GetColor("text");
    tb.TextShadow = GetColor("black");
    tb.Wrap = true;
    tb.X = 4;
    tb.Y = 4;
    tb.Width = 119;
    tb.Height = 50;
    tb.Text = text;
    li.ItemA.Children.Add(tb);
    return li;
}

class mapwindow : mapwindow_Designer with ChildWindow
{
    mapwindow()
    {
        base();
        this.ChildWindowInit();
        this.waypointPanelCount = -1;
        this.sidePanelAmount = 0;
        this.sidePanelStartAmount = 0;
        this.sidePanelTargetAmount = 0;
        this.sidePanelTime = 0;
        this.sidePanelDuration = 0.32;
        this.leftPanelOpenX = this.Elements.leftpanel.X;
        this.rightPanelOpenX = this.Elements.rightpanel.X;
        this.leftPanelClosedX = this.leftPanelOpenX + this.Elements.leftpanel.Width;
        this.rightPanelClosedX = this.rightPanelOpenX - this.Elements.rightpanel.Width;
        this.Elements.leftpanel.X = this.leftPanelClosedX;
        this.Elements.rightpanel.X = this.rightPanelClosedX;
        this.OnChildOpen = () => {
            this.ResetNavmap();
            this.CloseWaypointPanelsImmediate();
        };
        this.Widget.OnUpdate((delta) => {
            this.UpdateWaypointPanels();
            this.UpdateSidePanels(delta);
            this.UpdateTopButtons();
        });
        this.Elements.exit.OnClick(() => this.Close());
        this.Elements.universebutton.OnClick(() => this.Elements.navmap.ShowSectorView());
        this.Elements.playersystem.OnClick(() => this.Elements.navmap.ShowPlayerSystem());
        this.Elements.clear_waypoints.OnClick(() => {
            Game.ClearUserWaypoints();
            this.waypointPanelCount = -1;
            this.UpdateWaypointPanels();
        });
    }
    InitMap()
    {
        this.ResetNavmap();
        Game.PopulateNavmap(this.Elements.navmap);
        this.UpdateWaypointPanels();
    }
    Closing()
    {
        this.ResetNavmap();
        this.CloseWaypointPanelsImmediate();
    }
    ResetNavmap()
    {
        this.Elements.navmap.ResetView();
        this.UpdateTopButtons();
    }
    UpdateTopButtons()
    {
        local sector = this.Elements.navmap.SectorViewActive;
        this.Elements.universebutton.Visible = !sector;
        this.Elements.labels.Visible = !sector;
        this.Elements.physical.Visible = !sector;
        this.Elements.political.Visible = !sector;
        this.Elements.patrol.Visible = !sector;
        this.Elements.miningfilter.Visible = !sector;
        this.Elements.legendtoggle.Visible = !sector;
        this.Elements.knownbases.Visible = !sector;
        this.Elements.playersystem.Visible = sector;
    }
    UpdateWaypointPanels()
    {
        if (this.AnimatingOut) return;
        if (this.AnimatingIn) {
            this.CloseWaypointPanelsImmediate();
            return;
        }

        local count = Game.UserWaypointCount();
        local visible = count > 0;
        if (visible) this.OpenWaypointPanels();
        else this.CloseWaypointPanels();
        if (count == this.waypointPanelCount) return;

        this.waypointPanelCount = count;
        this.Elements.waypoint_list.Children.Clear();
        if (count <= 0) return;

        for (i in 1..count) {
            this.Elements.waypoint_list.Children.Add(waypoint_list_item(Game.UserWaypointPanelText(i - 1)));
        }
    }
    OpenWaypointPanels()
    {
        if (this.sidePanelTargetAmount == 1) return;
        this.Elements.leftpanel.Visible = true;
        this.Elements.rightpanel.Visible = true;
        this.sidePanelStartAmount = this.sidePanelAmount;
        this.sidePanelTargetAmount = 1;
        this.sidePanelTime = 0;
    }
    CloseWaypointPanels()
    {
        if (this.sidePanelTargetAmount == 0) return;
        this.sidePanelStartAmount = this.sidePanelAmount;
        this.sidePanelTargetAmount = 0;
        this.sidePanelTime = 0;
    }
    CloseWaypointPanelsImmediate()
    {
        this.sidePanelAmount = 0;
        this.sidePanelStartAmount = 0;
        this.sidePanelTargetAmount = 0;
        this.sidePanelTime = 0;
        this.Elements.leftpanel.X = this.leftPanelClosedX;
        this.Elements.rightpanel.X = this.rightPanelClosedX;
        this.Elements.leftpanel.Visible = false;
        this.Elements.rightpanel.Visible = false;
    }
    UpdateSidePanels(delta)
    {
        if (!this.Elements.leftpanel.Visible && this.sidePanelTargetAmount == 0) return;

        this.sidePanelTime += delta;
        local t = this.sidePanelTime / this.sidePanelDuration;
        if (t > 1) t = 1;

        this.sidePanelAmount = _Lerp(this.sidePanelStartAmount, this.sidePanelTargetAmount, t);
        this.Elements.leftpanel.X = _Lerp(this.leftPanelClosedX, this.leftPanelOpenX, this.sidePanelAmount);
        this.Elements.rightpanel.X = _Lerp(this.rightPanelClosedX, this.rightPanelOpenX, this.sidePanelAmount);

        if (this.sidePanelTime >= this.sidePanelDuration) {
            this.sidePanelTime = this.sidePanelDuration;
            this.sidePanelAmount = this.sidePanelTargetAmount;
            if (this.sidePanelTargetAmount == 0) {
                this.Elements.leftpanel.Visible = false;
                this.Elements.rightpanel.Visible = false;
            }
        }
    }
}
