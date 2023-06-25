class pausemenu : pausemenu_Designer with Modal
{
    pausemenu()
    {
        base()
		this.ModalInit();
        var e = this.Elements
        e.loadgame.Enabled = !Game.IsMultiplayer()
        e.loadgame.OnClick(() => {
            var lg = new loadgame();
            lg.asmodal();
            SwapModal(this, lg);
        });
        e.savegame.Enabled = !Game.IsMultiplayer()
        e.savegame.OnClick(() => {
            var sg = new savegame();
            SwapModal(this, sg);
        });
        e.options.OnClick(() => {
            var opt = new options()
            opt.asmodal();
            SwapModal(this, opt);
        });
		this.Widget.OnEscape(() => { Game.Resume(); this.Close(); });
        e.resume.OnClick(() => { Game.Resume(); this.Close(); });
        e.quittomenu.OnClick(() => { Game.QuitToMenu(); this.Close(); });
    }
}
