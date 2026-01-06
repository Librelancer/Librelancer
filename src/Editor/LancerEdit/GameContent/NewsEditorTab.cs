using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.Lookups;
using LancerEdit.GameContent.Popups;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Graphics;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent;

public class NewsEditorTab : GameContentTab
{
    public NewsCollection News;
    public GameDataContext Data;

    private EditorUndoBuffer undoBuffer = new();
    private PopupManager popups = new();

    private string[] newsIcons = ["world", "critical", "system", "universe", "faction"];
    private string[] newsImages = ["genericnews"];
    private Base[] allBases;

    private MainWindow window;

    public bool Dirty;

    public NewsEditorTab(GameDataContext gameData, MainWindow mainWindow)
    {
        Title = "News Editor";
        News = gameData.GameData.Items.News.Clone();
        SaveStrategy = new NewsSaveStrategy(this);
        Data = gameData;

        var newsVendor = gameData.GameData.Items.DataPath("INTERFACE/NEURONET/NEWSVENDOR/newsvendor.txm");
        if (newsVendor != null)
        {
            gameData.Resources.LoadResourceFile(newsVendor);
            newsImages = gameData.Resources.TexturesInFile(newsVendor).Order().ToArray();
        }

        allBases = Data.GameData.Items.Bases.OrderBy(x => x.Nickname).ToArray();
        window = mainWindow;
        undoBuffer.Hook = () => Dirty = true;
    }

    public void OnSaved() => window.OnSaved();

    private int tabIndex = 0;
    private NewsItem selectedItem = null;
    private bool showHistory = false;

    public override void Draw(double elapsed)
    {
        var sz = ImGui.GetContentRegionAvail();
        sz.Y -= 3 * ImGuiHelper.Scale;
        if (ImGui.BeginTable("##layout", 2,
                ImGuiTableFlags.BordersInnerV |
                ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendY, sz))
        {
            ImGui.TableSetupColumn("#a", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("#b", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            if (ImGui.Selectable($"{Icons.Newspaper} Articles", tabIndex == 0))
                tabIndex = 0;
            if (ImGui.Selectable($"{Icons.Map} Bases", tabIndex == 1))
                tabIndex = 1;
            ImGui.Separator();
            if (ImGuiExt.ToggleButton("History", showHistory))
            {
                showHistory = !showHistory;
            }
            ImGui.TableNextColumn();
            if (tabIndex == 0)
                DrawNews();
            else
                DrawBases();
            ImGui.EndTable();
        }
        if (showHistory)
        {
            undoBuffer.DisplayStack();
        }
        popups.Run();
    }


    private StringAutocomplete iconText;
    //Scroll state
    private bool firstSelected = false;
    private bool scrollNews = false;
    void SelectNews(NewsItem n)
    {
        selectedItem = n;
        if (n == null)
            return;
        iconText = new("##Icon", newsIcons, n.Icon, x =>
        {
            undoBuffer.Commit(new NewsSetIcon(selectedItem, selectedItem.Icon, x));
        });
        firstSelected = true;
    }

    public void CheckDeleted(NewsItem n)
    {
        if(selectedItem == n)
            SelectNews(null);
    }

    public override void OnHotkey(Hotkeys hk, bool shiftPressed)
    {
        if (hk == Hotkeys.Undo && undoBuffer.CanUndo)
        {
            undoBuffer.Undo();
            ResetLookups();
        }
        if (hk == Hotkeys.Redo && undoBuffer.CanRedo)
        {
            undoBuffer.Redo();
            ResetLookups();
        }
    }

    void ResetLookups()
    {
        if (selectedItem != null)
        {
            iconText.SetValue(selectedItem.Icon);
        }
    }

    private Texture2D registered;
    private ImTextureRef displayTex;

    void DrawNews()
    {
        if (ImGui.BeginTable("##news", 2, ImGuiTableFlags.BordersInnerV
                | ImGuiTableFlags.Resizable,
                ImGui.GetContentRegionAvail()))
        {
            ImGui.TableSetupColumn("#a", ImGuiTableColumnFlags.WidthStretch, 0.25f);
            ImGui.TableSetupColumn("#b", ImGuiTableColumnFlags.WidthStretch, 0.75f);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.BeginChild("##items");
            int i = 0;
            foreach (var n in News.AllNews)
            {
                ImGui.PushID(i++);
                var isSelected = selectedItem == n;
                if (isSelected && scrollNews)
                {
                    ImGui.SetScrollHereY();
                }
                if (ImGui.Selectable($"[{n.Icon}] {Data.GameData.GetString(n.Headline)}", isSelected))
                {
                    SelectNews(n);
                }
                ImGui.PopID();
            }
            ImGui.EndChild();
            ImGui.TableNextColumn();
            if (ImGui.Button($"{Icons.PlusCircle} New Article"))
            {
                var ni = new NewsItem
                {
                    From = Data.GameData.Items.Story[0],
                    To = Data.GameData.Items.Story[^1],
                    Logo = newsImages[0],
                    Icon = newsIcons[0]
                };
                undoBuffer.Commit(new NewsNew(ni, News));
                window.QueueUIThread(() =>
                {
                    SelectNews(ni); // Defer scroll to next frame
                    scrollNews = true;
                });
            }

            if (selectedItem != null)
            {
                ImGui.SameLine();
                if (ImGui.Button($"{Icons.TrashAlt} Delete"))
                {
                    window.Confirm("Are you sure you want to delete this item?", () =>
                    {
                        undoBuffer.Commit(new NewsDelete(selectedItem, News, this));
                    });
                }
            }

            ImGui.Separator();
            if (selectedItem != null)
            {
                Data.StoryIndices.DrawUndo("Visible From: ", undoBuffer,
                    () => ref selectedItem.From);
                Data.StoryIndices.DrawUndo("Visible To: ", undoBuffer,
                    () => ref selectedItem.To);
                if (selectedItem.From == null || selectedItem.To == null ||
                    selectedItem.From.Index > selectedItem.To.Index)
                {
                    ImGui.Text($"{Icons.Warning} Visible range selection invalid");
                }
                bool isAutoselect = selectedItem.AutoSelect;
                ImGui.Checkbox("Autoselect", ref isAutoselect);
                if (isAutoselect != selectedItem.AutoSelect)
                {
                    undoBuffer.Commit(new NewsSetAutoselect(selectedItem, selectedItem.AutoSelect, isAutoselect));
                }
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Icon: ");
                ImGui.SameLine();
                iconText.Draw();
                if (ImGui.Button($"{Icons.Edit}##headline"))
                {
                    popups.OpenPopup(new StringSelection(selectedItem.Headline, Data.Infocards, x =>
                    {
                        undoBuffer.Commit(new NewsSetHeadline(selectedItem, selectedItem.Headline, x));
                    }));
                }
                ImGui.SameLine();
                ImGui.TextWrapped($"Headline ({selectedItem.Headline}): {Data.GameData.GetString(selectedItem.Headline)}");
                if (ImGui.Button($"{Icons.Edit}##text"))
                {
                    popups.OpenPopup(new StringSelection(selectedItem.Text, Data.Infocards, x =>
                    {
                        undoBuffer.Commit(new NewsSetText(selectedItem, selectedItem.Text, x));
                    }));
                }
                ImGui.SameLine();
                ImGui.TextWrapped($"Text ({selectedItem.Text}): {Data.GameData.GetString(selectedItem.Text)}");
                ImGui.SeparatorText($"Image ({selectedItem.Logo})");
                var imgTableWidth = ImGui.GetContentRegionAvail().X;
                if (ImGui.BeginTable("##images", 2, ImGuiTableFlags.BordersInnerV
                                                  | ImGuiTableFlags.Resizable | ImGuiTableFlags.NoHostExtendY,
                        new Vector2(imgTableWidth, 300 * ImGuiHelper.Scale)))
                {
                    ImGui.TableSetupColumn("#a", ImGuiTableColumnFlags.WidthStretch, 0.5f);
                    ImGui.TableSetupColumn("#b", ImGuiTableColumnFlags.WidthStretch, 0.5f);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.BeginChild("##items");
                    foreach (var img in newsImages)
                    {
                       bool isSelected = img.Equals(selectedItem.Logo, StringComparison.OrdinalIgnoreCase);
                       if(isSelected && firstSelected)
                           ImGui.SetScrollHereY();
                       if(ImGui.Selectable(img, isSelected))
                       {
                            undoBuffer.Commit(new NewsSetLogo(selectedItem, selectedItem.Logo, img));
                       }
                    }
                    ImGui.EndChild();
                    ImGui.TableNextColumn();
                    if (Data.Resources.FindTexture(selectedItem.Logo) is Texture2D tex)
                    {
                        if (registered != tex)
                        {
                            if(registered != null)
                                ImGuiHelper.DeregisterTexture(registered);
                            displayTex = ImGuiHelper.RegisterTexture(tex);
                            registered = tex;
                        }
                        var availSize = ImGui.GetContentRegionAvail();
                        var iWidth = availSize.X;
                        var iHeight = ((float)tex.Height / tex.Width) * availSize.X;
                        if (iHeight > availSize.Y)
                        {
                            iWidth = ((float)tex.Width / tex.Height) * availSize.Y;
                            iHeight = availSize.Y;
                        }
                        ImGui.Image(displayTex, new Vector2(iWidth, iHeight), new(0, 1), new Vector2(1, 0));
                    }
                    ImGui.EndTable();
                }
                ImGui.SeparatorText("Bases");
                var tabSize = ImGui.GetContentRegionAvail();
                tabSize.Y -= 3 * ImGuiHelper.Scale;
                if (ImGui.Button($"{Icons.PlusCircle} Add Base"))
                {
                    popups.OpenPopup(new BaseSelection(
                        x => undoBuffer.Commit(new NewsAddBase(selectedItem, x, News)),
                        "Add Base",
                        null,
                        null, Data,
                        x => !News.BaseHasNews(x, selectedItem),
                        true
                        ));
                }
                if (ImGui.BeginTable("##bases", 2,
                        ImGuiTableFlags.ScrollY | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit,
                        tabSize))
                {
                    ImGui.TableSetupColumn("#a", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("#b", ImGuiTableColumnFlags.WidthFixed);
                    int k = 0;
                    foreach (var item in News.GetBases(selectedItem))
                    {
                        ImGui.PushID(k++);
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        if (ImGui.Selectable($"{item.Nickname} ({Data.Infocards.GetStringResource(item.IdsName)})"))
                        {
                            selectedBase = item;
                            scrollBases = true;
                            tabIndex = 1;
                        }
                        ImGui.TableNextColumn();
                        if (Controls.SmallButton($"{Icons.TrashAlt}"))
                        {
                            undoBuffer.Commit(new NewsRemoveBase(selectedItem, item, News));
                        }
                        ImGui.PopID();
                    }
                    ImGui.EndTable();
                }
            }
            else
            {
                ImGui.Text("No Selection");
            }
            ImGui.EndTable();
        }
        firstSelected = false;
        scrollNews = false;
    }

    private Base selectedBase;
    private bool scrollBases = false;

    void DrawBases()
    {
        if (ImGui.BeginTable("##bases", 2, ImGuiTableFlags.BordersInnerV
                                          | ImGuiTableFlags.Resizable,
                ImGui.GetContentRegionAvail()))
        {
            ImGui.TableSetupColumn("#a", ImGuiTableColumnFlags.WidthStretch, 0.25f);
            ImGui.TableSetupColumn("#b", ImGuiTableColumnFlags.WidthStretch, 0.75f);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.BeginChild("##items");
            int i = 0;
            foreach (var n in allBases)
            {
                ImGui.PushID(i++);
                var isSelected = selectedBase == n;
                if(isSelected && scrollBases)
                    ImGui.SetScrollHereY();
                if (ImGui.Selectable($"{n.Nickname} ({Data.GameData.GetString(n.IdsName)})", isSelected))
                {
                    selectedBase = n;
                }
                ImGui.PopID();
            }
            ImGui.EndChild();
            ImGui.TableNextColumn();
            if (selectedBase != null)
            {
                ImGui.Text($"{selectedBase.Nickname} ({Data.GameData.GetString(selectedBase.IdsName)})");
                if (ImGui.Button($"{Icons.PlusCircle} Add News"))
                {
                    popups.OpenPopup(new NewsSelection(
                        x => undoBuffer.Commit(new NewsAddBase(x, selectedBase, News)),
                        News, Data,
                        x => !News.BaseHasNews(selectedBase, x)));
                }
                var tabSize = ImGui.GetContentRegionAvail();
                tabSize.Y -= 3 * ImGuiHelper.Scale;
                if (ImGui.BeginTable("##articles", 2,
                        ImGuiTableFlags.ScrollY | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit,
                        tabSize))
                {
                    ImGui.TableSetupColumn("#a", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("#b", ImGuiTableColumnFlags.WidthFixed);
                    int k = 0;
                    foreach (var n in News.GetNewsForBase(selectedBase))
                    {
                        ImGui.PushID(k++);
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        if (ImGui.Selectable($"[{n.Icon}] {Data.GameData.GetString(n.Headline)}"))
                        {
                            SelectNews(n);
                            tabIndex = 0;
                            scrollNews = true;
                        }
                        ImGui.TableNextColumn();
                        if (Controls.SmallButton($"{Icons.TrashAlt}"))
                        {
                            undoBuffer.Commit(new NewsRemoveBase(n, selectedBase, News));
                        }
                        ImGui.PopID();
                    }
                    ImGui.EndTable();
                }
            }
            else
            {
                ImGui.Text("No Selection");
            }
            ImGui.EndTable();
        }
        scrollBases = false;
    }

    public override void Dispose()
    {
        if(registered != null)
            ImGuiHelper.DeregisterTexture(registered);
    }
}
