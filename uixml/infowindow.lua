require 'childwindow'

class infowindow : infowindow_Designer with ChildWindow
{
    infowindow()
    {
        base();
        this.ChildWindowInit();
        this.Elements.info.Infocard = Game.CurrentInfocard();
        this.Elements.close.OnClick(() => this.Close());
    }
}
