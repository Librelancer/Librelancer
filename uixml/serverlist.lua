function serverlist:ctor()
	local scn = self.Elements

	scn.mainmenu:OnClick(function()
		self:ExitAnimation(function()
			Game:StopNetworking()
			OpenScene("mainmenu")
		end)
	end)

	scn.listtable:SetData(Game:ServerList())
	scn.connect:OnClick(function()
		Game:ConnectSelection()
	end)

	scn.directip:OnClick(function()
		OpenModal(textentry(function(result, text)
			if result == "ok" then
				if not Game:ConnectAddress(text) then
					OpenModal(modal("Error", "Address not valid"))
				end
			end
		end, StringFromID(1861)))
	end)

	scn.animgroupA:Animate('flyinleft', 0, 0.8)
	scn.animgroupB:Animate('flyinright', 0, 0.8)
	Game:StartNetworking()
end

function serverlist:ExitAnimation(f)
	local scn = self.Elements
	scn.animgroupA:Animate('flyoutleft', 0, 0.8)
	scn.animgroupB:Animate('flyoutright', 0, 0.8)
	Timer(0.8, f)
end

function serverlist:CharacterList()
	self:ExitAnimation(function()
		OpenScene("characterlist")
	end)
end

function serverlist:Update()
	local scn = self.Elements
	local sv = Game:ServerList()
	scn.connect.Enabled = sv:ValidSelection()
	scn.descriptiontext.Text = sv:CurrentDescription()
end






