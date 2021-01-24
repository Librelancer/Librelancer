-- Manages child windows
childwindowmanager = class()

-- Window = { button, window } 1, 2

function childwindowmanager:init(widget, windows)
	self.windows = windows
	for i, w in ipairs(windows) do
		w[1]:OnClick(function() 
			self:OpenWindow(widget, w[2])
		end)
		w[2].OnClose = function()
			self.CanOpen = true
			self:SetButtonActive(nil)
		end
		w[2].OnOpen = function()
			self.CanOpen = true
		end
	end	
	self.CanOpen = true
end

function childwindowmanager:OpenWindow(widget, window)
	if self.ActiveWindow == window then
		self.CanOpen = false
		window:Close()
	elseif self.CanOpen then
		self.CanOpen = false
		if self.ActiveWindow then
			self.ActiveWindow:Close(function()
				self.CanOpen = false
				window:Open(widget)
				self:SetButtonActive(window)
			end)
		else
			window:Open(widget)
			self:SetButtonActive(window)
		end
	end
end

function childwindowmanager:SetButtonActive(window)
	self.ActiveWindow = window
	for i, w in ipairs(self.windows) do
		w[1].Selected = (w[2] == window)
	end
end

