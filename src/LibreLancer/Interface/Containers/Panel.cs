// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Graphics;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class Panel : Container
    {
        public bool CaptureMouse { get; set; } = true;

        public override void OnMouseClick(UiContext context)
        {
            if (!Visible) return;
            foreach (var child in Children)
                child.OnMouseClick(context);
        }

        public override void OnMouseDown(UiContext context)
        {
            if (!Visible) return;
            foreach(var child in Children)
                child.OnMouseDown(context);
        }

        public override void OnMouseUp(UiContext context)
        {
            if (!Visible) return;
            foreach(var child in Children)
                child.OnMouseUp(context);
        }

        public override void OnMouseDoubleClick(UiContext context)
        {
            if (!Visible) return;
            foreach(var child in Children)
                child.OnMouseDoubleClick(context);
        }

        public override void OnMouseWheel(UiContext context, float delta)
        {
            if (!Visible) return;
            foreach(var child in Children)
                child.OnMouseWheel(context, delta);
        }

        public override void Render(UiContext context, double delta, DrawList2D drawList)
        {
            ProcessAddChildren(context);
            if (!Visible) return;
            Background?.Draw(context, drawList, ClientRectangle);
            foreach(var child in Children)
                child.Render(context, delta, drawList);
            Border?.Draw(context, drawList, ClientRectangle);
        }

        public override bool MouseWanted(UiContext context, float x, float y)
        {
            if (!Visible || !CaptureMouse) return false;
            if (Background != null && ClientRectangle.Contains(x, y)) return true;
            return base.MouseWanted(context, x, y);
        }
    }
}
