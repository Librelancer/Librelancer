require 'childwindow.lua'

class mapwindow : mapwindow_Designer with ChildWindow
{
    mapwindow()
    {
        base();
        this.ChildWindowInit();
        this.Elements.exit.OnClick(() => this.Close());
    }
    InitMap()
    {
        Game.PopulateNavmap(this.Elements.navmap);
    }
}
