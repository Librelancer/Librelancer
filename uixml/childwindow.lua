-- Implements widget as a child that can be opened + closed

function MakeChildWindow(self)
	self.Opened = false
	self.Open = ChildWindow.Open
	self.AnimateIn = ChildWindow.AnimateIn
	self.AnimateOut = ChildWindow.AnimateOut
	self.Update = ChildWindow.Update
	self.Close = ChildWindow.Close
	self.BkWidth = self.Elements.background.Width
	self.BkHeight = self.Elements.background.Height
	self.Widget:OnUpdate(function(delta)
		self:Update(delta)
	end)
end


local function _Lerp(x, y, t)
	if t >= 1 then return y end
	if t <= 0 then return x end
	return x + (y - x) * t
end

ChildWindow = {}

function ChildWindow:Open(widget)
	if self.Opened then
		return
	end
	self.Opened = true
	self.Parent = widget
	self:AnimateIn()
	widget:AddChild(self.Widget)
end

function ChildWindow:AnimateIn()
	PlaySound('ui_motion_swish')
	self.Time = 0
	self.Duration = 0.25
	self.AnimatingIn = true
	self.Elements.contents.Visible = false
	self.Elements.background.Width = 0
	self.Elements.background.Height = 0
end

function ChildWindow:AnimateOut(cb)
	PlaySound('ui_motion_swish')
	self.OutCallback = cb
	self.Time = 0
	self.Duration = 0.25
	self.AnimatingOut = true
	self.Elements.contents.Visible = false
end

function ChildWindow:Update(delta)
	if self.AnimatingIn then
		self.Time = self.Time + delta
		local t = self.Time / self.Duration
		self.Elements.background.Width = _Lerp(0, self.BkWidth, t)
		self.Elements.background.Height = _Lerp(0, self.BkHeight, t)
		if self.Time > self.Duration then
			self.Elements.contents.Visible = true
			self.AnimatingIn = false
			PlaySound('ui_window_open')
			if self.OnOpen then
				self.OnOpen()
			end
		end
	end
	if self.AnimatingOut then
		self.Time = self.Time + delta
		local t = self.Time / self.Duration
		self.Elements.background.Width = _Lerp(self.BkWidth, 0, t)
		self.Elements.background.Height = _Lerp(self.BkHeight, 0, t)
		if self.Time > self.Duration then
			self.AnimatingOut = false
			self.Opened = false
			self.Parent:RemoveChild(self.Widget)
			if self.OnClose then
				self.OnClose()
			end
			if self.OutCallback then
				self.OutCallback()
				self.OutCallback = nil
			end
		end
	end
end

function ChildWindow:Close(cb)
	if self.Opened then
		self:AnimateOut(cb)
	end
end


