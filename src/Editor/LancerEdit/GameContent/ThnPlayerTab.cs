using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Thn;

namespace LancerEdit;

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
    }
    
    void Open(params string[] files)
    {
        var lastFile = Path.GetFileName(files.Last());
        Title = lastFile;
        toReload = files;
        decompiled = files.Select(x => new DecompiledThn()
        {
            Name = Path.GetFileName(x),
            Text = ThnDecompile.Decompile(x)
        }).ToArray();
        var ctx = new ThnScriptContext(null);
        cutscene = new Cutscene(ctx, gameData.GameData,  gameData.Resources, gameData.Sounds, new Rectangle(0,0,240,240), win);
        cutscene.BeginScene(files.Select(x => new ThnScript(x)));
    }
    
    void Reload()
    {
        if (toReload != null) Open(toReload);
    }

    public override void Update(double elapsed)
    {
        cutscene?.Update(elapsed);
    }

    public override void Draw()
    {
        if(ImGui.Button("Open"))
            FileDialog.Open(x => Open(x));
        ImGui.SameLine();
        if (ImGui.Button("Open Multiple")) {
            openFiles = new List<string>();
            ImGui.OpenPopup("Open Multiple##" + Unique);
        }
        if (decompiled != null) {
            ImGui.SameLine();
            if(ImGuiExt.ToggleButton("Decompiled", decompiledOpen)) decompiledOpen = !decompiledOpen;
        }
        ImGui.SameLine();
        if(ImGuiExt.Button("Reload", cutscene != null)) 
            Reload();
        
        viewport.Begin();
        cutscene?.UpdateViewport(new Rectangle(0,0, viewport.RenderWidth, viewport.RenderHeight));
        cutscene?.Draw(ImGui.GetIO().DeltaTime, viewport.RenderWidth, viewport.RenderHeight);
        viewport.End();
        
        bool popupopen = true;
        if (ImGui.BeginPopupModal("Open Multiple##" + Unique, ref popupopen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (ImGui.Button("+"))
            {
                FileDialog.Open(file => openFiles.Add(file));
            }
            ImGui.BeginChild("##files", new Vector2(200, 200), true, ImGuiWindowFlags.HorizontalScrollbar);
            int j = 0;
            foreach (var f in openFiles)
                ImGui.Selectable(ImGuiExt.IDWithExtra(f, j++));
            ImGui.EndChild();
            if (ImGuiExt.Button("Open", openFiles.Count > 0))
            {
                ImGui.CloseCurrentPopup();
                Open(openFiles.ToArray());
            }
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
                        ImGui.PushFont(ImGuiHelper.SystemMonospace);
                        ImGui.InputTextMultiline("##src", ref file.Text, uint.MaxValue, new Vector2(0, th),
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