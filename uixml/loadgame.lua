class loadgame : loadgame_Designer with Modal
{
	loadgame()
	{
		base();
		local e = this.Elements;
		this.isModal = false;

		e.listtable.SetData(Game.SaveGames());
		e.resume.OnClick(() => { Game.Resume(); this.Close(); });

		e.goback.OnClick(() => {
			if (this.isModal) {
				Game.QuitToMenu();
				this.Close();
			} else {
				this.ExitAnimation(() => OpenScene("mainmenu"));
			}
		});

		e.load.OnClick(() => this.ExitAnimation(() => {
			Game.LoadSelectedGame();
		}));
		
		e.delete.OnClick(() => {
			Game.DeleteSelectedGame();
		});
	}

	ExitAnimation(f) => f();

	asmodal()
	{
		this.ModalInit();
		local e = this.Elements;
		e.fllogo.Visible = false;
		e.resume.Visible = true;
		e.backdrop.Visible = true;
		this.isModal = true;
	}

	Update()
	{
		local e = this.Elements;
		local sv = Game.SaveGames();
		e.load.Enabled = sv.ValidSelection();
		e.delete.Enabled = sv.ValidSelection();
	}
}
