using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
namespace LancerEdit;

class ListingItem
{
    public bool IsDirectory;
    public string Name;
    public string Path;
    public long Size;
    public string SizeString;
    public DateTime LastModified;
}

enum FileSortKind
{
    NameAscending,
    NameDescending,
    DateAscending,
    DateDescending,
    SizeAscending,
    SizeDescending
}

static class SortExtensions
{
    public static IEnumerable<ListingItem> Sort(this IEnumerable<ListingItem> src, FileSortKind kind, bool dirs) => kind switch
    {
        FileSortKind.NameAscending => src.OrderBy(x =>x.Name),
        FileSortKind.NameDescending => src.OrderByDescending(x => x.Name),
        FileSortKind.DateAscending => src.OrderBy(x => x.LastModified),
        FileSortKind.DateDescending => src.OrderByDescending(x => x.LastModified),
        FileSortKind.SizeAscending when !dirs => src.OrderBy(x=> x.Size),
        FileSortKind.SizeDescending when !dirs => src.OrderByDescending(x => x.Size),
        _ => src.OrderBy(x => x.Name),
    };
}


class DirectoryListing
{

    public string Name;
    public string FullPath;
    public string Parent;
    public ListingItem[] Items;


    public ListingItem[] Directories;
    public ListingItem[] Files;

    public FileSortKind SortKind;

    static bool SupportedFile(string path)
    {
        return path.EndsWith(".cmp", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".utf", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".txm", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".3db", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".ale", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".dfm", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".anm", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".vms", StringComparison.OrdinalIgnoreCase);
    }

    bool Rename(ListingItem[] items, string old, string newpath)
    {
        int index = -1;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].Path == old) {
                index = i;
                break;
            }
        }
        if (index == -1)
            return false;
        items[index].Path = newpath;
        items[index].Name = Path.GetFileName(newpath);
        Resort();
        return true;
    }

    public void TryRename(string old, string newpath)
    {
        if (!Rename(Directories, old, newpath))
            Rename(Files, old, newpath);
    }

    ListingItem Delete(ref ListingItem[] items, string path)
    {
        int index = -1;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].Path == path) {
                index = i;
                break;
            }
        }
        if (index == -1)
            return null;
        var deleted = items[index];
        items = items.Where(x => x != deleted).ToArray();
        Resort();
        return deleted;
    }

    public ListingItem TryDelete(string path)
    {
        ListingItem ret;
        if ((ret = Delete(ref Directories, path)) != null)
            return ret;
        if ((ret = Delete(ref Files, path)) != null)
            return ret;
        return null;
    }

    public void Refresh(string path)
    {
        var item = Items.FirstOrDefault(x => x.Path == path);
        if (item == null) return;
        if (item.IsDirectory)
        {
            var dinfo = new DirectoryInfo(path);
            item.LastModified = dinfo.LastWriteTime;
        }
        else
        {
            var finfo = new FileInfo(path);
            item.LastModified = finfo.LastWriteTime;
            item.Size = finfo.Length;
            item.SizeString = DebugDrawing.SizeSuffix(item.Size);
        }
    }

    public void TryAdd(string path)
    {
        if (Directory.Exists(path))
        {
            var dinfo = new DirectoryInfo(path);
            if ((dinfo.Attributes & FileAttributes.Hidden) != 0)
                return;
            if (Directories.All(x => x.Path != dinfo.FullName))
            {
                Directories = Directories.Concat(new[]
                    {new ListingItem() {IsDirectory = true, Name = dinfo.Name, Path = dinfo.FullName, LastModified = dinfo.LastWriteTime}}).ToArray();
                Resort();
            }
        }
        else if (File.Exists(path))
        {
            if (!SupportedFile(path)) return;
            var finfo = new FileInfo(path);
            if ((finfo.Attributes & FileAttributes.Hidden) != 0)
                return;
            if (Files.All(x => x.Path != finfo.FullName))
            {
                Files = Files.Concat(new[]
                    {new ListingItem()
                    {
                        IsDirectory = false,
                        Name = finfo.Name,
                        Path = finfo.FullName,
                        LastModified = finfo.LastWriteTime,
                        Size = finfo.Length,
                        SizeString = DebugDrawing.SizeSuffix(finfo.Length)
                    }}).ToArray();
                Resort();
            }
        }
    }



    public void Resort()
    {
        Items = Directories.Sort(SortKind, true).Concat(Files.Sort(SortKind, false)).ToArray();
    }

    public DirectoryListing(string path, FileSortKind sortKind)
    {
        var dinfo = new DirectoryInfo(path);
        SortKind = sortKind;
        FullPath = dinfo.FullName;
        Name = dinfo.Name;
        Parent = dinfo.Parent?.FullName;
        Directories = dinfo.GetDirectories()
            .Where(x => (x.Attributes & FileAttributes.Hidden) == 0)
            .Select(x => new ListingItem()
            {
                Name = ImGuiExt.IDSafe(x.Name),
                Path = x.FullName,
                LastModified = x.LastWriteTime,
                IsDirectory = true,
            }).Sort(sortKind, true).ToArray();
        Files = dinfo.GetFiles()
            .Where(x => (x.Attributes & FileAttributes.Hidden) == 0 && SupportedFile(x.Name))
            .Select(x => new ListingItem()
            {
                Name = ImGuiExt.IDSafe(x.Name),
                Path = x.FullName,
                LastModified = x.LastWriteTime,
                Size = x.Length,
                SizeString = DebugDrawing.SizeSuffix(x.Length)
            }).Sort(sortKind, false).ToArray();

        Items = Directories.Concat(Files).ToArray();
    }

}

public class BrowserFavorite
{
    public string Name;
    public string FullPath;

    public BrowserFavorite(string name, string fullPath)
    {
        Name = name;
        FullPath = fullPath;
    }
}



public class QuickFileBrowser
{
    public event Action<string> FileSelected;

    private DirectoryListing currentDirectory;
    private MountInfo[] drives;

    class NavigationHistory
    {
        public NavigationHistory Previous;
        public NavigationHistory Next;
        public string Path;
    }
    private NavigationHistory history;
    private EditorConfiguration config;
    private IUIThread uiThread;

    private FileSortKind activeSortKind = FileSortKind.NameAscending;
    private FileSortKind tableSortKind = FileSortKind.NameAscending;

    void SortBy(FileSortKind kind)
    {
        if (activeSortKind == kind)
            return;
        activeSortKind = kind;
        currentDirectory.SortKind = kind;
        currentDirectory.Resort();
    }

    private PopupManager popups;

    public QuickFileBrowser(EditorConfiguration config, IUIThread uiThread, PopupManager popups)
    {
        drives = Platform.GetMounts();
        Platform.MountsChanged += MountsChanged;
        ResetToHome();
        this.config = config;
        this.uiThread = uiThread;
        this.popups = popups;
    }


    void MountsChanged(MountInfo[] mounts)
    {
        drives = mounts;
        if (!Directory.Exists(currentDirectory.FullPath)) {
            ResetToHome();
        }
    }

    void ClickedDirectory(string dir)
    {
        var h = new NavigationHistory() {Previous = history, Path = dir};
        if(history != null)
            history.Next = h;
        history = h;
        OpenDirectory(dir);
    }

    void ResetToHome()
    {
        var f = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!Directory.Exists(f) && Platform.RunningOS != OS.Windows)
            f = "/";
        OpenDirectory(f);
        history = new NavigationHistory() {Path = currentDirectory.FullPath};
    }
    private FileSystemWatcher parentWatcher;
    private FileSystemWatcher childWatcher;
    void OpenDirectory(string dir)
    {
        selected = null;
        currentDirectory = new DirectoryListing(dir, activeSortKind);
        parentWatcher?.Dispose();
        if (currentDirectory.Parent != null)
        {
            parentWatcher = new FileSystemWatcher();
            parentWatcher.Path = currentDirectory.Parent;
            parentWatcher.Renamed += (_, e) => uiThread.QueueUIThread(() =>
            {
                if (e.OldFullPath == currentDirectory.FullPath)
                {
                    currentDirectory.FullPath = e.FullPath;
                    history.Path = e.FullPath;
                }
            });
            parentWatcher.Deleted += (_, e) => uiThread.QueueUIThread(() =>
            {
                if (e.FullPath == currentDirectory.FullPath) {
                    ResetToHome();
                }
            });
            parentWatcher.IncludeSubdirectories = false;
            parentWatcher.EnableRaisingEvents = true;
        }
        else
        {
            parentWatcher = null;
        }
        CreateChildWatcher(currentDirectory.FullPath);
    }


    private ListingItem selected;

    void Renamed(string olditem, string newitem) =>
        currentDirectory.TryRename(olditem, newitem);

    void Created(string item) =>
        currentDirectory.TryAdd(item);

    void Deleted(string item)
    {
        if (selected == currentDirectory.TryDelete(item))
            selected = null;
    }

    void CreateChildWatcher(string dir)
    {
        childWatcher?.Dispose();
        childWatcher = new FileSystemWatcher();
        childWatcher.NotifyFilter = NotifyFilters.DirectoryName
                                    | NotifyFilters.FileName
                                    | NotifyFilters.LastWrite;
        childWatcher.Path = dir;
        childWatcher.IncludeSubdirectories = false;
        childWatcher.Deleted += (sender, e) =>
            uiThread.QueueUIThread(() => Deleted(e.FullPath));
        childWatcher.Created += (sender, e) =>
            uiThread.QueueUIThread(() => Created(e.FullPath));
        childWatcher.Renamed += (_, e) =>
            uiThread.QueueUIThread(() => Renamed(e.OldFullPath, e.FullPath));
        childWatcher.EnableRaisingEvents = true;
    }

    int DrawGridItem(string name, bool isSelected ,float itemWidth, float iconSize, int icon)
    {
        var sz = ImGui.CalcTextSize(name, itemWidth);
        var dl = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();
        var textHMax = Math.Min(sz.Y, ImGui.GetTextLineHeight() * 2);
        var btn = ImGui.InvisibleButton(name, new Vector2(itemWidth, iconSize + textHMax));
        var hovered = ImGui.IsItemHovered();
        if (hovered)
        {
            dl.AddRectFilled(pos, pos + new Vector2(itemWidth, iconSize + textHMax), ImGui.GetColorU32(ImGuiCol.NavHighlight));
            if(textHMax < sz.Y)
                ImGui.SetTooltip(name);
        }
        else if (isSelected)
        {
            dl.AddRectFilled(pos, pos + new Vector2(itemWidth, iconSize + textHMax), ImGui.GetColorU32(ImGuiCol.TextSelectedBg));
        }

        var imagePos = pos + new Vector2((itemWidth / 2) - (iconSize / 2), 0);
        dl.AddImage(icon, imagePos, imagePos + new Vector2(iconSize), new Vector2(0, 1), new Vector2(1, 0));
        AddText(dl, pos + new Vector2((itemWidth / 2) - (sz.X / 2), iconSize), sz.X, textHMax, ImGui.GetColorU32(ImGuiCol.Text), name, itemWidth);
        if (hovered && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            return 2;
        if (btn)
            return 1;
        return 0;
    }


    // Not bound in ImGui.NET
    static unsafe void AddText(ImDrawListPtr list, Vector2 pos, float w, float h, uint col, string text_begin, float wrap_width)
    {
        int text_begin_byteCount = Encoding.UTF8.GetByteCount(text_begin);
        byte* native_text_begin = stackalloc byte[text_begin_byteCount + 1];
        fixed (char* text_begin_ptr = text_begin)
        {
            int native_text_begin_offset = Encoding.UTF8.GetBytes(text_begin_ptr, text_begin.Length, native_text_begin, text_begin_byteCount);
            native_text_begin[native_text_begin_offset] = 0;
        }
        byte* native_text_end = null;
        Vector4 clip = new Vector4(pos.X, pos.Y, pos.X + w, pos.Y + h);
        ImGuiNative.ImDrawList_AddText_FontPtr(list.NativePtr, ImGui.GetFont().NativePtr, ImGui.GetFontSize(), pos, col, native_text_begin, native_text_end, wrap_width, &clip);
    }

    bool Star(bool fav)
    {
        if(fav)
            ImGui.PushStyleColor(ImGuiCol.Text,Color4.Yellow);
        var retval = ImGui.Button($"{Icons.Star}");
        if(fav)
            ImGui.PopStyleColor();
        return retval;
    }


    private bool isList = false;


    bool CheckExists(string item)
    {
        if (Directory.Exists(item))
            return true;
        if (File.Exists(item))
            return true;
        return false;
    }

    void DrawGrid()
    {
        ImGui.BeginChild("##directory", ImGui.GetContentRegionAvail() - new Vector2(0, 2 * ImGuiHelper.Scale));
        var selSize = 70 * ImGuiHelper.Scale;

        var measureSize = selSize + (ImGui.GetStyle().ItemSpacing.X);
        var perRow = (int)(ImGui.GetContentRegionAvail().X / measureSize);
        int j = 0;
        foreach (var item in currentDirectory.Items)
        {
            if (j != 0 && j % perRow != 0)
                ImGui.SameLine();
            var clicks = DrawGridItem(item.Name, selected == item, selSize, 48 * ImGuiHelper.Scale,
                item.IsDirectory ? ImGuiHelper.FolderId : ImGuiHelper.FileId);
            if (clicks == 1)
            {
                selected = item;
            }
            if (clicks == 2)
            {
                if (CheckExists(item.Path))
                {
                    if (item.IsDirectory)
                        ClickedDirectory(item.Path);
                    else if (FileSelected != null)
                        FileSelected(item.Path);
                }

            }
            j++;
        }
        ImGui.EndChild();
    }

    void DrawDetails()
    {
        var sz = ImGui.GetContentRegionAvail() - new Vector2(0, 2 * ImGuiHelper.Scale);
        if (!ImGui.BeginTable("##details", 3,
                ImGuiTableFlags.Borders |
                ImGuiTableFlags.Resizable |
                ImGuiTableFlags.Sortable |
                ImGuiTableFlags.ScrollY |
                ImGuiTableFlags.NoHostExtendY |
                ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame, sz))
            return;
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.DefaultSort, 0.5f);
        ImGui.TableSetupColumn("Last Modified", ImGuiTableColumnFlags.None, 0.3f);
        ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.None, 0.2f);
        ImGui.TableSetupScrollFreeze(0,1);
        ImGui.TableHeadersRow();
        var sortSpecs = ImGui.TableGetSortSpecs();
        if (sortSpecs.SpecsDirty)
        {
            var desc = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Descending;
            tableSortKind = sortSpecs.Specs.ColumnIndex switch
            {
                0 when desc => FileSortKind.NameDescending,
                0 => FileSortKind.NameAscending,
                1 when desc => FileSortKind.DateDescending,
                1 => FileSortKind.DateAscending,
                2 when desc => FileSortKind.SizeDescending,
                2 => FileSortKind.SizeAscending,
                _ => FileSortKind.NameAscending
            };
            SortBy(tableSortKind);
            sortSpecs.SpecsDirty = false;
        }
        foreach (var item in currentDirectory.Items)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(item.IsDirectory ? $"{Icons.Open}" : $"{Icons.File}");
            ImGui.SameLine();
            if (ImGui.Selectable(item.Name, selected == item, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick))
            {
                if (ImGui.IsMouseDoubleClicked(0))
                {
                    if (CheckExists(item.Path))
                    {
                        if (item.IsDirectory)
                            ClickedDirectory(item.Path);
                        else if (FileSelected != null)
                            FileSelected(item.Path);
                    }
                }
                else
                    selected = item;
            }
            ImGui.TableNextColumn();
            ImGui.Text(item.LastModified.ToString());
            ImGui.TableNextColumn();
            if (item.SizeString != null)
                ImGui.Text(item.SizeString);
        }
        ImGui.EndTable();
    }

    public void Draw()
    {
        if (ImGuiExt.Button($"{Icons.ArrowLeft}", history?.Previous != null))
        {
            history = history.Previous;
            OpenDirectory(history.Path);
        }
        ImGui.SameLine();
        if (ImGuiExt.Button($"{Icons.ArrowRight}", history?.Next != null))
        {
            history = history.Next;
            OpenDirectory(history.Path);
        }
        ImGui.SameLine();
        if (ImGuiExt.Button($"{Icons.TurnUp}", currentDirectory.Parent != null))
        {
            ClickedDirectory(currentDirectory.Parent);
        }
        ImGui.SameLine();
        if (ImGuiExt.ToggleButton($"{Icons.List}", isList) && !isList) {
            SortBy(tableSortKind);
            isList = true;
        }
        ImGui.SameLine();
        if (ImGuiExt.ToggleButton($"{Icons.Grip}", !isList) && isList) {
            SortBy(FileSortKind.NameAscending);
            isList = false;
        }
        ImGui.SameLine();
        int fav = -1;
        for (int i = 0; i < config.Favorites.Count; i++) {
            if (config.Favorites[i].FullPath.Equals(currentDirectory.FullPath, StringComparison.OrdinalIgnoreCase))
            {
                fav = i;
                break;
            }
        }
        if (Star(fav != -1))
        {
            if(fav == -1)
                config.Favorites.Add(new BrowserFavorite(currentDirectory.Name, currentDirectory.FullPath));
            else
                config.Favorites.RemoveAt(fav);
        }

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.Text(currentDirectory.FullPath);
        ImGui.Separator();
        var region = ImGui.GetContentRegionAvail() - new Vector2(0, 2 * ImGuiHelper.Scale);
        if (!ImGui.BeginTable("##main", 2,
                ImGuiTableFlags.Resizable | ImGuiTableFlags.NoHostExtendY | ImGuiTableFlags.SizingStretchSame,
                region))
            return;
        ImGui.TableSetupColumn("##fav", ImGuiTableColumnFlags.None, 0.2f);
        ImGui.TableSetupColumn("##stuff", ImGuiTableColumnFlags.None, 0.8f);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.BeginChild("##favorites");
        ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.FrameBg));
        if (ImGui.CollapsingHeader("Places", ImGuiTreeNodeFlags.DefaultOpen))
        {
            foreach (var d in drives)
            {
                var selected = d.Path == currentDirectory.FullPath;
                if (ImGui.Selectable(ImGuiExt.IDWithExtra(d.Name, d.Path), selected) && !selected)
                    ClickedDirectory(d.Path);
            }
        }
        if (ImGui.CollapsingHeader("Favorites", ImGuiTreeNodeFlags.DefaultOpen))
        {
            for (int i = 0; i < config.Favorites.Count; i++)
            {
                var d = config.Favorites[i];
                if (ImGui.Selectable(ImGuiExt.IDWithExtra(d.Name, d.FullPath), fav == i) && fav != i)
                    ClickedDirectory(d.FullPath);
                if (ImGui.BeginPopupContextItem($"##fav{i}"))
                {
                    if (ImGui.MenuItem("Rename"))
                    {
                        popups.OpenPopup(new NameInputPopup(NameInputConfig.Rename(), d.Name, x => d.Name = x));
                    }
                    ImGui.EndPopup();
                }
            }
        }
        ImGui.PopStyleColor();
        ImGui.EndChild();
        ImGui.TableNextColumn();
        if (isList)
            DrawDetails();
        else
            DrawGrid();
        ImGui.EndTable();
    }

    public void Dispose()
    {
        parentWatcher?.Dispose();
        childWatcher?.Dispose();
    }
}
