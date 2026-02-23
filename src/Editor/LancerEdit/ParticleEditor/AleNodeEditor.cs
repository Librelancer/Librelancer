using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Fx;
using LibreLancer.Graphics;
using LibreLancer.ImUI;
using LibreLancer.Utf.Ale;

namespace LancerEdit;

static class AleNodeEditor
{
    static bool EditBlendOp(string id, ref BlendOp op)
    {
        bool edited = false;
        ImGui.PushID(id);
        var og = op;
        if (ImGui.BeginCombo("##blend-op", op.ToString()))
        {
            if (ImGui.Selectable("Zero", op == BlendOp.Zero))
                op = BlendOp.Zero;
            if (ImGui.Selectable("One", op == BlendOp.One))
                op = BlendOp.One;
            if (ImGui.Selectable("SrcColor", op == BlendOp.SrcColor))
                op = BlendOp.SrcColor;
            if (ImGui.Selectable("1 - SrcColor", op == BlendOp.InvSrcColor))
                op = BlendOp.InvSrcColor;
            if (ImGui.Selectable("SrcAlpha", op == BlendOp.SrcAlpha))
                op = BlendOp.SrcAlpha;
            if (ImGui.Selectable("1 - SrcAlpha", op == BlendOp.InvSrcAlpha))
                op = BlendOp.InvSrcAlpha;
            if (ImGui.Selectable("DstAlpha", op == BlendOp.DstAlpha))
                op = BlendOp.DstAlpha;
            if (ImGui.Selectable("1 - DstAlpha", op == BlendOp.InvDstAlpha))
                op = BlendOp.InvDstAlpha;
            if (ImGui.Selectable("DstColor", op == BlendOp.DstColor))
                op = BlendOp.DstColor;
            if (ImGui.Selectable("1 - DstColor", op == BlendOp.InvDstColor))
                op = BlendOp.InvDstColor;
            if (ImGui.Selectable("SrcAlphaSat", op == BlendOp.SrcAlphaSat))
                op = BlendOp.SrcAlphaSat;
            ImGui.EndCombo();
        }

        ImGui.PopID();
        return og != op;
    }

    static void EditEasing(string id, EditorUndoBuffer undo, FieldAccessor<EasingTypes> easing)
    {
        ImGui.PushID(id);
        Controls.EditControlSetup(id, 100);
        ref var v = ref easing();
        if (ImGui.BeginCombo("##easing", v.ToString()))
        {
            if (ImGui.Selectable("Linear", v == EasingTypes.Linear) && v != EasingTypes.Linear)
                undo.Set("Easing", easing, EasingTypes.Linear);
            if (ImGui.Selectable("Ease In", v == EasingTypes.EaseIn) && v != EasingTypes.EaseIn)
                undo.Set("Easing", easing, EasingTypes.EaseIn);
            if (ImGui.Selectable("Ease Out", v == EasingTypes.EaseOut) && v != EasingTypes.EaseOut)
                undo.Set("Easing", easing, EasingTypes.EaseOut);
            if (ImGui.Selectable("Ease In-Out", v == EasingTypes.EaseInOut) && v != EasingTypes.EaseInOut)
                undo.Set("Easing", easing, EasingTypes.EaseInOut);
            if (ImGui.Selectable("Step", v == EasingTypes.Step) && v != EasingTypes.Step)
                undo.Set("Easing", easing, EasingTypes.Step);
            ImGui.EndCombo();
        }

        ImGui.PopID();
    }

    static void EditLoops(string id, EditorUndoBuffer undo, FieldAccessor<LoopFlags> loop)
    {
        ImGui.PushID(id);
        Controls.EditControlSetup("Loop", -1);
        ref var v = ref loop();
        if (ImGui.BeginCombo("##easing", v.ToString()))
        {
            if (ImGui.Selectable("Play Once", v == LoopFlags.PlayOnce) && v != LoopFlags.PlayOnce)
                undo.Set("Loop", loop, LoopFlags.PlayOnce);
            if (ImGui.Selectable("Repeat", v == LoopFlags.Repeat) && v != LoopFlags.Repeat)
                undo.Set("Loop", loop, LoopFlags.Repeat);
            if (ImGui.Selectable("Reverse", v == LoopFlags.Reverse) && v != LoopFlags.Reverse)
                undo.Set("Loop", loop, LoopFlags.Reverse);
            if (ImGui.Selectable("Continue", v == LoopFlags.Continue) && v != LoopFlags.Continue)
                undo.Set("Loop", loop, LoopFlags.PlayOnce);
            ImGui.EndCombo();
        }

        ImGui.PopID();
    }

    static bool PropertyHeader(string id)
    {
        ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.FrameBg));
        var r = ImGui.CollapsingHeader(id); //no defaultopen here
        ImGui.PopStyleColor();
        return r;
    }

    static bool BeginComplex(string property, object complex, bool canCreate, out bool create)
    {
        create = false;
        Controls.EndEditorTable();
        if (!PropertyHeader(property))
        {
            Controls.BeginEditorTable("##ale");
            return false;
        }

        ImGui.BeginGroup();
        if (complex == null)
        {
            ImGui.Text("No Value");
            ImGui.PushID(property);
            if (canCreate && ImGui.Button("Create"))
                create = true;
            ImGui.PopID();
            ImGui.EndGroup();
            Controls.BeginEditorTable("##ale");
            return false;
        }

        ImGui.PushID(property);
        return true;
    }

    static void EndComplex()
    {
        ImGui.EndGroup();
        ImGui.PopID();

        Controls.BeginEditorTable("##ale");
    }

    static void EditFloatAnimation(string property, FieldAccessor<AlchemyFloatAnimation> anim, EditorUndoBuffer undo, bool optional = false)
    {
        var floats = anim();
        if (!BeginComplex(property, floats, optional, out var create))
        {
            if (create)
                undo.Set(property, anim, new AlchemyFloatAnimation());
            return;
        }
        EditEasing("SParam Easing", undo, () => ref floats.Type);
        if (optional)
        {
            ImGui.SameLine();
            if (ImGui.Button("Clear Property"))
            {
                undo.Set(property, anim, null);
                return;
            }
        }
        ImGui.Separator();
        int delFloats = -1;
        for (int i = 0; i < floats.Items.Count; i++)
        {
            var c = floats.Items[i];
            ImGui.PushID(i);
            Controls.InputFloatUndo("SParam", undo, () => ref c.SParam, null, "%.3f", 100 * ImGuiHelper.Scale);
            if (c.Keyframes.Count < 1)
                c.Keyframes.Add(default);
            if (!ImGui.BeginTable("keyframes", c.Keyframes.Count > 1 ? 3 : 2,
                    ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit |
                    ImGuiTableFlags.NoPadOuterX))
                continue;
            ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
            if (c.Keyframes.Count > 1)
                ImGui.TableSetupColumn("##", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();
            int del = -1;
            for (int j = 0; j < c.Keyframes.Count; j++)
            {
                ImGui.PushID(j);
                var idx = j; // capture variable
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                Controls.InputFloatValueUndo("Time", undo, () => ref c.Keyframes[idx].Time, null, "%.5f");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                Controls.InputFloatValueUndo("Value", undo, () => ref c.Keyframes[idx].Value, null, "%.5f");
                if (c.Keyframes.Count > 1)
                {
                    ImGui.TableNextColumn();
                    if (ImGui.Button($"{Icons.TrashAlt}"))
                    {
                        del = idx;
                    }
                }

                ImGui.PopID();
            }

            ImGui.EndTable();
            if (ImGui.Button("Add Keyframe"))
                undo.Commit(new ListAdd<FloatKeyframe>("Add Keyframe", c.Keyframes, default));
            if (del != -1)
                undo.Commit(new ListRemove<FloatKeyframe>("Delete Keyframe", c.Keyframes, del, c.Keyframes[del]));
            if (floats.Items.Count > 1)
            {
                ImGui.SameLine();
                if (ImGui.Button($"{Icons.TrashAlt}"))
                {
                    delFloats = i;
                }
            }
            ImGui.PopID();
            ImGui.Separator();
        }
        if (ImGui.Button("Add SParam"))
        {
            undo.Commit(new ListAdd<AlchemyFloats>("Add SParam", floats.Items, new AlchemyFloats()));
        }
        if (delFloats != -1)
        {
            undo.Commit(
                new ListRemove<AlchemyFloats>("Remove SParam", floats.Items, delFloats, floats.Items[delFloats]));
        }
        EndComplex();
    }

    static void DrawColorGradient(AlchemyColors c)
    {
        var fh = ImGui.GetFrameHeight();
        var w = ImGui.GetContentRegionAvail().X;
        ImGui.Dummy(new Vector2(w, fh));
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var dl = ImGui.GetWindowDrawList();
        dl.PushClipRect(min, max, true);
        dl.AddRectFilled(min, max, (VertexDiffuse)c.Keyframes[^1].Value.ToColor4());
        for (int i = 0; i < c.Keyframes.Count - 1; i++)
        {
            var cLeft = (VertexDiffuse)c.Keyframes[i].Value.ToColor4();
            var cRight = (VertexDiffuse)c.Keyframes[i + 1].Value.ToColor4();
            var x1 = min.X + (c.Keyframes[i].Time) * w;
            var x2 = min.X + (c.Keyframes[i + 1].Time) * w;
            ImGuiHelper.DrawHorizontalGradient(dl, new(x1, min.Y), new (x2, max.Y),
                cLeft, cRight, c.Type);
        }
        dl.AddRect(min,max, ImGui.GetColorU32(ImGuiCol.Border));
        dl.PopClipRect();
    }

    static void EditColorAnimation(string property, AlchemyColorAnimation colors, EditorUndoBuffer undo)
    {
        if (!BeginComplex(property, colors, false, out _))
            return;
        EditEasing("SParam Easing", undo, () => ref colors.Type);
        ImGui.Separator();
        int delColors = -1;
        for (int i = 0; i < colors.Items.Count; i++)
        {
            var c = colors.Items[i];
            ImGui.PushID(i);
            Controls.InputFloatUndo("SParam", undo, () => ref c.SParam, null, "%.3f", 100 * ImGuiHelper.Scale);
            ImGui.SameLine();
            EditEasing("Easing", undo, () => ref c.Type);
            if (c.Keyframes.Count < 1)
                c.Keyframes.Add(default);
            DrawColorGradient(c);
            if (!ImGui.BeginTable("keyframes", c.Keyframes.Count > 1 ? 5 : 4,
                    ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit |
                    ImGuiTableFlags.NoPadOuterX))
                continue;
            ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("R", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("G", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("B", ImGuiTableColumnFlags.WidthStretch);
            if (c.Keyframes.Count > 1)
                ImGui.TableSetupColumn("##", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();
            int del = -1;
            for (int j = 0; j < c.Keyframes.Count; j++)
            {
                ImGui.PushID(j);
                var idx = j; // capture variable
                ImGui.TableNextRow();
                var val = c.Keyframes[idx].Value;
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, (VertexDiffuse)new Vector4(val.R, val.G, val.B, 1));
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                Controls.InputFloatValueUndo("Time", undo, () => ref c.Keyframes[idx].Time, null, "%.5f");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                Controls.InputFloatValueUndo("R", undo, () => ref c.Keyframes[idx].Value.R, null, "%.5f");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                Controls.InputFloatValueUndo("G", undo, () => ref c.Keyframes[idx].Value.G, null, "%.5f");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                Controls.InputFloatValueUndo("B", undo, () => ref c.Keyframes[idx].Value.B, null, "%.5f");
                if (c.Keyframes.Count > 1)
                {
                    ImGui.TableNextColumn();
                    if (ImGui.Button($"{Icons.TrashAlt}"))
                    {
                        del = idx;
                    }
                }
                ImGui.PopID();
            }

            ImGui.EndTable();
            if (ImGui.Button("Add Keyframe"))
                undo.Commit(new ListAdd<ColorKeyframe>("Add Keyframe", c.Keyframes, default));
            if (del != -1)
                undo.Commit(new ListRemove<ColorKeyframe>("Delete Keyframe", c.Keyframes, del, c.Keyframes[del]));
            if (colors.Items.Count > 1)
            {
                ImGui.SameLine();
                if (ImGui.Button($"{Icons.TrashAlt}"))
                {
                    delColors = i;
                }
            }
            ImGui.PopID();
            ImGui.Separator();
        }
        if (ImGui.Button("Add SParam"))
        {
            undo.Commit(new ListAdd<AlchemyColors>("Add SParam", colors.Items, new AlchemyColors()));
        }
        if (delColors != -1)
        {
            undo.Commit(
                new ListRemove<AlchemyColors>("Remove SParam", colors.Items, delColors, colors.Items[delColors]));
        }
        EndComplex();
    }


    static void EditCurveAnimation(string property, FieldAccessor<AlchemyCurveAnimation> anim, EditorUndoBuffer undo, bool optional = false)
    {
        var curve = anim();
        if (!BeginComplex(property, curve, optional, out var create))
        {
            if (create)
                undo.Set(property, anim, new AlchemyCurveAnimation(0));
            return;
        }
        EditEasing("SParam Easing", undo, () => ref curve.Type);
        if (optional)
        {
            ImGui.SameLine();
            if (ImGui.Button("Clear Property"))
            {
                undo.Set(property, anim, null);
                return;
            }
        }
        ImGui.Separator();
        int delCurve = -1;
        for (int i = 0; i < curve.Items.Count; i++)
        {
            var c = curve.Items[i];
            ImGui.PushID(i);
            Controls.InputFloatUndo("SParam", undo, () => ref c.SParam, null, "%.3f", 100 * ImGuiHelper.Scale);
            var old = c.IsCurve;
            ImGui.SameLine();
            ImGuiExt.ButtonDivided("##selector", "Curve", "Value", ref c.IsCurve);
            if (old != c.IsCurve)
                undo.Set("Is Curve", () => ref c.IsCurve, old, c.IsCurve);
            if (curve.Items.Count > 1)
            {
                ImGui.SameLine();
                if (ImGui.Button($"{Icons.TrashAlt}"))
                {
                    delCurve = i;
                }
            }

            if (c.IsCurve)
            {
                if (c.Keyframes.Count < 1)
                    c.Keyframes.Add(default);
                EditLoops("loop", undo, () => ref c.Flags);
                if (!ImGui.BeginTable("keyframes", c.Keyframes.Count > 1 ? 5 : 4,
                        ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit |
                        ImGuiTableFlags.NoPadOuterX))
                    continue;
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("End", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Start", ImGuiTableColumnFlags.WidthStretch);
                if (c.Keyframes.Count > 1)
                    ImGui.TableSetupColumn("##", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableHeadersRow();
                int del = -1;
                for (int j = 0; j < c.Keyframes.Count; j++)
                {
                    ImGui.PushID(j);
                    var idx = j; // capture variable
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    Controls.InputFloatValueUndo("Time", undo, () => ref c.Keyframes[idx].Time, null, "%.5f");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    Controls.InputFloatValueUndo("Value", undo, () => ref c.Keyframes[idx].Value, null, "%.5f");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    Controls.InputFloatValueUndo("End", undo, () => ref c.Keyframes[idx].End, null, "%.5f");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    Controls.InputFloatValueUndo("Start", undo, () => ref c.Keyframes[idx].Start, null, "%.5f");
                    if (c.Keyframes.Count > 1)
                    {
                        ImGui.TableNextColumn();
                        if (ImGui.Button($"{Icons.TrashAlt}"))
                        {
                            del = idx;
                        }
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();
                if (ImGui.Button("Add Keyframe"))
                    undo.Commit(new ListAdd<CurveKeyframe>("Add Keyframe", c.Keyframes, default));
                if (del != -1)
                    undo.Commit(new ListRemove<CurveKeyframe>("Delete Keyframe", c.Keyframes, del, c.Keyframes[del]));
            }
            else
            {
                Controls.InputFloatUndo("Value", undo, () => ref c.Value);
            }

            ImGui.PopID();
            ImGui.Separator();
        }

        if (ImGui.Button("Add SParam"))
        {
            undo.Commit(new ListAdd<AlchemyCurve>("Add SParam", curve.Items, new AlchemyCurve()));
        }

        if (delCurve != -1)
        {
            undo.Commit(new ListRemove<AlchemyCurve>("Remove SParam", curve.Items, delCurve, curve.Items[delCurve]));
        }

        EndComplex();
    }

    private static float oldFloat;

    static void EditLifespan(EditorUndoBuffer buffer, FieldAccessor<float> value)
    {
        ImGui.PushID("Lifespan");
        ref float v = ref value();
        if (v >= 3.4e36f) // Super huge value (not quite float.MaxValue but will definitely be way too big)
        {
            Controls.EditControlSetup("Lifespan", 0);
            if (ImGui.Button($"Add", new(ImGui.CalcItemWidth(), 0)))
            {
                buffer.Set("Lifespan", value, 1);
            }
        }
        else
        {
            Controls.EditControlSetup("Lifespan", 0, -Controls.ButtonWidth("∞"));
            float oldCopy = v;
            ImGui.InputFloat("##input", ref v, 0, 0);
            if (ImGui.IsItemActivated())
            {
                oldFloat = oldCopy;
            }

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                buffer.Set("Lifespan", value, oldFloat, v, null);
            }

            ImGui.SameLine();
            if (ImGui.Button("∞"))
            {
                buffer.Set("Lifespan", value, float.MaxValue);
            }
        }

        ImGui.PopID();
    }

    static void EditFxNode(FxNode node, EditorUndoBuffer undo)
    {
        if (node.Transform?.HasTransform ?? false)
        {
            Controls.TableSeparatorText("Transform");
            EditCurveAnimation("Translate X", () => ref node.Transform.TranslateX, undo);
            EditCurveAnimation("Translate Y", () => ref node.Transform.TranslateY, undo);
            EditCurveAnimation("Translate Z", () => ref node.Transform.TranslateZ, undo);
            EditCurveAnimation("Rotate Pitch", () => ref node.Transform.RotatePitch, undo);
            EditCurveAnimation("Rotate Yaw", () => ref node.Transform.RotateYaw, undo);
            EditCurveAnimation("Rotate Roll", () => ref node.Transform.RotateRoll, undo);
            EditCurveAnimation("Scale X", () => ref node.Transform.ScaleX, undo);
            EditCurveAnimation("Scale Y", () => ref node.Transform.ScaleY, undo);
            EditCurveAnimation("Scale Z", () => ref node.Transform.ScaleZ, undo);
            Controls.EditControlSetup("", 0);
            if (ImGui.Button("Remove Transform"))
            {
                undo.Set("Has Transform", () => ref node.Transform.HasTransform, false);
            }
        }
        else
        {
            Controls.EditControlSetup("Transform", 0);
            if (ImGui.Button("Add", new(ImGui.CalcItemWidth(), 0)))
            {
                node.Transform ??= new();
                node.Transform.TranslateX ??= new AlchemyCurveAnimation(0);
                node.Transform.TranslateY ??= new AlchemyCurveAnimation(0);
                node.Transform.TranslateZ ??= new AlchemyCurveAnimation(0);
                node.Transform.RotatePitch ??= new AlchemyCurveAnimation(0);
                node.Transform.RotateYaw ??= new AlchemyCurveAnimation(0);
                node.Transform.RotateRoll ??= new AlchemyCurveAnimation(0);
                node.Transform.ScaleX ??= new AlchemyCurveAnimation(1);
                node.Transform.ScaleY ??= new AlchemyCurveAnimation(1);
                node.Transform.ScaleZ ??= new AlchemyCurveAnimation(1);
                undo.Set("Has Transform", () => ref node.Transform.HasTransform, true);
            }
        }

        EditLifespan(undo, () => ref node.NodeLifeSpan);
    }

    // --- APPEARANCES ---





    static void EditBlend(FieldAccessor<ushort> blendMode, EditorUndoBuffer undo)
    {
        Controls.EditControlSetup("Blend Info", 0);
        var w = ImGui.CalcItemWidth() / 2;
        ref var v = ref blendMode();
        var (src, dst) = BlendMode.Deconstruct(v);
        ImGui.SetNextItemWidth(w);
        if (EditBlendOp("src", ref src))
            undo.Set("Blend Info", blendMode, BlendMode.Create(src, dst));
        ImGui.SameLine(0, 0);
        ImGui.SetNextItemWidth(w);
        if (EditBlendOp("dst", ref dst))
            undo.Set("Blend Info", blendMode, BlendMode.Create(src, dst));
    }

    static void AppearanceCommon(FxBasicAppearance node, EditorUndoBuffer undo)
    {
        Controls.InputTextIdUndo("Texture", undo, () => ref node.Texture);
        Controls.CheckboxUndo("Quad Texture", undo, () => ref node.QuadTexture);
        Controls.CheckboxUndo("Motion Blur", undo, () => ref node.MotionBlur);
        Controls.CheckboxUndo("Flip Horizontal", undo, () => ref node.FlipHorizontal);
        Controls.CheckboxUndo("Flip Vertical", undo, () => ref node.FlipVertical);
        Controls.CheckboxUndo("Use CommonTexFrame", undo, () => ref node.UseCommonTexFrame);
        EditBlend(() => ref node.BlendInfo, undo);
        EditColorAnimation("Color", node.Color, undo);
        EditFloatAnimation("Alpha", () => ref node.Alpha, undo);
        EditFloatAnimation("Tex Frame", () => ref node.TexFrame, undo, true);
        EditCurveAnimation("Common Tex Frame", () => ref node.CommonTexFrame, undo, true);
    }

    static void EditFxBasicAppearance(FxBasicAppearance node, EditorUndoBuffer undo)
    {
        AppearanceCommon(node, undo);
        EditFloatAnimation("Size", () => ref node.Size, undo);
        EditFloatAnimation("Rotate", () => ref node.Rotate, undo);
    }

    static void EditFLBeamAppearance(FLBeamAppearance node, EditorUndoBuffer undo)
    {
        AppearanceCommon(node, undo);
        Controls.CheckboxUndo("Dupe First Particle",  undo, () => ref node.DupeFirstParticle);
        Controls.CheckboxUndo("Disable Placeholder", undo, () => ref node.DisablePlaceholder);
        Controls.CheckboxUndo("Line Appearance", undo, () => ref node.LineAppearance);
    }

    static void EditFLDustAppearance(FLDustAppearance node, EditorUndoBuffer undo)
    {
        EditFxBasicAppearance(node, undo); // ?
    }

    static void EditFxMeshAppearance(FxMeshAppearance node, EditorUndoBuffer undo)
    {
        // Doesn't work
    }

    static void EditFxOrientedAppearance(FxOrientedAppearance node, EditorUndoBuffer undo)
    {
        AppearanceCommon(node, undo);
        EditFloatAnimation("Width", () => ref node.Width, undo);
        EditFloatAnimation("Height", () => ref node.Height, undo);
    }

    static void EditFxParticleAppearance(FxParticleAppearance node, EditorUndoBuffer undo)
    {
        Controls.InputTextUndo("Life Name", undo, () => ref node.LifeName);
        Controls.InputTextUndo("Death Name", undo, () => ref node.DeathName);
        Controls.CheckboxUndo("Dynamic Rotation", undo, () => ref node.UseDynamicRotation);
        Controls.CheckboxUndo("Smooth Rotation", undo, () => ref node.SmoothRotation);
    }

    static void EditFxPerpAppearance(FxPerpAppearance node, EditorUndoBuffer undo)
    {
        AppearanceCommon(node, undo);
        EditFloatAnimation("Size", () => ref node.Size, undo);
        EditFloatAnimation("Rotate", () => ref node.Rotate, undo);
    }

    static void EditFxRectAppearance(FxRectAppearance node, EditorUndoBuffer undo)
    {
        AppearanceCommon(node, undo);
        EditFloatAnimation("Width", () => ref node.Width, undo);
        EditFloatAnimation("Length", () => ref node.Length, undo);
        EditFloatAnimation("Scale", () => ref node.Width, undo);
        Controls.CheckboxUndo("Center On Pos", undo, () => ref node.CenterOnPos);
        Controls.CheckboxUndo("Viewing Angle Fade", undo, () => ref node.ViewingAngleFade);
    }

    // -- EMITTERS --

    static void EditFxEmitter(FxEmitter node, EditorUndoBuffer undo)
    {
        Controls.InputIntUndo("Initial Particles", undo, () => ref node.InitialParticles);
        EditCurveAnimation("Init Lifespan", () => ref node.InitLifeSpan, undo);
        EditCurveAnimation("Frequency", () => ref node.Frequency, undo);
        EditCurveAnimation("Emit Count", () => ref node.EmitCount, undo);
        EditCurveAnimation("Max Particles", () => ref node.MaxParticles, undo);
        EditCurveAnimation("Pressure", () => ref node.Pressure, undo);
        EditCurveAnimation("Velocity Approach", () => ref node.VelocityApproach, undo);
    }

    static void EditFxConeEmitter(FxConeEmitter node, EditorUndoBuffer undo)
    {
        EditFxEmitter(node, undo);
        EditCurveAnimation("Min Spread", () => ref node.MinSpread, undo);
        EditCurveAnimation("Max Spread", () => ref node.MaxSpread, undo);
        EditCurveAnimation("Min Radius", () => ref node.MinRadius, undo);
        EditCurveAnimation("Max Radius", () => ref node.MaxRadius, undo);
    }

    static void EditFxCubeEmitter(FxCubeEmitter node, EditorUndoBuffer undo)
    {
        EditFxEmitter(node, undo);
        EditCurveAnimation("Width", () => ref node.Width, undo);
        EditCurveAnimation("Height", () => ref node.Height, undo);
        EditCurveAnimation("Depth", () => ref node.Depth, undo);
        EditCurveAnimation("Min Spread", () => ref node.MinSpread, undo);
        EditCurveAnimation("Max Spread", () => ref node.MaxSpread, undo);
    }

    static void EditFxSphereEmitter(FxSphereEmitter node, EditorUndoBuffer undo)
    {
        EditFxEmitter(node, undo);
        EditCurveAnimation("Min Radius", () => ref node.MinRadius, undo);
        EditCurveAnimation("Max Radius", () => ref node.MaxRadius, undo);
    }

    // -- FIELDS --

    static void EditFLDustField(FLDustField node, EditorUndoBuffer undo)
    {
        EditCurveAnimation("Max Radius", () => ref node.MaxRadius, undo);
    }

    static void EditFLBeamField(FLBeamField node, EditorUndoBuffer undo)
    {
    }

    static void EditFxAirField(FxAirField node, EditorUndoBuffer undo)
    {
        EditCurveAnimation("Magnitude", () => ref node.Magnitude, undo);
        EditCurveAnimation("Approach", () => ref node.Approach, undo);
    }

    static void EditFxCollideField(FxCollideField node, EditorUndoBuffer undo)
    {
        EditCurveAnimation("Reflectivity", () => ref node.Reflectivity, undo);
        EditCurveAnimation("Width", () => ref node.Width, undo);
        EditCurveAnimation("Height", () => ref node.Height, undo);
    }

    static void EditFxGravityField(FxGravityField node, EditorUndoBuffer undo)
    {
        EditCurveAnimation("Gravity", () => ref node.Gravity, undo);
    }

    static void EditFxRadialField(FxRadialField node, EditorUndoBuffer undo)
    {
        EditCurveAnimation("Radius", () => ref node.Radius, undo);
        EditFloatAnimation("Attenuation", () => ref node.Attenuation, undo);
        EditCurveAnimation("Magnitude", () => ref node.Magnitude, undo);
        EditCurveAnimation("Approach", () => ref node.Approach, undo);
    }

    static void EditFxTurbulenceField(FxTurbulenceField node, EditorUndoBuffer undo)
    {
        EditCurveAnimation("Magnitude", () => ref node.Magnitude, undo);
        EditCurveAnimation("Approach", () => ref node.Approach, undo);
    }


    public static void EditNode(FxNode node, EditorUndoBuffer undo)
    {
        if (!Controls.BeginEditorTable("##ale"))
            return;
        EditFxNode(node, undo);
        switch (node)
        {
            case FLBeamAppearance bma:
                EditFLBeamAppearance(bma, undo);
                break;
            case FLDustAppearance dsa:
                EditFLDustAppearance(dsa, undo);
                break;
            case FxMeshAppearance msa:
                EditFxMeshAppearance(msa, undo);
                break;
            case FxOrientedAppearance oda:
                EditFxOrientedAppearance(oda, undo);
                break;
            case FxParticleAppearance fpa:
                EditFxParticleAppearance(fpa, undo);
                break;
            case FxPerpAppearance ppa:
                EditFxPerpAppearance(ppa, undo);
                break;
            case FxRectAppearance rta:
                EditFxRectAppearance(rta, undo);
                break;
            case FxBasicAppearance bsa:
                EditFxBasicAppearance(bsa, undo);
                break;
            case FxConeEmitter cne:
                EditFxConeEmitter(cne, undo);
                break;
            case FxCubeEmitter cbe:
                EditFxCubeEmitter(cbe, undo);
                break;
            case FxSphereEmitter spe:
                EditFxSphereEmitter(spe, undo);
                break;
            case FLDustField dsf:
                EditFLDustField(dsf, undo);
                break;
            case FLBeamField bmf:
                EditFLBeamField(bmf, undo);
                break;
            case FxAirField arf:
                EditFxAirField(arf, undo);
                break;
            case FxCollideField clf:
                EditFxCollideField(clf, undo);
                break;
            case FxGravityField grf:
                EditFxGravityField(grf, undo);
                break;
            case FxRadialField rdf:
                EditFxRadialField(rdf, undo);
                break;
            case FxTurbulenceField tbf:
                EditFxTurbulenceField(tbf, undo);
                break;
        }

        Controls.EndEditorTable();
    }
}
