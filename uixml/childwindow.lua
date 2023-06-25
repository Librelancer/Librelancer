// Implements widget as a child that can be opened + closed

local function _Lerp(x, y, t) {
	if (t >= 1) return y;
	if (t <= 0) return x;
	return x + (y - x) * t
}

mixin ChildWindow
{
    ChildWindowInit()
    {
        this.Opened = false;
        this.BkWidth = this.Elements.background.Width;
        this.BkHeight = this.Elements.background.Height;
        this.Widget.OnUpdate((delta) => this.Update(delta));
		this.Widget.OnEscape(() => this.Close());
    }
    
    Open(widget)
    {
        if(this.Opened) return;
        if(this.OnChildOpen) {
            this.OnChildOpen();
        }
        this.Opened = true;
        this.Parent = widget;
        this.AnimateIn();
        widget.AddChild(this.Widget);
    }
    
    AnimateIn()
    {
        PlaySound('ui_motion_swish')
	    this.Time = 0
	    this.Duration = 0.25
	    this.AnimatingIn = true
	    this.Elements.contents.Visible = false
	    this.Elements.background.Width = 0
	    this.Elements.background.Height = 0
    }
    
    AnimateOut()
    {
        PlaySound('ui_motion_swish')
	    this.OutCallback = cb
	    this.Time = 0
	    this.Duration = 0.25
	    this.AnimatingOut = true
	    this.Elements.contents.Visible = false
    }
    
    Update(delta)
    {
        if (this.AnimatingIn) {
		    this.Time += delta
		    local t = this.Time / this.Duration
		    this.Elements.background.Width = _Lerp(0, this.BkWidth, t)
		    this.Elements.background.Height = _Lerp(0, this.BkHeight, t)
		    if (this.Time > this.Duration) {
			    this.Elements.contents.Visible = true
			    this.AnimatingIn = false
			    PlaySound('ui_window_open')
			    if (this.OnOpen != nil) {
					this.OnOpen();
				}
		    }
	    }
	    if (this.AnimatingOut) {
		    this.Time += delta
		    local t = this.Time / this.Duration
		    this.Elements.background.Width = _Lerp(this.BkWidth, 0, t)
	    	this.Elements.background.Height = _Lerp(this.BkHeight, 0, t)
	    	if (this.Time > this.Duration) {
	    		this.AnimatingOut = false
	    		this.Opened = false
	    		this.Parent.RemoveChild(this.Widget)
	    		if (this.OnClose) this.OnClose();
	    		if (this.OutCallback) {
	    			this.OutCallback()
	    			this.OutCallback = nil
	    		}
	    	}
	    }
    }
    
    Close(cb)
    {
        if (this.Opened) this.AnimateOut(cb);
    }
}


