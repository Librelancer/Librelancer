using LibreLancer;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;

namespace LancerEdit.Tools.BulkAudio;

public static class BulkAudioTool
{
    public static void Open(MainWindow win, PopupManager pm)
    {
        pm.OpenPopup(new BulkAudioImportPopup(win, pm));
    }
}
