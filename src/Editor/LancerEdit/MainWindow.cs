// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.ImUI;
using LibreLancer.Media;
using ImGuiNET;
using LibreLancer.Data.Pilots;
using LibreLancer.Render;
using LibreLancer.Shaders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SharpDX.MediaFoundation;
using LibreLancer.Thorn;

namespace LancerEdit
{
	public class MainWindow : Game
	{
		public ImGuiHelper guiHelper;
		public AudioManager Audio;
		public GameResourceManager Resources;
        public Billboards Billboards;
		public PolylineRender Polyline;
		public LineRenderer LineRenderer;
		public CommandBuffer Commands; //This is a huge object - only have one
		public MaterialMap MaterialMap;
        public RichTextEngine RichText;
        public FontManager Fonts;
        public GameDataContext OpenDataContext;
        public string Version;
        TextBuffer logBuffer;
        StringBuilder logText = new StringBuilder();
        private RecentFilesHandler recentFiles;
        bool openError = false;
        bool finishLoading = false;

        public List<TextDisplayWindow> TextWindows = new List<TextDisplayWindow>();
        
        public EditorConfiguration Config;
        OptionsWindow options;
        public MainWindow() : base(800,600,false)
		{
            Version = "LancerEdit " + Platform.GetInformationalVersion<MainWindow>();
			MaterialMap = new MaterialMap();
			MaterialMap.AddRegex(new LibreLancer.Ini.StringKeyValue("^nomad.*$", "NomadMaterialNoBendy"));
			MaterialMap.AddRegex(new LibreLancer.Ini.StringKeyValue("^n-texture.*$", "NomadMaterialNoBendy"));
            FLLog.UIThread = this;
            FLLog.AppendLine = (x,severity) =>
            {
                logText.AppendLine(x);
                if (logText.Length > 16384)
                {
                    logText.Remove(0, logText.Length - 16384);
                }
                logBuffer.SetText(logText.ToString());
                //Suppressed while most game content logs errors
                /*if (severity == LogSeverity.Error)
                {
                    errorTimer = 9;
                    Bell.Play();
                }*/
            };
            Config = EditorConfiguration.Load();
            Config.LastExportPath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            logBuffer = new TextBuffer(32768);
            recentFiles = new RecentFilesHandler(OpenFile);
        }
        double errorTimer = 0;
        private int logoTexture;

        protected override bool UseSplash => true;

        protected override Texture2D GetSplash()
        {
            using (var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("LancerEdit.splash.png"))
            {
                return (Texture2D)LibreLancer.ImageLib.Generic.FromStream(stream);
            }
        }

        protected override void Load()
        {
            AllShaders.Compile();
            DefaultMaterialMap.Init();
            ZoneRenderer.Load();
			Title = "LancerEdit";
            guiHelper = new ImGuiHelper(this, DpiScale * Config.UiScale);
            guiHelper.PauseWhenUnfocused = Config.PauseWhenUnfocused;
            Audio = new AudioManager(this);
            Bell.Init(Audio);
            FileDialog.RegisterParent(this);
            options = new OptionsWindow(this);
            Resources = new GameResourceManager(this);
			Commands = new CommandBuffer();
			Polyline = new PolylineRender(Commands);
			LineRenderer = new LineRenderer();
            RenderContext.ReplaceViewport(0, 0, 800, 600);
            Keyboard.KeyDown += Keyboard_KeyDown;
            //TODO: Icon-setting code very messy
            using (var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("LancerEdit.reactor_64.png"))
            {
                var icon = LibreLancer.ImageLib.Generic.BytesFromStream(stream);
                SetWindowIcon(icon.Width, icon.Height, icon.Data);
            }
            using (var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("LancerEdit.reactor_128.png"))
            {
                var icon = (Texture2D)LibreLancer.ImageLib.Generic.FromStream(stream);
                logoTexture = ImGuiHelper.RegisterTexture(icon);
            }
            //Open passed in files!
            if(InitOpenFile != null)
                foreach(var f in InitOpenFile) 
                    OpenFile(f);
            RichText = RenderContext.Renderer2D.CreateRichTextEngine(); 
            Fonts = new FontManager();
            Fonts.ConstructDefaultFonts();
            Services.Add(Fonts);
            Billboards = new Billboards();
            Config.Validate(RenderContext);
            Services.Add(Billboards);
            Services.Add(Config);
            Make3dbDlg = new CommodityIconDialog(this);
            LoadScripts();
        }

        void Keyboard_KeyDown(KeyEventArgs e)
        {
            var mods = e.Modifiers;
            mods &= ~KeyModifiers.Numlock;
            mods &= ~KeyModifiers.Capslock;
            if ((mods == KeyModifiers.LeftControl || mods == KeyModifiers.RightControl) && e.Key == Keys.S) {
                if (TabControl.Selected != null) ((EditorTab)TabControl.Selected).SaveStrategy.Save();
            }
            if((mods == KeyModifiers.LeftControl || mods == KeyModifiers.RightControl) && e.Key == Keys.D) {
                if (TabControl.Selected != null) ((EditorTab)TabControl.Selected).OnHotkey(Hotkeys.Deselect);
            }
            if((mods == KeyModifiers.LeftControl || mods == KeyModifiers.RightControl) && e.Key == Keys.R) {
                if (TabControl.Selected != null) ((EditorTab)TabControl.Selected).OnHotkey(Hotkeys.ResetViewport);
            }
            if((mods == KeyModifiers.LeftControl || mods == KeyModifiers.RightControl) && e.Key == Keys.G) {
                if (TabControl.Selected != null) ((EditorTab)TabControl.Selected).OnHotkey(Hotkeys.ToggleGrid);
            }
            if (e.Key == Keys.F6) {
                if (TabControl.Selected != null) ((EditorTab)TabControl.Selected).OnHotkey(Hotkeys.ChangeSystem);
            }
        }


		bool openAbout = false;
        public TabControl TabControl = new TabControl();
		public List<MissingReference> MissingResources = new List<MissingReference>();
		public List<uint> ReferencedMaterials = new List<uint>();
		public List<string> ReferencedTextures = new List<string>();
		public bool ClipboardCopy = true;
		public LUtfNode Clipboard;
		List<DockTab> toAdd = new List<DockTab>();
		double frequency = 0;
		int updateTime = 10;
        public CommodityIconDialog Make3dbDlg;
		public void AddTab(DockTab tab)
		{
			toAdd.Add(tab);
		}
		protected override void Update(double elapsed)
        {
            if (!guiHelper.DoUpdate()) return;
			foreach (var tab in TabControl.Tabs)
				tab.Update(elapsed);
            if (errorTimer > 0) errorTimer -= elapsed;
            Audio.UpdateAsync().Wait();
        }
        public string[] InitOpenFile;
        public void OpenFile(string f)
        {
            if (f != null && File.Exists(f))
            {
                var detectedType = DetectFileType.Detect(f);
                switch (detectedType)
                {
                    case FileType.Utf:
                        var t = new UtfTab(this, new EditableUtf(f), Path.GetFileName(f));
                        recentFiles.FileOpened(f);
                        t.FilePath = f;
                        AddTab(t);
                        guiHelper.ResetRenderTimer();
                        break;
                    case FileType.Thn:
                    case FileType.Lua:
                        var lt = new ThornTab(this, f);
                        recentFiles.FileOpened(f);                        
                        AddTab(lt);
                        break;
                    case FileType.Blender:
                    case FileType.Other:
                        TryImportModel(f);
                        break;
                }
               
            }
        }

        private PopupManager popups = new PopupManager();

        bool showLog = false;
        float h1 = 200, h2 = 200;
        Vector2 errorWindowSize = Vector2.Zero;
        public double TimeStep;
        private RenderTarget2D lastFrame;
        private bool loadingSpinnerActive = false;
        bool openLoading = false;
        
        public void StartLoadingSpinner()
        {
            QueueUIThread(() =>
            {
                openLoading = true;
                finishLoading = false;
                loadingSpinnerActive = true;
            });
        }

        public void FinishLoadingSpinner()
        {
            QueueUIThread(() =>
            {
                loadingSpinnerActive = false;
                finishLoading = true;
            });
        }

        public List<EditScript> Scripts = new List<EditScript>();

        IEnumerable<string> GetScriptFiles(IEnumerable<string> directories)
        {
            foreach (var dir in directories)
            {
                if(!Directory.Exists(dir)) continue;
                foreach (var f in Directory.GetFiles(dir, "*.cs-script"))
                {
                    yield return f;
                }
            }
        }
        
        private string GetBasePath()
        {
            using var processModule = Process.GetCurrentProcess().MainModule;
            return Path.GetDirectoryName(processModule?.FileName);
        }

        string GetAssemblyFolder()
        {
            return Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
        }
        public void LoadScripts()
        {
            Scripts = new List<EditScript>();
            var scriptDirs = new List<string>(2);
            var baseDir = Path.Combine(GetBasePath(), "editorscripts");
            scriptDirs.Add(baseDir);
            var asmDir = Path.Combine(GetAssemblyFolder(), "editorscripts");
            if (asmDir != baseDir) scriptDirs.Add(asmDir);
            foreach (var file in GetScriptFiles(scriptDirs))
            {
                try
                {
                    var sc = new EditScript(file);
                    if (string.IsNullOrEmpty(sc.Info?.Name)) continue;
                    if(sc.Validate()) Scripts.Add(sc);
                    else FLLog.Error("Scripts", $"Failed to Validate {file}");
                }
                catch (Exception)
                {
                    FLLog.Error("Scripts", $"Failed to Validate {file}");
                }
            }
        }
        
        private List<ScriptRunner> activeScripts = new List<ScriptRunner>();
        public void RunScript(EditScript sc)
        {
            activeScripts.Add(new ScriptRunner(sc, this));
        }

        void TryImportModel(string filename)
        {
            StartLoadingSpinner();
            Task.Run(() =>
            {
                EditResult<SimpleMesh.Model> model = null;
                if (Blender.FileIsBlender(filename))
                {
                    model = Blender.LoadBlenderFile(filename, Config.BlenderPath);
                }
                else
                {
                    using var stream = File.OpenRead(filename);
                    model = EditResult<SimpleMesh.Model>.TryCatch(() =>
                        SimpleMesh.Model.FromStream(stream));
                }

                QueueUIThread(() => ResultMessages(model));
                if (model.IsSuccess)
                {
                    var mdl = model.Data.AutoselectRoot(out _).ApplyScale();
                    var x = Vector3.Transform(Vector3.Zero, mdl.Roots[0].Transform);
                    bool modelWarning = x.Length() > 0.0001;
                    mdl = mdl.ApplyRootTransforms(false).CalculateBounds();
                    QueueUIThread(() => FinishImporterLoad(mdl, modelWarning, Path.GetFileName(filename)));
                }
            });
        }

        void OpenGameData()
        {
            FileDialog.ChooseFolder(folder =>
            {
                if (!GameConfig.CheckFLDirectory(folder))
                    ErrorDialog("Selected directory is not a valid Freelancer folder");
                else
                {
                    if (OpenDataContext != null)
                    {
                        var toClose = TabControl.Tabs.OfType<GameContentTab>().ToArray();
                        foreach (var t in toClose)
                        {
                           TabControl.CloseTab(t);
                        }
                        OpenDataContext.Dispose();
                        OpenDataContext = null;
                    }
                    var c = new GameDataContext();
                    StartLoadingSpinner();
                    c.Load(this, folder, () =>
                    {
                        OpenDataContext = c;
                        Resources = c.Resources;
                        FinishLoadingSpinner();
                    });                
                }
            });
        }
      
		protected override void Draw(double elapsed)
        {
            //Don't process all the imgui stuff when it isn't needed
            if (!loadingSpinnerActive && !guiHelper.DoRender(elapsed))
            {
                if(Width !=0 && Height != 0 && lastFrame != null)
                    lastFrame.BlitToScreen();
                WaitForEvent(); //Yield like a regular GUI program
                return;
            }
            //
            TimeStep = elapsed;
			RenderContext.ReplaceViewport(0, 0, Width, Height);
			RenderContext.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
			RenderContext.ClearAll();
			guiHelper.NewFrame(elapsed);
			ImGui.PushFont(ImGuiHelper.Noto);
			ImGui.BeginMainMenuBar();
			if (ImGui.BeginMenu("File"))
            {
                var lst = ImGui.GetWindowDrawList();
				if (Theme.IconMenuItem(Icons.File, "New", true))
				{
					var t = new UtfTab(this, new EditableUtf(), "Untitled");
                    AddTab(t);
				}
                if (Theme.IconMenuItem(Icons.Open, "Open", true))
				{
                    FileDialog.Open(OpenFile, FileDialogFilters.UtfFilters + FileDialogFilters.ThnFilters);
                }

                recentFiles.Menu();

                if (TabControl.Selected is EditorTab editorTab)
                    editorTab.SaveStrategy.DrawMenuOptions();
                else
                    NoSaveStrategy.Instance.DrawMenuOptions();

				if (Theme.IconMenuItem(Icons.Quit, "Quit", true))
				{
					Exit();
				}
				ImGui.EndMenu();
			}
            if (ImGui.BeginMenu("View"))
            {
                Theme.IconMenuToggle(Icons.Log, "Log", ref showLog, true);
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Data"))
            {
                if (Theme.IconMenuItem(Icons.Info, "Load Data", true))
                {
                    var dataTabCount = TabControl.Tabs.OfType<GameContentTab>().Count();
                    if (dataTabCount > 0)
                        Confirm($"Opening another directory will close {dataTabCount} tab(s). Continue?", OpenGameData);
                    else
                        OpenGameData();
                }
                if(Theme.IconMenuItem(Icons.BookOpen, "Infocard Browser",OpenDataContext != null))
                    AddTab(new InfocardBrowserTab(OpenDataContext, this));
                if (Theme.IconMenuItem(Icons.Fire, "Projectile Viewer", OpenDataContext != null))
                    AddTab(new ProjectileViewerTab(this, OpenDataContext));
                if (Theme.IconMenuItem(Icons.Globe, "System Viewer", OpenDataContext != null))
                    AddTab(new SystemViewerTab(OpenDataContext, this));
                if (Theme.IconMenuItem(Icons.Play, "Thn Player", OpenDataContext != null))
                    AddTab(new ThnPlayerTab(OpenDataContext, this));
                ImGui.EndMenu();
            }
			if (ImGui.BeginMenu("Tools"))
			{
                if(Theme.IconMenuItem(Icons.Cog, "Options",true))
                {
                    options.Show();
                }
               
				if (Theme.IconMenuItem(Icons.Palette, "Resources",true))
				{
					AddTab(new ResourcesTab(this, Resources, MissingResources, ReferencedMaterials, ReferencedTextures));
				}
                if(Theme.IconMenuItem(Icons.FileImport, "Import Model",true))
                {
                    var filters = Blender.BlenderPathValid(Config.BlenderPath)
                        ? FileDialogFilters.ImportModelFilters
                        : FileDialogFilters.ImportModelFiltersNoBlender;
                    FileDialog.Open(TryImportModel, filters);
                }
                if (Theme.IconMenuItem(Icons.SprayCan, "Generate Icon", true))
                {
                    FileDialog.Open(input => Make3dbDlg.Open(input), FileDialogFilters.ImageFilter);
                }
                if (Theme.IconMenuItem(Icons.Table, "State Graph", true))
                {
                    FileDialog.Open(
                        input => AddTab(new StateGraphTab(new StateGraphDb(input, null), Path.GetFileName(input))),
                        FileDialogFilters.StateGraphFilter
                        );
                }

                if (Theme.IconMenuItem(Icons.BezierCurve, "ParamCurve Visualiser", true))
                {
                    TabControl.Tabs.Add(new ParamCurveVis());
                }
                ImGui.EndMenu();
			}
            if (ImGui.BeginMenu("Window"))
            {
                if (ImGui.MenuItem("Close All Tabs", TabControl.Tabs.Count > 0))
                {
                    Confirm("Are you sure you want to close all tabs?", () =>
                    {
                        TabControl.CloseAll();
                    });
                }
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Scripts"))
            {
                if (ImGui.MenuItem("Refresh")) {
                    LoadScripts();
                }
                ImGui.Separator();
                int k = 0;
                foreach (var sc in Scripts)
                {
                    var n = ImGuiExt.IDWithExtra(sc.Info.Name, k++);
                    if (ImGui.MenuItem(n)) {
                        RunScript(sc);
                    }
                }
                ImGui.EndMenu();
            }
			if (ImGui.BeginMenu("Help"))
			{
                if(Theme.IconMenuItem(Icons.Book, "Topics", true))
                {
                    var selfPath = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
                    var helpFile = Path.Combine(selfPath, "Docs", "index.html");
                    Shell.OpenCommand(helpFile);
                }
				if (Theme.IconMenuItem(Icons.Info, "About", true))
				{
					openAbout = true;
				}
				ImGui.EndMenu();
			}

            options.Draw();

			if (openAbout)
			{
				ImGui.OpenPopup("About");
				openAbout = false;
			}

            if (openLoading)
            {
                ImGui.OpenPopup("Processing");
                openLoading = false;
            }

            for (int i = activeScripts.Count - 1; i >= 0; i--)
            {
                if (!activeScripts[i].Draw()) activeScripts.RemoveAt(i);
            }
            bool pOpen = true;

            popups.Run();
            recentFiles.DrawErrors();
            pOpen = true;
			if (ImGui.BeginPopupModal("About", ref pOpen, ImGuiWindowFlags.AlwaysAutoResize))
			{
                ImGui.SameLine(ImGui.GetWindowWidth() / 2 - 64);
                ImGui.Image((IntPtr) logoTexture, new Vector2(128), new Vector2(0, 1), new Vector2(1, 0));
                CenterText(Version);
				CenterText("Callum McGing 2018-2023");
                ImGui.Separator();
                var btnW = ImGui.CalcTextSize("OK").X + ImGui.GetStyle().FramePadding.X * 2;
                ImGui.Dummy(Vector2.One);
                ImGui.SameLine(ImGui.GetWindowWidth() / 2 - (btnW / 2));
				if (ImGui.Button("OK")) ImGui.CloseCurrentPopup();
				ImGui.EndPopup();
			}
            pOpen = true;
            if(ImGuiExt.BeginModalNoClose("Processing", ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGuiExt.Spinner("##spinner", 10, 2, ImGui.GetColorU32(ImGuiCol.ButtonHovered, 1));
                ImGui.SameLine();
                ImGui.Text("Processing");
                if (finishLoading) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
            //Confirmation
            if (doConfirm)
            {
                ImGui.OpenPopup("Confirm?##mainwindow");
                doConfirm = false;
            }
            pOpen = true;
            if (ImGui.BeginPopupModal("Confirm?##mainwindow", ref pOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text(confirmText);
                if (ImGui.Button("Yes"))
                {
                    confirmAction();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("No")) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
            var menu_height = ImGui.GetWindowSize().Y;
			ImGui.EndMainMenuBar();
			var size = ImGui.GetIO().DisplaySize;
			size.Y -= menu_height;
			//Window
			MissingResources.Clear();
			ReferencedMaterials.Clear();
			ReferencedTextures.Clear();
			foreach (var tab in TabControl.Tabs.OfType<EditorTab>())
			{
                tab.DetectResources(MissingResources, ReferencedMaterials, ReferencedTextures);
			}
            ImGui.SetNextWindowSize(new Vector2(size.X, size.Y - (22 * ImGuiHelper.Scale)), ImGuiCond.Always);
            ImGui.SetNextWindowPos(new Vector2(0, menu_height), ImGuiCond.Always, Vector2.Zero);
            bool childopened = true;
            ImGui.Begin("tabwindow", ref childopened,
                              ImGuiWindowFlags.NoTitleBar |
                              ImGuiWindowFlags.NoSavedSettings |
                              ImGuiWindowFlags.NoBringToFrontOnFocus |
                              ImGuiWindowFlags.NoMove |
                              ImGuiWindowFlags.NoResize);
            
            TabControl.TabLabels();
            var totalH = ImGui.GetWindowHeight();
            if (showLog)
            {
                ImGuiExt.SplitterV(2f, ref h1, ref h2, 8, 8, -1);
                h1 = totalH - h2 - 24f * ImGuiHelper.Scale;
                if (TabControl.Tabs.Count > 0) h1 -= 20f * ImGuiHelper.Scale;
                ImGui.BeginChild("###tabcontent" + (TabControl.Selected != null ? TabControl.Selected.RenderTitle : ""),new Vector2(-1,h1),false,ImGuiWindowFlags.None);
            } else
                ImGui.BeginChild("###tabcontent" + (TabControl.Selected != null ? TabControl.Selected.RenderTitle : ""));

            TabControl.Selected?.Draw();

            ImGui.EndChild();
            if(showLog) {
                ImGui.BeginChild("###log", new Vector2(-1, h2), false, ImGuiWindowFlags.None);
                ImGui.Text("Log");
                ImGui.SameLine(ImGui.GetWindowWidth() - 30 * ImGuiHelper.Scale);
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
                if (ImGui.Button(Icons.X.ToString())) showLog = false;
                ImGui.PopStyleVar();
                logBuffer.InputTextMultiline("##logtext", new Vector2(-1, h2 - 28 * ImGuiHelper.Scale), ImGuiInputTextFlags.ReadOnly);
                ImGui.EndChild();
            }
            ImGui.End();
            Make3dbDlg.Draw();
            for (int i = TextWindows.Count - 1; i >= 0; i--) {
                if (!TextWindows[i].Draw()) {
                    TextWindows.RemoveAt(i);
                }
            }
			//Status bar
			ImGui.SetNextWindowSize(new Vector2(size.X, 22f * ImGuiHelper.Scale), ImGuiCond.Always);
			ImGui.SetNextWindowPos(new Vector2(0, size.Y - 6f), ImGuiCond.Always, Vector2.Zero);
			bool sbopened = true;
			ImGui.Begin("statusbar", ref sbopened, 
			                  ImGuiWindowFlags.NoTitleBar | 
			                  ImGuiWindowFlags.NoSavedSettings | 
			                  ImGuiWindowFlags.NoBringToFrontOnFocus | 
			                  ImGuiWindowFlags.NoMove | 
			                  ImGuiWindowFlags.NoResize);
			if (updateTime > 9)
			{
				updateTime = 0;
				frequency = RenderFrequency;
			}
			else { updateTime++; }

            string activename = TabControl.Selected == null ? "None" : TabControl.Selected.DocumentName;
            if (TabControl.Selected is UtfTab utftab)
            {
                activename += " - " + utftab.GetUtfPath();
            }
#if DEBUG
            const string statusFormat = "FPS: {0} | {1} Materials | {2} Textures | Active: {3}{4}";
#else
            const string statusFormat = "{1} Materials | {2} Textures | Active: {3}{4}";
#endif
            string openFolder = OpenDataContext != null ? $" | Open: {OpenDataContext.Folder}" : "";
			ImGui.Text(string.Format(statusFormat,
									 (int)Math.Round(frequency),
									 Resources.MaterialDictionary.Count,
									 Resources.TextureDictionary.Count,
									 activename,
                                     openFolder));
			ImGui.End();
            if(errorTimer > 0) {
                ImGuiExt.ToastText("An error has occurred\nCheck the log for details",
                                   new Color4(21, 21, 22, 128),
                                   Color4.Red);
            }
            ImGui.PopFont();
            if (Width != 0 && Height != 0)
            {
                if (lastFrame == null ||
                    lastFrame.Width != Width ||
                    lastFrame.Height != Height)
                {
                    if (lastFrame != null) lastFrame.Dispose();
                    lastFrame = new RenderTarget2D(Width, Height);
                }

                RenderContext.RenderTarget = lastFrame;
                RenderContext.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
                RenderContext.ClearAll();
                guiHelper.Render(RenderContext);
                RenderContext.RenderTarget = null;
                lastFrame.BlitToScreen();
            }
            foreach (var tab in toAdd)
            {
                TabControl.Tabs.Add(tab);
                TabControl.SetSelected(tab);
            }
            toAdd.Clear();
		}
        
        string confirmText;
        bool doConfirm = false;
        Action confirmAction;

        public void Confirm(string text, Action action)
        {
            doConfirm = true;
            confirmAction = action;
            confirmText = text;
        }

        void CenterText(string text)
        {
            ImGui.Dummy(new Vector2(1));
            var win = ImGui.GetWindowWidth();
            var txt = ImGui.CalcTextSize(text).X;
            ImGui.SameLine(Math.Max((win / 2f) - (txt / 2f),0));
            ImGui.Text(text);
        }
        void FinishImporterLoad(SimpleMesh.Model model, bool warnOffCenter, string tabName)
        {
           FinishLoadingSpinner();
           if (warnOffCenter)
           {
               Confirm("Model root is off-center, consider re-exporting.\n\nImport anyway?", () =>
               {
                   AddTab(new ImportModelTab(model, tabName, this));
               });
           }
           else
           {
               AddTab(new ImportModelTab(model, tabName, this));
           }
            
        }
        
        public void ResultMessages<T>(EditResult<T> result)
        {
            if (result.Messages.Count == 0) return;
            string text;
            if (result.Messages.Count == 1)
            {
                text = result.Messages[0].Message;
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var msg in result.Messages)
                    sb.Append(msg.Kind).Append(": ").AppendLine(msg.Message);
                text = sb.ToString();
            }
            popups.MessageBox(result.IsError ? "Error" : "Warning", text);
        }

        public void ErrorDialog(string text) =>  popups.MessageBox("Error", text);
        
        protected override void OnDrop(string file) => OpenFile(file);
        
        protected override void Cleanup()
		{
			Audio.Dispose();
		}
	}
}
