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

		e.listtable.SetData(Game.ServerList());
		e.connect.OnClick(() => Game.ConnectSelection());
		e.directip.OnClick(() => {
			OpenModal(new textentry((result,text) => {
				if(result == "ok") {
					if(!Game.ConnectAddress(text)) {
						OpenModal(new modal("Error", "Address not valid"));
					}
				}
			}), StringFromID(1861));
		});

		e.animgroupA.Animate('flyinleft', 0, 0.8)
		e.animgroupB.Animate('flyinright', 0, 0.8)
		Game.StartNetworking();
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
		this.ExitAnimation(() => OpenScene("characterlist"));
	}

	Update()
	{
		local scn = this.Elements
		local sv = Game.ServerList()
		scn.connect.Enabled = sv.ValidSelection()
		scn.descriptiontext.Text = sv.CurrentDescription()
	}
}
