require 'childwindow'

class chathistory : chathistory_Designer with ChildWindow
{
	chathistory()
    {
        base();
        this.ChildWindowInit();
        this.Elements.close.OnClick(() => this.Close());
		this.Elements.history.Chat = Game.GetChats();
    }
}