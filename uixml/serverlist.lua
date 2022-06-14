class serverlist : serverlist_Designer
{
	serverlist()
	{
		base();
		var e = this.Elements;
		e.mainmenu.OnClick(() => this.ExitAnimation(() => {
			Game.StopNetworking();
			OpenScene("mainmenu");
		}));

		e.connect.OnClick(() => {
			this.connecting = new connecting();
			OpenModal(this.connecting);
			Game.ConnectSelection()
		});
		e.directip.OnClick(() => OpenModal(new ipentry(this)));
		e.animgroupA.Animate('flyinleft', 0, 0.8)
		e.animgroupB.Animate('flyinright', 0, 0.8)
		this.InitNetwork();
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

 	Disconnect()
    {
		if(this.connecting != nil) {
			this.connecting.Close();
			this.connecting = nil;
		}
        OpenModal(new popup(0, STRID_DISCONNECT, "ok", () => this.InitNetwork()));
    }
}



