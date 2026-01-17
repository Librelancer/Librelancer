using System;
using System.Collections.Generic;
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

    public void SelectMultiple(IEnumerable<GameObject> objects)
    {
        if (Selection.Count > 0)
        {
            Selection = [];
        }

        foreach (var obj in objects)
        {
            SelectedTransform = (obj?.LocalTransform ?? Transform3D.Identity).Matrix();

            if (obj != null)
            {
                Selection.Add(obj);
            }
        }
        
    }

    private GameWorld prevWorld;

    public void Refresh()
    {
        SetObjects(prevWorld);
        ScrollToSelection();
    }

    public void SaveAndApply(StarSystem system)
    {
        system.Objects = allObjects.Select(x => x.SystemObject.Clone()).ToList();
        Dirty = false;
    }

    bool ObjectsChanged(SystemObject og, SystemObject up) =>
        og.Nickname != up.Nickname ||
        og.Position != up.Position ||
        MathHelper.QuatError(og.Rotation, up.Rotation) > 0.0001f ||
        og.IdsName != up.IdsName ||
        og.IdsInfo != up.IdsInfo ||
        og.Archetype != up.Archetype ||
        og.Star != up.Star ||
        og.Loadout != up.Loadout ||
        og.Visit != up.Visit ||
        og.Reputation != up.Reputation ||
        og.Base != up.Base ||
        og.Dock != up.Dock ||
        og.Parent != up.Parent ||
        og.Comment != up.Comment;

    public void CheckDirty(List<SystemObject> originalSystem)
    {
        Dirty = false;
        if (allObjects.Length != originalSystem.Count)
        {
            Dirty = true;
            return;
        }

        for (int i = 0; i < allObjects.Length; i++)
        {
            var og = originalSystem[i];
            var up = allObjects[i].SystemObject;
            if (ObjectsChanged(og, up))
            {
                Dirty = true;
                return;
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

    bool ShouldAddSecondary() => Selection.Count > 0 && (win.Keyboard.IsKeyDown(Keys.LeftShift)
        || win.Keyboard.IsKeyDown(Keys.RightShift)
        || win.Keyboard.IsKeyDown(Keys.LeftControl)
        || win.Keyboard.IsKeyDown(Keys.RightControl));

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
                if(Selection != null && Selection.Contains(obj) && Selection.Count > 1)
                {
                    if (ImGui.MenuItem("Delete Selected"))
                    {
                        var toDelete = Selection.ToArray(); // snapshot

                        foreach (var o in toDelete)
                        {
                            OnDelete(o);
                        }
                    }
                        
                }
                ImGui.EndPopup();
            }
            ImGui.PopID();
        }
        ImGui.EndChild();
        doScroll = false;
    }
}
