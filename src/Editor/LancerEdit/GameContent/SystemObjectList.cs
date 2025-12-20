using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.Filters;
using LibreLancer;
using LibreLancer.Data.GameData.World;
using LibreLancer.World;

namespace LancerEdit.GameContent;

public class SystemObjectList
{
    public event Action<GameObject> OnSelectionChanged;
    public event Action<GameObject> OnDelete;

    public List<GameObject> Selection = new List<GameObject>();
    public Matrix4x4 SelectedTransform;

    public IReadOnlyList<GameObject> Objects => allObjects;

    private GameObject[] allObjects = Array.Empty<GameObject>();
    private GameObject[] filteredObjects = Array.Empty<GameObject>();
    private string filterText = "";
    private MainWindow win;
    private ObjectFiltering<GameObject> filters = new GameObjectFilters();

    public bool Dirty { get; private set; }

    public int OriginalCount { get; set; }

    public unsafe SystemObjectList(MainWindow win)
    {
        this.win = win;
        textCallback = OnTextChanged;
    }

    public void SelectSingle(GameObject obj)
    {
        SelectedTransform = (obj?.LocalTransform ?? Transform3D.Identity).Matrix();
        if (Selection.Count > 0)
        {
            Selection = [];
        }

        if(obj != null)
        {
            Selection.Add(obj);
        }
    }

    private GameWorld prevWorld;

    public void Refresh()
    {
        SetObjects(prevWorld);
        ScrollToSelection();
    }

    public List<SystemObject> DeletedObjects = new();

    public void SaveAndApply(StarSystem system)
    {
        foreach (var item in allObjects)
        {
            if (item.TryGetComponent<ObjectEditData>(out var dat))
            {
                dat.Apply();
                if (dat.IsNewObject)
                {
                    system.Objects.Add(item.SystemObject);
                }
                item.RemoveComponent(dat);
            }
        }
        foreach (var o in DeletedObjects)
            system.Objects.Remove(o);
        OriginalCount = allObjects.Length;
        DeletedObjects = new List<SystemObject>();

        CheckDirty();
        Debug.Assert(!Dirty);
    }

    public void CheckDirty()
    {
        Dirty = false;
        if (allObjects.Length != OriginalCount ||
            DeletedObjects.Count > 0)
        {
            Dirty = true;
            return;
        }
        foreach (var o in allObjects)
        {
            if (o.TryGetComponent<ObjectEditData>(out var editData)) {
                if (editData.IsNewObject || editData.CheckDirty())
                {
                    Dirty = true;
                    break;
                }
            }
        }
    }

    public void SetObjects(GameWorld world)
    {
        prevWorld = world;
        allObjects = world.Objects.Where(x => x.SystemObject != null)
            .OrderBy(x => x.Nickname).ToArray();
        ApplyFilter();
    }

    private bool doScroll = false;

    bool IsPrimarySelection(GameObject obj) =>
        Selection.Count > 0 && Selection[0] == obj;

    bool ShouldAddSecondary() => Selection.Count > 0 && (win.Keyboard.IsKeyDown(Keys.LeftShift) ||
                                                         win.Keyboard.IsKeyDown(Keys.RightShift));

    public void ScrollToSelection()
    {
        doScroll = true;
    }

    void ApplyFilter()
    {
        filteredObjects = filters.Filter(filterText, allObjects).ToArray();
    }

    private bool doFiltering = false;

    private ImGuiInputTextCallback textCallback;
    unsafe int OnTextChanged(ImGuiInputTextCallbackData* d)
    {
        doFiltering = true;
        return 0;
    }
    public unsafe void Draw()
    {
        if (doFiltering) {
            ApplyFilter();
            doFiltering = false;
        }
        ImGui.PushItemWidth(-1);
        ImGui.InputTextWithHint("##filter", "Filter", ref filterText, 250, ImGuiInputTextFlags.CallbackEdit, textCallback);
        ImGui.PopItemWidth();
        ImGui.BeginChild("##objlist");
        int i = 0;
        foreach (var obj in filteredObjects) {
            ImGui.PushID(i++);
            bool isPrimary = IsPrimarySelection(obj);
            bool isSelected = Selection.Contains(obj);
            bool addSecondary = ShouldAddSecondary();
            if (doScroll && isPrimary)
                ImGui.SetScrollHereY();
            if (isSelected && !isPrimary)
                ImGui.PushStyleColor(ImGuiCol.Header, new Color4(120, 83, 101, 255));
            if(addSecondary && !isPrimary)
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Color4(156, 107, 131, 255));
            if (ImGui.Selectable(obj.Nickname, isSelected, ImGuiSelectableFlags.AllowDoubleClick))
            {
                if (!Selection.Contains(obj)) {
                    if(Selection.Count > 0 && (win.Keyboard.IsKeyDown(Keys.LeftShift) ||
                                               win.Keyboard.IsKeyDown(Keys.RightShift)))
                        Selection.Add(obj);
                    else
                        SelectSingle(obj);
                }
                if(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    OnSelectionChanged(obj);
            }
            if(isSelected && !isPrimary)
                ImGui.PopStyleColor();
            if(addSecondary && !isPrimary)
                ImGui.PopStyleColor();
            if (ImGui.BeginPopupContextItem(obj.Nickname))
            {
                if (ImGui.MenuItem("Select with Children"))
                {
                    SelectSingle(obj);
                    foreach (var c in allObjects)
                    {
                        if (obj.Nickname.Equals(c.SystemObject.Parent, StringComparison.OrdinalIgnoreCase))
                            Selection.Add(c);
                    }
                }
                if(ImGui.MenuItem("Delete"))
                    OnDelete(obj);
                ImGui.EndPopup();
            }
            ImGui.PopID();
        }
        ImGui.EndChild();
        doScroll = false;
    }
}
