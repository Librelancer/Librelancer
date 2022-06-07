class mapkey : mapkey_Designer with Modal
{
    mapkey(button, callback)
    {
        base()
        var e = this.Elements
        e.keyName.strid = button
        this.ModalInit();
        if(callback != null) this.ModalCallback(callback);
        e.clear.OnClick(() => this.Close('clear'));
        e.cancel.OnClick(() => this.Close('cancel'));
    }
}
