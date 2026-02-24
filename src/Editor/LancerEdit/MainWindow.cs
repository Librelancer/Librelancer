// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using LancerEdit.Audio;
using LancerEdit.GameContent;
using LancerEdit.GameContent.MissionEditor;
using LancerEdit.Shaders;
using LancerEdit.Updater;
using LancerEdit.Utf.Popups;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.ContentEdit.Model;
using LibreLancer.Data;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema;
using LibreLancer.Data.Schema.Pilots;
using LibreLancer.Dialogs;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Text;
using LibreLancer.ImUI;
using LibreLancer.Media;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.Shaders;

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
        private bool showDemoWindow = false;
        public bool RequestExit = false;

        public List<TextDisplayWindow> TextWindows = new List<TextDisplayWindow>();
        private HotkeyHelp hotkeys = new HotkeyHelp();

        public EditorConfiguration Config;
        OptionsWindow options;

        private QuickFileBrowser quickFileBrowser;
        public bool DrawDragTargets => dragActive > 0;
        private int dragActive = 0;

        public UpdateChecks Updater;

        public bool EnableAudioConversion;

        private const int LOG_SIZE = 128 * 1024; //128k UTF-16, 256k UTF-8

        public MainWindow(EditorConfiguration editorConfig, GameConfiguration configuration = null) : base(editorConfig.WindowWidth, editorConfig.WindowHeight, true, configuration)
        {
            Version = "LancerEdit " + Platform.GetInformationalVersion<MainWindow>();
            MaterialMap = new MaterialMap();
            MaterialMap.AddRegex(new StringKeyValue("^nomad.*$", "NomadMaterialNoBendy"));
            MaterialMap.AddRegex(new StringKeyValue("^n-texture.*$", "NomadMaterialNoBendy"));
            FLLog.UIThread = this;
            FLLog.AppendLine = (x, severity) =>
            {
                logText.AppendLine(x);
                if (logText.Length > LOG_SIZE)
                {
                    logText.Remove(0, logText.Length - LOG_SIZE);
                }
                logBuffer.SetText(logText.ToString());
            };
            Config = editorConfig;
            Config.LastExportPath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            logBuffer = new TextBuffer(LOG_SIZE * 2 + 1);
            recentFiles = new RecentFilesHandler(OpenFile);
            Updater = new UpdateChecks(this, GetBasePath());
            EnableAudioConversion = Mp3Encoder.EncoderAvailable();
            if (!string.IsNullOrWhiteSpace(Config.AutoLoadPath))
            {
                QueueUIThread(() => LoadGameData(Config.AutoLoadPath));
            }
        }
        double errorTimer = 0;
        private ImTextureRef logoTexture;

        protected override bool UseSplash => true;

        protected override Texture2D GetSplash()
        {
            using (var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("LancerEdit.splash.png"))
            {
                return (Texture2D)LibreLancer.ImageLib.Generic.TextureFromStream(RenderContext, stream);
            }
        }

        private SoundInstance bufferInstance;

        public bool PlayingBuffer => bufferInstance != null && bufferInstance.Playing;
        public void PlayBuffer(byte[] buffer, bool loop = false)
        {
            StopBuffer();
            SoundData soundData;
            try
            {
                soundData = new SoundData();
                using (var stream = new MemoryStream(buffer))
                    soundData.LoadStream(stream);
            }
            catch (Exception ex)
            {
                FLLog.Error("Audio", ex.ToString());
                ErrorDialog("Error:\n" + ex.Message);
                return;
            }
            bufferInstance = Audio.CreateInstance(soundData, SoundCategory.Sfx);
            if (bufferInstance != null)
            {
                bufferInstance.OnStop = () => soundData.Dispose();
                bufferInstance.Play(loop);
            }
        }

        public void StopBuffer()
        {
            if (bufferInstance != null)
            {
                bufferInstance.Stop();
                bufferInstance = null;
            }
        }

        protected override void Load()
        {
            AllShaders.Compile(RenderContext);
            EditorShaders.Compile(RenderContext);
            DefaultMaterialMap.Init();
            DisplayMesh.LoadAll(RenderContext);
#if DEBUG
            Title = "LancerEdit DEBUG";
#else
            Title = Version;
#endif
            guiHelper = new ImGuiHelper(this, Config.UiScale);
            guiHelper.PauseWhenUnfocused = Config.PauseWhenUnfocused;
            Audio = new AudioManager(this);
            Bell.Init(Audio);
            options = new OptionsWindow(this);
            Resources = new GameResourceManager(this, null);
            Commands = new CommandBuffer(RenderContext);
            Polyline = new PolylineRender(RenderContext, Commands);
            LineRenderer = new LineRenderer(RenderContext);
            RenderContext.ReplaceViewport(0, 0, 800, 600);
            Keyboard.KeyDown += Keyboard_KeyDown;
            //TODO: Icon-setting code very messy
            using (var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("LancerEdit.reactor_64.png"))
            {
                var icon = LibreLancer.ImageLib.Generic.ImageFromStream(stream);
                SetWindowIcon(icon.Width, icon.Height, Bgra8.BufferFromBytes(icon.Data));
            }
            using (var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("LancerEdit.reactor_128.png"))
            {
                var icon = (Texture2D)LibreLancer.ImageLib.Generic.TextureFromStream(RenderContext, stream);
                logoTexture = ImGuiHelper.RegisterTexture(icon);
            }
            //Open passed in files!
            if (InitOpenFile != null)
                foreach (var f in InitOpenFile)
                    OpenFile(f);
            RichText = RenderContext.Renderer2D.CreateRichTextEngine();
            Fonts = new FontManager();
            Fonts.ConstructDefaultFonts();
            Services.Add(Fonts);
            Billboards = new Billboards(RenderContext);
            Config.Validate(RenderContext);
            Services.Add(Commands);
            Services.Add(Billboards);
            Services.Add(Config);
            Make3dbDlg = new CommodityIconDialog(this);
            LoadScripts();
            MinimumWindowSize = new Point(200, 200);
            h1 *= ImGuiHelper.Scale;
            h2 *= ImGuiHelper.Scale;
        }

        void Keyboard_KeyDown(KeyEventArgs e)
        {
            var mods = e.Modifiers;
            mods &= ~KeyModifiers.Numlock;
            mods &= ~KeyModifiers.Capslock;

            if (TabControl.Selected is not EditorTab editor)
                return;
            bool control = (mods & KeyModifiers.Control) != 0;
            bool shift = (mods & KeyModifiers.Shift) != 0;
            bool popupOrTextEditing = ImGui.GetIO().WantCaptureKeyboard;
            if (e.Key == Keys.S && control)
            {
                editor.SaveStrategy.Save();
                return;
            }
            Hotkeys hk = e.Key.Map() switch
            {
                Keys.C when control && !popupOrTextEditing => Hotkeys.Copy,
                Keys.V when control && !popupOrTextEditing => Hotkeys.Paste,
                Keys.X when control && !popupOrTextEditing => Hotkeys.Cut,
                Keys.R when control => Hotkeys.ResetViewport,
                Keys.G when control => Hotkeys.ToggleGrid,
                Keys.D when control => Hotkeys.Deselect,
                Keys.D0 when control => Hotkeys.ClearRotation,
                Keys.Z when (control && shift) => Hotkeys.Redo,
                Keys.Z when control => Hotkeys.Undo,
                Keys.F6 => Hotkeys.ChangeSystem,
                _ => 0
            };
            if (hk != 0)
                editor.OnHotkey(hk, (mods & KeyModifiers.Shift) != 0);
        }

        public object SystemEditClipboard { get; private set; }
        private string clipboardMarkerString;

        private double onSet = 0;
        public void SystemEditCopy(object obj)
        {
            clipboardMarkerString = $"[OBJECT {IdSalt.New()}]";
            SetClipboardText(clipboardMarkerString);
            SystemEditClipboard = obj;
            onSet = TimerTick;
        }
        protected override void OnClipboardUpdate()
        {
            if ((onSet + 0.25) < TimerTick && SystemEditClipboard != null)
            {
                if (GetClipboardText() != clipboardMarkerString)
                {
                    SystemEditClipboard = null;
                    clipboardMarkerString = null;
                }
            }
        }

        private string GetDataPath() => OpenDataContext is not null ? OpenDataContext.Folder : Config.AutoLoadPath;


        bool openAbout = false;
        public TabControl TabControl = new TabControl();
        public List<MissingReference> MissingResources = new List<MissingReference>();
        public List<uint> ReferencedMaterials = new List<uint>();
        public List<TextureReference> ReferencedTextures = new List<TextureReference>();

        List<DockTab> toAdd = new List<DockTab>();
        double frequency = 0;
        int updateTime = 10;
        public CommodityIconDialog Make3dbDlg;
        public void AddTab(DockTab tab)
        {
            toAdd.Add(tab);
        }

        private Task lastAudio = null;
        protected override void Update(double elapsed)
        {
            if (!guiHelper.DoUpdate()) return;
            foreach (var tab in TabControl.Tabs)
                tab.Update(elapsed);
            if (errorTimer > 0) errorTimer -= elapsed;
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
                    case FileType.SaveGame:
                        var st = new SaveGameTab(f);
                        recentFiles.FileOpened(f);
                        AddTab(st);
                        break;
                    case FileType.Blender:
                    case FileType.Other:
                        TryImportModel(f);
                        break;
                    case FileType.Error:
                        ErrorDialog($"File `{f}` is empty (0 bytes)");
                        break;
                }

            }
        }

        public PopupManager Popups = new PopupManager();

        private int bottomTab = 0;
        float h1 = 200, h2 = 200;
        Vector2 errorWindowSize = Vector2.Zero;
        public double TimeStep;
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
                if (!Directory.Exists(dir)) continue;
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
                    if (sc.Validate()) Scripts.Add(sc);
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
            var popup = new TaskRunPopup("Loading Model");
            popup.Log($"Loading {filename}\n");
            Popups.OpenPopup(popup);
            SimpleMeshLoader.ModelFromFile(filename, Config.BlenderPath, popup.Token, popup.Log)
                .ContinueWith(x =>
                {
                    if (x.Exception != null)
                    {
                        popup.Log(x.Exception + "\n");
                        popup.Log("Opening model file failed\n");
                        popup.Finish();
                        return;
                    }
                    QueueUIThread(() =>
                    {
                        var model = x.Result;
                        ResultMessages(model, popup.Log);
                        if (model.IsSuccess)
                        {
                            popup.Log("Loaded\n");
                            QueueUIThread(() => FinishImporterLoad(model.Data, Path.GetFileName(filename), popup));
                        }
                        else
                        {
                            popup.Log("Opening model file failed\n");
                            popup.Finish();
                        }
                    });
                });
        }

        void OpenGameData()
        {
            FileDialog.ChooseFolder(folder =>
            {
                if (!GameConfig.CheckFLDirectory(folder))
                    ErrorDialog("Selected directory is not a valid Freelancer folder");
                else
                    LoadGameData(folder);
            });
        }

        void LoadGameData(string folder)
        {
            if (!GameConfig.CheckFLDirectory(folder))
            {
                ErrorDialog($"'{folder}' is not a valid Freelancer folder");
                return;
            }
            QueueUIThread(() =>
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
                c.Load(this, folder, GetCacheDirectory("LancerEdit"), () =>
                {
                    OpenDataContext = c;
                    FinishLoadingSpinner();
                }, e =>
                {
                    FinishLoadingSpinner();
                    ErrorDialog(GetExceptionText(e));
                });
            });
        }


        static string GetExceptionText(Exception e)
        {
            var sb = new StringBuilder();
            IterateExceptions(e, sb);
            return sb.ToString();
        }

        static void IterateExceptions(Exception e, StringBuilder sb)
        {
            if (e is AggregateException ag)
            {
                sb.AppendLine("Multiple Errors");
                foreach (var e2 in ag.InnerExceptions)
                {
                    sb.AppendLine("--");
                    IterateExceptions(e2, sb);
                }
            }
            else
            {
                sb.AppendLine(e.Message);
                sb.AppendLine(e.StackTrace);
                if (e.InnerException != null)
                {
                    sb.AppendLine(">");
                    sb.AppendLine("Inner Exception: ");
                    IterateExceptions(e.InnerException, sb);
                }
            }
        }

        protected override void Draw(double elapsed)
        {
            //Don't process all the imgui stuff when it isn't needed
            if (!loadingSpinnerActive)
            {
                var m = guiHelper.DoRender(elapsed);
                if (m == ImGuiProcessing.Sleep)
                {
                    WaitForEvent(2000); //Yield like a regular GUI program
                }
                else if (m == ImGuiProcessing.Slow)
                {
                    // Push enough frames to get good keyboard input
                    WaitForEvent(50);
                }
            }

            if ((Config.WindowWidth != Width || Config.WindowHeight != Height) && Width > 800 && Height > 600)
            {
                Config.WindowHeight = Height;
                Config.WindowWidth = Width;
            }

            TimeStep = elapsed;
            RenderContext.ReplaceViewport(0, 0, Width, Height);
            RenderContext.ClearColor = Theme.WorkspaceBackground;
            RenderContext.ClearAll();
            guiHelper.NewFrame(elapsed);
            ImGui.PushFont(ImGuiHelper.Roboto, 0);
            ImGui.BeginMainMenuBar();
            if (ImGui.BeginMenu("File"))
            {
                var lst = ImGui.GetWindowDrawList();
                if (Theme.BeginIconMenu(Icons.File, "New"))
                {
                    if (ImGui.MenuItem("Empty Utf File", true))
                    {
                        var t = new UtfTab(this, new EditableUtf(), "Untitled");
                        AddTab(t);
                    }
                    if (ImGui.MenuItem("Animated .txm", true))
                    {
                        Popups.OpenPopup(new NewTxmPopup(this, action => {
                            var t = new UtfTab(this, action.utf, action.name);
                            AddTab(t);
                        }));
                    }
                    ImGui.EndMenu();
                }

                if (Theme.IconMenuItem(Icons.Open, "Open", true))
                {
                    FileDialog.Open(OpenFile, AppFilters.UtfFilters + AppFilters.ThnFilters, GetDataPath());
                }

                recentFiles.Menu(Popups);

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
                Theme.IconMenuToggle(Icons.Log, "Log", ref Config.LogVisible, true);
                Theme.IconMenuToggle(Icons.File, "Files", ref Config.FilesVisible, true);
                Theme.IconMenuToggle(Icons.Info, "Status Bar", ref Config.StatusBarVisible, true);
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Data"))
            {
                if (Theme.IconMenuItem(Icons.BoxOpen, "Load Data", true))
                {
                    var dataTabCount = TabControl.Tabs.OfType<GameContentTab>().Count();
                    if (dataTabCount > 0)
                        Confirm($"Opening another directory will close {dataTabCount} tab(s). Continue?", OpenGameData);
                    else
                        OpenGameData();
                }
                if (Theme.IconMenuItem(Icons.SyncAlt, "Reload Data", OpenDataContext != null))
                {
                    var dataTabCount = TabControl.Tabs.OfType<GameContentTab>().Count();
                    if (dataTabCount > 0)
                        Confirm($"Reloading will close {dataTabCount} tab(s). Continue?",
                            () => LoadGameData(OpenDataContext.Folder));
                    else
                        LoadGameData(OpenDataContext!.Folder);
                }
                ImGui.Separator();
                if (Theme.IconMenuItem(Icons.BookOpen, "Infocard Browser", OpenDataContext != null))
                    AddTab(new InfocardBrowserTab(OpenDataContext, this));
                if (Theme.IconMenuItem(Icons.Globe, "Universe Editor", OpenDataContext != null))
                {
                    var fd = TabControl.Tabs.FirstOrDefault(x => x is UniverseEditorTab);
                    if (fd != null)
                        TabControl.SetSelected(fd);
                    else
                    {
                        Popups.OpenPopup(new UniverseLoadPopup(this));
                    }
                }
                if (Theme.IconMenuItem(Icons.Newspaper, "News Editor", OpenDataContext != null))
                {
                    var fd = TabControl.Tabs.FirstOrDefault(x => x is NewsEditorTab);
                    if (fd != null)
                        TabControl.SetSelected(fd);
                    else
                        AddTab(new NewsEditorTab(OpenDataContext, this));
                }
                if (Theme.IconMenuItem(Icons.Table, "Mission Script Editor", OpenDataContext != null))
                {
                    FileDialog.Open(x =>
                    {
                        AddTab(new MissionScriptEditorTab(OpenDataContext, this, x));
                    }, AppFilters.IniFilters, GetDataPath());
                }
                ImGui.Separator();
                if (Theme.IconMenuItem(Icons.Fire, "Projectile Viewer", OpenDataContext != null))
                    AddTab(new ProjectileViewerTab(this, OpenDataContext));
                if (Theme.IconMenuItem(Icons.Play, "Thn Player", OpenDataContext != null))
                    AddTab(new ThnPlayerTab(OpenDataContext, this));
                if (Theme.IconMenuItem(Icons.Check, "Check Faction Hashes", OpenDataContext != null))
                {
                    Dictionary<ushort, string> hashes = new Dictionary<ushort, string>();
                    var collisions = new StringBuilder();
                    foreach (var faction in OpenDataContext!.GameData.Items.Factions)
                    {
                        var hash = FLHash.FLFacHash(faction.Nickname);
                        if (hashes.TryGetValue(hash, out var og))
                        {
                            collisions.AppendLine(
                                $"Faction '{faction.Nickname}' collides with '{og}' (hash 0x{hash:X2})");
                        }
                        else
                        {
                            hashes[hash] = faction.Nickname;
                        }
                    }
                    if (collisions.Length > 0)
                    {
                        Popups.MessageBox("Check Faction Hashes", collisions.ToString());
                    }
                    else
                    {
                        Popups.MessageBox("Check Faction Hashes", "No hash collisions detected!");
                    }
                }
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Tools"))
            {
                if (Theme.IconMenuItem(Icons.Cog, "Options", true))
                {
                    options.Show();
                }

                if (Theme.IconMenuItem(Icons.Palette, "Resources", true))
                {
                    AddTab(new ResourcesTab(this, Resources, MissingResources, ReferencedMaterials, ReferencedTextures));
                }
                if (Theme.IconMenuItem(Icons.FileImport, "Import Model", true))
                {
                    var filters = Blender.BlenderPathValid(Config.BlenderPath)
                        ? AppFilters.ImportModelFilters
                        : AppFilters.ImportModelFiltersNoBlender;
                    FileDialog.Open(TryImportModel, filters, GetDataPath());
                }
                if (Theme.IconMenuItem(Icons.SyncAlt, "Convert Audio", EnableAudioConversion))
                {
                    AudioImportPopup.Run(this, Popups, null);
                }
                if (Theme.IconMenuItem(Icons.SyncAlt, "Bulk Convert Audio", EnableAudioConversion))
                {
                    BulkAudioTool.Open(this, Popups);
                }
                if (Theme.IconMenuItem(Icons.SprayCan, "Generate Icon", true))
                {
                    FileDialog.Open(input => Make3dbDlg.Open(input), AppFilters.ImageFilter, GetDataPath());
                }
                if (Theme.IconMenuItem(Icons.Table, "State Graph", true))
                {
                    FileDialog.Open(
                        input => AddTab(new StateGraphTab(this, new StateGraphDb(input, null), input)),
                        AppFilters.StateGraphFilter
                        , GetDataPath());
                }

                if (Theme.IconMenuItem(Icons.BezierCurve, "ParamCurve Visualiser", true))
                {
                    TabControl.Tabs.Add(new ParamCurveVis());
                }
                if (Theme.IconMenuItem(Icons.Calculator, "Hash Tool", true))
                {
                    TabControl.Tabs.Add(new HashToolTab());
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
                if (ImGui.MenuItem("Refresh"))
                {
                    LoadScripts();
                }
                ImGui.Separator();
                int k = 0;
                foreach (var sc in Scripts)
                {
                    var n = ImGuiExt.IDWithExtra(sc.Info.Name, k++);
                    if (ImGui.MenuItem(n))
                    {
                        RunScript(sc);
                    }
                }
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Help"))
            {
                if (Theme.IconMenuItem(Icons.Book, "Topics", true))
                {
                    var selfPath = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
                    var helpFile = Path.Combine(selfPath, "Docs", "index.html");
                    Shell.OpenCommand(helpFile);
                }

                Theme.IconMenuToggle(Icons.Keyboard, "Hotkeys", ref hotkeys.Open, true);

                if (Theme.IconMenuItem(Icons.Info, "About", true))
                {
                    openAbout = true;
                }

#if DEBUG
                if (Theme.IconMenuItem(Icons.Info, "Debug Memory", true))
                {
                    GC.Collect();
                    Popups.MessageBox("Native Memory", DebugDrawing.SizeSuffix(UnsafeHelpers.Allocated));
                }

                if (Theme.IconMenuItem(Icons.Info, "ImGui Demo", true))
                {
                    showDemoWindow = true;
                }
#endif

                if (Updater.Enabled && Theme.IconMenuItem(Icons.SyncAlt, "Check for updates", true))
                {
                    Popups.OpenPopup(Updater.CheckForUpdates());
                }
                ImGui.EndMenu();
            }

            options.Draw();

            if (openAbout)
            {
                ImGui.OpenPopup("About");
                openAbout = false;
            }

            if (showDemoWindow)
            {
                ImGui.ShowDemoWindow(ref showDemoWindow);
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

            Popups.Run();
            pOpen = true;
            if (ImGui.BeginPopupModal("About", ref pOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.SameLine(ImGui.GetWindowWidth() / 2 - 64);
                ImGui.Image(logoTexture, new Vector2(128), new Vector2(0, 1), new Vector2(1, 0));
                CenterText(Version);
                CenterText($"ImGui version: {ImGuiExt.Version}");
                CenterText("Callum McGing");
                CenterText("Librelancer Contributors");
                CenterText("2018-2026");
                ImGui.Separator();
                var btnW = ImGui.CalcTextSize("OK").X + ImGui.GetStyle().FramePadding.X * 2;
                ImGui.Dummy(Vector2.One);
                ImGui.SameLine(ImGui.GetWindowWidth() / 2 - (btnW / 2));
                if (ImGui.Button("OK")) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
            if (ImGuiExt.BeginModalNoClose("Processing", ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGuiExt.Spinner("##spinner", 10, 2, ImGui.GetColorU32(ImGuiCol.ButtonHovered, 1));
                ImGui.SameLine();
                ImGui.Text("Processing");
                if (finishLoading) ImGui.CloseCurrentPopup();
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

            var statusSz = Config.StatusBarVisible ? 22 * ImGuiHelper.Scale : 0;
            ImGui.SetNextWindowSize(new Vector2(size.X, size.Y - statusSz), ImGuiCond.Always);
            ImGui.SetNextWindowPos(new Vector2(0, menu_height), ImGuiCond.Always, Vector2.Zero);
            bool childopened = true;
            ImGui.Begin("tabwindow", ref childopened,
                              ImGuiWindowFlags.NoTitleBar |
                              ImGuiWindowFlags.NoSavedSettings |
                              ImGuiWindowFlags.NoBringToFrontOnFocus |
                              ImGuiWindowFlags.NoMove |
                              ImGuiWindowFlags.NoResize |
                              ImGuiWindowFlags.NoBackground |
                              ImGuiWindowFlags.NoDecoration |
                              ImGuiWindowFlags.NoScrollWithMouse);

            TabControl.TabLabels();
            var totalH = ImGui.GetWindowHeight();
            if (Config.LogVisible || Config.FilesVisible)
            {
                ImGuiExt.SplitterV(2f, ref h1, ref h2, 8, 28 * ImGuiHelper.Scale, -1);
                h1 = totalH - h2 - 3f * ImGuiHelper.Scale - statusSz;
                if (TabControl.Tabs.Count > 0) h1 -= 20f * ImGuiHelper.Scale;
                ImGui.BeginChild("###tabcontent" + (TabControl.Selected != null ? TabControl.Selected.Unique.ToString() : ""), new Vector2(-1, h1));
            }
            else
                ImGui.BeginChild("###tabcontent" + (TabControl.Selected != null ? TabControl.Selected.Unique.ToString() : ""));

            TabControl.Selected?.Draw(elapsed);

            var style = ImGui.GetStyle();
            ImGui.EndChild();
            if (Config.LogVisible || Config.FilesVisible)
            {
                ImGui.BeginChild("###bottom", new Vector2(-1, h2));
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ImGui.GetStyle().FramePadding + new Vector2(0, 2) * ImGuiHelper.Scale);
                ImGui.BeginTabBar("##tabbar", ImGuiTabBarFlags.AutoSelectNewTabs);
                if (!Config.FilesVisible) bottomTab = 0;
                if (!Config.LogVisible) bottomTab = 1;
                if (Config.LogVisible && ImGui.BeginTabItem("Log", ref Config.LogVisible, ImGuiTabItemFlags.None))
                {
                    if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0) && h2 < 29 * ImGuiHelper.Scale)
                        h2 = 200 * ImGuiHelper.Scale;
                    ImGui.EndTabItem();
                }
                if (Config.FilesVisible && ImGui.BeginTabItem("Files", ref Config.FilesVisible, ImGuiTabItemFlags.None))
                {
                    if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0) && h2 < 29 * ImGuiHelper.Scale)
                        h2 = 200 * ImGuiHelper.Scale;
                    bottomTab = 1;
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
                ImGui.PopStyleVar();
                if (h2 > 40 * ImGuiHelper.Scale) //Min Size
                {
                    if (bottomTab == 0)
                    {
                        logBuffer.InputTextMultiline("##logtext", new Vector2(-1, h2 - 40 * ImGuiHelper.Scale),
                            ImGuiInputTextFlags.ReadOnly);
                    }
                    else if (bottomTab == 1)
                    {
                        ImGui.SameLine();
                        DrawQuickFiles();
                    }
                }
                ImGui.EndChild();
            }
            ImGui.End();
            Make3dbDlg.Draw();
            hotkeys.Draw();
            for (int i = TextWindows.Count - 1; i >= 0; i--)
            {
                if (!TextWindows[i].Draw())
                {
                    TextWindows.RemoveAt(i);
                }
            }

            if (Config.StatusBarVisible)
            {
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
                else
                {
                    updateTime++;
                }

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
            }

            DrawSaveIcon(elapsed);
            if(errorTimer > 0) {
                ImGuiExt.ToastText("An error has occurred\nCheck the log for details",
                                   new Color4(21, 21, 22, 128),
                                   Color4.Red);
            }
            ImGui.PopFont();
            unsafe
            {
                if (ImGui.GetDragDropPayload() != null)
                    dragActive = 3;
                else
                {
                    dragActive--;
                    if (dragActive < 0) dragActive = 0;
                }
            }

            guiHelper.Render(RenderContext);

            foreach (var tab in toAdd)
            {
                TabControl.Tabs.Add(tab);
                TabControl.SetSelected(tab);
            }
            toAdd.Clear();
            if (RequestExit)
                Exit();
        }

        private int saveFrames = 2;
        private const double SAVE_ICON_DURATION = 0.7;
        private double saveIconTime = 0;

        void DrawSaveIcon(double time)
        {
            if (saveIconTime <= 0) {
                return;
            }
            ImGuiHelper.AnimatingElement();
            if (saveFrames > 0) {
                saveFrames--;
            }
            else {
                saveIconTime -= time;
            }
            double t = (SAVE_ICON_DURATION - saveIconTime) / SAVE_ICON_DURATION;
            var style = ImGui.GetStyle();
            var a = MathHelper.Clamp(t >= 0.5 ? 1 - ((t - 0.5) * 2) : (t * 2), 0, 1);
            style.Alpha = Easing.Ease(EasingTypes.EaseInOut, (float)a, 0, 1, 0, 1);

            float pad = 10 * ImGuiHelper.Scale;
            var flags = ImGuiWindowFlags.NoDecoration |
                        ImGuiWindowFlags.AlwaysAutoResize |
                        ImGuiWindowFlags.NoInputs;
            var vp = ImGui.GetMainViewport();
            var pos = vp.WorkPos + new Vector2(vp.WorkSize.X - pad, pad);
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always, new(1, 0));
            if (ImGui.Begin("SaveIcon", flags))
            {
                ImGui.PushFont(null, style.FontSizeBase * 2.2f);
                ImGui.Text($"{Icons.Save}");
                ImGui.PopFont();
            }
            ImGui.End();
            style.Alpha = 1;
        }

        public void OnSaved()
        {
            saveFrames = 2;
            saveIconTime = SAVE_ICON_DURATION;
        }

        void DrawQuickFiles()
        {
            if (quickFileBrowser == null)
            {
                quickFileBrowser = new QuickFileBrowser(Config, this, Popups);
                quickFileBrowser.FileSelected += OpenFile;
            }
            quickFileBrowser.Draw();
        }

        public void Confirm(string text, Action action)
        {
            Popups.MessageBox("Confirm?", text, false, MessageBoxButtons.YesNo,
                x =>
                {
                    if (x == MessageBoxResponse.Yes)
                    {
                        action();
                    }
                });
        }

        void CenterText(string text)
        {
            ImGui.Dummy(new Vector2(1));
            var win = ImGui.GetWindowWidth();
            var txt = ImGui.CalcTextSize(text).X;
            ImGui.SameLine(Math.Max((win / 2f) - (txt / 2f), 0));
            ImGui.Text(text);
        }
        void FinishImporterLoad(SimpleMesh.Model model, string tabName, TaskRunPopup popup)
        {
            AddTab(new ImportModelTab(model, tabName, this, popup));
        }

        public void ResultMessages<T>(EditResult<T> result, Action<string> logMessages)
        {
            if (result.Messages.Count == 0) return;
            string text;
            if (result.Messages.Count == 1)
            {
                text = result.Messages[0].Message + "\n";
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var msg in result.Messages)
                    sb.Append(msg.Kind).Append(": ").AppendLine(msg.Message);
                text = sb.ToString();
            }
            logMessages(text);
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
            Popups.MessageBox(result.IsError ? "Error" : "Warning", text);
        }

        public void ErrorDialog(string text) => Popups.MessageBox("Error", text);

        protected override void OnDrop(string file) => OpenFile(file);

        protected override void Cleanup()
        {
            Audio.Dispose();
            quickFileBrowser?.Dispose();
        }
    }
}
