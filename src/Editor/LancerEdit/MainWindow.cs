// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.ImUI;
using LibreLancer.Media;
using ImGuiNET;

namespace LancerEdit
{
	public class MainWindow : Game
	{
		public ImGuiHelper guiHelper;
		public AudioManager Audio;
		public GameResourceManager Resources;
		public Billboards Billboards;
		public PolylineRender Polyline;
		public PhysicsDebugRenderer DebugRender;
		public ViewportManager Viewport;
		public CommandBuffer Commands; //This is a huge object - only have one
		public MaterialMap MaterialMap;
        public Renderer2D Renderer2D;
        public RichTextEngine RichText;
        public FontManager Fonts;
        public string Version;
        TextBuffer logBuffer;
        StringBuilder logText = new StringBuilder();
      
        bool openError = false;
        bool finishLoading = false;

        FileDialogFilters UtfFilters = new FileDialogFilters(
            new FileFilter("All Utf Files","utf","cmp","3db","dfm","vms","sph","mat","txm","ale","anm"),
            new FileFilter("Utf Files","utf"),
            new FileFilter("Anm Files","anm"),
            new FileFilter("Cmp Files","cmp"),
            new FileFilter("3db Files","3db"),
            new FileFilter("Dfm Files","dfm"),
            new FileFilter("Vms Files","vms"),
            new FileFilter("Sph Files","sph"),
            new FileFilter("Mat Files","mat"),
            new FileFilter("Txm Files","txm"),
            new FileFilter("Ale Files","ale")
        );
        FileDialogFilters ColladaFilters = new FileDialogFilters(
            new FileFilter("Collada Files", "dae")
        );
        FileDialogFilters FreelancerIniFilter = new FileDialogFilters(
            new FileFilter("Freelancer.ini","freelancer.ini")
        );
        FileDialogFilters ImageFilter = new FileDialogFilters(
            new FileFilter("Images", "bmp", "png", "tga", "dds", "jpg", "jpeg")
        );
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
                if (severity == LogSeverity.Error)
                {
                    errorTimer = 9;
                    Bell.Play();
                }
            };
            Config = EditorConfiguration.Load();
            logBuffer = new TextBuffer(32768);
        }
        double errorTimer = 0;
		protected override void Load()
		{
			Title = "LancerEdit";
            guiHelper = new ImGuiHelper(this);
            guiHelper.PauseWhenUnfocused = Config.PauseWhenUnfocused;
            Audio = new AudioManager(this);
            FileDialog.RegisterParent(this);
			Viewport = new ViewportManager(RenderState);
            options = new OptionsWindow(this);
            Resources = new GameResourceManager(this);
			Commands = new CommandBuffer();
			Billboards = new Billboards();
			Polyline = new PolylineRender(Commands);
			DebugRender = new PhysicsDebugRenderer();
            Viewport.Push(0, 0, 800, 600);
            Keyboard.KeyDown += Keyboard_KeyDown;
            //TODO: Icon-setting code very messy
            using (var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("LancerEdit.reactor_64.png"))
            {
                var icon = LibreLancer.ImageLib.Generic.BytesFromStream(stream);
                SetWindowIcon(icon.Width, icon.Height, icon.Data);
            }
            //Open passed in files!
            if(InitOpenFile != null)
                foreach(var f in InitOpenFile) 
                    OpenFile(f);
            Renderer2D = new Renderer2D(RenderState);
            RichText = Renderer2D.CreateRichTextEngine();
            Services.Add(Renderer2D);
            Fonts = new FontManager();
            Fonts.ConstructDefaultFonts();
            Services.Add(Fonts);
            gen3dbDlg = new CommodityIconDialog(this);
        }

        void Keyboard_KeyDown(KeyEventArgs e)
        {
            var mods = e.Modifiers;
            mods &= ~KeyModifiers.Numlock;
            mods &= ~KeyModifiers.Capslock;
            if ((mods == KeyModifiers.LeftControl || mods == KeyModifiers.RightControl) && e.Key == Keys.S) {
                if (ActiveTab != null) Save();
            }
            if((mods == KeyModifiers.LeftControl || mods == KeyModifiers.RightControl) && e.Key == Keys.D) {
                if (selected != null) ((EditorTab)selected).OnHotkey(Hotkeys.Deselect);
            }
            if((mods == KeyModifiers.LeftControl || mods == KeyModifiers.RightControl) && e.Key == Keys.R) {
                if (selected != null) ((EditorTab)selected).OnHotkey(Hotkeys.ResetViewport);
            }
            if((mods == KeyModifiers.LeftControl || mods == KeyModifiers.RightControl) && e.Key == Keys.G) {
                if (selected != null) ((EditorTab)selected).OnHotkey(Hotkeys.ToggleGrid);
            }
        }


		bool openAbout = false;
		public List<DockTab> tabs = new List<DockTab>();
		public List<MissingReference> MissingResources = new List<MissingReference>();
		public List<uint> ReferencedMaterials = new List<uint>();
		public List<string> ReferencedTextures = new List<string>();
		public bool ClipboardCopy = true;
		public LUtfNode Clipboard;
		List<DockTab> toAdd = new List<DockTab>();
		public UtfTab ActiveTab;
		double frequency = 0;
		int updateTime = 10;
        CommodityIconDialog gen3dbDlg;
		public void AddTab(DockTab tab)
		{
			toAdd.Add(tab);
		}
		protected override void Update(double elapsed)
        {
            if (!guiHelper.DoUpdate()) return;
			foreach (var tab in tabs)
				tab.Update(elapsed);
            if (errorTimer > 0) errorTimer -= elapsed;
		}
        public string[] InitOpenFile;
        public void OpenFile(string f)
        {
            if (f != null && System.IO.File.Exists(f) && DetectFileType.Detect(f) == FileType.Utf)
            {
                var t = new UtfTab(this, new EditableUtf(f), System.IO.Path.GetFileName(f));
                t.FilePath = f;
                ActiveTab = t;
                AddTab(t);
                guiHelper.ResetRenderTimer();
            }
        }
        DockTab selected;
        TextBuffer errorText;
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
        
		protected override void Draw(double elapsed)
        {
            //Don't process all the imgui stuff when it isn't needed
            if (!loadingSpinnerActive && !guiHelper.DoRender(elapsed))
            {
                if (lastFrame != null) lastFrame.BlitToScreen();
                WaitForEvent(); //Yield like a regular GUI program
                return;
            }
            TimeStep = elapsed;
			Viewport.Replace(0, 0, Width, Height);
			RenderState.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
			RenderState.ClearAll();
			guiHelper.NewFrame(elapsed);
			ImGui.PushFont(ImGuiHelper.Noto);
			ImGui.BeginMainMenuBar();
			if (ImGui.BeginMenu("File"))
			{
				if (Theme.IconMenuItem("New", "new", Color4.White, true))
				{
					var t = new UtfTab(this, new EditableUtf(), "Untitled");
					ActiveTab = t;
                    AddTab(t);
				}
				if (Theme.IconMenuItem("Open", "open", Color4.White, true))
				{
                    var f = FileDialog.Open(UtfFilters);
                    OpenFile(f);
				}
				if (ActiveTab == null)
				{
					Theme.IconMenuItem("Save", "save", Color4.LightGray, false);
                    Theme.IconMenuItem("Save As", "saveas", Color4.LightGray, false);
                }
				else
				{
					if (Theme.IconMenuItem(string.Format("Save '{0}'", ActiveTab.DocumentName), "saveas", Color4.White, true))
                    {
                        Save();
                    }
                    if (Theme.IconMenuItem("Save As", "saveas", Color4.White, true))
                    {
                        SaveAs();
                    }
				}
				if (Theme.IconMenuItem("Quit", "quit", Color4.White, true))
				{
					Exit();
				}
				ImGui.EndMenu();
			}
            if (ImGui.BeginMenu("View"))
            {
                Theme.IconMenuToggle("Log", "log", Color4.White, ref showLog, true);
                ImGui.EndMenu();
            }
			if (ImGui.BeginMenu("Tools"))
			{
                if(Theme.IconMenuItem("Options","options",Color4.White,true))
                {
                    options.Show();
                }
               
				if (Theme.IconMenuItem("Resources","resources",Color4.White,true))
				{
					AddTab(new ResourcesTab(this, Resources, MissingResources, ReferencedMaterials, ReferencedTextures));
				}
                if(Theme.IconMenuItem("Import Collada","import",Color4.White,true))
                {
                    string input;
                    if((input = FileDialog.Open(ColladaFilters)) != null)
                    {
                        StartLoadingSpinner();
                        new Thread(() =>
                        {
                            List<ColladaObject> dae = null;
                            try
                            {
                                dae = ColladaSupport.Parse(input);
                                EnsureUIThread(() => FinishColladaLoad(dae, System.IO.Path.GetFileName(input)));
                            }
                            catch (Exception ex)
                            {
                                EnsureUIThread(() => ColladaError(ex));
                            }
                        }).Start();
                    }
                }
                if (Theme.IconMenuItem("Generate Icon", "genicon", Color4.White, true))
                {
                    string input;
                    if ((input = FileDialog.Open(ImageFilter)) != null) {
                        gen3dbDlg.Open(input);
                    }
                }
                if(Theme.IconMenuItem("Infocard Browser","browse",Color4.White,true))
                {
                    string input;
                    if((input = FileDialog.Open(FreelancerIniFilter)) != null) {
                        AddTab(new InfocardBrowserTab(input, this));
                    }
                }
                if (ImGui.MenuItem("Projectile Viewer"))
                {
                    if(ProjectileViewer.Create(this, out var pj))
                        tabs.Add(pj);
                }

                if (ImGui.MenuItem("ParamCurve Visualiser"))
                {
                    tabs.Add(new ParamCurveVis());
                }
                ImGui.EndMenu();
			}
			if (ImGui.BeginMenu("Help"))
			{
                if(Theme.IconMenuItem("Topics","help",Color4.White,true)) {
                    Shell.OpenCommand("https://wiki.librelancer.net/lanceredit:lanceredit");
                }
				if (Theme.IconMenuItem("About","about",Color4.White,true))
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
            if (openError)
            {
                ImGui.OpenPopup("Error");
                openError = false;
            }

            if (openLoading)
            {
                ImGui.OpenPopup("Processing");
                openLoading = false;
            }
            bool pOpen = true;

            if (ImGui.BeginPopupModal("Error", ref pOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Error:");
                errorText.InputTextMultiline("##etext", new Vector2(430, 200), ImGuiInputTextFlags.ReadOnly);
                if (ImGui.Button("OK")) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
            pOpen = true;
			if (ImGui.BeginPopupModal("About", ref pOpen, ImGuiWindowFlags.AlwaysAutoResize))
			{
                ImGui.SameLine(ImGui.GetWindowWidth() / 2 - 64);
                Theme.Icon("reactor_128", Color4.White);
                CenterText(Version);
				CenterText("Callum McGing 2018-2020");
                ImGui.Separator();
                CenterText("Icons from Icons8: https://icons8.com/");
                CenterText("Icons from komorra: https://opengameart.org/content/kmr-editor-icon-set");
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
			foreach (var tab in tabs)
			{
                ((EditorTab)tab).DetectResources(MissingResources, ReferencedMaterials, ReferencedTextures);
			}
            ImGui.SetNextWindowSize(new Vector2(size.X, size.Y - 25), ImGuiCond.Always);
            ImGui.SetNextWindowPos(new Vector2(0, menu_height), ImGuiCond.Always, Vector2.Zero);
            bool childopened = true;
            ImGui.Begin("tabwindow", ref childopened,
                              ImGuiWindowFlags.NoTitleBar |
                              ImGuiWindowFlags.NoSavedSettings |
                              ImGuiWindowFlags.NoBringToFrontOnFocus |
                              ImGuiWindowFlags.NoMove |
                              ImGuiWindowFlags.NoResize);
            TabHandler.TabLabels(tabs, ref selected);
            var totalH = ImGui.GetWindowHeight();
            if (showLog)
            {
                ImGuiExt.SplitterV(2f, ref h1, ref h2, 8, 8, -1);
                h1 = totalH - h2 - 24f;
                if (tabs.Count > 0) h1 -= 20f;
                ImGui.BeginChild("###tabcontent" + (selected != null ? selected.RenderTitle : ""),new Vector2(-1,h1),false,ImGuiWindowFlags.None);
            } else
                ImGui.BeginChild("###tabcontent" + (selected != null ? selected.RenderTitle : ""));
            if (selected != null)
            {
                selected.Draw();
                ((EditorTab)selected).SetActiveTab(this);
            }
            else
                ActiveTab = null;
            ImGui.EndChild();
            if(showLog) {
                ImGui.BeginChild("###log", new Vector2(-1, h2), false, ImGuiWindowFlags.None);
                ImGui.Text("Log");
                ImGui.SameLine(ImGui.GetWindowWidth() - 20);
                if (Theme.IconButton("closelog", "x", Color4.White))
                    showLog = false;
                logBuffer.InputTextMultiline("##logtext", new Vector2(-1, h2 - 24), ImGuiInputTextFlags.ReadOnly);
                ImGui.EndChild();
            }
            ImGui.End();
            gen3dbDlg.Draw();
			//Status bar
			ImGui.SetNextWindowSize(new Vector2(size.X, 25f), ImGuiCond.Always);
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
			string activename = ActiveTab == null ? "None" : ActiveTab.DocumentName;
			string utfpath = ActiveTab == null ? "None" : ActiveTab.GetUtfPath();
            #if DEBUG
            const string statusFormat = "FPS: {0} | {1} Materials | {2} Textures | Active: {3} - {4}";
            #else
            const string statusFormat = "{1} Materials | {2} Textures | Active: {3} - {4}";
            #endif
			ImGui.Text(string.Format(statusFormat,
									 (int)Math.Round(frequency),
									 Resources.MaterialDictionary.Count,
									 Resources.TextureDictionary.Count,
									 activename,
									 utfpath));
			ImGui.End();
            if(errorTimer > 0) {
                ImGuiExt.ToastText("An error has occurred\nCheck the log for details",
                                   new Color4(21, 21, 22, 128),
                                   Color4.Red);
            }
            ImGui.PopFont();
            if (lastFrame == null ||
                lastFrame.Width != Width ||
                lastFrame.Height != Height)
            {
                if (lastFrame != null) lastFrame.Dispose();
                lastFrame = new RenderTarget2D(Width, Height);
            }
            RenderState.RenderTarget = lastFrame;
            RenderState.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
            RenderState.ClearAll();
			guiHelper.Render(RenderState);
            RenderState.RenderTarget = null;
            lastFrame.BlitToScreen();
            foreach (var tab in toAdd)
            {
                tabs.Add(tab);
                selected = tab;
            }
            toAdd.Clear();
		}
        
        void Save()
        {
            var at = ActiveTab;
            Action save = () =>
            {
                if (!string.IsNullOrEmpty(at.FilePath))
                {
                    string errText = "";
                    if (!at.Utf.Save(at.FilePath, 0, ref errText))
                    {
                        openError = true;
                        if (errorText == null) errorText = new TextBuffer();
                        errorText.SetText(errText);
                    }
                }
                else
                    RunSaveDialog(at);
            };
            if (at.DirtyCountHp > 0 || at.DirtyCountPart > 0)
            {
                Confirm("This model has unapplied changes. Continue?", save);
            }
            else
                save();
        }

        void SaveAs()
        {
            var at = ActiveTab;
            Action save = () =>  RunSaveDialog(at);
            if (at.DirtyCountHp > 0 || at.DirtyCountPart > 0)
            {
                Confirm("This model has unapplied changes. Continue?", save);
            }
            else
                save();
        }

        void RunSaveDialog(UtfTab at)
        {
            var f = FileDialog.Save(UtfFilters);
            if (f != null)
            {
                string errText = "";
                if (!at.Utf.Save(f, 0, ref errText))
                {
                    openError = true;
                    if (errorText == null) errorText = new TextBuffer();
                    errorText.SetText(errText);
                }
                else
                {
                    at.DocumentName = System.IO.Path.GetFileName(f);
                    at.UpdateTitle();
                    at.FilePath = f;
                }
            }
        }
         

        string confirmText;
        bool doConfirm = false;
        Action confirmAction;

        void Confirm(string text, Action action)
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
        void FinishColladaLoad(List<ColladaObject> dae, string tabName)
        {
           FinishLoadingSpinner();
            AddTab(new ColladaTab(dae, tabName, this));
        }
        void ColladaError(Exception ex)          
        {
            FinishLoadingSpinner();
            ErrorDialog("Import Error:\n" + ex.Message + "\n" + ex.StackTrace);
        }

        public void ErrorDialog(string text)
        {
            errorText?.Dispose();
            errorText = new TextBuffer();
            errorText.SetText(text);
            openError = true;
        }
        protected override void OnDrop(string file)
        {
            if (DetectFileType.Detect(file) == FileType.Utf)
            {
                var t = new UtfTab(this, new EditableUtf(file), System.IO.Path.GetFileName(file));
                ActiveTab = t;
                AddTab(t);
            }
        }
        protected override void Cleanup()
		{
			Audio.Dispose();
		}
	}
}
