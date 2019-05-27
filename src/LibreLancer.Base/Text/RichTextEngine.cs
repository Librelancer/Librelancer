// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    public abstract class RichTextEngine : IDisposable
    {
        public abstract void Dispose();
        public abstract void RenderText(BuiltRichText txt, int x, int y);
        public abstract BuiltRichText BuildText(string markup, int width);
    }
    public abstract class BuiltRichText : IDisposable
    {
        public abstract void Dispose();
    }
}
