function characterlist:ctor()
	local scn = self.Elements
	scn.listtable:SetData(Game:CharacterList())

	scn.newchar:OnClick(function()
		Game:RequestNewCharacter()
	end)

	scn.loadchar:OnClick(function()
		Game:LoadCharacter()
	end)

	scn.deletechar:OnClick(function()
		Game:DeleteCharacter()
	end)

	scn.serverlist:OnClick(function()
		self:ExitAnimation(function()
			Game:StopNetworking()
			OpenScene("serverlist")
		end)
	end)
end

function characterlist:ExitAnimation(f)
	f()
end

function characterlist:Update()
	local scn = self.Elements
	local cl = Game:CharacterList()
	scn.loadchar.Enabled = cl:ValidSelection()
	scn.deletechar.Enabled = cl:ValidSelection()
end

function characterlist:OpenNewCharacter()
	OpenModal(newcharacter(function(result, name, index)
		self:CreateCharacter(result, name, index)
	end))
end

function characterlist:CreateCharacter(result, name, index)
	if result == 'ok' then
		Game:NewCharacter(name, index)
	end
end

function characterlist:Disconnect()
	OpenModal(modal("Error", "You were disconnected from the server", "ok", function()
		self:ExitAnimation(function()
			OpenScene("mainmenu")
		end)
	end))
end


