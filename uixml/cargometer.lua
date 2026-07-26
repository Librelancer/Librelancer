local CargoMeter = {}

local function cargo_segment_background(color)
{
	local renderable = NewObject("UiRenderable");
	local fill = NewObject("DisplayColor");
	fill.Color = GetColor(color);
	renderable.AddElement(fill);
	return renderable;
}

function CargoMeter.Create(panel)
{
	local meter = {
		segments = {}
	};
	for (i in 1..10) {
		local segment = NewObject("Panel");
		segment.X = (i - 1) * 7;
		segment.Y = 2;
		segment.Width = 6;
		segment.Height = 8;
		segment.Background = cargo_segment_background("#737780FF");

		local usedFill = NewObject("Panel");
		usedFill.Height = 8;
		usedFill.Background = cargo_segment_background("text");
		segment.Children.Add(usedFill);

		local previewFill = NewObject("Panel");
		previewFill.Height = 8;
		previewFill.Background = cargo_segment_background("yellow");
		segment.Children.Add(previewFill);

		panel.Children.Add(segment);
		meter.segments[i] = { usedFill, previewFill };
	}
	return meter;
}

local function cargo_meter_units(space, holdSize)
{
	if (holdSize <= 0 || space <= 0)
		return 0;
	return math.min(10, (space / holdSize) * 10);
}

local function cargo_fill_segment(fill, segmentStart, rangeStart, rangeEnd)
{
	local fillStart = math.max(segmentStart, rangeStart);
	local fillEnd = math.min(segmentStart + 1, rangeEnd);
	local amount = math.max(0, fillEnd - fillStart);
	fill.Visible = amount > 0;
	fill.X = (fillStart - segmentStart) * 6;
	fill.Width = amount * 6;
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

	for (i in 1..10) {
		local segmentStart = i - 1;
		cargo_fill_segment(meter.segments[i][1], segmentStart, 0, usedUnits);
		cargo_fill_segment(meter.segments[i][2], segmentStart, previewStart, previewEnd);
	}
}

return CargoMeter
