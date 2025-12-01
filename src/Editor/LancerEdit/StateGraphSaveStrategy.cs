using LibreLancer.ContentEdit;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;

namespace LancerEdit
{
    public sealed class StateGraphSaveStrategy : ISaveStrategy
    {
        private readonly StateGraphTab tab;

        public StateGraphSaveStrategy(StateGraphTab tab)
        {
            this.tab = tab;
        }

        public bool ShouldSave => false;

        public void DrawMenuOptions()
        {
            if (Theme.IconMenuItem(Icons.Save, "Save", true))
            {
                Save(false);
            }
            if (Theme.IconMenuItem(Icons.Save, "Save As...", true))
            {
                Save(true);
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
                }, AppFilters.StateGraphFilter);
            }
        }

    }
}
