require 'childwindow.lua'

class mapwindow : mapwindow_Designer with ChildWindow
{
    mapwindow()
    {
        base();
        this.ChildWindowInit();
        this.Elements.exit.OnClick(() => this.Close());
        this.InitFilterButtons();
    }

    InitMap()
    {
        Game.PopulateNavmap(this.Elements.navmap);
    }

    InitFilterButtons()
    {
        local e = this.Elements;
        local nav = e.navmap;

        // FUNCTIONAL: Toggle universe map view (triggers repopulation)
        e.universebutton.OnClick(() => {
            e.universebutton.Selected = !e.universebutton.Selected;
            nav.ShowUniverseMap = e.universebutton.Selected;
            this.InitMap();  // Force repopulation for universe/system view switch
        });

        // Functional: hides/shows object labels
        e.labels.Selected = true;
        e.labels.OnClick(() => {
            e.labels.Selected = !e.labels.Selected;
            nav.ShowLabels = e.labels.Selected;
        });

        // Functional: hides/shows zones (physical terrain)
        e.physical.Selected = true;
        e.physical.OnClick(() => {
            e.physical.Selected = !e.physical.Selected;
            nav.ShowPhysical = e.physical.Selected;
        });

        // Visual-only toggle (political view)
        e.political.OnClick(() => {
            e.political.Selected = !e.political.Selected;
            nav.ShowPolitical = e.political.Selected;
        });

        // Visual-only toggle (patrol routes)
        e.patrol.OnClick(() => {
            e.patrol.Selected = !e.patrol.Selected;
            nav.ShowPatrolRoutes = e.patrol.Selected;
        });

        // Visual-only toggle (minable zones)
        e.miningfilter.OnClick(() => {
            e.miningfilter.Selected = !e.miningfilter.Selected;
            nav.ShowMinableZones = e.miningfilter.Selected;
        });

        // Visual-only toggle (legend display)
        e.legendtoggle.OnClick(() => {
            e.legendtoggle.Selected = !e.legendtoggle.Selected;
            nav.ShowLegend = e.legendtoggle.Selected;
        });

        // Functional: shows only dockable objects (bases, jump gates/holes)
        e.knownbases.OnClick(() => {
            e.knownbases.Selected = !e.knownbases.Selected;
            nav.ShowOnlyKnownBases = e.knownbases.Selected;
        });
    }
}
