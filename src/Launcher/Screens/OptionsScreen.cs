using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;

namespace Launcher.Screens
{
    public class OptionsScreen : Screen
    {
        readonly GameConfig config;
        readonly MainWindow win;

        public OptionsScreen(MainWindow win, GameConfig config, ScreenManager screens, PopupManager popups) : base(screens, popups)
        {
            this.config = config;
            this.win = win;
        }

        static readonly float LABEL_WIDTH = 135f;
        static readonly float BUTTON_WIDTH = 110f;
        static readonly float BACK_BUTTON_HEIGHT = 45f;

        string widthBuffer;
        string heightBuffer;

        public override void OnEnter()
        {

        }
        public override void OnExit()
        {
        }

        public override void Draw(double elapsed)
        {
            ImGui.PushFont(ImGuiHelper.Roboto, 32);
            ImGuiExt.CenterText("Game Options");
            ImGui.PopFont();
            ImGui.Separator();
            ImGui.NewLine();

            ImGui.BeginChild("#scroll", new Vector2(0, ImGui.GetContentRegionAvail().Y -(BACK_BUTTON_HEIGHT + ImGui.GetFrameHeightWithSpacing()*2)));
            ImGui.CollapsingHeader("Graphics Settings", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Selected | ImGuiTreeNodeFlags.Bullet);
            ImGui.NewLine();

            DrawGraphicsSettings();
            ImGui.NewLine();

            ImGui.CollapsingHeader("Sound Settings", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Selected | ImGuiTreeNodeFlags.Bullet);
            ImGui.NewLine();

            SoundSlider("Master Volume", ref config.Settings.MasterVolume);
            SoundSlider("Music Volume", ref config.Settings.MusicVolume);
            SoundSlider("Sfx Volume", ref config.Settings.SfxVolume);

            ImGui.NewLine();
            ImGui.EndChild();

            ImGui.NewLine();
            ImGui.Separator();
            ImGui.NewLine();
            ImGui.NewLine();
            ImGui.SameLine((ImGui.GetWindowWidth() / 2) - (BUTTON_WIDTH / 2));
            if (ImGui.Button("Back", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, BACK_BUTTON_HEIGHT))) LaunchClicked();

        }

        private void DrawGraphicsSettings()
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Resolution"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.PushItemWidth(BUTTON_WIDTH * ImGuiHelper.Scale);
            ref string widthInput = ref widthBuffer;
            widthInput ??= config.BufferWidth.ToString();
            if (ImGui.InputText("##resX", ref widthInput, 6, ImGuiInputTextFlags.CharsDecimal))
            {
                if (int.TryParse(widthInput, out int width))
                {
                    config.BufferWidth = MathHelper.Clamp(width, 600, 16384);
                }
            }

            ImGui.SameLine();
            ImGui.Text("x");
            ImGui.SameLine();

            ref string heightInput = ref heightBuffer;
            heightInput ??= config.BufferHeight.ToString();
            if (ImGui.InputText("##resY", ref heightInput, 6, ImGuiInputTextFlags.CharsDecimal))
            {
                if (int.TryParse(widthInput, out int width))
                {
                    config.BufferHeight = MathHelper.Clamp(width, 400, 16384);
                }
            }

            ImGui.Text("Enable VSync"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.Checkbox("##vsync", ref config.Settings.VSync);

            ImGui.Text("Use Fullscreen"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.Checkbox("##fullscreen", ref config.Settings.FullScreen);
        }

        void LaunchClicked()
        {
            win.QueueUIThread(() =>
            {
                sm.SetScreen(
                    new LauncherScreen(win, config, sm, pm)
                );
            });
        }
        static void SoundSlider(string text, ref float flt)
        {
            ImGui.PushID(text);
            ImGui.AlignTextToFramePadding();
            ImGui.Text(text);
            ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.PushItemWidth(-1);
            ImGui.SliderFloat("##slider", ref flt, 0, 1, "", ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoInput);
            ImGui.PopID();
        }
    }
}
