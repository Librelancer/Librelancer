using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ImGuiNET;
using LibreLancer.ImUI;
using WattleScript.Interpreter;

namespace LLServer.Popups
{
    class OptionsPopup : PopupWindow
    {
        readonly LLServerGuiConfig guiConfig;
        readonly MainWindow win;

        public OptionsPopup(LLServerGuiConfig guiConfig, MainWindow win)
        {
            this.guiConfig = guiConfig;
            this.win = win;
        }
        public override string Title { get; set; } = "LL Server GUI Options";
        public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize;
        //public override Vector2 InitSize => new Vector2(400,225);

        static readonly float LABEL_WIDTH = 200f;
        static readonly float BUTTON_WIDTH = 110f;

        string inputScreenIntervalBuffer;


        public override void Draw(bool appearing)
        {
            // guiConfig.AutoStartServer
            ImGui.NewLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Auto start server on launch"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.Checkbox("##autoStart", ref guiConfig.AutoStartServer);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Auto refresh server data"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.Checkbox("##autoRefresh", ref guiConfig.AutoRefreshData);
            ImGui.Spacing();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Auto refresh interval (seconds)"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);

            ref string refreshIntervalInput = ref inputScreenIntervalBuffer;
            refreshIntervalInput ??= guiConfig.AutoRefreshinterval.ToString();
            ImGui.SetNextItemWidth(BUTTON_WIDTH);
            if(ImGui.InputText("##autorefreshinterval", ref refreshIntervalInput, int.MaxValue, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.CharsDecimal))
            {
                if (int.TryParse( refreshIntervalInput, out int refreshInterval))
                {
                    guiConfig.AutoRefreshinterval = refreshInterval;
                }
            }

            ImGui.NewLine();
            ImGui.Separator();
            ImGui.NewLine();
            ImGui.SetCursorPosX((ImGui.GetWindowSize().X / 2 - BUTTON_WIDTH / 2)  * ImGuiHelper.Scale );
            if(ImGui.Button("Close", new Vector2(BUTTON_WIDTH, 0f))) {
                win.SaveServerGuiConfig();
                ImGui.CloseCurrentPopup();
            }
            ImGui.NewLine();

        }
    }
}
