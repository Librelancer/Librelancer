//auto-generated
#define IMPLOT_DISABLE_OBSOLETE_FUNCTIONS
#include "imgui.h"
#include "implot.h"

namespace cimgui
{
    #include "cimplot.h"
}

static inline ::ImVec2 ConvertToCPP_ImVec2(const cimgui::ImVec2& src)
{
    ::ImVec2 dest;
    dest.x = src.x;
    dest.y = src.y;
    return dest;
}

static inline cimgui::ImVec2 ConvertFromCPP_ImVec2(const ::ImVec2& src)
{
    cimgui::ImVec2 dest;
    dest.x = src.x;
    dest.y = src.y;
    return dest;
}

static inline ::ImVec4 ConvertToCPP_ImVec4(const cimgui::ImVec4& src)
{
    ::ImVec4 dest;
    dest.x = src.x;
    dest.y = src.y;
    dest.z = src.z;
    dest.w = src.w;
    return dest;
}

static inline cimgui::ImVec4 ConvertFromCPP_ImVec4(const ::ImVec4& src)
{
    cimgui::ImVec4 dest;
    dest.x = src.x;
    dest.y = src.y;
    dest.z = src.z;
    dest.w = src.w;
    return dest;
}

static inline ::ImTextureRef ConvertToCPP_ImTextureRef(const cimgui::ImTextureRef& src)
{
    ::ImTextureRef dest;
    dest._TexData = reinterpret_cast<::ImTextureData*>(src._TexData);
    dest._TexID = src._TexID;
    return dest;
}


static inline ::ImPlotPoint ConvertToCPP_ImPlotPoint(const cimgui::ImPlotPoint& src)
{
    ::ImPlotPoint dest;
    dest.x = src.x;
    dest.y = src.y;
    return dest;
}
static inline cimgui::ImPlotPoint ConvertFromCPP_ImPlotPoint(const ::ImPlotPoint& src)
{
    cimgui::ImPlotPoint dest;
    dest.x = src.x;
    dest.y = src.y;
    return dest;
}
static inline ::ImPlotRange ConvertToCPP_ImPlotRange(const cimgui::ImPlotRange& src)
{
    ::ImPlotRange dest;
    dest.Min = src.Min;
    dest.Max = src.Max;
    return dest;
}
static inline cimgui::ImPlotRange ConvertFromCPP_ImPlotRange(const ::ImPlotRange& src)
{
    cimgui::ImPlotRange dest;
    dest.Min = src.Min;
    dest.Max = src.Max;
    return dest;
}
static inline ::ImPlotRect ConvertToCPP_ImPlotRect(const cimgui::ImPlotRect& src)
{
    ::ImPlotRect dest;
    dest.X = ConvertToCPP_ImPlotRange(src.X);
    dest.Y = ConvertToCPP_ImPlotRange(src.Y);
    return dest;
}
static inline cimgui::ImPlotRect ConvertFromCPP_ImPlotRect(const ::ImPlotRect& src)
{
    cimgui::ImPlotRect dest;
    dest.X = ConvertFromCPP_ImPlotRange(src.X);
    dest.Y = ConvertFromCPP_ImPlotRange(src.Y);
    return dest;
}
static inline ::ImPlotSpec ConvertToCPP_ImPlotSpec(const cimgui::ImPlotSpec& src)
{
    ::ImPlotSpec dest;
    dest.LineColor = ConvertToCPP_ImVec4(src.LineColor);
    dest.LineWeight = src.LineWeight;
    dest.FillColor = ConvertToCPP_ImVec4(src.FillColor);
    dest.FillAlpha = src.FillAlpha;
    dest.Marker = static_cast<::ImPlotMarker>(src.Marker);
    dest.MarkerSize = src.MarkerSize;
    dest.MarkerLineColor = ConvertToCPP_ImVec4(src.MarkerLineColor);
    dest.MarkerFillColor = ConvertToCPP_ImVec4(src.MarkerFillColor);
    dest.Size = src.Size;
    dest.Offset = src.Offset;
    dest.Stride = src.Stride;
    dest.Flags = static_cast<::ImPlotItemFlags>(src.Flags);
    return dest;
}
static inline cimgui::ImPlotSpec ConvertFromCPP_ImPlotSpec(const ::ImPlotSpec& src)
{
    cimgui::ImPlotSpec dest;
    dest.LineColor = ConvertFromCPP_ImVec4(src.LineColor);
    dest.LineWeight = src.LineWeight;
    dest.FillColor = ConvertFromCPP_ImVec4(src.FillColor);
    dest.FillAlpha = src.FillAlpha;
    dest.Marker = static_cast<cimgui::ImPlotMarker>(src.Marker);
    dest.MarkerSize = src.MarkerSize;
    dest.MarkerLineColor = ConvertFromCPP_ImVec4(src.MarkerLineColor);
    dest.MarkerFillColor = ConvertFromCPP_ImVec4(src.MarkerFillColor);
    dest.Size = src.Size;
    dest.Offset = src.Offset;
    dest.Stride = src.Stride;
    dest.Flags = static_cast<cimgui::ImPlotItemFlags>(src.Flags);
    return dest;
}
CIMGUI_API cimgui::ImPlotColormap cimgui::ImPlot_AddColormap_Vec4Ptr(const char* name, const cimgui::ImVec4* cols, int size, bool qual)
{
    return static_cast<cimgui::ImPlotColormap>(ImPlot::AddColormap(name, reinterpret_cast<const ::ImVec4*>(cols), size, qual));
}
CIMGUI_API cimgui::ImPlotColormap cimgui::ImPlot_AddColormap_U32Ptr(const char* name, const cimgui::ImU32* cols, int size, bool qual)
{
    return static_cast<cimgui::ImPlotColormap>(ImPlot::AddColormap(name, reinterpret_cast<const ::ImU32*>(cols), size, qual));
}
CIMGUI_API void cimgui::ImPlot_Annotation_Bool(double x, double y, const cimgui::ImVec4 col, const cimgui::ImVec2 pix_offset, bool clamp, bool round)
{
    ImPlot::Annotation(x, y, ConvertToCPP_ImVec4(col), ConvertToCPP_ImVec2(pix_offset), clamp, round);
}
CIMGUI_API bool cimgui::ImPlot_BeginAlignedPlots(const char* group_id, bool vertical)
{
    return ImPlot::BeginAlignedPlots(group_id, vertical);
}
CIMGUI_API bool cimgui::ImPlot_BeginDragDropSourceAxis(cimgui::ImAxis axis, ImGuiDragDropFlags flags)
{
    return ImPlot::BeginDragDropSourceAxis(static_cast<::ImAxis>(axis), flags);
}
CIMGUI_API bool cimgui::ImPlot_BeginDragDropSourceItem(const char* label_id, ImGuiDragDropFlags flags)
{
    return ImPlot::BeginDragDropSourceItem(label_id, flags);
}
CIMGUI_API bool cimgui::ImPlot_BeginDragDropSourcePlot(ImGuiDragDropFlags flags)
{
    return ImPlot::BeginDragDropSourcePlot(flags);
}
CIMGUI_API bool cimgui::ImPlot_BeginDragDropTargetAxis(cimgui::ImAxis axis)
{
    return ImPlot::BeginDragDropTargetAxis(static_cast<::ImAxis>(axis));
}
CIMGUI_API bool cimgui::ImPlot_BeginDragDropTargetLegend()
{
    return ImPlot::BeginDragDropTargetLegend();
}
CIMGUI_API bool cimgui::ImPlot_BeginDragDropTargetPlot()
{
    return ImPlot::BeginDragDropTargetPlot();
}
CIMGUI_API bool cimgui::ImPlot_BeginLegendPopup(const char* label_id, ImGuiMouseButton mouse_button)
{
    return ImPlot::BeginLegendPopup(label_id, mouse_button);
}
CIMGUI_API bool cimgui::ImPlot_BeginPlot(const char* title_id, const cimgui::ImVec2 size, cimgui::ImPlotFlags flags)
{
    return ImPlot::BeginPlot(title_id, ConvertToCPP_ImVec2(size), static_cast<::ImPlotFlags>(flags));
}
CIMGUI_API bool cimgui::ImPlot_BeginSubplots(const char* title_id, int rows, int cols, const cimgui::ImVec2 size, cimgui::ImPlotSubplotFlags flags, float* row_ratios, float* col_ratios)
{
    return ImPlot::BeginSubplots(title_id, rows, cols, ConvertToCPP_ImVec2(size), static_cast<::ImPlotSubplotFlags>(flags), row_ratios, col_ratios);
}
CIMGUI_API void cimgui::ImPlot_BustColorCache(const char* plot_title_id)
{
    ImPlot::BustColorCache(plot_title_id);
}
CIMGUI_API void cimgui::ImPlot_CancelPlotSelection()
{
    ImPlot::CancelPlotSelection();
}
CIMGUI_API bool cimgui::ImPlot_ColormapButton(const char* label, const cimgui::ImVec2 size, cimgui::ImPlotColormap cmap)
{
    return ImPlot::ColormapButton(label, ConvertToCPP_ImVec2(size), static_cast<::ImPlotColormap>(cmap));
}
CIMGUI_API void cimgui::ImPlot_ColormapIcon(cimgui::ImPlotColormap cmap)
{
    ImPlot::ColormapIcon(static_cast<::ImPlotColormap>(cmap));
}
CIMGUI_API void cimgui::ImPlot_ColormapScale(const char* label, double scale_min, double scale_max, const cimgui::ImVec2 size, const char* format, cimgui::ImPlotColormapScaleFlags flags, cimgui::ImPlotColormap cmap)
{
    ImPlot::ColormapScale(label, scale_min, scale_max, ConvertToCPP_ImVec2(size), format, static_cast<::ImPlotColormapScaleFlags>(flags), static_cast<::ImPlotColormap>(cmap));
}
CIMGUI_API bool cimgui::ImPlot_ColormapSlider(const char* label, float* t, cimgui::ImVec4* out, const char* format, cimgui::ImPlotColormap cmap)
{
    return ImPlot::ColormapSlider(label, t, reinterpret_cast<::ImVec4*>(out), format, static_cast<::ImPlotColormap>(cmap));
}
CIMGUI_API ImPlotContext* cimgui::ImPlot_CreateContext()
{
    return ImPlot::CreateContext();
}
CIMGUI_API void cimgui::ImPlot_DestroyContext(ImPlotContext* ctx)
{
    ImPlot::DestroyContext(ctx);
}
CIMGUI_API bool cimgui::ImPlot_DragLineX(int id, double* x, const cimgui::ImVec4 col, float thickness, cimgui::ImPlotDragToolFlags flags, bool* out_clicked, bool* out_hovered, bool* out_held)
{
    return ImPlot::DragLineX(id, x, ConvertToCPP_ImVec4(col), thickness, static_cast<::ImPlotDragToolFlags>(flags), out_clicked, out_hovered, out_held);
}
CIMGUI_API bool cimgui::ImPlot_DragLineY(int id, double* y, const cimgui::ImVec4 col, float thickness, cimgui::ImPlotDragToolFlags flags, bool* out_clicked, bool* out_hovered, bool* out_held)
{
    return ImPlot::DragLineY(id, y, ConvertToCPP_ImVec4(col), thickness, static_cast<::ImPlotDragToolFlags>(flags), out_clicked, out_hovered, out_held);
}
CIMGUI_API bool cimgui::ImPlot_DragPoint(int id, double* x, double* y, const cimgui::ImVec4 col, float size, cimgui::ImPlotDragToolFlags flags, bool* out_clicked, bool* out_hovered, bool* out_held)
{
    return ImPlot::DragPoint(id, x, y, ConvertToCPP_ImVec4(col), size, static_cast<::ImPlotDragToolFlags>(flags), out_clicked, out_hovered, out_held);
}
CIMGUI_API bool cimgui::ImPlot_DragRect(int id, double* x1, double* y1, double* x2, double* y2, const cimgui::ImVec4 col, cimgui::ImPlotDragToolFlags flags, bool* out_clicked, bool* out_hovered, bool* out_held)
{
    return ImPlot::DragRect(id, x1, y1, x2, y2, ConvertToCPP_ImVec4(col), static_cast<::ImPlotDragToolFlags>(flags), out_clicked, out_hovered, out_held);
}
CIMGUI_API void cimgui::ImPlot_EndAlignedPlots()
{
    ImPlot::EndAlignedPlots();
}
CIMGUI_API void cimgui::ImPlot_EndDragDropSource()
{
    ImPlot::EndDragDropSource();
}
CIMGUI_API void cimgui::ImPlot_EndDragDropTarget()
{
    ImPlot::EndDragDropTarget();
}
CIMGUI_API void cimgui::ImPlot_EndLegendPopup()
{
    ImPlot::EndLegendPopup();
}
CIMGUI_API void cimgui::ImPlot_EndPlot()
{
    ImPlot::EndPlot();
}
CIMGUI_API void cimgui::ImPlot_EndSubplots()
{
    ImPlot::EndSubplots();
}
CIMGUI_API cimgui::ImVec4 cimgui::ImPlot_GetColormapColor(int idx, cimgui::ImPlotColormap cmap)
{
    return ConvertFromCPP_ImVec4(ImPlot::GetColormapColor(idx, static_cast<::ImPlotColormap>(cmap)));
}
CIMGUI_API int cimgui::ImPlot_GetColormapCount()
{
    return ImPlot::GetColormapCount();
}
CIMGUI_API cimgui::ImPlotColormap cimgui::ImPlot_GetColormapIndex(const char* name)
{
    return static_cast<cimgui::ImPlotColormap>(ImPlot::GetColormapIndex(name));
}
CIMGUI_API const char* cimgui::ImPlot_GetColormapName(cimgui::ImPlotColormap cmap)
{
    return ImPlot::GetColormapName(static_cast<::ImPlotColormap>(cmap));
}
CIMGUI_API int cimgui::ImPlot_GetColormapSize(cimgui::ImPlotColormap cmap)
{
    return ImPlot::GetColormapSize(static_cast<::ImPlotColormap>(cmap));
}
CIMGUI_API ImPlotContext* cimgui::ImPlot_GetCurrentContext()
{
    return ImPlot::GetCurrentContext();
}
CIMGUI_API cimgui::ImPlotInputMap* cimgui::ImPlot_GetInputMap()
{
    return reinterpret_cast<cimgui::ImPlotInputMap*>(&ImPlot::GetInputMap());
}
CIMGUI_API cimgui::ImVec4 cimgui::ImPlot_GetLastItemColor()
{
    return ConvertFromCPP_ImVec4(ImPlot::GetLastItemColor());
}
CIMGUI_API const char* cimgui::ImPlot_GetMarkerName(cimgui::ImPlotMarker idx)
{
    return ImPlot::GetMarkerName(static_cast<::ImPlotMarker>(idx));
}
CIMGUI_API cimgui::ImDrawList* cimgui::ImPlot_GetPlotDrawList()
{
    return reinterpret_cast<cimgui::ImDrawList*>(ImPlot::GetPlotDrawList());
}
CIMGUI_API cimgui::ImPlotRect cimgui::ImPlot_GetPlotLimits(cimgui::ImAxis x_axis, cimgui::ImAxis y_axis)
{
    return ConvertFromCPP_ImPlotRect(ImPlot::GetPlotLimits(static_cast<::ImAxis>(x_axis), static_cast<::ImAxis>(y_axis)));
}
CIMGUI_API cimgui::ImPlotPoint cimgui::ImPlot_GetPlotMousePos(cimgui::ImAxis x_axis, cimgui::ImAxis y_axis)
{
    return ConvertFromCPP_ImPlotPoint(ImPlot::GetPlotMousePos(static_cast<::ImAxis>(x_axis), static_cast<::ImAxis>(y_axis)));
}
CIMGUI_API cimgui::ImVec2 cimgui::ImPlot_GetPlotPos()
{
    return ConvertFromCPP_ImVec2(ImPlot::GetPlotPos());
}
CIMGUI_API cimgui::ImPlotRect cimgui::ImPlot_GetPlotSelection(cimgui::ImAxis x_axis, cimgui::ImAxis y_axis)
{
    return ConvertFromCPP_ImPlotRect(ImPlot::GetPlotSelection(static_cast<::ImAxis>(x_axis), static_cast<::ImAxis>(y_axis)));
}
CIMGUI_API cimgui::ImVec2 cimgui::ImPlot_GetPlotSize()
{
    return ConvertFromCPP_ImVec2(ImPlot::GetPlotSize());
}
CIMGUI_API cimgui::ImPlotStyle* cimgui::ImPlot_GetStyle()
{
    return reinterpret_cast<cimgui::ImPlotStyle*>(&ImPlot::GetStyle());
}
CIMGUI_API const char* cimgui::ImPlot_GetStyleColorName(cimgui::ImPlotCol idx)
{
    return ImPlot::GetStyleColorName(static_cast<::ImPlotCol>(idx));
}
CIMGUI_API void cimgui::ImPlot_HideNextItem(bool hidden, cimgui::ImPlotCond cond)
{
    ImPlot::HideNextItem(hidden, static_cast<::ImPlotCond>(cond));
}
CIMGUI_API bool cimgui::ImPlot_IsAxisHovered(cimgui::ImAxis axis)
{
    return ImPlot::IsAxisHovered(static_cast<::ImAxis>(axis));
}
CIMGUI_API bool cimgui::ImPlot_IsLegendEntryHovered(const char* label_id)
{
    return ImPlot::IsLegendEntryHovered(label_id);
}
CIMGUI_API bool cimgui::ImPlot_IsPlotHovered()
{
    return ImPlot::IsPlotHovered();
}
CIMGUI_API bool cimgui::ImPlot_IsPlotSelected()
{
    return ImPlot::IsPlotSelected();
}
CIMGUI_API bool cimgui::ImPlot_IsSubplotsHovered()
{
    return ImPlot::IsSubplotsHovered();
}
CIMGUI_API void cimgui::ImPlot_ItemIcon_Vec4(const cimgui::ImVec4 col)
{
    ImPlot::ItemIcon(ConvertToCPP_ImVec4(col));
}
CIMGUI_API void cimgui::ImPlot_ItemIcon_U32(cimgui::ImU32 col)
{
    ImPlot::ItemIcon(static_cast<::ImU32>(col));
}
CIMGUI_API void cimgui::ImPlot_MapInputDefault(cimgui::ImPlotInputMap* dst)
{
    ImPlot::MapInputDefault(reinterpret_cast<::ImPlotInputMap*>(dst));
}
CIMGUI_API void cimgui::ImPlot_MapInputReverse(cimgui::ImPlotInputMap* dst)
{
    ImPlot::MapInputReverse(reinterpret_cast<::ImPlotInputMap*>(dst));
}
CIMGUI_API cimgui::ImVec4 cimgui::ImPlot_NextColormapColor()
{
    return ConvertFromCPP_ImVec4(ImPlot::NextColormapColor());
}
CIMGUI_API cimgui::ImPlotMarker cimgui::ImPlot_NextMarker()
{
    return static_cast<cimgui::ImPlotMarker>(ImPlot::NextMarker());
}
CIMGUI_API cimgui::ImPlotPoint cimgui::ImPlot_PixelsToPlot_Vec2(const cimgui::ImVec2 pix, cimgui::ImAxis x_axis, cimgui::ImAxis y_axis)
{
    return ConvertFromCPP_ImPlotPoint(ImPlot::PixelsToPlot(ConvertToCPP_ImVec2(pix), static_cast<::ImAxis>(x_axis), static_cast<::ImAxis>(y_axis)));
}
CIMGUI_API cimgui::ImPlotPoint cimgui::ImPlot_PixelsToPlot_Float(float x, float y, cimgui::ImAxis x_axis, cimgui::ImAxis y_axis)
{
    return ConvertFromCPP_ImPlotPoint(ImPlot::PixelsToPlot(x, y, static_cast<::ImAxis>(x_axis), static_cast<::ImAxis>(y_axis)));
}
CIMGUI_API void cimgui::ImPlot_PlotBarGroups_FloatPtr(const char* const label_ids[], const float* values, int item_count, int group_count, double group_size, double shift, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBarGroups(label_ids, values, item_count, group_count, group_size, shift, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBarGroups_doublePtr(const char* const label_ids[], const double* values, int item_count, int group_count, double group_size, double shift, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBarGroups(label_ids, values, item_count, group_count, group_size, shift, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBarGroups_S16Ptr(const char* const label_ids[], const cimgui::ImS16* values, int item_count, int group_count, double group_size, double shift, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBarGroups(label_ids, reinterpret_cast<const ::ImS16*>(values), item_count, group_count, group_size, shift, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBarGroups_U16Ptr(const char* const label_ids[], const cimgui::ImU16* values, int item_count, int group_count, double group_size, double shift, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBarGroups(label_ids, reinterpret_cast<const ::ImU16*>(values), item_count, group_count, group_size, shift, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBarGroups_S32Ptr(const char* const label_ids[], const cimgui::ImS32* values, int item_count, int group_count, double group_size, double shift, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBarGroups(label_ids, reinterpret_cast<const ::ImS32*>(values), item_count, group_count, group_size, shift, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBarGroups_U32Ptr(const char* const label_ids[], const cimgui::ImU32* values, int item_count, int group_count, double group_size, double shift, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBarGroups(label_ids, reinterpret_cast<const ::ImU32*>(values), item_count, group_count, group_size, shift, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBarGroups_S64Ptr(const char* const label_ids[], const cimgui::ImS64* values, int item_count, int group_count, double group_size, double shift, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBarGroups(label_ids, reinterpret_cast<const ::ImS64*>(values), item_count, group_count, group_size, shift, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBarGroups_U64Ptr(const char* const label_ids[], const cimgui::ImU64* values, int item_count, int group_count, double group_size, double shift, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBarGroups(label_ids, reinterpret_cast<const ::ImU64*>(values), item_count, group_count, group_size, shift, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBars_FloatPtrInt(const char* label_id, const float* values, int count, double bar_size, double shift, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBars(label_id, values, count, bar_size, shift, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBars_doublePtrInt(const char* label_id, const double* values, int count, double bar_size, double shift, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBars(label_id, values, count, bar_size, shift, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBars_S16PtrInt(const char* label_id, const cimgui::ImS16* values, int count, double bar_size, double shift, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBars(label_id, reinterpret_cast<const ::ImS16*>(values), count, bar_size, shift, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBars_U16PtrInt(const char* label_id, const cimgui::ImU16* values, int count, double bar_size, double shift, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBars(label_id, reinterpret_cast<const ::ImU16*>(values), count, bar_size, shift, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBars_S32PtrInt(const char* label_id, const cimgui::ImS32* values, int count, double bar_size, double shift, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBars(label_id, reinterpret_cast<const ::ImS32*>(values), count, bar_size, shift, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBars_U32PtrInt(const char* label_id, const cimgui::ImU32* values, int count, double bar_size, double shift, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBars(label_id, reinterpret_cast<const ::ImU32*>(values), count, bar_size, shift, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBars_S64PtrInt(const char* label_id, const cimgui::ImS64* values, int count, double bar_size, double shift, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBars(label_id, reinterpret_cast<const ::ImS64*>(values), count, bar_size, shift, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBars_U64PtrInt(const char* label_id, const cimgui::ImU64* values, int count, double bar_size, double shift, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBars(label_id, reinterpret_cast<const ::ImU64*>(values), count, bar_size, shift, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBars_FloatPtrFloatPtr(const char* label_id, const float* xs, const float* ys, int count, double bar_size, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBars(label_id, xs, ys, count, bar_size, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBars_doublePtrdoublePtr(const char* label_id, const double* xs, const double* ys, int count, double bar_size, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBars(label_id, xs, ys, count, bar_size, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBars_S16PtrS16Ptr(const char* label_id, const cimgui::ImS16* xs, const cimgui::ImS16* ys, int count, double bar_size, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBars(label_id, reinterpret_cast<const ::ImS16*>(xs), reinterpret_cast<const ::ImS16*>(ys), count, bar_size, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBars_U16PtrU16Ptr(const char* label_id, const cimgui::ImU16* xs, const cimgui::ImU16* ys, int count, double bar_size, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBars(label_id, reinterpret_cast<const ::ImU16*>(xs), reinterpret_cast<const ::ImU16*>(ys), count, bar_size, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBars_S32PtrS32Ptr(const char* label_id, const cimgui::ImS32* xs, const cimgui::ImS32* ys, int count, double bar_size, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBars(label_id, reinterpret_cast<const ::ImS32*>(xs), reinterpret_cast<const ::ImS32*>(ys), count, bar_size, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBars_U32PtrU32Ptr(const char* label_id, const cimgui::ImU32* xs, const cimgui::ImU32* ys, int count, double bar_size, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBars(label_id, reinterpret_cast<const ::ImU32*>(xs), reinterpret_cast<const ::ImU32*>(ys), count, bar_size, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBars_S64PtrS64Ptr(const char* label_id, const cimgui::ImS64* xs, const cimgui::ImS64* ys, int count, double bar_size, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBars(label_id, reinterpret_cast<const ::ImS64*>(xs), reinterpret_cast<const ::ImS64*>(ys), count, bar_size, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBars_U64PtrU64Ptr(const char* label_id, const cimgui::ImU64* xs, const cimgui::ImU64* ys, int count, double bar_size, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBars(label_id, reinterpret_cast<const ::ImU64*>(xs), reinterpret_cast<const ::ImU64*>(ys), count, bar_size, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBarsG(const char* label_id, cimgui::ImPlotGetter getter, void* data, int count, double bar_size, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBarsG(label_id, reinterpret_cast<::ImPlotGetter>(getter), data, count, bar_size, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBubbles_FloatPtrFloatPtrInt(const char* label_id, const float* values, const float* szs, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBubbles(label_id, values, szs, count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBubbles_doublePtrdoublePtrInt(const char* label_id, const double* values, const double* szs, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBubbles(label_id, values, szs, count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBubbles_S16PtrS16PtrInt(const char* label_id, const cimgui::ImS16* values, const cimgui::ImS16* szs, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBubbles(label_id, reinterpret_cast<const ::ImS16*>(values), reinterpret_cast<const ::ImS16*>(szs), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBubbles_U16PtrU16PtrInt(const char* label_id, const cimgui::ImU16* values, const cimgui::ImU16* szs, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBubbles(label_id, reinterpret_cast<const ::ImU16*>(values), reinterpret_cast<const ::ImU16*>(szs), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBubbles_S32PtrS32PtrInt(const char* label_id, const cimgui::ImS32* values, const cimgui::ImS32* szs, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBubbles(label_id, reinterpret_cast<const ::ImS32*>(values), reinterpret_cast<const ::ImS32*>(szs), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBubbles_U32PtrU32PtrInt(const char* label_id, const cimgui::ImU32* values, const cimgui::ImU32* szs, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBubbles(label_id, reinterpret_cast<const ::ImU32*>(values), reinterpret_cast<const ::ImU32*>(szs), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBubbles_S64PtrS64PtrInt(const char* label_id, const cimgui::ImS64* values, const cimgui::ImS64* szs, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBubbles(label_id, reinterpret_cast<const ::ImS64*>(values), reinterpret_cast<const ::ImS64*>(szs), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBubbles_U64PtrU64PtrInt(const char* label_id, const cimgui::ImU64* values, const cimgui::ImU64* szs, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBubbles(label_id, reinterpret_cast<const ::ImU64*>(values), reinterpret_cast<const ::ImU64*>(szs), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBubbles_FloatPtrFloatPtrFloatPtr(const char* label_id, const float* xs, const float* ys, const float* szs, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBubbles(label_id, xs, ys, szs, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBubbles_doublePtrdoublePtrdoublePtr(const char* label_id, const double* xs, const double* ys, const double* szs, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBubbles(label_id, xs, ys, szs, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBubbles_S16PtrS16PtrS16Ptr(const char* label_id, const cimgui::ImS16* xs, const cimgui::ImS16* ys, const cimgui::ImS16* szs, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBubbles(label_id, reinterpret_cast<const ::ImS16*>(xs), reinterpret_cast<const ::ImS16*>(ys), reinterpret_cast<const ::ImS16*>(szs), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBubbles_U16PtrU16PtrU16Ptr(const char* label_id, const cimgui::ImU16* xs, const cimgui::ImU16* ys, const cimgui::ImU16* szs, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBubbles(label_id, reinterpret_cast<const ::ImU16*>(xs), reinterpret_cast<const ::ImU16*>(ys), reinterpret_cast<const ::ImU16*>(szs), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBubbles_S32PtrS32PtrS32Ptr(const char* label_id, const cimgui::ImS32* xs, const cimgui::ImS32* ys, const cimgui::ImS32* szs, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBubbles(label_id, reinterpret_cast<const ::ImS32*>(xs), reinterpret_cast<const ::ImS32*>(ys), reinterpret_cast<const ::ImS32*>(szs), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBubbles_U32PtrU32PtrU32Ptr(const char* label_id, const cimgui::ImU32* xs, const cimgui::ImU32* ys, const cimgui::ImU32* szs, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBubbles(label_id, reinterpret_cast<const ::ImU32*>(xs), reinterpret_cast<const ::ImU32*>(ys), reinterpret_cast<const ::ImU32*>(szs), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBubbles_S64PtrS64PtrS64Ptr(const char* label_id, const cimgui::ImS64* xs, const cimgui::ImS64* ys, const cimgui::ImS64* szs, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBubbles(label_id, reinterpret_cast<const ::ImS64*>(xs), reinterpret_cast<const ::ImS64*>(ys), reinterpret_cast<const ::ImS64*>(szs), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotBubbles_U64PtrU64PtrU64Ptr(const char* label_id, const cimgui::ImU64* xs, const cimgui::ImU64* ys, const cimgui::ImU64* szs, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotBubbles(label_id, reinterpret_cast<const ::ImU64*>(xs), reinterpret_cast<const ::ImU64*>(ys), reinterpret_cast<const ::ImU64*>(szs), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotDigital_FloatPtr(const char* label_id, const float* xs, const float* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotDigital(label_id, xs, ys, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotDigital_doublePtr(const char* label_id, const double* xs, const double* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotDigital(label_id, xs, ys, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotDigital_S16Ptr(const char* label_id, const cimgui::ImS16* xs, const cimgui::ImS16* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotDigital(label_id, reinterpret_cast<const ::ImS16*>(xs), reinterpret_cast<const ::ImS16*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotDigital_U16Ptr(const char* label_id, const cimgui::ImU16* xs, const cimgui::ImU16* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotDigital(label_id, reinterpret_cast<const ::ImU16*>(xs), reinterpret_cast<const ::ImU16*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotDigital_S32Ptr(const char* label_id, const cimgui::ImS32* xs, const cimgui::ImS32* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotDigital(label_id, reinterpret_cast<const ::ImS32*>(xs), reinterpret_cast<const ::ImS32*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotDigital_U32Ptr(const char* label_id, const cimgui::ImU32* xs, const cimgui::ImU32* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotDigital(label_id, reinterpret_cast<const ::ImU32*>(xs), reinterpret_cast<const ::ImU32*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotDigital_S64Ptr(const char* label_id, const cimgui::ImS64* xs, const cimgui::ImS64* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotDigital(label_id, reinterpret_cast<const ::ImS64*>(xs), reinterpret_cast<const ::ImS64*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotDigital_U64Ptr(const char* label_id, const cimgui::ImU64* xs, const cimgui::ImU64* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotDigital(label_id, reinterpret_cast<const ::ImU64*>(xs), reinterpret_cast<const ::ImU64*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotDigitalG(const char* label_id, cimgui::ImPlotGetter getter, void* data, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotDigitalG(label_id, reinterpret_cast<::ImPlotGetter>(getter), data, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotDummy(const char* label_id, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotDummy(label_id, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotErrorBars_FloatPtrFloatPtrFloatPtrInt(const char* label_id, const float* xs, const float* ys, const float* err, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotErrorBars(label_id, xs, ys, err, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotErrorBars_doublePtrdoublePtrdoublePtrInt(const char* label_id, const double* xs, const double* ys, const double* err, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotErrorBars(label_id, xs, ys, err, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotErrorBars_S16PtrS16PtrS16PtrInt(const char* label_id, const cimgui::ImS16* xs, const cimgui::ImS16* ys, const cimgui::ImS16* err, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotErrorBars(label_id, reinterpret_cast<const ::ImS16*>(xs), reinterpret_cast<const ::ImS16*>(ys), reinterpret_cast<const ::ImS16*>(err), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotErrorBars_U16PtrU16PtrU16PtrInt(const char* label_id, const cimgui::ImU16* xs, const cimgui::ImU16* ys, const cimgui::ImU16* err, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotErrorBars(label_id, reinterpret_cast<const ::ImU16*>(xs), reinterpret_cast<const ::ImU16*>(ys), reinterpret_cast<const ::ImU16*>(err), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotErrorBars_S32PtrS32PtrS32PtrInt(const char* label_id, const cimgui::ImS32* xs, const cimgui::ImS32* ys, const cimgui::ImS32* err, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotErrorBars(label_id, reinterpret_cast<const ::ImS32*>(xs), reinterpret_cast<const ::ImS32*>(ys), reinterpret_cast<const ::ImS32*>(err), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotErrorBars_U32PtrU32PtrU32PtrInt(const char* label_id, const cimgui::ImU32* xs, const cimgui::ImU32* ys, const cimgui::ImU32* err, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotErrorBars(label_id, reinterpret_cast<const ::ImU32*>(xs), reinterpret_cast<const ::ImU32*>(ys), reinterpret_cast<const ::ImU32*>(err), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotErrorBars_S64PtrS64PtrS64PtrInt(const char* label_id, const cimgui::ImS64* xs, const cimgui::ImS64* ys, const cimgui::ImS64* err, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotErrorBars(label_id, reinterpret_cast<const ::ImS64*>(xs), reinterpret_cast<const ::ImS64*>(ys), reinterpret_cast<const ::ImS64*>(err), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotErrorBars_U64PtrU64PtrU64PtrInt(const char* label_id, const cimgui::ImU64* xs, const cimgui::ImU64* ys, const cimgui::ImU64* err, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotErrorBars(label_id, reinterpret_cast<const ::ImU64*>(xs), reinterpret_cast<const ::ImU64*>(ys), reinterpret_cast<const ::ImU64*>(err), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotErrorBars_FloatPtrFloatPtrFloatPtrFloatPtr(const char* label_id, const float* xs, const float* ys, const float* neg, const float* pos, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotErrorBars(label_id, xs, ys, neg, pos, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotErrorBars_doublePtrdoublePtrdoublePtrdoublePtr(const char* label_id, const double* xs, const double* ys, const double* neg, const double* pos, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotErrorBars(label_id, xs, ys, neg, pos, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotErrorBars_S16PtrS16PtrS16PtrS16Ptr(const char* label_id, const cimgui::ImS16* xs, const cimgui::ImS16* ys, const cimgui::ImS16* neg, const cimgui::ImS16* pos, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotErrorBars(label_id, reinterpret_cast<const ::ImS16*>(xs), reinterpret_cast<const ::ImS16*>(ys), reinterpret_cast<const ::ImS16*>(neg), reinterpret_cast<const ::ImS16*>(pos), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotErrorBars_U16PtrU16PtrU16PtrU16Ptr(const char* label_id, const cimgui::ImU16* xs, const cimgui::ImU16* ys, const cimgui::ImU16* neg, const cimgui::ImU16* pos, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotErrorBars(label_id, reinterpret_cast<const ::ImU16*>(xs), reinterpret_cast<const ::ImU16*>(ys), reinterpret_cast<const ::ImU16*>(neg), reinterpret_cast<const ::ImU16*>(pos), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotErrorBars_S32PtrS32PtrS32PtrS32Ptr(const char* label_id, const cimgui::ImS32* xs, const cimgui::ImS32* ys, const cimgui::ImS32* neg, const cimgui::ImS32* pos, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotErrorBars(label_id, reinterpret_cast<const ::ImS32*>(xs), reinterpret_cast<const ::ImS32*>(ys), reinterpret_cast<const ::ImS32*>(neg), reinterpret_cast<const ::ImS32*>(pos), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotErrorBars_U32PtrU32PtrU32PtrU32Ptr(const char* label_id, const cimgui::ImU32* xs, const cimgui::ImU32* ys, const cimgui::ImU32* neg, const cimgui::ImU32* pos, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotErrorBars(label_id, reinterpret_cast<const ::ImU32*>(xs), reinterpret_cast<const ::ImU32*>(ys), reinterpret_cast<const ::ImU32*>(neg), reinterpret_cast<const ::ImU32*>(pos), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotErrorBars_S64PtrS64PtrS64PtrS64Ptr(const char* label_id, const cimgui::ImS64* xs, const cimgui::ImS64* ys, const cimgui::ImS64* neg, const cimgui::ImS64* pos, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotErrorBars(label_id, reinterpret_cast<const ::ImS64*>(xs), reinterpret_cast<const ::ImS64*>(ys), reinterpret_cast<const ::ImS64*>(neg), reinterpret_cast<const ::ImS64*>(pos), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotErrorBars_U64PtrU64PtrU64PtrU64Ptr(const char* label_id, const cimgui::ImU64* xs, const cimgui::ImU64* ys, const cimgui::ImU64* neg, const cimgui::ImU64* pos, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotErrorBars(label_id, reinterpret_cast<const ::ImU64*>(xs), reinterpret_cast<const ::ImU64*>(ys), reinterpret_cast<const ::ImU64*>(neg), reinterpret_cast<const ::ImU64*>(pos), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotHeatmap_FloatPtr(const char* label_id, const float* values, int rows, int cols, double scale_min, double scale_max, const char* label_fmt, const cimgui::ImPlotPoint bounds_min, const cimgui::ImPlotPoint bounds_max, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotHeatmap(label_id, values, rows, cols, scale_min, scale_max, label_fmt, ConvertToCPP_ImPlotPoint(bounds_min), ConvertToCPP_ImPlotPoint(bounds_max), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotHeatmap_doublePtr(const char* label_id, const double* values, int rows, int cols, double scale_min, double scale_max, const char* label_fmt, const cimgui::ImPlotPoint bounds_min, const cimgui::ImPlotPoint bounds_max, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotHeatmap(label_id, values, rows, cols, scale_min, scale_max, label_fmt, ConvertToCPP_ImPlotPoint(bounds_min), ConvertToCPP_ImPlotPoint(bounds_max), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotHeatmap_S16Ptr(const char* label_id, const cimgui::ImS16* values, int rows, int cols, double scale_min, double scale_max, const char* label_fmt, const cimgui::ImPlotPoint bounds_min, const cimgui::ImPlotPoint bounds_max, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotHeatmap(label_id, reinterpret_cast<const ::ImS16*>(values), rows, cols, scale_min, scale_max, label_fmt, ConvertToCPP_ImPlotPoint(bounds_min), ConvertToCPP_ImPlotPoint(bounds_max), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotHeatmap_U16Ptr(const char* label_id, const cimgui::ImU16* values, int rows, int cols, double scale_min, double scale_max, const char* label_fmt, const cimgui::ImPlotPoint bounds_min, const cimgui::ImPlotPoint bounds_max, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotHeatmap(label_id, reinterpret_cast<const ::ImU16*>(values), rows, cols, scale_min, scale_max, label_fmt, ConvertToCPP_ImPlotPoint(bounds_min), ConvertToCPP_ImPlotPoint(bounds_max), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotHeatmap_S32Ptr(const char* label_id, const cimgui::ImS32* values, int rows, int cols, double scale_min, double scale_max, const char* label_fmt, const cimgui::ImPlotPoint bounds_min, const cimgui::ImPlotPoint bounds_max, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotHeatmap(label_id, reinterpret_cast<const ::ImS32*>(values), rows, cols, scale_min, scale_max, label_fmt, ConvertToCPP_ImPlotPoint(bounds_min), ConvertToCPP_ImPlotPoint(bounds_max), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotHeatmap_U32Ptr(const char* label_id, const cimgui::ImU32* values, int rows, int cols, double scale_min, double scale_max, const char* label_fmt, const cimgui::ImPlotPoint bounds_min, const cimgui::ImPlotPoint bounds_max, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotHeatmap(label_id, reinterpret_cast<const ::ImU32*>(values), rows, cols, scale_min, scale_max, label_fmt, ConvertToCPP_ImPlotPoint(bounds_min), ConvertToCPP_ImPlotPoint(bounds_max), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotHeatmap_S64Ptr(const char* label_id, const cimgui::ImS64* values, int rows, int cols, double scale_min, double scale_max, const char* label_fmt, const cimgui::ImPlotPoint bounds_min, const cimgui::ImPlotPoint bounds_max, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotHeatmap(label_id, reinterpret_cast<const ::ImS64*>(values), rows, cols, scale_min, scale_max, label_fmt, ConvertToCPP_ImPlotPoint(bounds_min), ConvertToCPP_ImPlotPoint(bounds_max), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotHeatmap_U64Ptr(const char* label_id, const cimgui::ImU64* values, int rows, int cols, double scale_min, double scale_max, const char* label_fmt, const cimgui::ImPlotPoint bounds_min, const cimgui::ImPlotPoint bounds_max, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotHeatmap(label_id, reinterpret_cast<const ::ImU64*>(values), rows, cols, scale_min, scale_max, label_fmt, ConvertToCPP_ImPlotPoint(bounds_min), ConvertToCPP_ImPlotPoint(bounds_max), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double cimgui::ImPlot_PlotHistogram_FloatPtr(const char* label_id, const float* values, int count, int bins, double bar_scale, cimgui::ImPlotRange range, const cimgui::ImPlotSpec spec)
{
    return ImPlot::PlotHistogram(label_id, values, count, bins, bar_scale, ConvertToCPP_ImPlotRange(range), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double cimgui::ImPlot_PlotHistogram_doublePtr(const char* label_id, const double* values, int count, int bins, double bar_scale, cimgui::ImPlotRange range, const cimgui::ImPlotSpec spec)
{
    return ImPlot::PlotHistogram(label_id, values, count, bins, bar_scale, ConvertToCPP_ImPlotRange(range), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double cimgui::ImPlot_PlotHistogram_S16Ptr(const char* label_id, const cimgui::ImS16* values, int count, int bins, double bar_scale, cimgui::ImPlotRange range, const cimgui::ImPlotSpec spec)
{
    return ImPlot::PlotHistogram(label_id, reinterpret_cast<const ::ImS16*>(values), count, bins, bar_scale, ConvertToCPP_ImPlotRange(range), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double cimgui::ImPlot_PlotHistogram_U16Ptr(const char* label_id, const cimgui::ImU16* values, int count, int bins, double bar_scale, cimgui::ImPlotRange range, const cimgui::ImPlotSpec spec)
{
    return ImPlot::PlotHistogram(label_id, reinterpret_cast<const ::ImU16*>(values), count, bins, bar_scale, ConvertToCPP_ImPlotRange(range), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double cimgui::ImPlot_PlotHistogram_S32Ptr(const char* label_id, const cimgui::ImS32* values, int count, int bins, double bar_scale, cimgui::ImPlotRange range, const cimgui::ImPlotSpec spec)
{
    return ImPlot::PlotHistogram(label_id, reinterpret_cast<const ::ImS32*>(values), count, bins, bar_scale, ConvertToCPP_ImPlotRange(range), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double cimgui::ImPlot_PlotHistogram_U32Ptr(const char* label_id, const cimgui::ImU32* values, int count, int bins, double bar_scale, cimgui::ImPlotRange range, const cimgui::ImPlotSpec spec)
{
    return ImPlot::PlotHistogram(label_id, reinterpret_cast<const ::ImU32*>(values), count, bins, bar_scale, ConvertToCPP_ImPlotRange(range), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double cimgui::ImPlot_PlotHistogram_S64Ptr(const char* label_id, const cimgui::ImS64* values, int count, int bins, double bar_scale, cimgui::ImPlotRange range, const cimgui::ImPlotSpec spec)
{
    return ImPlot::PlotHistogram(label_id, reinterpret_cast<const ::ImS64*>(values), count, bins, bar_scale, ConvertToCPP_ImPlotRange(range), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double cimgui::ImPlot_PlotHistogram_U64Ptr(const char* label_id, const cimgui::ImU64* values, int count, int bins, double bar_scale, cimgui::ImPlotRange range, const cimgui::ImPlotSpec spec)
{
    return ImPlot::PlotHistogram(label_id, reinterpret_cast<const ::ImU64*>(values), count, bins, bar_scale, ConvertToCPP_ImPlotRange(range), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double cimgui::ImPlot_PlotHistogram2D_FloatPtr(const char* label_id, const float* xs, const float* ys, int count, int x_bins, int y_bins, cimgui::ImPlotRect range, const cimgui::ImPlotSpec spec)
{
    return ImPlot::PlotHistogram2D(label_id, xs, ys, count, x_bins, y_bins, ConvertToCPP_ImPlotRect(range), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double cimgui::ImPlot_PlotHistogram2D_doublePtr(const char* label_id, const double* xs, const double* ys, int count, int x_bins, int y_bins, cimgui::ImPlotRect range, const cimgui::ImPlotSpec spec)
{
    return ImPlot::PlotHistogram2D(label_id, xs, ys, count, x_bins, y_bins, ConvertToCPP_ImPlotRect(range), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double cimgui::ImPlot_PlotHistogram2D_S16Ptr(const char* label_id, const cimgui::ImS16* xs, const cimgui::ImS16* ys, int count, int x_bins, int y_bins, cimgui::ImPlotRect range, const cimgui::ImPlotSpec spec)
{
    return ImPlot::PlotHistogram2D(label_id, reinterpret_cast<const ::ImS16*>(xs), reinterpret_cast<const ::ImS16*>(ys), count, x_bins, y_bins, ConvertToCPP_ImPlotRect(range), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double cimgui::ImPlot_PlotHistogram2D_U16Ptr(const char* label_id, const cimgui::ImU16* xs, const cimgui::ImU16* ys, int count, int x_bins, int y_bins, cimgui::ImPlotRect range, const cimgui::ImPlotSpec spec)
{
    return ImPlot::PlotHistogram2D(label_id, reinterpret_cast<const ::ImU16*>(xs), reinterpret_cast<const ::ImU16*>(ys), count, x_bins, y_bins, ConvertToCPP_ImPlotRect(range), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double cimgui::ImPlot_PlotHistogram2D_S32Ptr(const char* label_id, const cimgui::ImS32* xs, const cimgui::ImS32* ys, int count, int x_bins, int y_bins, cimgui::ImPlotRect range, const cimgui::ImPlotSpec spec)
{
    return ImPlot::PlotHistogram2D(label_id, reinterpret_cast<const ::ImS32*>(xs), reinterpret_cast<const ::ImS32*>(ys), count, x_bins, y_bins, ConvertToCPP_ImPlotRect(range), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double cimgui::ImPlot_PlotHistogram2D_U32Ptr(const char* label_id, const cimgui::ImU32* xs, const cimgui::ImU32* ys, int count, int x_bins, int y_bins, cimgui::ImPlotRect range, const cimgui::ImPlotSpec spec)
{
    return ImPlot::PlotHistogram2D(label_id, reinterpret_cast<const ::ImU32*>(xs), reinterpret_cast<const ::ImU32*>(ys), count, x_bins, y_bins, ConvertToCPP_ImPlotRect(range), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double cimgui::ImPlot_PlotHistogram2D_S64Ptr(const char* label_id, const cimgui::ImS64* xs, const cimgui::ImS64* ys, int count, int x_bins, int y_bins, cimgui::ImPlotRect range, const cimgui::ImPlotSpec spec)
{
    return ImPlot::PlotHistogram2D(label_id, reinterpret_cast<const ::ImS64*>(xs), reinterpret_cast<const ::ImS64*>(ys), count, x_bins, y_bins, ConvertToCPP_ImPlotRect(range), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double cimgui::ImPlot_PlotHistogram2D_U64Ptr(const char* label_id, const cimgui::ImU64* xs, const cimgui::ImU64* ys, int count, int x_bins, int y_bins, cimgui::ImPlotRect range, const cimgui::ImPlotSpec spec)
{
    return ImPlot::PlotHistogram2D(label_id, reinterpret_cast<const ::ImU64*>(xs), reinterpret_cast<const ::ImU64*>(ys), count, x_bins, y_bins, ConvertToCPP_ImPlotRect(range), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotImage(const char* label_id, ImTextureRef tex_ref, const cimgui::ImPlotPoint bounds_min, const cimgui::ImPlotPoint bounds_max, const cimgui::ImVec2 uv0, const cimgui::ImVec2 uv1, const cimgui::ImVec4 tint_col, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotImage(label_id, ConvertToCPP_ImTextureRef(tex_ref), ConvertToCPP_ImPlotPoint(bounds_min), ConvertToCPP_ImPlotPoint(bounds_max), ConvertToCPP_ImVec2(uv0), ConvertToCPP_ImVec2(uv1), ConvertToCPP_ImVec4(tint_col), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotInfLines_FloatPtr(const char* label_id, const float* values, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotInfLines(label_id, values, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotInfLines_doublePtr(const char* label_id, const double* values, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotInfLines(label_id, values, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotInfLines_S16Ptr(const char* label_id, const cimgui::ImS16* values, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotInfLines(label_id, reinterpret_cast<const ::ImS16*>(values), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotInfLines_U16Ptr(const char* label_id, const cimgui::ImU16* values, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotInfLines(label_id, reinterpret_cast<const ::ImU16*>(values), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotInfLines_S32Ptr(const char* label_id, const cimgui::ImS32* values, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotInfLines(label_id, reinterpret_cast<const ::ImS32*>(values), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotInfLines_U32Ptr(const char* label_id, const cimgui::ImU32* values, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotInfLines(label_id, reinterpret_cast<const ::ImU32*>(values), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotInfLines_S64Ptr(const char* label_id, const cimgui::ImS64* values, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotInfLines(label_id, reinterpret_cast<const ::ImS64*>(values), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotInfLines_U64Ptr(const char* label_id, const cimgui::ImU64* values, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotInfLines(label_id, reinterpret_cast<const ::ImU64*>(values), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLine_FloatPtrInt(const char* label_id, const float* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLine(label_id, values, count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLine_doublePtrInt(const char* label_id, const double* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLine(label_id, values, count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLine_S16PtrInt(const char* label_id, const cimgui::ImS16* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLine(label_id, reinterpret_cast<const ::ImS16*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLine_U16PtrInt(const char* label_id, const cimgui::ImU16* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLine(label_id, reinterpret_cast<const ::ImU16*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLine_S32PtrInt(const char* label_id, const cimgui::ImS32* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLine(label_id, reinterpret_cast<const ::ImS32*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLine_U32PtrInt(const char* label_id, const cimgui::ImU32* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLine(label_id, reinterpret_cast<const ::ImU32*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLine_S64PtrInt(const char* label_id, const cimgui::ImS64* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLine(label_id, reinterpret_cast<const ::ImS64*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLine_U64PtrInt(const char* label_id, const cimgui::ImU64* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLine(label_id, reinterpret_cast<const ::ImU64*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLine_FloatPtrFloatPtr(const char* label_id, const float* xs, const float* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLine(label_id, xs, ys, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLine_doublePtrdoublePtr(const char* label_id, const double* xs, const double* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLine(label_id, xs, ys, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLine_S16PtrS16Ptr(const char* label_id, const cimgui::ImS16* xs, const cimgui::ImS16* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLine(label_id, reinterpret_cast<const ::ImS16*>(xs), reinterpret_cast<const ::ImS16*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLine_U16PtrU16Ptr(const char* label_id, const cimgui::ImU16* xs, const cimgui::ImU16* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLine(label_id, reinterpret_cast<const ::ImU16*>(xs), reinterpret_cast<const ::ImU16*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLine_S32PtrS32Ptr(const char* label_id, const cimgui::ImS32* xs, const cimgui::ImS32* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLine(label_id, reinterpret_cast<const ::ImS32*>(xs), reinterpret_cast<const ::ImS32*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLine_U32PtrU32Ptr(const char* label_id, const cimgui::ImU32* xs, const cimgui::ImU32* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLine(label_id, reinterpret_cast<const ::ImU32*>(xs), reinterpret_cast<const ::ImU32*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLine_S64PtrS64Ptr(const char* label_id, const cimgui::ImS64* xs, const cimgui::ImS64* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLine(label_id, reinterpret_cast<const ::ImS64*>(xs), reinterpret_cast<const ::ImS64*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLine_U64PtrU64Ptr(const char* label_id, const cimgui::ImU64* xs, const cimgui::ImU64* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLine(label_id, reinterpret_cast<const ::ImU64*>(xs), reinterpret_cast<const ::ImU64*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotLineG(const char* label_id, cimgui::ImPlotGetter getter, void* data, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotLineG(label_id, reinterpret_cast<::ImPlotGetter>(getter), data, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotPieChart_FloatPtrPlotFormatter(const char* const label_ids[], const float* values, int count, double x, double y, double radius, cimgui::ImPlotFormatter fmt, void* fmt_data, double angle0, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotPieChart(label_ids, values, count, x, y, radius, reinterpret_cast<::ImPlotFormatter>(fmt), fmt_data, angle0, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotPieChart_doublePtrPlotFormatter(const char* const label_ids[], const double* values, int count, double x, double y, double radius, cimgui::ImPlotFormatter fmt, void* fmt_data, double angle0, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotPieChart(label_ids, values, count, x, y, radius, reinterpret_cast<::ImPlotFormatter>(fmt), fmt_data, angle0, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotPieChart_S16PtrPlotFormatter(const char* const label_ids[], const cimgui::ImS16* values, int count, double x, double y, double radius, cimgui::ImPlotFormatter fmt, void* fmt_data, double angle0, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotPieChart(label_ids, reinterpret_cast<const ::ImS16*>(values), count, x, y, radius, reinterpret_cast<::ImPlotFormatter>(fmt), fmt_data, angle0, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotPieChart_U16PtrPlotFormatter(const char* const label_ids[], const cimgui::ImU16* values, int count, double x, double y, double radius, cimgui::ImPlotFormatter fmt, void* fmt_data, double angle0, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotPieChart(label_ids, reinterpret_cast<const ::ImU16*>(values), count, x, y, radius, reinterpret_cast<::ImPlotFormatter>(fmt), fmt_data, angle0, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotPieChart_S32PtrPlotFormatter(const char* const label_ids[], const cimgui::ImS32* values, int count, double x, double y, double radius, cimgui::ImPlotFormatter fmt, void* fmt_data, double angle0, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotPieChart(label_ids, reinterpret_cast<const ::ImS32*>(values), count, x, y, radius, reinterpret_cast<::ImPlotFormatter>(fmt), fmt_data, angle0, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotPieChart_U32PtrPlotFormatter(const char* const label_ids[], const cimgui::ImU32* values, int count, double x, double y, double radius, cimgui::ImPlotFormatter fmt, void* fmt_data, double angle0, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotPieChart(label_ids, reinterpret_cast<const ::ImU32*>(values), count, x, y, radius, reinterpret_cast<::ImPlotFormatter>(fmt), fmt_data, angle0, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotPieChart_S64PtrPlotFormatter(const char* const label_ids[], const cimgui::ImS64* values, int count, double x, double y, double radius, cimgui::ImPlotFormatter fmt, void* fmt_data, double angle0, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotPieChart(label_ids, reinterpret_cast<const ::ImS64*>(values), count, x, y, radius, reinterpret_cast<::ImPlotFormatter>(fmt), fmt_data, angle0, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotPieChart_U64PtrPlotFormatter(const char* const label_ids[], const cimgui::ImU64* values, int count, double x, double y, double radius, cimgui::ImPlotFormatter fmt, void* fmt_data, double angle0, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotPieChart(label_ids, reinterpret_cast<const ::ImU64*>(values), count, x, y, radius, reinterpret_cast<::ImPlotFormatter>(fmt), fmt_data, angle0, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotPieChart_FloatPtrStr(const char* const label_ids[], const float* values, int count, double x, double y, double radius, const char* label_fmt, double angle0, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotPieChart(label_ids, values, count, x, y, radius, label_fmt, angle0, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotPieChart_doublePtrStr(const char* const label_ids[], const double* values, int count, double x, double y, double radius, const char* label_fmt, double angle0, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotPieChart(label_ids, values, count, x, y, radius, label_fmt, angle0, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotPieChart_S16PtrStr(const char* const label_ids[], const cimgui::ImS16* values, int count, double x, double y, double radius, const char* label_fmt, double angle0, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotPieChart(label_ids, reinterpret_cast<const ::ImS16*>(values), count, x, y, radius, label_fmt, angle0, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotPieChart_U16PtrStr(const char* const label_ids[], const cimgui::ImU16* values, int count, double x, double y, double radius, const char* label_fmt, double angle0, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotPieChart(label_ids, reinterpret_cast<const ::ImU16*>(values), count, x, y, radius, label_fmt, angle0, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotPieChart_S32PtrStr(const char* const label_ids[], const cimgui::ImS32* values, int count, double x, double y, double radius, const char* label_fmt, double angle0, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotPieChart(label_ids, reinterpret_cast<const ::ImS32*>(values), count, x, y, radius, label_fmt, angle0, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotPieChart_U32PtrStr(const char* const label_ids[], const cimgui::ImU32* values, int count, double x, double y, double radius, const char* label_fmt, double angle0, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotPieChart(label_ids, reinterpret_cast<const ::ImU32*>(values), count, x, y, radius, label_fmt, angle0, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotPieChart_S64PtrStr(const char* const label_ids[], const cimgui::ImS64* values, int count, double x, double y, double radius, const char* label_fmt, double angle0, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotPieChart(label_ids, reinterpret_cast<const ::ImS64*>(values), count, x, y, radius, label_fmt, angle0, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotPieChart_U64PtrStr(const char* const label_ids[], const cimgui::ImU64* values, int count, double x, double y, double radius, const char* label_fmt, double angle0, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotPieChart(label_ids, reinterpret_cast<const ::ImU64*>(values), count, x, y, radius, label_fmt, angle0, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatter_FloatPtrInt(const char* label_id, const float* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatter(label_id, values, count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatter_doublePtrInt(const char* label_id, const double* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatter(label_id, values, count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatter_S16PtrInt(const char* label_id, const cimgui::ImS16* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatter(label_id, reinterpret_cast<const ::ImS16*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatter_U16PtrInt(const char* label_id, const cimgui::ImU16* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatter(label_id, reinterpret_cast<const ::ImU16*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatter_S32PtrInt(const char* label_id, const cimgui::ImS32* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatter(label_id, reinterpret_cast<const ::ImS32*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatter_U32PtrInt(const char* label_id, const cimgui::ImU32* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatter(label_id, reinterpret_cast<const ::ImU32*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatter_S64PtrInt(const char* label_id, const cimgui::ImS64* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatter(label_id, reinterpret_cast<const ::ImS64*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatter_U64PtrInt(const char* label_id, const cimgui::ImU64* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatter(label_id, reinterpret_cast<const ::ImU64*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatter_FloatPtrFloatPtr(const char* label_id, const float* xs, const float* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatter(label_id, xs, ys, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatter_doublePtrdoublePtr(const char* label_id, const double* xs, const double* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatter(label_id, xs, ys, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatter_S16PtrS16Ptr(const char* label_id, const cimgui::ImS16* xs, const cimgui::ImS16* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatter(label_id, reinterpret_cast<const ::ImS16*>(xs), reinterpret_cast<const ::ImS16*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatter_U16PtrU16Ptr(const char* label_id, const cimgui::ImU16* xs, const cimgui::ImU16* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatter(label_id, reinterpret_cast<const ::ImU16*>(xs), reinterpret_cast<const ::ImU16*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatter_S32PtrS32Ptr(const char* label_id, const cimgui::ImS32* xs, const cimgui::ImS32* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatter(label_id, reinterpret_cast<const ::ImS32*>(xs), reinterpret_cast<const ::ImS32*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatter_U32PtrU32Ptr(const char* label_id, const cimgui::ImU32* xs, const cimgui::ImU32* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatter(label_id, reinterpret_cast<const ::ImU32*>(xs), reinterpret_cast<const ::ImU32*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatter_S64PtrS64Ptr(const char* label_id, const cimgui::ImS64* xs, const cimgui::ImS64* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatter(label_id, reinterpret_cast<const ::ImS64*>(xs), reinterpret_cast<const ::ImS64*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatter_U64PtrU64Ptr(const char* label_id, const cimgui::ImU64* xs, const cimgui::ImU64* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatter(label_id, reinterpret_cast<const ::ImU64*>(xs), reinterpret_cast<const ::ImU64*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotScatterG(const char* label_id, cimgui::ImPlotGetter getter, void* data, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotScatterG(label_id, reinterpret_cast<::ImPlotGetter>(getter), data, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_FloatPtrInt(const char* label_id, const float* values, int count, double yref, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, values, count, yref, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_doublePtrInt(const char* label_id, const double* values, int count, double yref, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, values, count, yref, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_S16PtrInt(const char* label_id, const cimgui::ImS16* values, int count, double yref, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImS16*>(values), count, yref, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_U16PtrInt(const char* label_id, const cimgui::ImU16* values, int count, double yref, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImU16*>(values), count, yref, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_S32PtrInt(const char* label_id, const cimgui::ImS32* values, int count, double yref, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImS32*>(values), count, yref, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_U32PtrInt(const char* label_id, const cimgui::ImU32* values, int count, double yref, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImU32*>(values), count, yref, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_S64PtrInt(const char* label_id, const cimgui::ImS64* values, int count, double yref, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImS64*>(values), count, yref, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_U64PtrInt(const char* label_id, const cimgui::ImU64* values, int count, double yref, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImU64*>(values), count, yref, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_FloatPtrFloatPtrInt(const char* label_id, const float* xs, const float* ys, int count, double yref, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, xs, ys, count, yref, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_doublePtrdoublePtrInt(const char* label_id, const double* xs, const double* ys, int count, double yref, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, xs, ys, count, yref, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_S16PtrS16PtrInt(const char* label_id, const cimgui::ImS16* xs, const cimgui::ImS16* ys, int count, double yref, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImS16*>(xs), reinterpret_cast<const ::ImS16*>(ys), count, yref, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_U16PtrU16PtrInt(const char* label_id, const cimgui::ImU16* xs, const cimgui::ImU16* ys, int count, double yref, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImU16*>(xs), reinterpret_cast<const ::ImU16*>(ys), count, yref, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_S32PtrS32PtrInt(const char* label_id, const cimgui::ImS32* xs, const cimgui::ImS32* ys, int count, double yref, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImS32*>(xs), reinterpret_cast<const ::ImS32*>(ys), count, yref, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_U32PtrU32PtrInt(const char* label_id, const cimgui::ImU32* xs, const cimgui::ImU32* ys, int count, double yref, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImU32*>(xs), reinterpret_cast<const ::ImU32*>(ys), count, yref, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_S64PtrS64PtrInt(const char* label_id, const cimgui::ImS64* xs, const cimgui::ImS64* ys, int count, double yref, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImS64*>(xs), reinterpret_cast<const ::ImS64*>(ys), count, yref, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_U64PtrU64PtrInt(const char* label_id, const cimgui::ImU64* xs, const cimgui::ImU64* ys, int count, double yref, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImU64*>(xs), reinterpret_cast<const ::ImU64*>(ys), count, yref, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_FloatPtrFloatPtrFloatPtr(const char* label_id, const float* xs, const float* ys1, const float* ys2, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, xs, ys1, ys2, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_doublePtrdoublePtrdoublePtr(const char* label_id, const double* xs, const double* ys1, const double* ys2, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, xs, ys1, ys2, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_S16PtrS16PtrS16Ptr(const char* label_id, const cimgui::ImS16* xs, const cimgui::ImS16* ys1, const cimgui::ImS16* ys2, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImS16*>(xs), reinterpret_cast<const ::ImS16*>(ys1), reinterpret_cast<const ::ImS16*>(ys2), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_U16PtrU16PtrU16Ptr(const char* label_id, const cimgui::ImU16* xs, const cimgui::ImU16* ys1, const cimgui::ImU16* ys2, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImU16*>(xs), reinterpret_cast<const ::ImU16*>(ys1), reinterpret_cast<const ::ImU16*>(ys2), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_S32PtrS32PtrS32Ptr(const char* label_id, const cimgui::ImS32* xs, const cimgui::ImS32* ys1, const cimgui::ImS32* ys2, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImS32*>(xs), reinterpret_cast<const ::ImS32*>(ys1), reinterpret_cast<const ::ImS32*>(ys2), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_U32PtrU32PtrU32Ptr(const char* label_id, const cimgui::ImU32* xs, const cimgui::ImU32* ys1, const cimgui::ImU32* ys2, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImU32*>(xs), reinterpret_cast<const ::ImU32*>(ys1), reinterpret_cast<const ::ImU32*>(ys2), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_S64PtrS64PtrS64Ptr(const char* label_id, const cimgui::ImS64* xs, const cimgui::ImS64* ys1, const cimgui::ImS64* ys2, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImS64*>(xs), reinterpret_cast<const ::ImS64*>(ys1), reinterpret_cast<const ::ImS64*>(ys2), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShaded_U64PtrU64PtrU64Ptr(const char* label_id, const cimgui::ImU64* xs, const cimgui::ImU64* ys1, const cimgui::ImU64* ys2, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShaded(label_id, reinterpret_cast<const ::ImU64*>(xs), reinterpret_cast<const ::ImU64*>(ys1), reinterpret_cast<const ::ImU64*>(ys2), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotShadedG(const char* label_id, cimgui::ImPlotGetter getter1, void* data1, cimgui::ImPlotGetter getter2, void* data2, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotShadedG(label_id, reinterpret_cast<::ImPlotGetter>(getter1), data1, reinterpret_cast<::ImPlotGetter>(getter2), data2, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairs_FloatPtrInt(const char* label_id, const float* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairs(label_id, values, count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairs_doublePtrInt(const char* label_id, const double* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairs(label_id, values, count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairs_S16PtrInt(const char* label_id, const cimgui::ImS16* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairs(label_id, reinterpret_cast<const ::ImS16*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairs_U16PtrInt(const char* label_id, const cimgui::ImU16* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairs(label_id, reinterpret_cast<const ::ImU16*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairs_S32PtrInt(const char* label_id, const cimgui::ImS32* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairs(label_id, reinterpret_cast<const ::ImS32*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairs_U32PtrInt(const char* label_id, const cimgui::ImU32* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairs(label_id, reinterpret_cast<const ::ImU32*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairs_S64PtrInt(const char* label_id, const cimgui::ImS64* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairs(label_id, reinterpret_cast<const ::ImS64*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairs_U64PtrInt(const char* label_id, const cimgui::ImU64* values, int count, double xscale, double xstart, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairs(label_id, reinterpret_cast<const ::ImU64*>(values), count, xscale, xstart, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairs_FloatPtrFloatPtr(const char* label_id, const float* xs, const float* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairs(label_id, xs, ys, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairs_doublePtrdoublePtr(const char* label_id, const double* xs, const double* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairs(label_id, xs, ys, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairs_S16PtrS16Ptr(const char* label_id, const cimgui::ImS16* xs, const cimgui::ImS16* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairs(label_id, reinterpret_cast<const ::ImS16*>(xs), reinterpret_cast<const ::ImS16*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairs_U16PtrU16Ptr(const char* label_id, const cimgui::ImU16* xs, const cimgui::ImU16* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairs(label_id, reinterpret_cast<const ::ImU16*>(xs), reinterpret_cast<const ::ImU16*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairs_S32PtrS32Ptr(const char* label_id, const cimgui::ImS32* xs, const cimgui::ImS32* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairs(label_id, reinterpret_cast<const ::ImS32*>(xs), reinterpret_cast<const ::ImS32*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairs_U32PtrU32Ptr(const char* label_id, const cimgui::ImU32* xs, const cimgui::ImU32* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairs(label_id, reinterpret_cast<const ::ImU32*>(xs), reinterpret_cast<const ::ImU32*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairs_S64PtrS64Ptr(const char* label_id, const cimgui::ImS64* xs, const cimgui::ImS64* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairs(label_id, reinterpret_cast<const ::ImS64*>(xs), reinterpret_cast<const ::ImS64*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairs_U64PtrU64Ptr(const char* label_id, const cimgui::ImU64* xs, const cimgui::ImU64* ys, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairs(label_id, reinterpret_cast<const ::ImU64*>(xs), reinterpret_cast<const ::ImU64*>(ys), count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStairsG(const char* label_id, cimgui::ImPlotGetter getter, void* data, int count, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStairsG(label_id, reinterpret_cast<::ImPlotGetter>(getter), data, count, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStems_FloatPtrInt(const char* label_id, const float* values, int count, double ref, double scale, double start, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStems(label_id, values, count, ref, scale, start, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStems_doublePtrInt(const char* label_id, const double* values, int count, double ref, double scale, double start, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStems(label_id, values, count, ref, scale, start, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStems_S16PtrInt(const char* label_id, const cimgui::ImS16* values, int count, double ref, double scale, double start, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStems(label_id, reinterpret_cast<const ::ImS16*>(values), count, ref, scale, start, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStems_U16PtrInt(const char* label_id, const cimgui::ImU16* values, int count, double ref, double scale, double start, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStems(label_id, reinterpret_cast<const ::ImU16*>(values), count, ref, scale, start, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStems_S32PtrInt(const char* label_id, const cimgui::ImS32* values, int count, double ref, double scale, double start, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStems(label_id, reinterpret_cast<const ::ImS32*>(values), count, ref, scale, start, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStems_U32PtrInt(const char* label_id, const cimgui::ImU32* values, int count, double ref, double scale, double start, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStems(label_id, reinterpret_cast<const ::ImU32*>(values), count, ref, scale, start, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStems_S64PtrInt(const char* label_id, const cimgui::ImS64* values, int count, double ref, double scale, double start, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStems(label_id, reinterpret_cast<const ::ImS64*>(values), count, ref, scale, start, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStems_U64PtrInt(const char* label_id, const cimgui::ImU64* values, int count, double ref, double scale, double start, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStems(label_id, reinterpret_cast<const ::ImU64*>(values), count, ref, scale, start, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStems_FloatPtrFloatPtr(const char* label_id, const float* xs, const float* ys, int count, double ref, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStems(label_id, xs, ys, count, ref, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStems_doublePtrdoublePtr(const char* label_id, const double* xs, const double* ys, int count, double ref, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStems(label_id, xs, ys, count, ref, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStems_S16PtrS16Ptr(const char* label_id, const cimgui::ImS16* xs, const cimgui::ImS16* ys, int count, double ref, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStems(label_id, reinterpret_cast<const ::ImS16*>(xs), reinterpret_cast<const ::ImS16*>(ys), count, ref, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStems_U16PtrU16Ptr(const char* label_id, const cimgui::ImU16* xs, const cimgui::ImU16* ys, int count, double ref, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStems(label_id, reinterpret_cast<const ::ImU16*>(xs), reinterpret_cast<const ::ImU16*>(ys), count, ref, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStems_S32PtrS32Ptr(const char* label_id, const cimgui::ImS32* xs, const cimgui::ImS32* ys, int count, double ref, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStems(label_id, reinterpret_cast<const ::ImS32*>(xs), reinterpret_cast<const ::ImS32*>(ys), count, ref, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStems_U32PtrU32Ptr(const char* label_id, const cimgui::ImU32* xs, const cimgui::ImU32* ys, int count, double ref, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStems(label_id, reinterpret_cast<const ::ImU32*>(xs), reinterpret_cast<const ::ImU32*>(ys), count, ref, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStems_S64PtrS64Ptr(const char* label_id, const cimgui::ImS64* xs, const cimgui::ImS64* ys, int count, double ref, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStems(label_id, reinterpret_cast<const ::ImS64*>(xs), reinterpret_cast<const ::ImS64*>(ys), count, ref, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotStems_U64PtrU64Ptr(const char* label_id, const cimgui::ImU64* xs, const cimgui::ImU64* ys, int count, double ref, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotStems(label_id, reinterpret_cast<const ::ImU64*>(xs), reinterpret_cast<const ::ImU64*>(ys), count, ref, ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void cimgui::ImPlot_PlotText(const char* text, double x, double y, const cimgui::ImVec2 pix_offset, const cimgui::ImPlotSpec spec)
{
    ImPlot::PlotText(text, x, y, ConvertToCPP_ImVec2(pix_offset), ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API cimgui::ImVec2 cimgui::ImPlot_PlotToPixels_PlotPoint(const cimgui::ImPlotPoint plt, cimgui::ImAxis x_axis, cimgui::ImAxis y_axis)
{
    return ConvertFromCPP_ImVec2(ImPlot::PlotToPixels(ConvertToCPP_ImPlotPoint(plt), static_cast<::ImAxis>(x_axis), static_cast<::ImAxis>(y_axis)));
}
CIMGUI_API cimgui::ImVec2 cimgui::ImPlot_PlotToPixels_double(double x, double y, cimgui::ImAxis x_axis, cimgui::ImAxis y_axis)
{
    return ConvertFromCPP_ImVec2(ImPlot::PlotToPixels(x, y, static_cast<::ImAxis>(x_axis), static_cast<::ImAxis>(y_axis)));
}
CIMGUI_API void cimgui::ImPlot_PopColormap(int count)
{
    ImPlot::PopColormap(count);
}
CIMGUI_API void cimgui::ImPlot_PopPlotClipRect()
{
    ImPlot::PopPlotClipRect();
}
CIMGUI_API void cimgui::ImPlot_PopStyleColor(int count)
{
    ImPlot::PopStyleColor(count);
}
CIMGUI_API void cimgui::ImPlot_PopStyleVar(int count)
{
    ImPlot::PopStyleVar(count);
}
CIMGUI_API void cimgui::ImPlot_PushColormap_PlotColormap(cimgui::ImPlotColormap cmap)
{
    ImPlot::PushColormap(static_cast<::ImPlotColormap>(cmap));
}
CIMGUI_API void cimgui::ImPlot_PushColormap_Str(const char* name)
{
    ImPlot::PushColormap(name);
}
CIMGUI_API void cimgui::ImPlot_PushPlotClipRect(float expand)
{
    ImPlot::PushPlotClipRect(expand);
}
CIMGUI_API void cimgui::ImPlot_PushStyleColor_U32(cimgui::ImPlotCol idx, cimgui::ImU32 col)
{
    ImPlot::PushStyleColor(static_cast<::ImPlotCol>(idx), static_cast<::ImU32>(col));
}
CIMGUI_API void cimgui::ImPlot_PushStyleColor_Vec4(cimgui::ImPlotCol idx, const cimgui::ImVec4 col)
{
    ImPlot::PushStyleColor(static_cast<::ImPlotCol>(idx), ConvertToCPP_ImVec4(col));
}
CIMGUI_API void cimgui::ImPlot_PushStyleVar_Float(cimgui::ImPlotStyleVar idx, float val)
{
    ImPlot::PushStyleVar(static_cast<::ImPlotStyleVar>(idx), val);
}
CIMGUI_API void cimgui::ImPlot_PushStyleVar_Int(cimgui::ImPlotStyleVar idx, int val)
{
    ImPlot::PushStyleVar(static_cast<::ImPlotStyleVar>(idx), val);
}
CIMGUI_API void cimgui::ImPlot_PushStyleVar_Vec2(cimgui::ImPlotStyleVar idx, const cimgui::ImVec2 val)
{
    ImPlot::PushStyleVar(static_cast<::ImPlotStyleVar>(idx), ConvertToCPP_ImVec2(val));
}
CIMGUI_API cimgui::ImVec4 cimgui::ImPlot_SampleColormap(float t, cimgui::ImPlotColormap cmap)
{
    return ConvertFromCPP_ImVec4(ImPlot::SampleColormap(t, static_cast<::ImPlotColormap>(cmap)));
}
CIMGUI_API void cimgui::ImPlot_SetAxes(cimgui::ImAxis x_axis, cimgui::ImAxis y_axis)
{
    ImPlot::SetAxes(static_cast<::ImAxis>(x_axis), static_cast<::ImAxis>(y_axis));
}
CIMGUI_API void cimgui::ImPlot_SetAxis(cimgui::ImAxis axis)
{
    ImPlot::SetAxis(static_cast<::ImAxis>(axis));
}
CIMGUI_API void cimgui::ImPlot_SetCurrentContext(ImPlotContext* ctx)
{
    ImPlot::SetCurrentContext(ctx);
}
CIMGUI_API void cimgui::ImPlot_SetImGuiContext(cimgui::ImGuiContext* ctx)
{
    ImPlot::SetImGuiContext(reinterpret_cast<::ImGuiContext*>(ctx));
}
CIMGUI_API void cimgui::ImPlot_SetNextAxesLimits(double x_min, double x_max, double y_min, double y_max, cimgui::ImPlotCond cond)
{
    ImPlot::SetNextAxesLimits(x_min, x_max, y_min, y_max, static_cast<::ImPlotCond>(cond));
}
CIMGUI_API void cimgui::ImPlot_SetNextAxesToFit()
{
    ImPlot::SetNextAxesToFit();
}
CIMGUI_API void cimgui::ImPlot_SetNextAxisLimits(cimgui::ImAxis axis, double v_min, double v_max, cimgui::ImPlotCond cond)
{
    ImPlot::SetNextAxisLimits(static_cast<::ImAxis>(axis), v_min, v_max, static_cast<::ImPlotCond>(cond));
}
CIMGUI_API void cimgui::ImPlot_SetNextAxisLinks(cimgui::ImAxis axis, double* link_min, double* link_max)
{
    ImPlot::SetNextAxisLinks(static_cast<::ImAxis>(axis), link_min, link_max);
}
CIMGUI_API void cimgui::ImPlot_SetNextAxisToFit(cimgui::ImAxis axis)
{
    ImPlot::SetNextAxisToFit(static_cast<::ImAxis>(axis));
}
CIMGUI_API void cimgui::ImPlot_SetupAxes(const char* x_label, const char* y_label, cimgui::ImPlotAxisFlags x_flags, cimgui::ImPlotAxisFlags y_flags)
{
    ImPlot::SetupAxes(x_label, y_label, static_cast<::ImPlotAxisFlags>(x_flags), static_cast<::ImPlotAxisFlags>(y_flags));
}
CIMGUI_API void cimgui::ImPlot_SetupAxesLimits(double x_min, double x_max, double y_min, double y_max, cimgui::ImPlotCond cond)
{
    ImPlot::SetupAxesLimits(x_min, x_max, y_min, y_max, static_cast<::ImPlotCond>(cond));
}
CIMGUI_API void cimgui::ImPlot_SetupAxis(cimgui::ImAxis axis, const char* label, cimgui::ImPlotAxisFlags flags)
{
    ImPlot::SetupAxis(static_cast<::ImAxis>(axis), label, static_cast<::ImPlotAxisFlags>(flags));
}
CIMGUI_API void cimgui::ImPlot_SetupAxisFormat_Str(cimgui::ImAxis axis, const char* fmt)
{
    ImPlot::SetupAxisFormat(static_cast<::ImAxis>(axis), fmt);
}
CIMGUI_API void cimgui::ImPlot_SetupAxisFormat_PlotFormatter(cimgui::ImAxis axis, cimgui::ImPlotFormatter formatter, void* data)
{
    ImPlot::SetupAxisFormat(static_cast<::ImAxis>(axis), reinterpret_cast<::ImPlotFormatter>(formatter), data);
}
CIMGUI_API void cimgui::ImPlot_SetupAxisLimits(cimgui::ImAxis axis, double v_min, double v_max, cimgui::ImPlotCond cond)
{
    ImPlot::SetupAxisLimits(static_cast<::ImAxis>(axis), v_min, v_max, static_cast<::ImPlotCond>(cond));
}
CIMGUI_API void cimgui::ImPlot_SetupAxisLimitsConstraints(cimgui::ImAxis axis, double v_min, double v_max)
{
    ImPlot::SetupAxisLimitsConstraints(static_cast<::ImAxis>(axis), v_min, v_max);
}
CIMGUI_API void cimgui::ImPlot_SetupAxisLinks(cimgui::ImAxis axis, double* link_min, double* link_max)
{
    ImPlot::SetupAxisLinks(static_cast<::ImAxis>(axis), link_min, link_max);
}
CIMGUI_API void cimgui::ImPlot_SetupAxisScale_PlotScale(cimgui::ImAxis axis, cimgui::ImPlotScale scale)
{
    ImPlot::SetupAxisScale(static_cast<::ImAxis>(axis), static_cast<::ImPlotScale>(scale));
}
CIMGUI_API void cimgui::ImPlot_SetupAxisScale_PlotTransform(cimgui::ImAxis axis, cimgui::ImPlotTransform forward, cimgui::ImPlotTransform inverse, void* data)
{
    ImPlot::SetupAxisScale(static_cast<::ImAxis>(axis), reinterpret_cast<::ImPlotTransform>(forward), reinterpret_cast<::ImPlotTransform>(inverse), data);
}
CIMGUI_API void cimgui::ImPlot_SetupAxisTicks_doublePtr(cimgui::ImAxis axis, const double* values, int n_ticks, const char* const labels[], bool keep_default)
{
    ImPlot::SetupAxisTicks(static_cast<::ImAxis>(axis), values, n_ticks, labels, keep_default);
}
CIMGUI_API void cimgui::ImPlot_SetupAxisTicks_double(cimgui::ImAxis axis, double v_min, double v_max, int n_ticks, const char* const labels[], bool keep_default)
{
    ImPlot::SetupAxisTicks(static_cast<::ImAxis>(axis), v_min, v_max, n_ticks, labels, keep_default);
}
CIMGUI_API void cimgui::ImPlot_SetupAxisZoomConstraints(cimgui::ImAxis axis, double z_min, double z_max)
{
    ImPlot::SetupAxisZoomConstraints(static_cast<::ImAxis>(axis), z_min, z_max);
}
CIMGUI_API void cimgui::ImPlot_SetupFinish()
{
    ImPlot::SetupFinish();
}
CIMGUI_API void cimgui::ImPlot_SetupLegend(cimgui::ImPlotLocation location, cimgui::ImPlotLegendFlags flags)
{
    ImPlot::SetupLegend(static_cast<::ImPlotLocation>(location), static_cast<::ImPlotLegendFlags>(flags));
}
CIMGUI_API void cimgui::ImPlot_SetupMouseText(cimgui::ImPlotLocation location, cimgui::ImPlotMouseTextFlags flags)
{
    ImPlot::SetupMouseText(static_cast<::ImPlotLocation>(location), static_cast<::ImPlotMouseTextFlags>(flags));
}
CIMGUI_API bool cimgui::ImPlot_ShowColormapSelector(const char* label)
{
    return ImPlot::ShowColormapSelector(label);
}
CIMGUI_API void cimgui::ImPlot_ShowDemoWindow(bool* p_open)
{
    ImPlot::ShowDemoWindow(p_open);
}
CIMGUI_API bool cimgui::ImPlot_ShowInputMapSelector(const char* label)
{
    return ImPlot::ShowInputMapSelector(label);
}
CIMGUI_API void cimgui::ImPlot_ShowMetricsWindow(bool* p_popen)
{
    ImPlot::ShowMetricsWindow(p_popen);
}
CIMGUI_API void cimgui::ImPlot_ShowStyleEditor(cimgui::ImPlotStyle* ref)
{
    ImPlot::ShowStyleEditor(reinterpret_cast<::ImPlotStyle*>(ref));
}
CIMGUI_API bool cimgui::ImPlot_ShowStyleSelector(const char* label)
{
    return ImPlot::ShowStyleSelector(label);
}
CIMGUI_API void cimgui::ImPlot_ShowUserGuide()
{
    ImPlot::ShowUserGuide();
}
CIMGUI_API void cimgui::ImPlot_StyleColorsAuto(cimgui::ImPlotStyle* dst)
{
    ImPlot::StyleColorsAuto(reinterpret_cast<::ImPlotStyle*>(dst));
}
CIMGUI_API void cimgui::ImPlot_StyleColorsClassic(cimgui::ImPlotStyle* dst)
{
    ImPlot::StyleColorsClassic(reinterpret_cast<::ImPlotStyle*>(dst));
}
CIMGUI_API void cimgui::ImPlot_StyleColorsDark(cimgui::ImPlotStyle* dst)
{
    ImPlot::StyleColorsDark(reinterpret_cast<::ImPlotStyle*>(dst));
}
CIMGUI_API void cimgui::ImPlot_StyleColorsLight(cimgui::ImPlotStyle* dst)
{
    ImPlot::StyleColorsLight(reinterpret_cast<::ImPlotStyle*>(dst));
}
CIMGUI_API void cimgui::ImPlot_TagX_Bool(double x, const cimgui::ImVec4 col, bool round)
{
    ImPlot::TagX(x, ConvertToCPP_ImVec4(col), round);
}
CIMGUI_API void cimgui::ImPlot_TagY_Bool(double y, const cimgui::ImVec4 col, bool round)
{
    ImPlot::TagY(y, ConvertToCPP_ImVec4(col), round);
}
