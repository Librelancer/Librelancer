class serverlist : serverlist_Designer
{
	serverlist()
	{
		base();
		var e = this.Elements;
		e.mainmenu.OnClick(() => this.Leave());
		this.Widget.OnEscape(() => this.Leave());
		e.listtable.OnDoubleClick(() => this.TryConnect());
		e.refreshlist.OnClick(() => Game.RefreshServers());
		e.connect.OnClick(() => this.TryConnect());
		e.directip.OnClick(() => OpenModal(new ipentry(this)));
		e.animgroupA.Animate('flyinleft', 0, 0.8)
		e.animgroupB.Animate('flyinright', 0, 0.8)
		this.InitNetwork();
	}

    Leave()
    {
        this.ExitAnimation(() => {
            Game.StopNetworking();
            OpenScene("mainmenu");
        });
    }
	TryConnect()
	{
		this.connecting = new connecting();
		OpenModal(this.connecting);
		Game.ConnectSelection();
	}

	InitNetwork()
	{
		Game.StartNetworking();
		this.Elements.listtable.SetData(Game.ServerList());
	}

	ExitAnimation(f)
	{
		local e = this.Elements
		e.animgroupA.Animate('flyoutleft', 0, 0.8)
		e.animgroupB.Animate('flyoutright', 0, 0.8)
		Timer(0.8, f)
	}

	CharacterList()
	{
		if(this.connecting != nil) {
			this.connecting.Close();
		}
		this.ExitAnimation(() => OpenScene("characterlist"));
	}

	Update()
	{
		local scn = this.Elements
		local sv = Game.ServerList()
		scn.connect.Enabled = sv.ValidSelection()
		scn.descriptiontext.Text = sv.CurrentDescription()
	}

	Login()
	{
		if(this.connecting != nil) {
			this.connecting.Close();
			this.connecting = nil;
		}
		OpenModal(new login(this));
	}

	IncorrectPassword()
	{
		if(this.connecting != nil) {
			this.connecting.Close();
			this.connecting = nil;
		}
		OpenModal(new login(this, true));
	}

 	Disconnect(reason)
    {
		if(this.connecting != nil) {
			this.connecting.Close();
			this.connecting = nil;
		}
		var id = (reason == "Banned" ? STRID_BANNED : STRID_DISCONNECT);
        OpenModal(new popup(0, id, "ok", () => this.InitNetwork()));
    }
}







