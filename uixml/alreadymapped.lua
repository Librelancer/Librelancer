class alreadymapped : alreadymapped_Designer with Modal 
{    
    alreadymapped(key, button, callback)
    {
        base();
        var e = this.Elements;
        e.input.Text = key;
        e.keyName.Strid = button;
        this.ModalInit();
        if (callback != nil) this.ModalCallback(callback);
        e.btnContinue.OnClick(() => this.Close('continue'));
        e.cancel.OnClick(() => this.Close('cancel'));
    }
}
