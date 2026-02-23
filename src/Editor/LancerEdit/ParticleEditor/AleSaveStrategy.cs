// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.ImUI;
using System;
using System.Linq;
using LibreLancer.ContentEdit;
using LibreLancer.ContentEdit.Ale;
using LibreLancer.Dialogs;
using LibreLancer.Utf.Ale;

namespace LancerEdit
{
    /// <summary>
    /// Implements the save features for the UTF files
    /// </summary>
    internal class AleSaveStrategy : ISaveStrategy
    {
        private readonly MainWindow window;
        private readonly AleEditor aleTab;

        public AleSaveStrategy(MainWindow window, AleEditor aleTab)
        {
            this.window = window;
            this.aleTab = aleTab;
        }

        public bool ShouldSave => false;

        public void DrawMenuOptions()
        {
            if (Theme.IconMenuItem(Icons.Save, string.Format("Save '{0}'", aleTab.DocumentName), true))
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

        EditResult<UtfStatistics> WriteTo(string path)
        {
            var fxlibNode = aleTab.Utf.Root.Children
                .First(x => x.Name.Equals("ALEffectLib", StringComparison.OrdinalIgnoreCase))
                .Children.First(x => x.Name.Equals("ALEffectLib", StringComparison.OrdinalIgnoreCase));
            var nodelibNode =aleTab.Utf.Root.Children
                .First(x => x.Name.Equals("AlchemyNodeLibrary", StringComparison.OrdinalIgnoreCase))
                .Children.First(x => x.Name.Equals("AlchemyNodeLibrary", StringComparison.OrdinalIgnoreCase));
            var (fxlib, nodelib) = aleTab.ParticleFile.Serialize();
            fxlibNode.Data = AleNodeWriter.WriteALEffectLib(fxlib);
            nodelibNode.Data = AleNodeWriter.WriteAlchemyNodeLibrary(nodelib);
            return aleTab.Utf.Save(path, 0);
        }


        internal void Save(bool forceSaveAs)
        {
            if (!forceSaveAs && !string.IsNullOrEmpty(aleTab.FilePath))
            {
                var res = WriteTo(aleTab.FilePath);
                window.ResultMessages(res);
                if(res.IsSuccess)
                    window.OnSaved();
            }
            else
                RunSaveDialog();
        }

        internal void RunSaveDialog()
        {
            FileDialog.Save(f =>
            {
                var result = WriteTo(f);
                window.ResultMessages(result);
                if (result.IsSuccess)
                {
                    window.OnSaved();
                    aleTab.DocumentName = System.IO.Path.GetFileName(f);
                    aleTab.UpdateTitle();
                    aleTab.FilePath = f;
                }
            }, AppFilters.UtfFilters);
        }
    }
}
