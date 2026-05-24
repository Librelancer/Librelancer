require 'childwindow.lua'

class mapwindow : mapwindow_Designer with ChildWindow
{
    mapwindow()
    {
        base();
        this.ChildWindowInit();
        this.OnChildOpen = () => this.ResetNavmap();
        this.Elements.exit.OnClick(() => this.Close());
    }
    InitMap()
    {
        this.ResetNavmap();
        Game.PopulateNavmap(this.Elements.navmap);
    }
    Closing()
    {
        this.ResetNavmap();
    }
    ResetNavmap()
    {
        this.Elements.navmap.ResetView();
    }
}
