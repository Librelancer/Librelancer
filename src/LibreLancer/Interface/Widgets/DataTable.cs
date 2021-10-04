// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
using System.Numerics;
using MoonSharp.Interpreter;

namespace LibreLancer.Interface
{
    public interface ITableData
    {
        int Count { get; }
        int Selected { get; set; }
        string GetContentString(int row, string column);
        bool ValidSelection();
    }
    
    [UiLoadable]
    [MoonSharpUserData]
    public class TableColumn
    {
        InfoTextAccessor txtAccess = new InfoTextAccessor();
        public string Label
        {
            get => txtAccess.Text;
            set => txtAccess.Text = value;
        }
        public int Strid
        {
            get => txtAccess.Strid;
            set => txtAccess.Strid = value;
        }
        public int InfoId
        {
            get => txtAccess.InfoId;
            set => txtAccess.InfoId = value;
        }
        public string Data { get; set; }
        public int InitialWidthPercent { get; set; }

        public HorizontalAlignment TextAlignment { get; set; } = HorizontalAlignment.Center;
        public bool Clip { get; set; } = true;

        public string GetLabel(UiContext context) => txtAccess.GetText(context);
    }
    
    [UiLoadable]
    [MoonSharpUserData]
    public class DataTable : UiWidget
    {
        public List<TableColumn> Columns { get; set; } = new List<TableColumn>();
        public string BodyFont { get; set; } = "$ListText";
        public int BodyTextSize { get; set; }
        public InterfaceColor BodyColor { get; set; }
        public InterfaceColor BodyHover { get; set; }
        public InterfaceColor SelectedColor { get; set; }
        public string HeaderFont { get; set; } = "$Header";
        public int HeaderTextSize { get; set; } = 14;
        public InterfaceColor HeaderColor { get; set; }
        public InterfaceColor LineColor { get; set; }
        public InterfaceColor LineHover { get; set; }
        public InterfaceColor LineDown { get; set; }
        public InterfaceColor TextShadow { get; set; }
        public int DisplayRowCount { get; set; } = 5;

        public bool ShowHeaders { get; set; } = true;
        public bool ShowColumnBorders { get; set; } = true;

        private ITableData data;
        private float[] dividerPositions;
        void GenerateDividerPositions()
        {
            if (Columns.Count < 1)
            {
                dividerPositions = null;
                return;
            }
            dividerPositions = new float[Columns.Count - 1];
            int pct = 0;
            for (int i = 0; i < Columns.Count - 1; i++)
            {
                pct += Columns[i].InitialWidthPercent;
                dividerPositions[i] = pct / 100f;
            }
        }

        public void Reset()
        {
            GenerateDividerPositions();
        }

        public void SetData(ITableData data)
        {
            this.data = data;
        }

        private int dragging = -1;

        RectangleF GetDividerRect(int index, RectangleF rect)
        {
            var realPos = rect.X + dividerPositions[index] * rect.Width;
            var dragRect = new RectangleF(
                realPos - 1,
                rect.Y,
                2,
                rect.Height);
            return dragRect;
        }
        public override void OnMouseDown(UiContext context, RectangleF parentRectangle)
        {
            if (Width <= 0 || Height <= 0) return;
            if (!Visible) return;
            var rect = GetMyRectangle(context, parentRectangle);
            if (rect.Contains(context.MouseX, context.MouseY))
            {
                if (dragging == -1) {
                    if (ShowColumnBorders)
                    {
                        for (int i = 0; i < dividerPositions.Length; i++)
                        {
                            var dragRect = GetDividerRect(i, rect);
                            if (dragRect.Contains(context.MouseX, context.MouseY))
                            {
                                dragging = i;
                                break;
                            }
                        }
                    }
                }
            }
        }
        bool CanDragTo(int i, float pos, RectangleF rect)
        {
            float posm1 = (i - 1 < 0) ? 0 : dividerPositions[i - 1];
            float pos1 = (i + 1 >= dividerPositions.Length) ? 1 : dividerPositions[i + 1];
            float xm1 = posm1 * rect.Width;
            float x = pos * rect.Width;
            float x1 = pos1 * rect.Width;

            return (x - xm1) > 6 &&
                   (x1 - x) > 6;
        }
        
        public override void OnMouseClick(UiContext context, RectangleF parentRectangle)
        {
            if (Width <= 0 || Height <= 0) return;
            if (!Visible) return;
            if (data == null) return;
            var rect = GetMyRectangle(context, parentRectangle);
            var rowCount = Math.Min(DisplayRowCount, data.Count);
            for (int row = 0; row < rowCount; row++)
            {
                for (int column = 0; column < Columns.Count; column++)
                {
                    var str = data.GetContentString(row, Columns[column].Data);
                    if (string.IsNullOrWhiteSpace(str)) continue;
                    var c = GetCell(rect, row, column);
                    if (c.Contains(context.MouseX, context.MouseY))
                    {
                        data.Selected = row;
                        onSelect?.Invoke();
                        break;
                    }
                }
            }
        }

        private Action onSelect;
        public void OnItemSelected(Closure c)
        {
            onSelect = () => c.Call();
        }
        public override void OnMouseUp(UiContext context, RectangleF parentRectangle)
        {
            dragging = -1;
        }

        RectangleF GetCell(RectangleF parentRect, int row, int column)
        {
            var lineHeight = parentRect.Height / (DisplayRowCount + 1);
            var y = parentRect.Y + (row + 1) * lineHeight;
            float x = 0;
            if (column > 0)
            {
                x = dividerPositions[column - 1] * parentRect.Width;
            }
            if (column == (Columns.Count - 1)) {
                return new RectangleF(parentRect.X + x + 1, y + 1, parentRect.Width - x - 2, lineHeight - 2);   
            }
            var width = (dividerPositions[column] * parentRect.Width) - x;
            return new RectangleF(parentRect.X + x + 1, y + 1, width - 2, lineHeight - 2);
        }

        private CachedRenderString[] columnStrings;
        private CachedRenderString[][] rowStrings;
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (Width <= 0 || Height <= 0) return;
            if (!Visible) return;
            if (dividerPositions == null) GenerateDividerPositions();
            if (dividerPositions == null) return;
            var rect = GetMyRectangle(context, parentRectangle);
            Background?.Draw(context, rect);
            context.Mode2D();
            //Handle resizing columns
            if (dragging != -1 && context.MouseLeftDown)
            {
                var pct = MathHelper.Clamp((context.MouseX - rect.X) / (float) rect.Width, 0, 1);
                if (CanDragTo(dragging, pct, rect))
                {
                    dividerPositions[dragging] = pct;
                }
            }
            //Draw headers
            if (ShowHeaders)
            {
                if (columnStrings == null || columnStrings.Length != Columns.Count)
                    columnStrings = new CachedRenderString[Columns.Count];
                for (int i = 0; i < Columns.Count; i++)
                {
                    var c = GetCell(rect, -1, i);
                    DrawText(context, ref columnStrings[i], c, HeaderTextSize, HeaderFont,
                        HeaderColor ?? InterfaceColor.White, TextShadow,
                        HorizontalAlignment.Center, VerticalAlignment.Default,
                        true, Columns[i].GetLabel(context));
                }
            }
            //Draw content
            if (data != null)
            {
                var rowCount = Math.Min(DisplayRowCount, data.Count);
                if (rowStrings == null || rowCount != rowStrings.Length || (rowStrings.Length > 0 && rowStrings[0].Length != Columns.Count))
                {
                    rowStrings = new CachedRenderString[rowCount][];
                    for (int i = 0; i < rowCount; i++) rowStrings[i] = new CachedRenderString[Columns.Count];
                }
                for (int row = 0; row < rowCount; row++)
                {
                    //Process hovering on a row
                    bool hovered = false;
                    if (data.Selected != row)
                    {
                        for (int column = 0; column < Columns.Count; column++)
                        {
                            var str = data.GetContentString(row, Columns[column].Data);
                            if (string.IsNullOrWhiteSpace(str)) continue;
                            var c = GetCell(rect, row, column);
                            if (c.Contains(context.MouseX, context.MouseY))
                            {
                                hovered = true;
                                break;
                            }
                        }
                    }
                    //Render the row
                    var rowColor = Cascade(BodyColor ?? InterfaceColor.White, hovered ? BodyHover : null,
                        data.Selected == row ? SelectedColor : null);
                    for (int column = 0; column < Columns.Count; column++)
                    {
                        var str = data.GetContentString(row, Columns[column].Data);
                        if (string.IsNullOrWhiteSpace(str)) continue;
                        var c = GetCell(rect, row, column);
                        DrawText(context, ref rowStrings[row][column], c, BodyTextSize, BodyFont, rowColor, TextShadow, Columns[column].TextAlignment,
                            VerticalAlignment.Default, Columns[column].Clip, str);
                    }
                }
            }

            //Draw dividers
            if (ShowColumnBorders)
            {
                for (int i = 0; i < dividerPositions.Length; i++)
                {
                    var x = rect.X + dividerPositions[i] * rect.Width;
                    var y1 = rect.Y;
                    var y2 = rect.Y + rect.Height;
                    InterfaceColor dragCol = (dragging == i) ? LineDown : null;
                    InterfaceColor overCol = null;
                    var dragRect = GetDividerRect(i, rect);
                    if (dragRect.Contains(context.MouseX, context.MouseY)) overCol = LineHover;
                    var color =
                        (Cascade(LineColor ?? InterfaceColor.White, overCol, dragCol)).GetColor(context.GlobalTime);
                    context.RenderContext.Renderer2D.DrawLine(color, context.PointsToPixels(new Vector2(x, y1)),
                        context.PointsToPixels(new Vector2(x, y2)));
                }
            }
            //Draw row lines
            var lineHeight = rect.Height / (DisplayRowCount + 1);
            var x1 = rect.X;
            var x2 = rect.X + rect.Width;
            var lineColor = (LineColor ?? InterfaceColor.White).GetColor(context.GlobalTime);
            //Headers
            context.RenderContext.Renderer2D.DrawLine(lineColor, context.PointsToPixels(new Vector2(x1, rect.Y + lineHeight)),
                context.PointsToPixels(new Vector2(x2, rect.Y + lineHeight)));
            //Rows
            for (int i = 0; i < DisplayRowCount; i++)
            {
                var h = rect.Y + lineHeight * (i + 2);
                context.RenderContext.Renderer2D.DrawLine(lineColor, context.PointsToPixels(new Vector2(x1, h)),
                    context.PointsToPixels(new Vector2(x2, h)));
            }
            Border?.Draw(context, rect);
        }
        RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            Update(context, myPos);
            myPos = AnimatedPosition(myPos);
            var myRect = new RectangleF(myPos.X,myPos.Y, Width, Height);
            return myRect;
        }
    }
}