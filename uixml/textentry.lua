class textentry : textentry_Designer with Modal
{
    textentry(cb, title)
    {
        base();
        this.ModalInit();
        this.ModalCallback(cb);
        var scn = this.Elements;
        scn.content.SetFocus();
        scn.title.Text = title;
        scn.content.OnTextEntered((name) => this.OnTextEnter(name));
		scn.ok.OnClick(() => this.OnTextEnter(scn.content.CurrentText));
        scn.close.OnClick(() => this.Close('cancel'));
		this.Widget.OnEscape(() => this.Close('cancel'));
		this.characters = Game.GetNewCharacters()
		this.index = 1;
		this.SetInfo(this.characters[1])
		scn.char_left.OnClick(() => {
			if(this.index == 1) {
				this.index = this.characters.length;
			} else {
				this.index -= 1;
			}
			this.SetInfo(this.characters[this.index])
		});
		scn.char_right.OnClick(() => {
			if(this.index == this.characters.length) {
				this.index = 1;
			} else {
				this.index += 1;
			}
			this.SetInfo(this.characters[this.index])
		});
		if(this.characters.length < 2) {
			scn.char_left.Visible = false;
			scn.char_right.Visible = false;
		}
    }

	SetInfo(charinfo)
	{
		var e = this.Elements;
		e.char_info.SetString(
`{StringFromID(1428)} {StringFromID(STRID_CREDIT_SIGN)}{NumberToStringCS(charinfo.Money, "N0")}
{StringFromID(1425)} {charinfo.ShipName}
{StringFromID(1427)} {charinfo.Location}
{StringFromID(charinfo.StridName)}
{StringFromID(charinfo.StridDesc)}`
		);
		e.ship_preview.ModelPath = charinfo.ShipModel;
	}

	OnTextEnter(text)
	{
		this.Close('ok', text, this.index - 1);
	}
}











