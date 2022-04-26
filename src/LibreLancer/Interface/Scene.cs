// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class Scene : Container
    {
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (Visible)
            {
                Update(context, Vector2.Zero);
                Background?.Draw(context, parentRectangle);
                base.Render(context, parentRectangle);
            }
        }
        
        private Stylesheet currentSheet;
        public void ApplyStyles()
        {
            if(currentSheet != null) ApplyStylesheet(currentSheet);
        }

        public override void ApplyStylesheet(Stylesheet sheet)
        {
            currentSheet = sheet;
            base.ApplyStylesheet(sheet);
        }
    }
}