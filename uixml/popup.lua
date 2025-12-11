class popup : popup_Designer with Modal
{
    popup(title, contents, buttons, callback)
    {
        base()
        var e = this.Elements;
        if((title ?? 0) == 0) {
			e.title.Visible = false;
		} else {
			e.title.Strid = title;
		}

        e.contents.SetString(StringFromID(contents ?? 0));
        this.ModalInit();
        if(buttons == 'ok') {
            e.ok_ok.Visible = true
            e.accept.Visible = false
            e.decline.Visible = false
			this.Widget.OnEscape(() => this.Close('ok'));
        } else {
			this.Widget.OnEscape(() => this.Close('decline'));
		}
        
        if (callback != nil) this.ModalCallback(callback);
        
        e.ok_ok.OnClick(() => this.Close('ok'));
        e.accept.OnClick(() => this.Close('accept'));
        e.decline.OnClick(() => this.Close('decline'));
    }
}