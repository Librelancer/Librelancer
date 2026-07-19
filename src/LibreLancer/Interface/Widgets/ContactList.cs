using System;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Text;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class ContactList : UiWidget
    {
        public string Font { get; set; } = "$ListText";
        public int TextSize { get; set; }

        public InterfaceColor? FriendlyColor { get; set; }

        public InterfaceColor? HostileColor { get; set; }
        public InterfaceColor? WaypointColor { get; set; }
        public InterfaceColor? NeutralColor { get; set; }
        public InterfaceColor? HoverColor { get; set; }
        public InterfaceColor? SelectedColor { get; set; }
        public InterfaceColor? TextShadow { get; set; }

        public int DisplayRowCount { get; set; }

        private IContactListData? data;
        private int childOffset = 0;
        private int _lastScroll = 0;
        private CachedRenderString[]? rowLabels;
        private CachedRenderString[]? rowDistances;

        private CachedRenderString? distanceWidth;

        private ContactListStyle listStyle = new();

        UiRenderable? GetIcon(ContactIcon icon) => icon switch
        {
            ContactIcon.WeaponPlatform => listStyle.WeaponPlatform,
            ContactIcon.LootableDepot => listStyle.LootableDepot,
            ContactIcon.Loot => listStyle.Loot,
            ContactIcon.Nomad => listStyle.Nomad,
            ContactIcon.Transport => listStyle.Transport,
            ContactIcon.Freighter => listStyle.Freighter,
            ContactIcon.Tradelane => listStyle.Tradelane,
            ContactIcon.Jumpgate => listStyle.Jumpgate,
            ContactIcon.Battleship => listStyle.Battleship,
            ContactIcon.Waypoint => listStyle.Waypoint,
            ContactIcon.Station => listStyle.Station,
            ContactIcon.Planet => listStyle.Planet,
            ContactIcon.Ship => listStyle.Ship,
            ContactIcon.OtherPlayer => listStyle.OtherPlayer,
            _ => listStyle.WeaponPlatform
        } ?? listStyle.WeaponPlatform;

        // Remove in future refactor
        static T? Cascade<T>(T? style, T? style2, T? self) where T : class => (self ?? style2 ?? style);

        public Scrollbar Scrollbar { get; set; } = new();

        protected override ElementStyle OnRestyle(UiContext context)
        {
            listStyle = new StyleResolver()
                .Add(context.Data.Stylesheet?.Styles.DefaultStyle<ContactListStyle>())
                .Add(Style)
                .Add(WidthProperty)
                .Add(HeightProperty)
                .Add(BackgroundProperty)
                .Add(BorderProperty)
                .Create<ContactListStyle>();
            return listStyle;
        }

        public override void OnLayout(UiContext context, Layout layout, double delta)
        {
            base.OnLayout(context, layout, delta);
            Scrollbar.OnLayout(context, new Layout(ClientRectangle), delta);
        }

        public override void Update(UiContext context, double delta)
        {
            base.Update(context, delta);
            Scrollbar.Update(context, delta);
        }

        public void SetData(IContactListData data)
        {
            this.data = data;
        }

        private int ScrollCount()
        {
            if (data == null)
            {
                return 0;
            }

            int c = data.Count - DisplayRowCount;
            return c <= 0 ? 0 : c;
        }

        private RectangleF GetCell(RectangleF parentRect, int row)
        {
            var lineHeight = parentRect.Height / DisplayRowCount;
            var y = parentRect.Y + row * lineHeight;
            var width = parentRect.Width;
            return new RectangleF(parentRect.X + 1, y + 1, width - 2, lineHeight - 2);
        }

        private bool CanRender()
        {
            return Width > 0 && Height > 0 &&
                   Visible;
        }

        public override void OnMouseWheel(UiContext context, float delta)
        {
            if (!CanRender() || data == null) return;
            var rect = ClientRectangle;
            if (rect.Contains(context.MouseX, context.MouseY))
                Scrollbar.OnMouseWheel(context, delta);
        }

        public override void OnMouseClick(UiContext context)
        {
            if (!CanRender() || data == null) return;
            var rect = ClientRectangle;

            if (_lastScroll > 0)
            {
                rect.Width -= Scrollbar.ClientRectangle.Width;
            }

            var rowCount = Math.Min(DisplayRowCount, (data.Count - childOffset));

            for (int row = 0; row < rowCount; row++)
            {
                var c = GetCell(rect, row);

                if (c.Contains(context.MouseX, context.MouseY) &&
                    row + childOffset < data.Count)
                {
                    data.SelectIndex(row + childOffset);
                }
            }
        }

        public override void OnMouseDown(UiContext context)
        {
            if (!CanRender() || data == null) return;
            Scrollbar.OnMouseDown(context);
        }

        public override void OnMouseUp(UiContext context)
        {
            if (!CanRender() || data == null) return;
            Scrollbar.OnMouseUp(context);
        }

        public override void Render(UiContext context, double delta, DrawList2D drawList)
        {
            if (!CanRender())
            {
                return;
            }
            var rect = ClientRectangle;
            Background?.Draw(context, drawList, rect);

            if (data != null)
            {
                // Update scrolling
                int scrollCount = ScrollCount();

                if (scrollCount <= 0)
                {
                    childOffset = 0;
                    Scrollbar.ScrollOffset = 0;
                    Scrollbar.Tick = 0;
                    Scrollbar.ThumbSize = 1;
                    _lastScroll = 0;
                }
                else if (scrollCount != _lastScroll)
                {
                    _lastScroll = scrollCount;
                    var tickAmount = 0.75f / (DisplayRowCount * 3);
                    Scrollbar.ThumbSize = 1.0f - (Math.Min(scrollCount, DisplayRowCount * 3) * tickAmount);
                    Scrollbar.Tick = 1.0f / scrollCount;
                    Scrollbar.ScrollOffset = childOffset / (float) scrollCount;
                }
                else
                {
                    childOffset = (int) (Scrollbar.ScrollOffset * scrollCount);
                }

                Scrollbar.Visible = scrollCount > 0;
                if (scrollCount > 0)
                {
                    Scrollbar.Render(context, delta, drawList);
                    rect.Width -= Scrollbar.ClientRectangle.Width;
                }

                var rowCount = Math.Min(DisplayRowCount, (data.Count - childOffset));
                if (rowLabels == null || rowLabels.Length < rowCount || rowDistances == null)
                {
                    rowLabels = new CachedRenderString[DisplayRowCount];
                    rowDistances = new CachedRenderString[DisplayRowCount];
                }

                var distSize = MeasureText(context, ref distanceWidth, GetCell(rect, 0),
                    TextSize, Font, TextShadow != null, "9999", 0);

                for (int row = 0; row < rowCount; row++)
                {
                    // Get row state
                    bool hovered = false;
                    var selected = data.IsSelected(row + childOffset);
                    var label = data.GetLabel(row + childOffset);
                    var distance = data.GetDistanceString(row + childOffset);
                    var icon = data.GetIcon(row + childOffset);
                    if (!selected)
                    {
                        var c = GetCell(rect, row);

                        if (c.Contains(context.MouseX, context.MouseY))
                        {
                            hovered = true;
                        }
                    }

                    InterfaceColor? textColor = null;

                    switch (data.GetAttitude(row + childOffset))
                    {
                        case RepAttitude.Friendly:
                            textColor = FriendlyColor;
                            break;
                        case RepAttitude.Hostile:
                            textColor = HostileColor;
                            break;
                        case RepAttitude.Neutral:
                            textColor = NeutralColor;
                            break;
                    }

                    if (WaypointColor != null && icon == ContactIcon.Waypoint)
                    {
                        textColor = WaypointColor;
                    }

                    // Render row
                    var rowColor = Cascade(textColor ?? InterfaceColor.White, hovered ? HoverColor : null,
                        selected ? SelectedColor : null) ?? InterfaceColor.White;
                    var rowRect = GetCell(rect, row);

                    var iconRect = rowRect with { Width = rowRect.Height };
                    GetIcon(icon)?.Draw(context, drawList, iconRect, rowColor.GetColor(context.GlobalTime));
                    rowRect.X += iconRect.Width + 4;
                    rowRect.Width -= iconRect.Width + 4;
                    rowRect.Height *= 2;

                    RenderText(context, drawList, ref rowDistances![row], rowRect, TextSize, Font, rowColor, TextShadow,
                        HorizontalAlignment.Left,
                        VerticalAlignment.Top, true, distance);
                    rowRect.X += distSize.X;
                    rowRect.Width -= distSize.X;

                    RenderText(context, drawList, ref rowLabels![row], rowRect, TextSize, Font, rowColor, TextShadow,
                        HorizontalAlignment.Left,
                        VerticalAlignment.Top, true, label);
                }
            }

            Border?.Draw(context, drawList, rect);
        }
    }
}
