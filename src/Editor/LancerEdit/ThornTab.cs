using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Thn;
using LibreLancer.Thorn;
using System;
using System.IO;
using System.Numerics;

namespace LancerEdit
{
    public class ThornTab : EditorTab
    {
        private readonly MainWindow window;
        private readonly ColorTextEdit colorTextEdit;
        private string lastError = null;
        private bool showErrorPopUp = false;
        private readonly Viewport3D thornViewport;
        private Cutscene cutscene = null;

        private string _filePath;

        public string FilePath {
            get
            {
                return _filePath;
            }
            private set 
            {
                _filePath = value;
                DocumentName = FilePath != null ? Path.GetFileName(FilePath) : "Untitled";
                Title = DocumentName;
            } 
        }

        public ThornTab(MainWindow win, string file, bool isSourceCode = false)
        {
            window = win;
            FilePath = file;
            colorTextEdit = new ColorTextEdit();
            colorTextEdit.SetMode(ColorTextEditMode.Lua);
            thornViewport = new Viewport3D(window);
            SaveStrategy = new ThornSaveStrategy(this);

            if (FilePath != null)
            {
                if (!isSourceCode)
                {
                    colorTextEdit.SetText(ThnDecompile.Decompile(FilePath));
                }
                else
                {
                    colorTextEdit.SetText(File.ReadAllText(FilePath));
                }
            }

            ReloadCutscene();
        }
        public override void Update(double elapsed)
        {
            cutscene?.Update(elapsed);
        }

        private void ReloadCutscene()
        {
            if (window.OpenDataContext != null)
            {
                try
                {                    
                    var source = colorTextEdit.GetText();
                    var compiledBytes = LuaCompiler.Compile(source, "");
                    lastError = null;
                    var thornScript = new ThnScript(compiledBytes);
                    var ctx = new ThnScriptContext(null);
                    cutscene?.Dispose();
                    cutscene = new Cutscene(ctx, window.OpenDataContext.GameData, window.OpenDataContext.Resources, window.OpenDataContext.Sounds, new Rectangle(0, 0, 240, 240), window);
                    cutscene.BeginScene(thornScript);
                    cutscene.Update(0.1f);
                }
                catch (LuaCompileException lce)
                {
                    lastError = lce.Message;
                    showErrorPopUp = true;
                }
            }            
        }

        public override void Draw()
        {
            var contentw = ImGui.GetContentRegionAvail().X;

            if (window.OpenDataContext != null)
            {
                ImGui.Columns(2, "##panels", true);
                ImGui.BeginChild("##thornViewer");
                DrawThornViewer();
                ImGui.EndChild();
                ImGui.NextColumn();
            }

            ImGui.BeginChild("##thornEditor");
            DrawThornEditor();
            ImGui.EndChild();

            if (lastError != null && showErrorPopUp)
            {
                ImGui.SetNextWindowSize(new Vector2(600, 300) * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
                if (ImGui.Begin("Compile Error"))
                {
                    if (ImGui.Button("Ok"))
                    {
                        showErrorPopUp = false;
                    }
                    ImGui.SetNextItemWidth(-1);
                    var th = ImGui.GetWindowHeight() - 100;
                    ImGui.PushFont(ImGuiHelper.SystemMonospace);
                    ImGui.InputTextMultiline("##lastError", ref lastError, uint.MaxValue, new Vector2(0, th),
                        ImGuiInputTextFlags.ReadOnly);
                    ImGui.PopFont();
                    ImGui.EndTabItem();
                }
            }
        }

        private void DrawThornViewer()
        {
            if (ImGui.Button("Reload"))
            {                
                ReloadCutscene();
            }
            int rpanelWidth = (int)ImGui.GetWindowWidth() - 20;
            int rpanelHeight = Math.Min((int)(rpanelWidth * (3.0 / 4.0)), 4096);
            ImGui.Spacing();
            thornViewport.Background = cutscene == null ? window.Config.Background : Color4.Black;
            thornViewport.Begin(rpanelWidth, rpanelHeight);
            if (cutscene != null)
            {
                ImGuiHelper.AnimatingElement();
                cutscene.UpdateViewport(new Rectangle(0, 0, thornViewport.RenderWidth, thornViewport.RenderHeight));
                cutscene.Draw(ImGui.GetIO().DeltaTime, thornViewport.RenderWidth, thornViewport.RenderHeight);
            }
            thornViewport.End();
        }

        private void DrawThornEditor()
        {
            if (lastError != null)
            {
                ImGui.SameLine();
                if (ImGui.Button("Show Last Error"))
                {
                    showErrorPopUp = true;
                }
            }

            colorTextEdit.Render("##ColorTextEditor");
        }

        private void CompileAndSave(string filePath)
        {
            var source = colorTextEdit.GetText();
            try
            {
                var compiledBytes = LuaCompiler.Compile(source, "");
                File.WriteAllBytes(filePath, compiledBytes);
                lastError = null;
            }
            catch (LuaCompileException lce)
            {
                lastError = lce.Message;
                showErrorPopUp = true;
            }
        }

        public void Save(string filePath)
        {
            if (filePath == null)
            {
                filePath = FilePath;
            }

            if (filePath.EndsWith(".lua"))
            {
                File.WriteAllText(filePath, colorTextEdit.GetText());
            }
            else
            {
                CompileAndSave(filePath);
            }
            FilePath = filePath;
        }
        
        public override void Dispose()
        {
            cutscene?.Dispose();
            thornViewport.Dispose();
        }
    }
}
