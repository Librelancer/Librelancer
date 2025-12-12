using System;
using System.Collections.Generic;

using LibreLancer.ImUI;

namespace LancerEdit.Tools.BulkAudio;

public static class BulkAudioTool
{
    public static void Open(MainWindow win, PopupManager pm)
    {
        pm.OpenPopup(new BulkAudioImportPopup(win, pm));
    }

    public static void Open(MainWindow win, PopupManager pm, Action<List<ImportEntry>> onImport)
    {
        pm.OpenPopup(new BulkAudioImportPopup(win, pm, onImport));
    }
}
