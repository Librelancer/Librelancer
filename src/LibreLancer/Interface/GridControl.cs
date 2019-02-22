// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer
{
    public interface IGridContent
    {
        int Count { get; }
        int Selected { get; set; }
        string GetContentString(int row, int column);
    }
    public class GridControl
    {
        float[] dividerPositions;
        string[] columnTitles;
        Func<Rectangle> getRect;
        List<GridHitRect> children = new List<GridHitRect>();
        XmlUIManager manager;
        IGridContent content;
        int rowCount;

        Font headerFont;
        Font contentFont;

        public Color4 TextColor = new Color4(160, 196, 210, 255);
        public Color4 BorderColor = new Color4(160, 196, 210, 255);

        public GridControl(XmlUIManager manager, float[] dividerPositions, string[] columnTitles, Func<Rectangle> getRect, IGridContent content, int rowCount)
        {
            this.dividerPositions = dividerPositions;
            this.getRect = getRect;
            this.rowCount = rowCount;
            this.content = content;
            this.columnTitles = columnTitles;
            this.manager = manager;
            for (int i = -1; i < dividerPositions.Length; i++)
            {
                for (int j = 0; j < rowCount; j++)
                    children.Add(new GridHitRect(j, i, this));
            }
            headerFont = manager.Game.Fonts.GetSystemFont("Agency FB");
            contentFont = manager.Game.Fonts.GetSystemFont("Arial Unicode MS");
        }

        class GridHitRect
        {
            public int Row;
            public int Divider;
            public GridControl List;

            public GridHitRect(int row, int divider, GridControl lst)
            {
                Row = row;
                Divider = divider;
                List = lst;
            }
            public Rectangle GetHitRectangle()
            {
                float pos0 = Divider < 0 ? 0 : List.dividerPositions[Divider];
                float pos1 = (Divider + 1 >= List.dividerPositions.Length) ? 1 : List.dividerPositions[Divider + 1];

                var srcrect = List.getRect();

                float x0 = srcrect.X + pos0 * srcrect.Width;
                float x1 = srcrect.X + pos1 * srcrect.Width;

                var rowSize = srcrect.Height / List.rowCount;

                var y = srcrect.Y + Row * rowSize;
                return new Rectangle(
                    (int)x0 + 4,
                    (int)y,
                    (int)(x1 - x0) - 4,
                    (int)rowSize
                );

            }
                
            public void WasClicked()
            {
                if (Row < List.content.Count) List.content.Selected = Row;
            }
        }

        bool lastDown = false;
        int dragging = -1;

        public void Update()
        {
            var rect = getRect();
            var mouse = manager.Game.Mouse;
            if (!mouse.IsButtonDown(MouseButtons.Left))
            {
                dragging = -1;
                lastDown = false;
            }
            if (mouse.IsButtonDown(MouseButtons.Left) && dragging > -1)
            {
                var pct = MathHelper.Clamp((mouse.X - rect.X) / (float)rect.Width, 0, 1);
                if (CanDragTo(dragging, pct))
                    dividerPositions[dragging] = pct;
            }
            if (mouse.IsButtonDown(MouseButtons.Left) && !lastDown)
            {
                lastDown = true;
                for (int i = 0; i < dividerPositions.Length; i++)
                {
                    var realPos = (int)(rect.X + dividerPositions[i] * rect.Width);
                    var dragRect = new Rectangle(
                        realPos - 2,
                        rect.Y,
                        4,
                        rect.Height);
                    if (dragRect.Contains(mouse.X, mouse.Y))
                    {
                        dragging = i;
                        break;
                    }
                }
            }
        }

        GridHitRect moused;

        public void OnMouseDown()
        {
            moused = GetHit();
        }

        public void OnMouseUp()
        {
            if (moused != null)
            {
                var elem2 = GetHit();
                if (moused == elem2) moused.WasClicked();
            }
        }

        GridHitRect GetHit()
        {
            foreach (var c in children)
            {
                if (c.GetHitRectangle().Contains(manager.Game.Mouse.X, manager.Game.Mouse.Y))
                    return c;
            }
            return null;
        }
        public void Draw()
        {
            //Get Resources
            var rect = getRect();
            var rowSize = rect.Height / (float)rowCount;
            var textSize = (rowSize * 0.8f) * (72.0f / 96.0f);
            //Draw Lines
            for (int i = 0; i < rowCount; i++)
            {
                manager.Game.Renderer2D.DrawLine(BorderColor, new Vector2(rect.X, rect.Y + rowSize * i), new Vector2(rect.X + rect.Width, rect.Y + rowSize * i));
            }
            manager.Game.Renderer2D.DrawLine(BorderColor, new Vector2(rect.X, rect.Y + rowSize * rowCount), new Vector2(rect.X + rect.Width, rect.Y + rowSize * rowCount));
            for (int i = 0; i < dividerPositions.Length; i++)
            {
                manager.Game.Renderer2D.DrawLine(
                    TextColor,
                    new Vector2(rect.X + dividerPositions[i] * rect.Width, rect.Y),
                    new Vector2(rect.X + dividerPositions[i] * rect.Width, rect.Y + rect.Height)
                );
            }
            //Draw Content
            for (int i = -1; i < dividerPositions.Length; i++)
            {
                float pos0 = i < 0 ? 0 : dividerPositions[i];
                float pos1 = (i + 1 >= dividerPositions.Length) ? 1 : dividerPositions[i + 1];

                float x0 = rect.X + pos0 * rect.Width;
                float x1 = rect.X + pos1 * rect.Width;
                var str = columnTitles[i + 1];
                var titleRect = new Rectangle(
                    (int)x0,
                    rect.Y - (int)(headerFont.LineHeight(textSize)),
                    (int)(x1 - x0),
                    (int)headerFont.LineHeight(textSize)
                );
                DrawTextCentered(headerFont, textSize, str, titleRect, TextColor);
                for (int j = 0; j < content.Count; j++)
                {
                    var y = rect.Y + j * rowSize;
                    var contentRect = new Rectangle(
                        (int)x0,
                        (int)y,
                        (int)(x1 - x0),
                        (int)rowSize
                    );
                    var contentStr = content.GetContentString(j, i + 1);
                    if (contentStr != null)
                    {
                        DrawTextCentered(contentFont, textSize * 0.7f, contentStr, contentRect, content.Selected == j ? Color4.Yellow : TextColor);
                    }
                }
            }

        }

        void DrawShadowedText(Font font, float sz, string text, float x, float y, Color4 c)
        {
            manager.Game.Renderer2D.DrawString(font, sz, text, x + 2, y + 2, Color4.Black);
            manager.Game.Renderer2D.DrawString(font, sz, text, x, y, c);
        }

        void DrawTextCentered(Font font, float sz, string text, Rectangle rect, Color4 c)
        {
            var size = manager.Game.Renderer2D.MeasureString(font, sz, text);
            var pos = new Vector2(
                rect.X + (rect.Width / 2f - size.X / 2),
                rect.Y + (rect.Height / 2f - size.Y / 2)
            );
            DrawShadowedText(font, sz, text, pos.X, pos.Y, c);
        }

        bool CanDragTo(int i, float pos)
        {
            float posm1 = (i - 1 < 0) ? 0 : dividerPositions[i - 1];
            float pos1 = (i + 1 >= dividerPositions.Length) ? 1 : dividerPositions[i + 1];
            var rect = getRect();

            float xm1 = posm1 * rect.Width;
            float x = pos * rect.Width;
            float x1 = pos1 * rect.Width;

            return (x - xm1) > 6 &&
                (x1 - x) > 6;
        }
    }
}
