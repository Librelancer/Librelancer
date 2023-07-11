// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.ImUI;

namespace LancerEdit
{
    /// <summary>
    /// This save strategy is used when the EditorTab has no ability to save
    /// </summary>
    internal class NoSaveStrategy : ISaveStrategy
    {
        public void DrawMenuOptions()
        {
            Theme.IconMenuItem(Icons.Save, "Save", false);
            Theme.IconMenuItem(Icons.Save, "Save As", false);
        }

        public void Save()
        {
            // Does nothing
        }

        public bool ShouldSave => false;

        public static NoSaveStrategy Instance = new NoSaveStrategy();
    }
}
