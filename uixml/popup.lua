class popup : popup_Designer with Modal
{
    popup(title, contents, buttons, callback)
    {
        base()
        var e = this.Elements;
        e.title.Strid = title ?? 0;
        e.contents.SetString(StringFromID(contents ?? 0));
        this.ModalInit();
        if(buttons == 'ok') {
            e.ok_ok.Visible = true
            e.accept.Visible = false
            e.decline.Visible = false
        }
        
        if (callback != nil) this.ModalCallback(callback);
        
        e.ok_ok.OnClick(() => this.Close('ok'));
        e.accept.OnClick(() => this.Close('accept'));
        e.decline.OnClick(() => this.Close('decline'));
    }
}
