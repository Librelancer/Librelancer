using System;
using System.Collections.Generic;
using LibreLancer.ImUI;
namespace LancerEdit
{
    public abstract class EditorTab : DockTab
    {
        public virtual void DetectResources(List<MissingReference> missing, List<uint> matrefs, List<string> texrefs)
        {
        }
        public virtual void SetActiveTab(MainWindow win)
        {
            win.ActiveTab = null;
        }
    }
}
