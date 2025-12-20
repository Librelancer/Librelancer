using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;
using LibreLancer.Render;
using LibreLancer.Thn;

namespace LancerEdit.GameContent;

public class ThnPlayerTab : GameContentTab
{
    private MainWindow win;

    private DecompiledThn[] decompiled;
    private bool decompiledOpen = false;

    private Cutscene cutscene;
    private GameDataContext gameData;

    private List<string> openFiles = new List<string>();
    private string[] toReload = null;

    private Viewport3D viewport;

    class DecompiledThn
    {
        public string Name;
        public string Text;
    }

    public ThnPlayerTab(GameDataContext gameData, MainWindow mw)
    {
        Title = "Thn Player";
        this.gameData = gameData;
        this.win = mw;
        viewport = new Viewport3D(mw);
        viewport.EnableMSAA = false;
        viewport.Draw3D = DrawGL;
    }

    void Open(params string[] files)
    {
        var lastFile = Path.GetFileName(files.Last());
        Title = lastFile;
        toReload = files;
        decompiled = files.Select(x => new DecompiledThn()
        {
            Name = Path.GetFileName(x),
            Text = ThnDecompile.Decompile(x, gameData.GameData.Items.ThornReadCallback)
        }).ToArray();
        var ctx = new ThnScriptContext(null);
        cutscene = new Cutscene(ctx, gameData.GameData,  gameData.Resources, gameData.Sounds, new Rectangle(0,0,240,240), win);
        cutscene.BeginScene(files.Select(x => new ThnScript(File.ReadAllBytes(x), gameData.GameData.Items.ThornReadCallback, x)));
    }

    void Reload()
    {
        if (toReload != null) Open(toReload);
    }

    public override void Update(double elapsed)
    {
    }

    DropdownOption[] dfmOptions = new DropdownOption[]
    {
        new DropdownOption("Mesh", Icons.Cube),
        new DropdownOption("Mesh+Bones", Icons.Cube),
        new DropdownOption("Mesh+Hardpoints", Icons.Cube),
        new DropdownOption("Mesh+Bones+Hardpoints", Icons.Cube),
        new DropdownOption("Bones", Icons.Cube),
        new DropdownOption("Hardpoints", Icons.Cube),
        new DropdownOption("Bones+Hardpoints", Icons.Cube),
    };

    private int selectedDfmMode = 0;

    private double drawElapsed;

    void DrawGL(int w, int h)
    {
        cutscene.Update(drawElapsed);
        cutscene.UpdateViewport(new Rectangle(0, 0, w, h), (float)w / h);
        cutscene.Renderer.DfmMode = (DfmDrawMode)selectedDfmMode;
        cutscene.Draw(drawElapsed, w, h);
    }

    public override void Draw(double elapsed)
    {
        if(ImGui.Button("Open"))
            FileDialog.Open(x => Open(x));
        ImGui.SameLine();
        if (ImGui.Button("Open Multiple")) {
            openFiles = new List<string>();
            ImGui.OpenPopup("Open Multiple##" + Unique);
        }
        ImGui.SameLine();
        if(ImGuiExt.ToggleButton("Decompiled", decompiledOpen, decompiled != null))
            decompiledOpen = !decompiledOpen;
        ImGui.SameLine();
        if(ImGuiExt.Button("Reload", cutscene != null))
            Reload();
        ImGui.SameLine();
        ImGui.Text($"T: {(cutscene?.CurrentTime ?? 0):F4}");
        ImGui.SameLine();
        #if DEBUG
        ImGuiExt.DropdownButton("Dfm Mode", ref selectedDfmMode, dfmOptions);
        #endif
        if (cutscene != null)
        {
            drawElapsed = elapsed;
            ImGuiHelper.AnimatingElement();
            viewport.Draw();
        }
        bool popupopen = true;
        if (ImGui.BeginPopupModal("Open Multiple##" + Unique, ref popupopen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (ImGui.Button("+"))
            {
                FileDialog.Open(file => openFiles.Add(file));
            }
            ImGui.BeginChild("##files", new Vector2(200, 200), ImGuiChildFlags.Borders, ImGuiWindowFlags.HorizontalScrollbar);
            int j = 0;
            foreach (var f in openFiles)
                ImGui.Selectable(ImGuiExt.IDWithExtra(f, j++));
            ImGui.EndChild();
            if (ImGuiExt.Button("Open", openFiles.Count > 0))
            {
                ImGui.CloseCurrentPopup();
                Open(openFiles.ToArray());
            }
            ImGuiHelper.FileModal();
            ImGui.EndPopup();
        }
        DrawDecompiled();
    }

    void DrawDecompiled()
    {
        if (decompiled != null && decompiledOpen)
        {
            ImGui.SetNextWindowSize(new Vector2(300,300), ImGuiCond.FirstUseEver);
            int j = 0;
            if (ImGui.Begin("Decompiled", ref decompiledOpen))
            {
                ImGui.BeginTabBar("##tabs", ImGuiTabBarFlags.Reorderable);
                foreach (var file in decompiled)
                {
                    var tab = ImGuiExt.IDWithExtra(file.Name, j++);
                    if (ImGui.BeginTabItem(tab))
                    {
                        if (ImGui.Button("Copy"))
                        {
                            win.SetClipboardText(file.Text);
                        }

                        ImGui.SetNextItemWidth(-1);
                        var th = ImGui.GetWindowHeight() - 100;
                        ImGui.PushFont(ImGuiHelper.SystemMonospace, 0);
                        var bufSize = Encoding.UTF8.GetByteCount(file.Text) + 1;
                        ImGui.InputTextMultiline("##src", ref file.Text, (uint)bufSize, new Vector2(0, th),
                            ImGuiInputTextFlags.ReadOnly);
                        ImGui.PopFont();
                        ImGui.EndTabItem();
                    }
                }
                ImGui.EndTabBar();
            }
        }
    }

    public override void Dispose()
    {
        cutscene?.Dispose();
        viewport.Dispose();
    }
}
