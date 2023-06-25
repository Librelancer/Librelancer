class modal : modal_Designer with Modal
{
    modal(title, contents, buttons, callback)
    {
        base();
        this.ModalInit();
        local e = this.Elements;
        e.title.Text = title;
        e.content.Text = contents;

        if(buttons == 'ok') e.ok_ok.Visible = true;
        if(callback != nil) this.ModalCallback(callback);

        e.close.OnClick(() => this.Close('cancel'));
        e.ok_ok.OnClick(() => this.Close('ok'));
		this.Widget.OnEscape(() => this.Close('cancel'));
    }
}
