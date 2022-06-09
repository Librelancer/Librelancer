// Manages child windows
// Window = { button, window } 1, 2

class childwindowmanager
{
    childwindowmanager(widget, windows)
    {
        this.windows = windows;
        for (i, w in ipairs(windows)) {
            w[1].OnClick(() => this.OpenWindow(widget, w[2]));
            w[2].OnClose = () => { this.CanOpen = true; this.SetButtonActive(nil); };
            w[2].OnOpen = () => { this.CanOpen = true; }
        }
        this.CanOpen = true;
    }
    
    OpenWindow(widget, window)
    {
        if(this.ActiveWindow == window) {
            this.CanOpen = false;
            window.Close();
        } elseif (this.CanOpen) {
            this.CanOpen = false;
            if (this.ActiveWindow != nil) {
                this.ActiveWindow.Close(() => { 
                    this.CanOpen = false; 
                    window.Open(widget); 
                    this.SetButtonActive(window); 
                });
            } else {
                window.Open(widget);
                this.SetButtonActive(window);
            }
        }
    }
    
    SetButtonActive(window)
    {
        this.ActiveWindow = window
	    for (w in this.windows)
		    w[1].Selected = (w[2] == window);
    }
}
