// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;

namespace LibreLancer
{
    public class XmlUICharacterList : XmlUIPanel
    {
        const int NUM_ROWS = 12;

        GridControl grid;

        float[] dividerPositions =
        {
            0.327f,
            0.425f,
            0.653f,
            0.79f,
        };

        string[] columnTitles =
        {
            "CHARACTER NAME",
            "RANK",
            "FUNDS",
            "SHIP TYPE",
            "LOCATION",
        };
        public CharacterSelectInfo Info;

        public XmlUICharacterList(XInt.CharacterList sl, XInt.Style style, XmlUIScene scene) : base(style, scene, false)
        {
            Positioning = sl;
            ID = sl.ID;
            Lua = new CharacterListLua(this);
            grid = new GridControl(scene, dividerPositions, columnTitles, GetGridRect, new CharacterListContent(this), NUM_ROWS);
        }

        public override void OnMouseDown() => grid.OnMouseDown();
        public override void OnMouseUp() => grid.OnMouseUp();

        int _selected = -1;

        public int Selection
        {
            get { return _selected; }
            set { _selected = value; }
        }

        void UpdateSelection()
        {

        }

        class CharacterListContent : IGridContent
        {
            XmlUICharacterList charList;
            public CharacterListContent(XmlUICharacterList list) => charList = list;

            public int Count => charList.Info == null ? 0 : charList.Info.Characters.Count;

            public int Selected { get => charList._selected; set { charList._selected = value; charList.UpdateSelection(); } }

            public string GetContentString(int row, int column)
            {
                switch(column)
                {
                    case 0:
                        return charList.Info.Characters[row].Name;
                    case 1:
                        return charList.Info.Characters[row].Rank.ToString();
                    case 2:
                        return charList.Info.Characters[row].Funds.ToString();
                    case 3:
                        return charList.Info.Characters[row].Ship;
                    case 4:
                        return charList.Info.Characters[row].Location;
                }
                return null;
            }
        }
            

        public class CharacterListLua : PanelAPI
        {
            public XmlUICharacterList CharList;
            public CharacterListLua(XmlUICharacterList l) : base(l)
            {
                CharList = l;
            }
            public bool anyselected()
            {
                return CharList._selected != -1;
            }
        }

        Rectangle GetGridRect()
        {
            var pos = CalculatePosition();
            var sz = CalculateSize();
            return new Rectangle((int)pos.X, (int)pos.Y, (int)sz.X, (int)sz.Y);
        }

        protected override void UpdateInternal(TimeSpan delta, bool updateInput)
        {
            base.UpdateInternal(delta, updateInput);
            if (updateInput)
                grid.Update();
        }

        protected override void DrawInternal(TimeSpan delta)
        {
            base.DrawInternal(delta);
            Scene.Renderer2D.Start(Scene.GWidth, Scene.GHeight);
            grid.Draw();
            Scene.Renderer2D.Finish();
        }
    }
}
