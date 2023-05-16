// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.ImUI;

namespace LancerEdit;

public enum Hotkeys
{
    Deselect,
    ResetViewport,
    ToggleGrid,
    ChangeSystem
}

public abstract class EditorTab : DockTab
{ 
    public ISaveStrategy SaveStrategy { get; set; } = new NoSaveStrategy();

    public virtual void DetectResources(List<MissingReference> missing, List<uint> matrefs, List<string> texrefs)
    {
    }

    public virtual void OnHotkey(Hotkeys hk)
    {
    }
}