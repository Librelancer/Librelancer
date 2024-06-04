// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.ImUI;

namespace LancerEdit;

public enum Hotkeys
{
    Deselect = 1,
    ResetViewport,
    ToggleGrid,
    ChangeSystem,
    Cut,
    Copy,
    Paste,
    ClearRotation,
    Undo,
    Redo,
}

public abstract class EditorTab : DockTab
{
    public ISaveStrategy SaveStrategy { get; set; } = new NoSaveStrategy();

    public override bool UnsavedDocument => SaveStrategy.ShouldSave;

    public virtual void DetectResources(List<MissingReference> missing, List<uint> matrefs, List<TextureReference> texrefs)
    {
    }

    public virtual void OnHotkey(Hotkeys hk, bool shiftPressed)
    {
    }
}
