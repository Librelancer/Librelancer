// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.ImUI;


namespace InterfaceEdit
{
    public abstract class SaveableTab : DockTab
    {
        public virtual string Filename { get; }
        public virtual void Save()
        {
        }
    }
}