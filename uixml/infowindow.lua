require 'childwindow'

class infowindow : infowindow_Designer with ChildWindow
{
    infowindow()
    {
        base();
        this.ChildWindowInit();

        this.Elements.close.OnClick(() => this.Close());
    }

	OnChildOpen()
	{
		this.Elements.title.Text = Game.CurrentInfoString() ?? StringFromID(905);
        this.Elements.info.Infocard = Game.CurrentInfocard();
	}
}



