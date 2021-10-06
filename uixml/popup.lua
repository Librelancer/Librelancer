ModalClass(popup)

function popup:ctor(title, contents, buttons, callback)
    local e = self.Elements
    e.title.Strid = title or 0
    e.contents:SetString(StringFromID(contents or 0))
    self:ModalInit()
	if buttons == 'ok' then
		e.ok_ok.Visible = true
		e.accept.Visible = false
		e.decline.Visible = false
	end
	if callback ~= nil then
    	self:ModalCallback(callback)
	end
    e.ok_ok:OnClick(function()
        self:Close('ok')
    end)
	e.accept:OnClick(function()
		self:Close('accept')
	end)
	e.decline:OnClick(function()
		self:Close('decline')
	end)
end



