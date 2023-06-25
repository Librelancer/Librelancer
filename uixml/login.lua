require 'ids.lua'

class login : login_Designer with Modal
{
    login(serverlist, incorrect)
    {
        base();
        this.ModalInit();
        var scn = this.Elements;
        scn.username.SetFocus();
		scn.errorDisplay.SetString(StringFromID(incorrect ? STRID_INCORRECT_PASSWORD : STRID_PASSWORD_PROMPT));
		scn.ok.OnClick(() => {
			if(scn.username.NotEmpty && scn.password.NotEmpty) {
				serverlist.connecting = new connecting();
				SwapModal(this, serverlist.connecting);
				Game.Login(scn.username.CurrentText, scn.password.CurrentText);
			}
		});
        scn.close.OnClick(() => this.Close());
		this.Widget.OnEscape(() => this.Close());
    }
}


