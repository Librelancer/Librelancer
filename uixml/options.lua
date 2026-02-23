require 'ids.lua'

local msaa_levels = {
	"NONE",
	"MSAA 2x",
	"MSAA 4x",
	"MSAA 8x"
}

local function val_selection(left, right, display, values, vmin, vmax, vcurrent)
{
	local state = {}
	state.vmin = vmin
	state.vmax = vmax
	state.vcurrent = vcurrent
	state.values = values
	local function setval(idx)
	{
		local i = idx
		if (i < vmin) i = vmax;
		if (i > vmax) i = vmin;
		state.vcurrent = i
		display.Text = state.values[state.vcurrent]
	}
	left.OnClick(() => setval(state.vcurrent - 1));
	right.OnClick(() => setval(state.vcurrent + 1));
	setval(vcurrent)
	return state
}

// Map strings to MSAA Amounts
local function msaa_to_idx(i)
{
	if (i == 2) return 2;
	if (i == 4) return 3;
	if (i >= 8) return 4;
	return 1
}

local function idx_to_msaa(i)
{
	switch(i) {
		case 2: return 2;
		case 3: return 4;
		case 4: return 8;
		default: return 0;
	}
}

// Anisotropy Levels
local function idx_to_anisotropy(i) => i == 1 ? 0 : math.pow(2, i - 1);

local function anisotropy_to_idx(i)
{
	if (i == 0) return 1;
	local x = 2
	for (j in 2..10) {
		if(x == i) return j;
		x *= 2;
	}
	return 1;
}

class options : options_Designer with Modal
{
	options()
	{
		base();
		local e = this.Elements
		this.isModal = false
		this.Elements.goback.OnClick(() => this.do_goback());
		this.Widget.OnEscape(() => this.do_goback());
		this.Panels = {
			{ e.performance, e.win_performance },
			{ e.audio, e.win_audio },
			{ e.controls, e.win_controls }
		}
		for (p in this.Panels)
			p[1].OnClick(() => this.panel(p));
		
		this.panel(this.Panels[1])
		this.opts = Game.GetCurrentSettings()
		e.sfxvol.Value = this.opts.SfxVolume
		e.voicevol.Value = this.opts.VoiceVolume
		this.keymap = Game.GetKeyMap()

		e.listtable.SetData(this.keymap)

		e.listtable.OnDoubleClick((row, column) => {

			local mk = new mapkey(this.keymap.GetKeyId(row), (reason) => {
				if (reason == 'cancel')
					this.keymap.CancelCapture();
				elseif (reason == 'clear')
					this.keymap.ClearCapture();
			});
			OpenModal(mk)
			this.keymap.CaptureInput(row, column != 2, (state, combo, key, accept) => {
				mk.Close('captured')
				if (state == 'overwrite')
					OpenModal(new alreadymapped(combo, key, (e) => { if (e == 'continue')  accept(); }));
			});

		});

		e.musicvol.Value = this.opts.MusicVolume

		this.AnisotropyLevels = this.opts.AnisotropyLevels()
		local anisotropy = { "NONE" }
		for (i in this.AnisotropyLevels)
			table.insert(anisotropy, tostring(i) + "x AF");
	
		this.MSAA = val_selection(e.msaa_left, e.msaa_right, e.msaa_display, msaa_levels, 1, msaa_to_idx(this.opts.MaxMSAA()), msaa_to_idx(this.opts.MSAA))
		this.AF = val_selection(e.af_left, e.af_right, e.af_display, anisotropy, 1, anisotropy.length, anisotropy_to_idx(this.opts.Anisotropy))

		this.controlcategories = { e.cat_ship, e.cat_ui, e.cat_mp }
		e.cat_ship.OnClick(() => this.setcontrolcategory(1))
		e.cat_ui.OnClick(() => this.setcontrolcategory(2))
		e.cat_mp.OnClick(() => this.setcontrolcategory(3))
		e.ctrl_default.OnClick(() => this.keymap.DefaultBindings())
		e.ctrl_cancel.OnClick(() => this.keymap.ResetBindings())
	}

	do_goback()
	{
		local e = this.Elements
		this.opts.SfxVolume = e.sfxvol.Value
		this.opts.MusicVolume = e.musicvol.Value
		this.opts.VoiceVolume = e.voicevol.Value
		this.opts.MSAA = idx_to_msaa(this.MSAA.vcurrent)
		this.opts.Anisotropy = idx_to_anisotropy(this.AF.vcurrent)
		this.keymap.Save();
		Game.ApplySettings(this.opts)
		if (this.isModal) {
			Game.Resume()
			this.Close()
		} else {
			OpenScene("mainmenu")
		}
	}
	asmodal()
	{
		this.ModalInit()
		this.Elements.fllogo.Visible = false
		this.Elements.backdrop.Visible = true
		this.Elements.goback.Strid = STRID_RETURN_TO_GAME
		this.isModal = true
		return this;
	}

	panel(p)
	{
		for(panel in this.Panels) {
			if(panel[1] == p[1]) {
				panel[1].Selected = true;
				panel[2].Visible = true;
			} else {
				panel[1].Selected = false;
				panel[2].Visible = false;
			}
		}
	}

	setcontrolcategory(cat)
	{
		this.keymap.SetGroup(cat - 1);
		for (index, value in ipairs(this.controlcategories)) {
			value.Selected = index == cat
		}
	}
}
