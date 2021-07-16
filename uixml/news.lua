require 'childwindow.lua'


local function news_list_item(strid)
	local li = NewObject("ListItem")
	-- Border
	li.Border = NewObject("UiRenderable")
	local wire = NewObject("DisplayWireBorder")
	wire.Color = GetColor("text")
	li.Border:AddElement(wire)
	li.HoverBorder = NewObject("UiRenderable")
	wire = NewObject("DisplayWireBorder")
	wire.Color = GetColor("slow_blue_yellow")
	li.HoverBorder:AddElement(wire)
	li.SelectedBorder = NewObject("UiRenderable")
	wire = NewObject("DisplayWireBorder")
	wire.Color = GetColor("yellow")
	li.SelectedBorder:AddElement(wire)
	-- Item
	li.ItemMarginX = 5
	li.ItemA = NewObject("Panel")
	li.ItemA.Background = NewObject("UiRenderable")
	li.ItemA.Width = 40
	local red = NewObject("DisplayColor")
	red.Color = GetColor("red")
	li.ItemA.Background:AddElement(red)
	li.ItemB = NewObject("Panel")
	local tb = NewObject("TextBlock")	
	tb.HorizontalAlignment = HorizontalAlignment.Left
	tb.TextColor = GetColor("text")
	tb.TextShadow = GetColor("black")
	tb.Fill = true
	tb.Strid = strid
	tb.MarginX = 3
	li.ItemB.Children:Add(tb)
	return li
end

function news:setstory(article)
	local e = self.Elements
	e.news_headline.Strid = article.Headline
	e.news_logo.Name = article.Logo
	e.news_text:SetString(StringFromID(article.Text))
end

function news:ctor()
	MakeChildWindow(self)
	local e = self.Elements
	e.close:OnClick(function() 
		self:Close() 
	end)
	self.Articles = Game:GetNewsArticles()
	for index, article in ipairs(self.Articles) do
		local item = news_list_item(article.Category)
		e.news_list.Children:Add(item)
	end
	self:setstory(self.Articles[1])
	e.news_list.SelectedIndex = 0
	e.news_list:OnSelectedIndexChanged(function()
		self:setstory(self.Articles[e.news_list.SelectedIndex + 1])
	end)
end





