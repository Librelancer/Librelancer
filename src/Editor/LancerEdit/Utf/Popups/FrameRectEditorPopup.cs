using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.ImUI;

namespace LancerEdit.Utf.Popups
{
    public class FrameRectEditorPopup : PopupWindow
    {
        private readonly LUtfNode selectedNode;
        public FrameRectEditorPopup(LUtfNode selectedNode)
        {
            this.selectedNode = selectedNode;

            try
            {
                frameRectsOriginal = FrameRectCalculator.ParseFrameRects(selectedNode.Data);
                frameRectsUpdated = new List<FrameRect>(frameRectsOriginal);
            }
            catch (Exception ex)
            {
                sourceDataError = true;
                sourceDataErrorMessage = ex.Message;

            }


        }

        public override string Title { get; set; } = "Frame Rect Editor";
        public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize;
        public override bool NoClose => false;
        public override Vector2 InitSize => new Vector2(400, 300);

        private readonly float tableWidth = 550f;
        private readonly float tableHeight = 300f;

        private int texWidth = 256;
        private int texHeight = 256;
        private int gridSizeX = 4;
        private int gridSizeY = 4;
        private int frameCount = 16;
        private int selectedFrameIndex = -1;

        private bool generateRect;
        private bool sourceDataError;
        private string sourceDataErrorMessage = "";

        private readonly List<FrameRect> frameRectsOriginal = new();
        private List<FrameRect> frameRectsUpdated = new();

        public override void Draw(bool appearing)
        {
            if (generateRect)
            {
                DrawGenerateOptions();
            }
            else if (sourceDataError)
            {
                DrawErrorMessage();
            }
            else
            {
                DrawEditor();
            }
        }

        private void DrawErrorMessage()
        {
            ImGui.Spacing();
            ImGui.PushFont(ImGuiHelper.Roboto, 22);
            ImGuiExt.CenterText(sourceDataErrorMessage, Theme.ErrorTextColour);
            ImGui.PopFont();
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGuiExt.CenterText("New frame rect data needs to be generated.");
            ImGuiExt.CenterText("This will delete any existing data on the selected node.", Theme.WarnTextColour);
            ImGui.NewLine();
            ImGuiExt.CenterText("Do you want to continue?");

            ImGui.NewLine();
            ImGui.Separator();
            ImGui.NewLine();
            float avail = ImGui.GetContentRegionAvail().X;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Theme.ButtonPadding * ImGuiHelper.Scale);
            if (ImGui.Button("Continue", new Vector2(Theme.ButtonWidth, 0)))
            {
                generateRect = true;
                sourceDataError = false;
                sourceDataErrorMessage = "";
            }
            ImGui.SameLine();
            ImGui.SetCursorPosX((avail - Theme.ButtonWidth - Theme.ButtonPadding) * ImGuiHelper.Scale);
            if (ImGui.Button("Cancel", new Vector2(Theme.ButtonWidth, 0)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.Spacing();
        }

        private void DrawGenerateOptions()
        {
            ImGui.Spacing();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Texture size");
            ImGui.SameLine(Theme.LabelWidthMedium);

            ImGui.InputInt("##TextureSizeX", ref texWidth);
            ImGui.SameLine();
            ImGui.InputInt("##TextureSizeY", ref texHeight);

            ImGui.Spacing();

            ImGui.Text("Grid size"); ImGui.SameLine(Theme.LabelWidthMedium);
            ImGui.InputInt("##GridSizeX", ref gridSizeX);
            ImGui.SameLine();
            ImGui.InputInt("##GridSizeY", ref gridSizeY);
            ImGui.Spacing();
            ImGui.Text("Frame Count"); ImGui.SameLine(Theme.LabelWidthMedium);
            ImGui.InputInt("##frameCount", ref frameCount);

            ImGui.NewLine();
            ImGui.Separator();
            ImGui.NewLine();

            ImGui.Dummy(new Vector2(ImGui.GetFrameHeightWithSpacing()));
            ImGui.SameLine();
            if (ImGui.Button("Generate Frame Rects", new Vector2(Theme.ButtonWidthLong, 0)))
            {
                frameRectsUpdated = FrameRectCalculator.GenerateFrameRects(gridSizeX, gridSizeY, frameCount);
                generateRect = false;
            }
            ImGui.SameLine();
            ImGui.Dummy(new Vector2(ImGui.GetFrameHeightWithSpacing()));
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(Theme.ButtonWidthLong, 0)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            ImGui.Dummy(new Vector2(ImGui.GetFrameHeightWithSpacing()));

        }

        private void DrawEditor()
        {
            float scale = ImGuiHelper.Scale;
            float buttonColWidth = Theme.SquareButtonWidth + 16 * scale;

            // ---------- Top bar ----------
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"Frames: {frameRectsUpdated.Count}");


            ImGui.SameLine();
            float btnWidth = 220 * scale;
            float avail = ImGui.GetContentRegionAvail().X;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + avail - btnWidth - Theme.ButtonPadding);

            if (ImGui.Button("Generate New Frame Rect Data", new Vector2(btnWidth, 0)))
            {
                generateRect = true;
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // ---------- Table ----------
            if (ImGui.BeginTable(
                "##FrameRectTable",
                6,
                ImGuiTableFlags.Borders |
                ImGuiTableFlags.RowBg |
                ImGuiTableFlags.ScrollY,
                new Vector2(tableWidth, tableHeight)))
            {
                ImGui.TableSetupColumn("Frame #", ImGuiTableColumnFlags.WidthFixed, 60f);
                ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("U0", ImGuiTableColumnFlags.WidthFixed, 80f);
                ImGui.TableSetupColumn("V0", ImGuiTableColumnFlags.WidthFixed, 80f);
                ImGui.TableSetupColumn("U1", ImGuiTableColumnFlags.WidthFixed, 80f);
                ImGui.TableSetupColumn("V1", ImGuiTableColumnFlags.WidthFixed, 80f);
                ImGui.TableHeadersRow();

                for (int i = 0; i < frameRectsUpdated.Count; i++)
                {
                    ImGui.TableNextRow();
                    ImGui.PushID(i);

                    bool selected = selectedFrameIndex == i;

                    // ---- Frame number (selectable row) ----
                    ImGui.TableNextColumn();

                    ImGui.AlignTextToFramePadding();
                    if (ImGui.Selectable(
                        $"{i + 1}",
                        selected,
                        ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.NoAutoClosePopups))
                    {
                        selectedFrameIndex = i;
                    }

                    var rect = frameRectsUpdated[i];

                    ImGui.TableNextColumn();

                    int index = (int)rect.Index;
                    ImGui.PushItemWidth(-1);
                    if (ImGui.InputInt("##idx", ref index))
                    {
                        rect.Index = (uint)Math.Max(0, index);
                    }

                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(-1);
                    ImGui.InputFloat("##u0", ref rect.U0);

                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(-1);
                    ImGui.InputFloat("##v0", ref rect.V0);

                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(-1);
                    ImGui.InputFloat("##u1", ref rect.U1);

                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(-1);
                    ImGui.InputFloat("##v1", ref rect.V1);

                    frameRectsUpdated[i] = rect;
                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            // ---------- Right-side buttons ----------
            ImGui.SameLine();
            ImGui.BeginChild(
                "##frameRectButtons",
                new Vector2(buttonColWidth, tableHeight));

            ImGui.BeginDisabled(selectedFrameIndex < 0);

            if (ImGui.Button(Icons.ArrowUp.ToString(), new Vector2(Theme.SquareButtonWidth)))
            {
                if (selectedFrameIndex > 0)
                {
                    (frameRectsUpdated[selectedFrameIndex - 1], frameRectsUpdated[selectedFrameIndex]) =
                        (frameRectsUpdated[selectedFrameIndex], frameRectsUpdated[selectedFrameIndex - 1]);
                    selectedFrameIndex--;
                }
            }

            ImGui.Spacing();

            if (ImGui.Button(Icons.ArrowDown.ToString(), new Vector2(Theme.SquareButtonWidth)))
            {
                if (selectedFrameIndex < frameRectsUpdated.Count - 1)
                {
                    (frameRectsUpdated[selectedFrameIndex + 1], frameRectsUpdated[selectedFrameIndex]) =
                        (frameRectsUpdated[selectedFrameIndex], frameRectsUpdated[selectedFrameIndex + 1]);
                    selectedFrameIndex++;
                }
            }

            ImGui.EndDisabled();

            ImGui.Dummy(new Vector2(0, ImGui.GetFrameHeightWithSpacing()));

            if (ImGui.Button(Icons.SquarePlus.ToString(), new Vector2(Theme.SquareButtonWidth)))
            {
                frameRectsUpdated.Add(new FrameRect
                {
                    Index = 0,
                    U0 = 0,
                    V0 = 0,
                    U1 = 1,
                    V1 = 1
                });
                selectedFrameIndex = frameRectsUpdated.Count - 1;
            }

            ImGui.Spacing();

            ImGui.BeginDisabled(selectedFrameIndex < 0);
            if (ImGui.Button(Icons.TrashAlt.ToString(), new Vector2(Theme.SquareButtonWidth)))
            {
                frameRectsUpdated.RemoveAt(selectedFrameIndex);
                selectedFrameIndex = Math.Clamp(selectedFrameIndex, 0, frameRectsUpdated.Count - 1);
            }
            ImGui.EndDisabled();

            ImGui.EndChild();

            // ---------- Bottom action area ----------
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            float padding = 12 * scale;
            float bottomBtnWidth = 120 * scale;
            float startX = ImGui.GetCursorPosX();
            float bottomAvail = ImGui.GetContentRegionAvail().X;

            ImGui.SetCursorPosX(startX + padding);
            if (ImGui.Button("Apply", new Vector2(bottomBtnWidth, 0)))
            {
                selectedNode.Data = UnsafeHelpers.CastArray(frameRectsUpdated.ToArray());
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(startX + bottomAvail - bottomBtnWidth - padding);
            if (ImGui.Button("Cancel", new Vector2(bottomBtnWidth, 0)))
            {

                ImGui.CloseCurrentPopup();
            }
        }

    }
}
