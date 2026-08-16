local CargoMeter = {}

local CARGO_BAR_WIDTH = 70
local CARGO_BAR_HEIGHT = 10

local function cargo_renderable(model, tint)
{
	local renderable = NewObject("UiRenderable")
	local element = NewObject("DisplayModel")
	element.Model = model
	if (tint != nil) {
		element.Tint = tint
		element.ForceTint = true
	}
	renderable.AddElement(element)
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
	usedFill.Background = cargo_renderable(GetModel("cargo_empty"))
	usedFill.Fill = cargo_renderable(GetModel("cargo_full"))

	local previewFill = NewObject("Gauge")
	previewFill.X = 0
	previewFill.Y = 2
	previewFill.Width = CARGO_BAR_WIDTH
	previewFill.Height = CARGO_BAR_HEIGHT
	previewFill.Fill = cargo_renderable(GetModel("cargo_full"), GetColor("yellow"))
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
