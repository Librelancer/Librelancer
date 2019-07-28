// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer
{
    public abstract class RichTextEngine : IDisposable
    {
        public abstract void Dispose();
        public abstract void RenderText(BuiltRichText txt, int x, int y);
        public abstract BuiltRichText BuildText(IList<RichTextNode> nodes, int width, float sizeMultiplier = 1f);
    }
    public abstract class BuiltRichText : IDisposable
    {
        public abstract void Recalculate(float width);
        public abstract float Height { get; }
        public abstract void Dispose();
    }
}
