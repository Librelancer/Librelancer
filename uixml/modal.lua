ModalClass(modal)

function modal:ctor(title, contents, buttons, callback)
    local e = self.Elements
    e.title.Text = title
    e.content.Text = contents
    if buttons == 'ok' then
       e.ok_ok.Visible = true 
    end
    self:ModalInit()
	if callback ~= nil then
    	self:ModalCallback(callback)
	end
    e.close:OnClick(function()
        self:Close('cancel')
    end)
    e.ok_ok:OnClick(function()
        self:Close('ok')
    end)
end






