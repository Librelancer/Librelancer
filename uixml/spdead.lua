class spdead : spdead_Designer with Modal
{
    spdead(contents)
    {
        base()
        var e = this.Elements;
        
        e.contents.SetString(StringFromID(contents ?? 0));
        this.ModalInit();
        
        e.main_menu.OnClick(() => { Game.QuitToMenu(); this.Close(); });
        e.load_game.OnClick(() => {
            var lg = new loadgame();
            lg.asdeath();
            SwapModal(this, lg);
        });
    }
}