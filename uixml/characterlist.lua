class characterlist : characterlist_Designer 
{
    characterlist()
    {
        base()
        var e = this.Elements;
        e.listtable.SetData(Game.CharacterList());
        
        e.newchar.OnClick(() => Game.RequestNewCharacter());
        e.loadchar.OnClick(() => Game.LoadCharacter());
        e.deletechar.OnClick(() => Game.DeleteCharacter());
        e.serverlist.OnClick(() => this.ExitAnimation(() => {
            Game.StopNetworking();
            OpenScene("serverlist");
        }));
        e.mainmenu.OnClick(() => this.ExitAnimation(() => {
            Game.StopNetworking();
            OpenScene("mainmenu");
        }));
    }
    
    //TODO: Animate out
    ExitAnimation(f) => f();
    
    Update()
    {
        var scn = this.Elements;
        var cl = Game.CharacterList();
        scn.loadchar.Enabled = cl.ValidSelection();
	    scn.deletechar.Enabled = cl.ValidSelection();
    }
    
    OpenNewCharacter()
    {
        OpenModal(new textentry((r,n,i) => this.CreateCharacter(r,n,i), StringFromID(STRID_NEW_CHARACTER)));
    }
    
    CreateCharacter(result, name, index)
    {
        if(result == 'ok')
            Game.NewCharacter(name, index);
    }
    
    Disconnect()
    {
        OpenModal(new modal("Error", "You were disconnected from the server", "ok", () => this.ExitAnimation(() => OpenScene("mainmenu"))));
    }
}

