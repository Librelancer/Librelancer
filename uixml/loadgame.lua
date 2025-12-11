class loadgame : loadgame_Designer with Modal
{
	loadgame()
	{
		base();
		local e = this.Elements;
		this.isResume = false;
		this.isDeath = false;

		e.listtable.SetData(Game.SaveGames());
		e.resume.OnClick(() => { Game.Resume(); this.Close(); });

		e.goback.OnClick(() => this.do_goback());
		this.Widget.OnEscape(() => this.do_goback())

		e.load.OnClick(() => this.ExitAnimation(() => {
			Game.LoadSelectedGame();
		}));
		
		e.delete.OnClick(() => {
			Game.DeleteSelectedGame();
		});
	}

	ExitAnimation(f) => f();

	do_goback()
	{
		if (this.isResume) {
			Game.Resume();
			this.Close();
		} else if (this.isDeath) {
			Game.QuitToMenu();
			this.Close();
		} else {
			this.ExitAnimation(() => OpenScene("mainmenu"));
		}
	}

	asmodal()
	{
		this.ModalInit();
		local e = this.Elements;
		e.fllogo.Visible = false;
		e.resume.Visible = true;
		e.backdrop.Visible = true;
		this.isResume = true;
	}
	
	asdeath()
	{
		this.asmodal();
		local e = this.Elements;
		e.resume.Visible = false;
		this.isResume = false;
		this.isDeath = true;
	}

	Update()
	{
		local e = this.Elements;
		local sv = Game.SaveGames();
		e.load.Enabled = sv.ValidSelection();
		e.delete.Enabled = sv.ValidSelection();
	}
}
