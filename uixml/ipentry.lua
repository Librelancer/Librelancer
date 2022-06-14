class ipentry : ipentry_Designer with Modal
{
    ipentry(serverlist)
    {
        base();
        this.ModalInit();
        var scn = this.Elements;
        scn.content.SetFocus();
		local connect = (name) => {
			serverlist.connecting = new connecting();
			SwapModal(this, serverlist.connecting);
			Game.ConnectAddress(name);
		}
        scn.content.OnTextEntered(connect);
		scn.ok.OnClick(() => {
			if(scn.content.NotEmpty) connect(scn.content.CurrentText);
		});
        scn.close.OnClick(() => this.Close());
    }
}


