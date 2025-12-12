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
    public sealed class ThornTab : EditorTab
    {
        private readonly MainWindow window;
        private readonly ColorTextEdit colorTextEdit;
        private readonly Viewport3D thornViewport;

        private string lastError = null;
        private bool showErrorPopUp = false;
        private Cutscene cutscene = null;
        private bool showNotSourceMessage = true;

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

        public bool IsSourceCode { get; private set; }

        public ThornTab(MainWindow window, string file)
        {
            this.window = window;
            FilePath = file;
            colorTextEdit = new ColorTextEdit();
            colorTextEdit.SetMode(ColorTextEditMode.Lua);
            thornViewport = new Viewport3D(window);
            thornViewport.EnableMSAA = false;
            thornViewport.Draw3D = DrawGL;
            SaveStrategy = new ThornSaveStrategy(this);

            Reload();
        }

        public void Reload()
        {
            if (FilePath != null)
            {
                var fileType = DetectFileType.Detect(FilePath);
                if (fileType == FileType.Thn)
                {
                    colorTextEdit.SetText(ThnDecompile.Decompile(FilePath));
                    IsSourceCode = false;
                }
                else if (fileType == FileType.Lua)
                {
                    colorTextEdit.SetText(File.ReadAllText(FilePath));
                    IsSourceCode = true;
                }
                else
                {
                    throw new NotSupportedException("Attempt to load THN file but detected unknown file type");
                }
            }

            RefreshCutscene();
        }

        public override void Update(double elapsed)
        {
            cutscene?.Update(elapsed);
        }

        private void RefreshCutscene()
        {
            if (window.OpenDataContext != null)
            {
                try
                {
                    var source = colorTextEdit.GetText();
                    var compiledBytes = ThornCompiler.Compile(source, "");
                    lastError = null;
                    var thornScript = new ThnScript(compiledBytes, null, "[SOURCE]");
                    var ctx = new ThnScriptContext(null);
                    cutscene?.Dispose();
                    cutscene = new Cutscene(ctx,
                        window.OpenDataContext.GameData,
                        window.OpenDataContext.Resources,
                        window.OpenDataContext.Sounds,
                        new Rectangle(0, 0, 240, 240), window);
                    cutscene.BeginScene(thornScript);
                    cutscene.Update(0.1f);
                }
                catch (ThornCompileException lce)
                {
                    lastError = lce.Message;
                    showErrorPopUp = true;
                }
            }
        }

        public override void Draw(double elapsed)
        {
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
                ImGui.SetNextWindowSize(new Vector2(600, 300) * ImGuiHelper.Scale,
                    ImGuiCond.FirstUseEver);
                if (ImGui.Begin("Compile Error"))
                {
                    if (ImGui.Button("Ok"))
                    {
                        showErrorPopUp = false;
                    }
                    ImGui.SetNextItemWidth(-1);
                    var th = ImGui.GetWindowHeight() - 100;
                    ImGui.PushFont(ImGuiHelper.SystemMonospace, 0);
                    ImGui.InputTextMultiline("##lastError",
                        ref lastError,
                        uint.MaxValue,
                        new Vector2(0, th),
                        ImGuiInputTextFlags.ReadOnly);
                    ImGui.PopFont();
                    ImGui.EndTabItem();
                }
            }
        }

        void DrawGL(int w, int h)
        {
            if (cutscene != null)
            {
                ImGuiHelper.AnimatingElement();
                cutscene.UpdateViewport(new Rectangle(0, 0, w,h), (float)w / h);
                cutscene.Draw(ImGui.GetIO().DeltaTime,w,h);
            }
        }

        private void DrawThornViewer()
        {
            if (ImGui.Button("Refresh"))
            {
                RefreshCutscene();
            }
            int rpanelWidth = (int)ImGui.GetWindowWidth() - 20;
            int rpanelHeight = Math.Min((int)(rpanelWidth * (3.0 / 4.0)), 4096);
            ImGui.Spacing();
            thornViewport.Background = cutscene == null ? window.Config.Background : Color4.Black;
            thornViewport.Draw(rpanelWidth, rpanelHeight);
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
            if (!IsSourceCode && showNotSourceMessage)
            {
                ImGui.SameLine();
                var bb = ImGui.CalcTextSize(COMPILED_THN_MESSAGE);
                var pos = ImGui.GetCursorScreenPos();
                ImGui.PushStyleColor(ImGuiCol.Text, (VertexDiffuse)Color4.Yellow);
                ImGui.Text(COMPILED_THN_MESSAGE);
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
                if (ImGui.Button("X", new Vector2(20, bb.Y)))
                {
                    showNotSourceMessage = false;
                }
                ImGui.PopStyleVar();
            }

            colorTextEdit.Render("##ColorTextEditor");
        }

        private const string COMPILED_THN_MESSAGE = "Compiled THN files do not support preservation of comments or formatting, consider exporting as source code.";

        private void CompileAndSave(string filePath)
        {
            var source = colorTextEdit.GetText();
            try
            {
                var compiledBytes = ThornCompiler.Compile(source, "");
                File.WriteAllBytes(filePath, compiledBytes);
                lastError = null;
            }
            catch (ThornCompileException lce)
            {
                lastError = lce.Message;
                showErrorPopUp = true;
            }
        }

        public void ExportCompiled(string filePath)
        {
            var source = colorTextEdit.GetText();
            try
            {
                var compiledBytes = ThornCompiler.Compile(source, "");
                File.WriteAllBytes(filePath, compiledBytes);
                lastError = null;
            }
            catch (ThornCompileException lce)
            {
                lastError = lce.Message;
                showErrorPopUp = true;
            }
            if (Path.GetFullPath(filePath) == Path.GetFullPath(FilePath)) Reload();
        }

        public void ExportSource(string filePath)
        {
            File.WriteAllText(filePath, colorTextEdit.GetText());
            if (Path.GetFullPath(filePath) == Path.GetFullPath(FilePath)) Reload();
        }

        public void Save(string filePath)
        {
            if (filePath == null)
            {
                filePath = FilePath;
            }

            if (filePath.EndsWith(".lua"))
            {
                ExportSource(filePath);
            }
            else
            {
                ExportCompiled(filePath);
            }
            FilePath = filePath;
            window.OnSaved();
        }

        public override void Dispose()
        {
            cutscene?.Dispose();
            thornViewport.Dispose();
        }
    }
}
