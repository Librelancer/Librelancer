using LibreLancer.ImUI;

namespace LancerEdit
{
    public sealed class ThornSaveStrategy : ISaveStrategy
    {
        private readonly ThornTab tab;

        public ThornSaveStrategy(ThornTab tab)
        {
            this.tab = tab;
        }

        public void DrawMenuOptions()
        {
            if (Theme.IconMenuItem(Icons.Save, string.Format("Save '{0}'", tab.DocumentName), true))
            {
                Save(false);
            }
            if (Theme.IconMenuItem(Icons.Save, "Save As...", true))
            {
                Save(true);
            }
            if (Theme.IconMenuItem(Icons.Export, "Export as " + (tab.IsSourceCode ? "Compiled Thorn" : "Lua Source") + "...", true))
            {
                Export();
            }
        }

        public void Save()
        {
            Save(false);
        }

        private void Save(bool forceSaveAs)
        {
            if (!forceSaveAs && !string.IsNullOrEmpty(tab.FilePath))
            {
                tab.Save(null);
            }
            else
            {
                FileDialog.Save(f =>
                {
                    tab.Save(f);
                }, FileDialogFilters.ThnFilters);
            }
        }

        private void Export()
        {
            FileDialog.Save(f =>
            {
                if (tab.IsSourceCode)
                {
                    tab.ExportCompiled(f);
                }
                else
                {
                    tab.ExportSource(f);
                }
            }, FileDialogFilters.ThnFilters);           
        }
    }
}
