require 'childwindow.lua'

local paths_3db = {
	world = "INTERFACE/NEURONET/NEWSVENDOR/news_worldnews.3db",
	critical = "INTERFACE/NEURONET/NEWSVENDOR/news_missioncriticalnews.3db",
	system = "INTERFACE/NEURONET/NEWSVENDOR/news_system.3db",
	universe = "INTERFACE/NEURONET/NEWSVENDOR/news_universe.3db",
	faction = "INTERFACE/NEURONET/NEWSVENDOR/news_universe.3db",
}

local function control_icon_3db(name, control)
{
	control.Background = NewObject("UiRenderable")
	local colorElem = NewObject("DisplayColor")
	colorElem.Color = GetColor("black")
	control.Background.AddElement(colorElem)
	local elem = NewObject("DisplayModel")
	elem.Tint = GetColor("text")
	local model = NewObject("InterfaceModel")
	model.Path = paths_3db[name]
	model.XScale = 32
	model.YScale = 32
	elem.Model = model
	control.Background.AddElement(elem)
}

local function news_list_item(icon, strid)
{
	local li = NewObject("ListItem")
	// Border
	li.Border = NewObject("UiRenderable")
	local wire = NewObject("DisplayWireBorder")
	wire.Color = GetColor("text")
	li.Border.AddElement(wire)
	li.HoverBorder = NewObject("UiRenderable")
	wire = NewObject("DisplayWireBorder")
	wire.Color = GetColor("slow_blue_yellow")
	li.HoverBorder.AddElement(wire)
	li.SelectedBorder = NewObject("UiRenderable")
	wire = NewObject("DisplayWireBorder")
	wire.Color = GetColor("yellow")
	li.SelectedBorder.AddElement(wire)
	// Item
	li.ItemMarginX = 14
	li.ItemA = NewObject("Panel")
	control_icon_3db(icon, li.ItemA)
	li.ItemA.Width = 26
	li.ItemB = NewObject("Panel")
	local tb = NewObject("TextBlock")	
	tb.HorizontalAlignment = HorizontalAlignment.Left
	tb.TextColor = GetColor("text")
	tb.TextShadow = GetColor("black")
	tb.Fill = true
	tb.Strid = strid
	tb.MarginX = 3
	li.ItemB.Children.Add(tb)
	return li
}

class news : news_Designer with ChildWindow
{
	news()
	{
		base();
		this.ChildWindowInit();
		local e = this.Elements;
		e.close.OnClick(() => this.Close());
		this.Articles = Game.GetNewsArticles();
		for (article in this.Articles) {
			local item = news_list_item(article.Icon, article.Category);
			e.news_list.Children.Add(item);
		}
		this.setstory(this.Articles[1]);
		e.news_list.SelectedIndex = 0;
		e.news_list.OnSelectedIndexChanged(() => this.setstory(this.Articles[e.news_list.SelectedIndex + 1]));
	}
	setstory(article)
	{
		local e = this.Elements
		e.news_headline.Strid = article.Headline
		e.news_logo.Name = article.Logo
		e.news_text.SetString(StringFromID(article.Text))
		control_icon_3db(article.Icon, e.news_icon)
	}
}