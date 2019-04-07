// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Media;
using ImGuiNET;
namespace LancerEdit
{
	public class MainWindow : Game
	{
		ImGuiHelper guiHelper;
		public AudioManager Audio;
		public ResourceManager Resources;
		public Billboards Billboards;
		public PolylineRender Polyline;
		public PhysicsDebugRenderer DebugRender;
		public ViewportManager Viewport;
		public CommandBuffer Commands; //This is a huge object - only have one
		public MaterialMap MaterialMap;
        public string Version;
        TextBuffer logBuffer;
        StringBuilder logText = new StringBuilder();
        static readonly string[] defaultFilters = {
            "Linear", "Bilinear", "Trilinear"
        };
        bool openError = false;
        bool finishLoading = false;
        string[] filters;
        int[] anisotropyLevels;
        int cFilter = 2;
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
            logBuffer = new TextBuffer(32768);
		}
        double errorTimer = 0;
		protected override void Load()
		{
			Title = "LancerEdit";
			guiHelper = new ImGuiHelper(this);
			Audio = new AudioManager(this);
            FileDialog.RegisterParent(this);
			Viewport = new ViewportManager(RenderState);
            var texturefilters = new List<string>(defaultFilters);
            if (RenderState.MaxAnisotropy > 0) {
                anisotropyLevels = RenderState.GetAnisotropyLevels();
                foreach(var lvl in anisotropyLevels) {
                    texturefilters.Add(string.Format("Anisotropic {0}x", lvl));
                }
            }
            filters = texturefilters.ToArray();
			Resources = new ResourceManager(this);
			Commands = new CommandBuffer();
			Billboards = new Billboards();
			Polyline = new PolylineRender(Commands);
			DebugRender = new PhysicsDebugRenderer();
			Viewport.Push(0, 0, 800, 600);
            Keyboard.KeyDown += Keyboard_KeyDown;

            //TODO: Icon-setting code very messy
            int w, h, c;
            var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("LancerEdit.reactor_64.png");
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, (int)stream.Length);
            StbSharp.Stb.stbi_set_flip_vertically_on_load(0);
            var img = StbSharp.Stb.stbi_load_from_memory(bytes, out w, out h, out c, StbSharp.Stb.STBI_rgb_alpha);
            StbSharp.Stb.stbi_set_flip_vertically_on_load(1);
            SetWindowIcon(w, h, img);

            //Open passed in files!
            if(InitOpenFile != null)
                foreach(var f in InitOpenFile) 
                    OpenFile(f);
        }

        void Keyboard_KeyDown(KeyEventArgs e)
        {
            var mods = e.Modifiers;
            mods &= ~KeyModifiers.Numlock;
            mods &= ~KeyModifiers.Capslock;
            if((mods == KeyModifiers.LeftControl || mods == KeyModifiers.RightControl) && e.Key == Keys.D) {
                if (selected != null) ((EditorTab)selected).OnHotkey(Hotkeys.Deselect);
            }
            if((mods == KeyModifiers.LeftControl || mods == KeyModifiers.RightControl) && e.Key == Keys.R) {
                if (selected != null) ((EditorTab)selected).OnHotkey(Hotkeys.ResetViewport);
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
		public void AddTab(DockTab tab)
		{
			toAdd.Add(tab);
		}
		protected override void Update(double elapsed)
		{
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
                ActiveTab = t;
                AddTab(t);
            }
        }
        DockTab selected;
        TextBuffer errorText;
        bool showLog = false;
        bool showOptions = false;
        float h1 = 200, h2 = 200;
        Vector2 errorWindowSize = Vector2.Zero;
        public double TimeStep;
		protected override void Draw(double elapsed)
		{
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
				}
				else
				{
					if (Theme.IconMenuItem(string.Format("Save '{0}'", ActiveTab.DocumentName), "save", Color4.White, true))
					{
                        var f = FileDialog.Save(UtfFilters);
						if (f != null)
						{
							ActiveTab.DocumentName = System.IO.Path.GetFileName(f);
                            ActiveTab.UpdateTitle();
                            string errText = "";
                            if(!ActiveTab.Utf.Save(f, ref errText)) {
                                openError = true;
                                if (errorText == null) errorText = new TextBuffer();
                                errorText.SetText(errText);
                            }
						}
					}
				}
				if (Theme.IconMenuItem("Quit", "quit", Color4.White, true))
				{
					Exit();
				}
				ImGui.EndMenu();
			}
            bool openLoading = false;
            if (ImGui.BeginMenu("View"))
            {
                Theme.IconMenuToggle("Log", "log", Color4.White, ref showLog, true);
                ImGui.EndMenu();
            }
			if (ImGui.BeginMenu("Tools"))
			{
                if(Theme.IconMenuItem("Options","options",Color4.White,true))
                {
                    showOptions = true;
                }
               
				if (Theme.IconMenuItem("Resources","resources",Color4.White,true))
				{
					AddTab(new ResourcesTab(Resources, MissingResources, ReferencedMaterials, ReferencedTextures));
				}
                if(Theme.IconMenuItem("Import Collada","import",Color4.White,true))
                {
                    string input;
                    if((input = FileDialog.Open(ColladaFilters)) != null) {
                        openLoading = true;
                        finishLoading = false;
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
            if (openLoading) ImGui.OpenPopup("Processing");
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
				CenterText("Callum McGing 2018-2019");
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
            if(ImGui.BeginPopupModal("Processing", ref pOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGuiExt.Spinner("##spinner", 10, 2, ImGuiNative.igGetColorU32(ImGuiCol.ButtonHovered, 1));
                ImGui.SameLine();
                ImGui.Text("Processing");
                if (finishLoading) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
			var menu_height = ImGui.GetWindowSize().Y;
			ImGui.EndMainMenuBar();
			var size = (Vector2)ImGui.GetIO().DisplaySize;
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
                ImGui.BeginChild("###tabcontent" + (selected != null ? selected.Title : ""),new Vector2(-1,h1),false,ImGuiWindowFlags.None);
            } else
                ImGui.BeginChild("###tabcontent" + (selected != null ? selected.Title : ""));
            if (selected != null)
            {
                selected.Draw();
                ((EditorTab)selected).SetActiveTab(this);
            }
            else
                ActiveTab = null;
            ImGui.EndChild();
            TabHandler.DrawTabDrag(tabs);
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
			ImGui.Text(string.Format("FPS: {0} | {1} Materials | {2} Textures | Active: {3} - {4}",
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
            if(showOptions) {
                ImGui.Begin("Options", ref showOptions, ImGuiWindowFlags.AlwaysAutoResize);
                var pastC = cFilter;
                ImGui.Combo("Texture Filter", ref cFilter, filters, filters.Length);
                if(cFilter != pastC) {
                    switch(cFilter) {
                        case 0:
                            RenderState.PreferredFilterLevel = TextureFiltering.Linear;
                            break;
                        case 1:
                            RenderState.PreferredFilterLevel = TextureFiltering.Bilinear;
                            break;
                        case 2:
                            RenderState.PreferredFilterLevel = TextureFiltering.Trilinear;
                            break;
                        default:
                            RenderState.AnisotropyLevel = anisotropyLevels[cFilter - 3];
                            RenderState.PreferredFilterLevel = TextureFiltering.Anisotropic;
                            break;
                    }
                }
                ImGui.End();
            }
			ImGui.PopFont();
			guiHelper.Render(RenderState);
            foreach (var tab in toAdd)
            {
                tabs.Add(tab);
                selected = tab;
            }
            toAdd.Clear();
		}
        void CenterText(string text)
        {
            ImGui.Dummy(new Vector2(1));
            var win = ImGui.GetWindowWidth();
            var txt = ImGui.CalcTextSize(text).X;
            ImGui.SameLine(Math.Max((win / 2f) - (txt / 2f),0));
            ImGui.Text(text);
        }
        void FinishColladaLoad(List<ColladaObject> dae, string tabName) {
            finishLoading = true;
            AddTab(new ColladaTab(dae, tabName, this));
        }
        void ColladaError(Exception ex)          
        {
            finishLoading = true;
            if (errorText != null) errorText.Dispose();
            var str = "Import Error:\n" + ex.Message + "\n" + ex.StackTrace;
            if(errorText == null)
                errorText = new TextBuffer();
            errorText.SetText(str);
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
