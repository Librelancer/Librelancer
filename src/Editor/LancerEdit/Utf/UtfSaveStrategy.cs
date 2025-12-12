// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.ImUI;
using System;
using LibreLancer.ContentEdit;
using LibreLancer.Dialogs;

namespace LancerEdit
{
    /// <summary>
    /// Implements the save features for the UTF files
    /// </summary>
    internal class UtfSaveStrategy : ISaveStrategy
    {
        private readonly MainWindow window;
        private readonly UtfTab utfTab;

        public UtfSaveStrategy(MainWindow window, UtfTab utfTab)
        {
            this.window = window;
            this.utfTab = utfTab;
        }

        public bool ShouldSave => false;

        public void DrawMenuOptions()
        {
            if (Theme.IconMenuItem(Icons.Save, string.Format("Save '{0}'", utfTab.DocumentName), true))
            {
                Save(false);
            }
            if (Theme.IconMenuItem(Icons.Save, "Save As", true))
            {
                Save(true);
            }
        }

        public void Save()
        {
            Save(false);
        }

        internal void Save(bool forceSaveAs)
        {
            Action save = () =>
            {
                if (!forceSaveAs && !string.IsNullOrEmpty(utfTab.FilePath))
                {
                    var res = utfTab.Utf.Save(utfTab.FilePath, 0);
                    window.ResultMessages(res);
                    if(res.IsSuccess)
                        window.OnSaved();
                }
                else
                    RunSaveDialog();
            };
            if (utfTab.DirtyCountHp > 0 || utfTab.DirtyCountPart > 0 || utfTab.DirtyCountAnm > 0)
            {
                window.Confirm("This model has unapplied changes. Continue?", save);
            }
            else
                save();
        }

        internal void RunSaveDialog()
        {
            FileDialog.Save(f =>
            {
                var result = utfTab.Utf.Save(f, 0);
                window.ResultMessages(result);
                if (result.IsSuccess)
                {
                    window.OnSaved();
                    utfTab.DocumentName = System.IO.Path.GetFileName(f);
                    utfTab.UpdateTitle();
                    utfTab.FilePath = f;
                }
            }, AppFilters.UtfFilters);
        }
    }
}
