function infowindow:ctor()
	self.Opened = false
	self.Elements.close:OnClick(function()
		self:Close()
	end)
	self.BkWidth = self.Elements.background.Width
	self.BkHeight = self.Elements.background.Height
	self.Elements.info.Infocard = Game:CurrentInfocard()
	self.Widget:OnUpdate(function(delta)
		self:Update(delta)
	end)
end

function infowindow:Open(widget)
	if self.Opened then
		return
	end
	self.Opened = true
	self.Parent = widget
	self:AnimateIn()
	widget:AddChild(self.Widget)
end

function infowindow:AnimateIn()
	self.Time = 0
	self.Duration = 0.25
	self.AnimatingIn = true
	self.Elements.contents.Visible = false
	self.Elements.background.Width = 0
	self.Elements.background.Height = 0
end

function infowindow:AnimateOut()
	self.Time = 0
	self.Duration = 0.25
	self.AnimatingOut = true
	self.Elements.contents.Visible = false
end

function infowindow:Update(delta)
	if self.AnimatingIn then
		self.Time = self.Time + delta
		local t = self.Time / self.Duration
		self.Elements.background.Width = self:Lerp(0, self.BkWidth, t)
		self.Elements.background.Height = self:Lerp(0, self.BkHeight, t)
		if self.Time > self.Duration then
			self.Elements.contents.Visible = true
			self.AnimatingIn = false
		end
	end
	if self.AnimatingOut then
		self.Time = self.Time + delta
		local t = self.Time / self.Duration
		self.Elements.background.Width = self:Lerp(self.BkWidth, 0, t)
		self.Elements.background.Height = self:Lerp(self.BkHeight, 0, t)
		if self.Time > self.Duration then
			self.AnimatingOut = false
			self.Opened = false
			self.Parent:RemoveChild(self.Widget)
		end
	end
end

function infowindow:Lerp(x, y, t)
	if t >= 1 then return y end
	if t <= 0 then return x end
	return x + (y - x) * t
end

function infowindow:Close()
	if self.Opened then
		self:AnimateOut()
	end
end

