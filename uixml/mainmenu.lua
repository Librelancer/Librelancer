require 'ids.lua'

class mainmenu : mainmenu_Designer
{
	mainmenu(delay)
	{
		base();
		local scn = this.Elements;
		scn.version.Text = FormatStringID(1271, 1, 0)
		delay ??= 0;
		LoadSound('ui_motion_swish')
		// hack as we shouldn't run animations in constructors for now
		this.EnterAnimation(delay);

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

	EnterAnimation(delay)
	{
		Timer(delay, () => PlaySound('ui_motion_swish'))
		local scn = this.Elements;
		scn.newgame.Animate('flyinleft', delay + 0.1, 0.65)
		scn.loadgame.Animate('flyinleft', delay + 0.25, 0.65)
		scn.multiplayer.Animate('flyinleft', delay + 0.4, 0.65)
		scn.options.Animate('flyinleft', delay + 0.55, 0.65)
		scn.exit.Animate('flyinleft', delay + 0.7, 0.65)
	}

	ExitAnimation(f)
	{
		local e = this.Elements;
		PlaySound('ui_motion_swish')
		e.exit.Animate('flyoutleft', 0, 0.6)
		e.options.Animate('flyoutleft', 0.05, 0.6)
		e.multiplayer.Animate('flyoutleft', 0.1, 0.6)
		e.loadgame.Animate('flyoutleft', 0.15, 0.6)
		e.newgame.Animate('flyoutleft', 0.2, 0.6)
		Timer(0.8, f)
	}
}
