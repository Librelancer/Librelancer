using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Dialogs;
using LibreLancer.Exceptions;
using LibreLancer.ImUI;

namespace Launcher.Screens
{
    public class LauncherScreen : Screen
    {

        readonly GameConfig config;
        readonly MainWindow win;

        public LauncherScreen(MainWindow win, GameConfig config, ScreenManager sm, PopupManager pm) : base(sm, pm)
        {
            this.config = config;
            this.win = win;
        }

        static readonly float LABEL_WIDTH = 135f;
        static readonly float BUTTON_WIDTH = 110f;
        static readonly string LAUNCHER_DESCRIPTION = "A cross-platform, open source game engine re-implementation of the 2003 game Freelancer";

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
            ImGuiExt.CenterText("Librelancer");
            ImGui.PopFont();
            ImGui.PushFont(ImGuiHelper.Roboto, 14);
            ImGuiExt.CenterText(LAUNCHER_DESCRIPTION);
            ImGui.PopFont();

            ImGui.NewLine();
            ImGui.Separator();
            ImGui.NewLine();

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Freelancer Directory"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - LABEL_WIDTH * ImGuiHelper.Scale); ImGui.InputText("##flpath", ref config.FreelancerPath, 4096);
            ImGui.SameLine();
            if (ImGui.Button("Select Folder", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
            {
                win.QueueUIThread(() =>
                {
                    FileDialog.ChooseFolder(folder =>
                    {
                        if (folder == null || folder.Length == 0)
                        {
                            return;
                        }
                        config.FreelancerPath = folder;

                    });
                });
            }

            ImGui.NewLine();
            ImGui.Separator();
            ImGui.NewLine();

            if (ImGui.BeginTable("LauncherButtons", 3,
                ImGuiTableFlags.SizingFixedSame))
            {
                ImGui.TableSetupColumn("LeftPadding", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Buttons", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("RightPadding", ImGuiTableColumnFlags.WidthStretch);

                // Row 1
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(1);
                if (ImGui.Button("Launch", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 45f))) LaunchClicked();
                ImGui.SameLine();
                ImGui.Dummy(new Vector2(LABEL_WIDTH, -1));
                ImGui.SameLine();
                if (ImGui.Button("Options", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 45f)))
                {
                    win.QueueUIThread(() =>
                    {
                        sm.SetScreen(
                            new OptionsScreen(win, config, sm, pm)
                        );
                    });
                }
                ImGui.EndTable();
            }
        }
        void LaunchClicked()
        {
            try
            {
                config.Validate();
            }
            catch (InvalidFreelancerDirectory)
            {
                pm.MessageBox("Launch Failed", "Failed to launch the game due to an invalid Freelancer directory");
                return;
            }
            catch (Exception)
            {
                pm.MessageBox("Launch Failed", "Failed to launch the game due to an invalid configuration");
                return;
            }
            config.Save();

            win.StartGame();
        }
    }
}
