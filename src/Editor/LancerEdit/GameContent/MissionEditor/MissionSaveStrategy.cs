using System;
using System.IO;
using LibreLancer.ContentEdit;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.MissionEditor;

internal class MissionSaveStrategy(MainWindow window, MissionScriptEditorTab tab) : ISaveStrategy
{
    public bool ShouldSave => false;

    public void DrawMenuOptions()
    {
        if (Theme.IconMenuItem(Icons.Save, $"Save '{tab.DocumentName}'", true))
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
        if (!forceSaveAs && !string.IsNullOrEmpty(tab.FileSaveLocation))
        {
            var result = tab.SaveMission(tab.FileSaveLocation);
            window.ResultMessages(result);

            if(!result.IsError)
            {
                window.OnSaved();
            }
        }
        else
        {
            RunSaveDialog();
        }
    }

    internal void RunSaveDialog()
    {
        FileDialog.Save(f =>
        {
            var result = tab.SaveMission(f);
            window.ResultMessages(result);

            if (!result.IsSuccess)
            {
                return;
            }
            window.OnSaved();
            tab.DocumentName = Path.GetFileName(f);
            tab.Title = $"Mission Script Editor - {Path.GetFileName(f)}";
            tab.FileSaveLocation = f;
        }, AppFilters.IniFilters);
    }
}
