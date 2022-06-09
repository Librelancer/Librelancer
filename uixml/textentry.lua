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
        scn.content.OnTextEntered(() => this.Close('ok', name, 0));
        scn.close.OnClick(() => this.Close('cancel'));
    }
}
