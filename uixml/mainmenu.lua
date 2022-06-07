class mainmenu : mainmenu_Designer
{
	mainmenu()
	{
		base();
		local scn = this.Elements;
		scn.newgame.Animate('flyinleft', 0, 0.6)
		scn.loadgame.Animate('flyinleft', 0.05, 0.6)
		scn.multiplayer.Animate('flyinleft', 0.1, 0.6)
		scn.options.Animate('flyinleft', 0.15, 0.6)
		scn.exit.Animate('flyinleft', 0.2, 0.6)

		scn.newgame.OnClick(() => this.ExitAnimation(() => {
			Game.NewGame();
		}));

		scn.loadgame.OnClick(() => this.ExitAnimation(() => {
			OpenScene("loadgame");
		}));

		scn.multiplayer.OnClick(() => this.ExitAnimation(() => {
			OpenScene("serverlist");
		}));

		scn.options.OnClick(() => this.ExitAnimation(() => {
			OpenScene("options");
		}));

		scn.exit.OnClick(() => this.ExitAnimation(() => {
			Game.Exit();
		}));
	}

	ExitAnimation(f)
	{
		local e = this.Elements;
		e.exit.Animate('flyoutleft', 0, 0.6)
		e.options.Animate('flyoutleft', 0.05, 0.6)
		e.multiplayer.Animate('flyoutleft', 0.1, 0.6)
		e.loadgame.Animate('flyoutleft', 0.15, 0.6)
		e.newgame.Animate('flyoutleft', 0.2, 0.6)
		Timer(0.8, f)
	}
}