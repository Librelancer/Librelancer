// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LancerEdit
{
    /// <summary>
    /// Specifies the saving method for a particular EditorTab
    /// </summary>
    public interface ISaveStrategy
    {
        /// <summary>
        /// Saves the currently loaded file
        /// </summary>
        void Save();

        /// <summary>
        /// Draws the menu options in the File menu
        /// </summary>
        void DrawMenuOptions();

        /// <summary>
        /// Specifies whether the tab should have an asterisk drawn
        /// </summary>
        bool ShouldSave { get; }
    }
}
