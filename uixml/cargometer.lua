local CargoMeter = {}

local CARGO_BAR_PATH = "INTERFACE/MULTIPLAYER/TRADE/"
local CARGO_BAR_WIDTH = 70
local CARGO_BAR_HEIGHT = 10
local CARGO_MODEL_XSCALE = 16.4
local CARGO_MODEL_YSCALE = 123

local function cargo_model(path, tint)
{
	local model = NewObject("InterfaceModel")
	model.Path = CARGO_BAR_PATH + path
	model.XScale = CARGO_MODEL_XSCALE
	model.YScale = CARGO_MODEL_YSCALE

	local element = NewObject("DisplayModel")
	element.Model = model
	if (tint != nil) {
		element.Tint = tint
		element.ForceTint = true
	}
	return element
}

local function cargo_renderable(path, tint)
{
	local renderable = NewObject("UiRenderable")
	renderable.AddElement(cargo_model(path, tint))
	return renderable
}

function CargoMeter.Create(panel)
{
	local meter = {}
	local usedFill = NewObject("Gauge")
	usedFill.X = 0
	usedFill.Y = 2
	usedFill.Width = CARGO_BAR_WIDTH
	usedFill.Height = CARGO_BAR_HEIGHT
	usedFill.Background = cargo_renderable("trade_cargoempty.3db")
	usedFill.Fill = cargo_renderable("trade_cargofull.3db")

	local previewFill = NewObject("Gauge")
	previewFill.X = 0
	previewFill.Y = 2
	previewFill.Width = CARGO_BAR_WIDTH
	previewFill.Height = CARGO_BAR_HEIGHT
	previewFill.Fill = cargo_renderable("trade_cargofull.3db", GetColor("yellow"))
	previewFill.Visible = false

	panel.Children.Add(usedFill)
	panel.Children.Add(previewFill)
	meter.used = usedFill
	meter.preview = previewFill
	return meter;
}

local function cargo_meter_units(space, holdSize)
{
	if (holdSize <= 0 || space <= 0)
		return 0;
	return math.min(10, (space / holdSize) * 10);
}

function CargoMeter.Update(meter, holdSize, usedSpace, previewSpace, previewInside)
{
	usedSpace = math.max(0, math.min(usedSpace, holdSize));
	previewSpace = math.max(0, previewSpace);
	local usedUnits = cargo_meter_units(usedSpace, holdSize);
	local previewUnits = cargo_meter_units(previewSpace, holdSize);
	local previewStart = usedUnits;
	local previewEnd = math.min(10, usedUnits + previewUnits);

	if (previewInside) {
		previewEnd = usedUnits;
		previewStart = math.max(0, usedUnits - previewUnits);
	}

	meter.used.PercentStart = 0;
	meter.used.PercentFilled = usedUnits / 10;
	meter.preview.PercentStart = previewStart / 10;
	meter.preview.PercentFilled = (previewEnd - previewStart) / 10;
	meter.preview.Visible = previewEnd > previewStart;
}

return CargoMeter
