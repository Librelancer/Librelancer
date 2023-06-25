class savegame : savegame_Designer with Modal
{
	savegame()
	{
		base();
		this.ModalInit();
		local e = this.Elements;
		
		e.listtable.SetData(Game.SaveGames());
		e.listtable.OnItemSelected(() => e.content.CurrentText = Game.SaveGames().CurrentDescription());
		e.content.MaxChars = 32;

		e.resume.OnClick(() => {
			Game.Resume();
			this.Close();
		})
		this.Widget.OnEscape(() => { Game.Resume(); this.Close(); });

		e.goback.OnClick(() => {
			Game.QuitToMenu();
			this.Close();
		})

		e.save.OnClick(() => Game.SaveGame(e.content.CurrentText));
		e.content.OnTextEntered((name) => Game.SaveGame(name));
		e.delete.OnClick(() => Game.DeleteSelectedGame());
	}
	Update()
	{
		local scn = this.Elements
		local sv = Game.SaveGames()	
		scn.delete.Enabled = sv.ValidSelection()
	}
}



