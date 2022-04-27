require 'ids.lua'

ModalClass(options)

function options:asmodal()
	self:ModalInit()
	self.Elements.fllogo.Visible = false
	self.Elements.backdrop.Visible = true
	self.Elements.goback.Strid = STRID_RETURN_TO_GAME
	self.isModal = true
end

function options:panel(p)
	for _, panel in ipairs(self.Panels) do
		if panel[1] == p[1] then
			panel[1].Selected = true
			panel[2].Visible = true
		else
			panel[1].Selected = false
			panel[2].Visible = false
		end
	end
end

local msaa_levels = {
	"NONE",
	"MSAA 2x",
	"MSAA 4x",
	"MSAA 8x"
}

local function val_selection(left, right, display, values, vmin, vmax, vcurrent)
	local state = {}
	state.vmin = vmin
	state.vmax = vmax
	state.vcurrent = vcurrent
	state.values = values
	local function setval(idx)
		local i = idx
		if i < vmin then i = vmax end
		if i > vmax then i = vmin end
		state.vcurrent = i
		display.Text = state.values[state.vcurrent]
	end
	left:OnClick(function()
		setval(state.vcurrent - 1)
	end)
	right:OnClick(function()
		setval(state.vcurrent + 1)
	end)
	setval(vcurrent)
	return state
end

-- Map strings to MSAA Amounts
local function msaa_to_idx(i)
	if i == 2 then return 2 end
	if i == 4 then return 3 end
	if i >= 8 then return 4 end
	return 1
end

local function idx_to_msaa(i)
	if i == 2 then return 2 end
	if i == 3 then return 4 end
	if i == 4 then return 8 end
	return 0
end

-- Anisotropy Levels
local function idx_to_anisotropy(i)
	if i == 1 then return 0 end
	return math.pow(2, i - 1)
end
local function anisotropy_to_idx(i)
	if i == 0 then return 1 end
	local x = 2
	for j = 2, 10 do
		if x == i then
			return j
		else
			x = x * 2
		end
	end
	return 0
end

function options:setcontrolcategory(cat)
	self.keymap:SetGroup(cat - 1)
	for index, value in ipairs(self.controlcategories) do
		value.Selected = index == cat
	end
end

function options:ctor()
	local e = self.Elements
	self.isModal = false
	self.Elements.goback:OnClick(function()
		self.opts.SfxVolume = e.sfxvol.Value
		self.opts.MusicVolume = e.musicvol.Value
		self.opts.MSAA = idx_to_msaa(self.MSAA.vcurrent)
		self.opts.Anisotropy = idx_to_anisotropy(self.AF.vcurrent)
		self.keymap:Save()
		Game:ApplySettings(self.opts)
		if self.isModal then
			Game:Resume()
			self:Close()
		else
			OpenScene("mainmenu")
		end
	end)
	self.Panels = {
		{ e.performance, e.win_performance },
		{ e.audio, e.win_audio },
		{ e.controls, e.win_controls }
	}
	for _, p in ipairs(self.Panels) do
		p[1]:OnClick(function() self:panel(p) end)
	end
	self:panel(self.Panels[1])
	self.opts = Game:GetCurrentSettings()
	e.sfxvol.Value = self.opts.SfxVolume
	self.keymap = Game:GetKeyMap()
	e.listtable:SetData(self.keymap)
	e.listtable:OnDoubleClick(function(row, column)
		local mk = mapkey(self.keymap:GetKeyId(row), function(reason)
			if reason == 'cancel' then
				self.keymap:CancelCapture()
			elseif reason == 'clear' then
				self.keymap:ClearCapture()
			end
		end)
		OpenModal(mk)
		self.keymap:CaptureInput(row, column != 2, function(state, combo, key, accept)
			mk:Close('captured')
			if state == 'overwrite' then
				OpenModal(alreadymapped(combo, key, function(e) if e == 'continue' then accept() end end))
			end
		end)
	end)
	e.musicvol.Value = self.opts.MusicVolume
	self.AnisotropyLevels = self.opts.AnisotropyLevels()
	local anisotropy = {
		"NONE"
	}
	for _, i in ipairs(self.AnisotropyLevels) do
		table.insert(anisotropy, tostring(i) .."x AF")
	end
	self.MSAA = val_selection(e.msaa_left, e.msaa_right, e.msaa_display, msaa_levels, 1, msaa_to_idx(self.opts:MaxMSAA()), msaa_to_idx(self.opts.MSAA))
	self.AF = val_selection(e.af_left, e.af_right, e.af_display, anisotropy, 1, #anisotropy, anisotropy_to_idx(self.opts.Anisotropy))

	self.controlcategories = { e.cat_ship, e.cat_ui, e.cat_mp }
	e.cat_ship:OnClick(function() self:setcontrolcategory(1) end)
	e.cat_ui:OnClick(function() self:setcontrolcategory(2) end)
	e.cat_mp:OnClick(function() self:setcontrolcategory(3) end)
	e.ctrl_default:OnClick(function() self.keymap:DefaultBindings() end)
	e.ctrl_cancel:OnClick(function() self.keymap:ResetBindings() end)
end














