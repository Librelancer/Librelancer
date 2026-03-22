//auto-generated
#define IMPLOT_DISABLE_OBSOLETE_FUNCTIONS
#include "imgui.h"
#include "cimplot.h"
#include "implot.h"
#define ConvertToCPP_ImVec4(x) (x)
#define ConvertToCPP_ImVec2(x) (x)
#define ConvertToCPP_ImPlotSpec(x) (*x)
#define ConvertToCPP_ImPlotPoint(x) (x)
#define ConvertToCPP_ImPlotRect(x) (x)
#define ConvertToCPP_ImPlotRange(x) (x)
#define ConvertToCPP_ImTextureRef(x) (x)

CIMGUI_API ImPlotColormap ImPlot_AddColormap_Vec4Ptr(const char* name,const ImVec4* cols,int size,bool qual)
{
    return ImPlot::AddColormap(name,cols,size,qual);
}
CIMGUI_API ImPlotColormap ImPlot_AddColormap_U32Ptr(const char* name,const ImU32* cols,int size,bool qual)
{
    return ImPlot::AddColormap(name,cols,size,qual);
}
CIMGUI_API void ImPlot_Annotation_Bool(double x,double y,ImVec4 col,ImVec2 pix_offset,bool clamp,bool round)
{
    ImPlot::Annotation(x,y,ConvertToCPP_ImVec4(col),ConvertToCPP_ImVec2(pix_offset),clamp,round);
}
CIMGUI_API bool ImPlot_BeginAlignedPlots(const char* group_id,bool vertical)
{
    return ImPlot::BeginAlignedPlots(group_id,vertical);
}
CIMGUI_API bool ImPlot_BeginDragDropSourceAxis(ImAxis axis,ImGuiDragDropFlags flags)
{
    return ImPlot::BeginDragDropSourceAxis(axis,flags);
}
CIMGUI_API bool ImPlot_BeginDragDropSourceItem(const char* label_id,ImGuiDragDropFlags flags)
{
    return ImPlot::BeginDragDropSourceItem(label_id,flags);
}
CIMGUI_API bool ImPlot_BeginDragDropSourcePlot(ImGuiDragDropFlags flags)
{
    return ImPlot::BeginDragDropSourcePlot(flags);
}
CIMGUI_API bool ImPlot_BeginDragDropTargetAxis(ImAxis axis)
{
    return ImPlot::BeginDragDropTargetAxis(axis);
}
CIMGUI_API bool ImPlot_BeginDragDropTargetLegend()
{
    return ImPlot::BeginDragDropTargetLegend();
}
CIMGUI_API bool ImPlot_BeginDragDropTargetPlot()
{
    return ImPlot::BeginDragDropTargetPlot();
}
CIMGUI_API bool ImPlot_BeginLegendPopup(const char* label_id,ImGuiMouseButton mouse_button)
{
    return ImPlot::BeginLegendPopup(label_id,mouse_button);
}
CIMGUI_API bool ImPlot_BeginPlot(const char* title_id,ImVec2 size,ImPlotFlags flags)
{
    return ImPlot::BeginPlot(title_id,ConvertToCPP_ImVec2(size),flags);
}
CIMGUI_API bool ImPlot_BeginSubplots(const char* title_id,int rows,int cols,ImVec2 size,ImPlotSubplotFlags flags,float* row_ratios,float* col_ratios)
{
    return ImPlot::BeginSubplots(title_id,rows,cols,ConvertToCPP_ImVec2(size),flags,row_ratios,col_ratios);
}
CIMGUI_API void ImPlot_BustColorCache(const char* plot_title_id)
{
    ImPlot::BustColorCache(plot_title_id);
}
CIMGUI_API void ImPlot_CancelPlotSelection()
{
    ImPlot::CancelPlotSelection();
}
CIMGUI_API bool ImPlot_ColormapButton(const char* label,ImVec2 size,ImPlotColormap cmap)
{
    return ImPlot::ColormapButton(label,ConvertToCPP_ImVec2(size),cmap);
}
CIMGUI_API void ImPlot_ColormapIcon(ImPlotColormap cmap)
{
    ImPlot::ColormapIcon(cmap);
}
CIMGUI_API void ImPlot_ColormapScale(const char* label,double scale_min,double scale_max,ImVec2 size,const char* format,ImPlotColormapScaleFlags flags,ImPlotColormap cmap)
{
    ImPlot::ColormapScale(label,scale_min,scale_max,ConvertToCPP_ImVec2(size),format,flags,cmap);
}
CIMGUI_API bool ImPlot_ColormapSlider(const char* label,float* t,ImVec4* out,const char* format,ImPlotColormap cmap)
{
    return ImPlot::ColormapSlider(label,t,out,format,cmap);
}
CIMGUI_API ImPlotContext* ImPlot_CreateContext()
{
    return ImPlot::CreateContext();
}
CIMGUI_API void ImPlot_DestroyContext(ImPlotContext* ctx)
{
    ImPlot::DestroyContext(ctx);
}
CIMGUI_API bool ImPlot_DragLineX(int id,double* x,ImVec4 col,float thickness,ImPlotDragToolFlags flags,bool* out_clicked,bool* out_hovered,bool* out_held)
{
    return ImPlot::DragLineX(id,x,ConvertToCPP_ImVec4(col),thickness,flags,out_clicked,out_hovered,out_held);
}
CIMGUI_API bool ImPlot_DragLineY(int id,double* y,ImVec4 col,float thickness,ImPlotDragToolFlags flags,bool* out_clicked,bool* out_hovered,bool* out_held)
{
    return ImPlot::DragLineY(id,y,ConvertToCPP_ImVec4(col),thickness,flags,out_clicked,out_hovered,out_held);
}
CIMGUI_API bool ImPlot_DragPoint(int id,double* x,double* y,ImVec4 col,float size,ImPlotDragToolFlags flags,bool* out_clicked,bool* out_hovered,bool* out_held)
{
    return ImPlot::DragPoint(id,x,y,ConvertToCPP_ImVec4(col),size,flags,out_clicked,out_hovered,out_held);
}
CIMGUI_API bool ImPlot_DragRect(int id,double* x1,double* y1,double* x2,double* y2,ImVec4 col,ImPlotDragToolFlags flags,bool* out_clicked,bool* out_hovered,bool* out_held)
{
    return ImPlot::DragRect(id,x1,y1,x2,y2,ConvertToCPP_ImVec4(col),flags,out_clicked,out_hovered,out_held);
}
CIMGUI_API void ImPlot_EndAlignedPlots()
{
    ImPlot::EndAlignedPlots();
}
CIMGUI_API void ImPlot_EndDragDropSource()
{
    ImPlot::EndDragDropSource();
}
CIMGUI_API void ImPlot_EndDragDropTarget()
{
    ImPlot::EndDragDropTarget();
}
CIMGUI_API void ImPlot_EndLegendPopup()
{
    ImPlot::EndLegendPopup();
}
CIMGUI_API void ImPlot_EndPlot()
{
    ImPlot::EndPlot();
}
CIMGUI_API void ImPlot_EndSubplots()
{
    ImPlot::EndSubplots();
}
CIMGUI_API ImVec4 ImPlot_GetColormapColor(int idx,ImPlotColormap cmap)
{
    return ImPlot::GetColormapColor(idx,cmap);
}
CIMGUI_API int ImPlot_GetColormapCount()
{
    return ImPlot::GetColormapCount();
}
CIMGUI_API ImPlotColormap ImPlot_GetColormapIndex(const char* name)
{
    return ImPlot::GetColormapIndex(name);
}
CIMGUI_API const char* ImPlot_GetColormapName(ImPlotColormap cmap)
{
    return ImPlot::GetColormapName(cmap);
}
CIMGUI_API int ImPlot_GetColormapSize(ImPlotColormap cmap)
{
    return ImPlot::GetColormapSize(cmap);
}
CIMGUI_API ImPlotContext* ImPlot_GetCurrentContext()
{
    return ImPlot::GetCurrentContext();
}
CIMGUI_API ImPlotInputMap* ImPlot_GetInputMap()
{
    return reinterpret_cast<ImPlotInputMap*>(&
ImPlot::GetInputMap());
}
CIMGUI_API ImVec4 ImPlot_GetLastItemColor()
{
    return ImPlot::GetLastItemColor();
}
CIMGUI_API const char* ImPlot_GetMarkerName(ImPlotMarker idx)
{
    return ImPlot::GetMarkerName(idx);
}
CIMGUI_API ImDrawList* ImPlot_GetPlotDrawList()
{
    return ImPlot::GetPlotDrawList();
}
CIMGUI_API ImPlotRect ImPlot_GetPlotLimits(ImAxis x_axis,ImAxis y_axis)
{
    return ImPlot::GetPlotLimits(x_axis,y_axis);
}
CIMGUI_API ImPlotPoint ImPlot_GetPlotMousePos(ImAxis x_axis,ImAxis y_axis)
{
    return ImPlot::GetPlotMousePos(x_axis,y_axis);
}
CIMGUI_API ImVec2 ImPlot_GetPlotPos()
{
    return ImPlot::GetPlotPos();
}
CIMGUI_API ImPlotRect ImPlot_GetPlotSelection(ImAxis x_axis,ImAxis y_axis)
{
    return ImPlot::GetPlotSelection(x_axis,y_axis);
}
CIMGUI_API ImVec2 ImPlot_GetPlotSize()
{
    return ImPlot::GetPlotSize();
}
CIMGUI_API ImPlotStyle* ImPlot_GetStyle()
{
    return reinterpret_cast<ImPlotStyle*>(&
ImPlot::GetStyle());
}
CIMGUI_API const char* ImPlot_GetStyleColorName(ImPlotCol idx)
{
    return ImPlot::GetStyleColorName(idx);
}
CIMGUI_API void ImPlot_HideNextItem(bool hidden,ImPlotCond cond)
{
    ImPlot::HideNextItem(hidden,cond);
}
CIMGUI_API bool ImPlot_IsAxisHovered(ImAxis axis)
{
    return ImPlot::IsAxisHovered(axis);
}
CIMGUI_API bool ImPlot_IsLegendEntryHovered(const char* label_id)
{
    return ImPlot::IsLegendEntryHovered(label_id);
}
CIMGUI_API bool ImPlot_IsPlotHovered()
{
    return ImPlot::IsPlotHovered();
}
CIMGUI_API bool ImPlot_IsPlotSelected()
{
    return ImPlot::IsPlotSelected();
}
CIMGUI_API bool ImPlot_IsSubplotsHovered()
{
    return ImPlot::IsSubplotsHovered();
}
CIMGUI_API void ImPlot_ItemIcon_Vec4(ImVec4 col)
{
    ImPlot::ItemIcon(ConvertToCPP_ImVec4(col));
}
CIMGUI_API void ImPlot_ItemIcon_U32(ImU32 col)
{
    ImPlot::ItemIcon(col);
}
CIMGUI_API void ImPlot_MapInputDefault(ImPlotInputMap* dst)
{
    ImPlot::MapInputDefault(dst);
}
CIMGUI_API void ImPlot_MapInputReverse(ImPlotInputMap* dst)
{
    ImPlot::MapInputReverse(dst);
}
CIMGUI_API ImVec4 ImPlot_NextColormapColor()
{
    return ImPlot::NextColormapColor();
}
CIMGUI_API ImPlotMarker ImPlot_NextMarker()
{
    return ImPlot::NextMarker();
}
CIMGUI_API ImPlotPoint ImPlot_PixelsToPlot_Vec2(ImVec2 pix,ImAxis x_axis,ImAxis y_axis)
{
    return ImPlot::PixelsToPlot(ConvertToCPP_ImVec2(pix),x_axis,y_axis);
}
CIMGUI_API ImPlotPoint ImPlot_PixelsToPlot_Float(float x,float y,ImAxis x_axis,ImAxis y_axis)
{
    return ImPlot::PixelsToPlot(x,y,x_axis,y_axis);
}
CIMGUI_API void ImPlot_PlotBarGroups_FloatPtr(const char* const label_ids[],const float* values,int item_count,int group_count,double group_size,double shift,const ImPlotSpec* spec)
{
    ImPlot::PlotBarGroups(label_ids,values,item_count,group_count,group_size,shift,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBarGroups_doublePtr(const char* const label_ids[],const double* values,int item_count,int group_count,double group_size,double shift,const ImPlotSpec* spec)
{
    ImPlot::PlotBarGroups(label_ids,values,item_count,group_count,group_size,shift,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBarGroups_S16Ptr(const char* const label_ids[],const ImS16* values,int item_count,int group_count,double group_size,double shift,const ImPlotSpec* spec)
{
    ImPlot::PlotBarGroups(label_ids,values,item_count,group_count,group_size,shift,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBarGroups_U16Ptr(const char* const label_ids[],const ImU16* values,int item_count,int group_count,double group_size,double shift,const ImPlotSpec* spec)
{
    ImPlot::PlotBarGroups(label_ids,values,item_count,group_count,group_size,shift,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBarGroups_S32Ptr(const char* const label_ids[],const ImS32* values,int item_count,int group_count,double group_size,double shift,const ImPlotSpec* spec)
{
    ImPlot::PlotBarGroups(label_ids,values,item_count,group_count,group_size,shift,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBarGroups_U32Ptr(const char* const label_ids[],const ImU32* values,int item_count,int group_count,double group_size,double shift,const ImPlotSpec* spec)
{
    ImPlot::PlotBarGroups(label_ids,values,item_count,group_count,group_size,shift,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBarGroups_S64Ptr(const char* const label_ids[],const ImS64* values,int item_count,int group_count,double group_size,double shift,const ImPlotSpec* spec)
{
    ImPlot::PlotBarGroups(label_ids,values,item_count,group_count,group_size,shift,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBarGroups_U64Ptr(const char* const label_ids[],const ImU64* values,int item_count,int group_count,double group_size,double shift,const ImPlotSpec* spec)
{
    ImPlot::PlotBarGroups(label_ids,values,item_count,group_count,group_size,shift,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBars_FloatPtrInt(const char* label_id,const float* values,int count,double bar_size,double shift,const ImPlotSpec* spec)
{
    ImPlot::PlotBars(label_id,values,count,bar_size,shift,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBars_doublePtrInt(const char* label_id,const double* values,int count,double bar_size,double shift,const ImPlotSpec* spec)
{
    ImPlot::PlotBars(label_id,values,count,bar_size,shift,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBars_S16PtrInt(const char* label_id,const ImS16* values,int count,double bar_size,double shift,const ImPlotSpec* spec)
{
    ImPlot::PlotBars(label_id,values,count,bar_size,shift,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBars_U16PtrInt(const char* label_id,const ImU16* values,int count,double bar_size,double shift,const ImPlotSpec* spec)
{
    ImPlot::PlotBars(label_id,values,count,bar_size,shift,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBars_S32PtrInt(const char* label_id,const ImS32* values,int count,double bar_size,double shift,const ImPlotSpec* spec)
{
    ImPlot::PlotBars(label_id,values,count,bar_size,shift,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBars_U32PtrInt(const char* label_id,const ImU32* values,int count,double bar_size,double shift,const ImPlotSpec* spec)
{
    ImPlot::PlotBars(label_id,values,count,bar_size,shift,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBars_S64PtrInt(const char* label_id,const ImS64* values,int count,double bar_size,double shift,const ImPlotSpec* spec)
{
    ImPlot::PlotBars(label_id,values,count,bar_size,shift,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBars_U64PtrInt(const char* label_id,const ImU64* values,int count,double bar_size,double shift,const ImPlotSpec* spec)
{
    ImPlot::PlotBars(label_id,values,count,bar_size,shift,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBars_FloatPtrFloatPtr(const char* label_id,const float* xs,const float* ys,int count,double bar_size,const ImPlotSpec* spec)
{
    ImPlot::PlotBars(label_id,xs,ys,count,bar_size,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBars_doublePtrdoublePtr(const char* label_id,const double* xs,const double* ys,int count,double bar_size,const ImPlotSpec* spec)
{
    ImPlot::PlotBars(label_id,xs,ys,count,bar_size,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBars_S16PtrS16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,int count,double bar_size,const ImPlotSpec* spec)
{
    ImPlot::PlotBars(label_id,xs,ys,count,bar_size,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBars_U16PtrU16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,int count,double bar_size,const ImPlotSpec* spec)
{
    ImPlot::PlotBars(label_id,xs,ys,count,bar_size,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBars_S32PtrS32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,int count,double bar_size,const ImPlotSpec* spec)
{
    ImPlot::PlotBars(label_id,xs,ys,count,bar_size,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBars_U32PtrU32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,int count,double bar_size,const ImPlotSpec* spec)
{
    ImPlot::PlotBars(label_id,xs,ys,count,bar_size,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBars_S64PtrS64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,int count,double bar_size,const ImPlotSpec* spec)
{
    ImPlot::PlotBars(label_id,xs,ys,count,bar_size,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBars_U64PtrU64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,int count,double bar_size,const ImPlotSpec* spec)
{
    ImPlot::PlotBars(label_id,xs,ys,count,bar_size,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBarsG(const char* label_id,ImPlotGetter getter,void* data,int count,double bar_size,const ImPlotSpec* spec)
{
    ImPlot::PlotBarsG(label_id,getter,data,count,bar_size,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBubbles_FloatPtrFloatPtrInt(const char* label_id,const float* values,const float* szs,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotBubbles(label_id,values,szs,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBubbles_doublePtrdoublePtrInt(const char* label_id,const double* values,const double* szs,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotBubbles(label_id,values,szs,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBubbles_S16PtrS16PtrInt(const char* label_id,const ImS16* values,const ImS16* szs,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotBubbles(label_id,values,szs,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBubbles_U16PtrU16PtrInt(const char* label_id,const ImU16* values,const ImU16* szs,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotBubbles(label_id,values,szs,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBubbles_S32PtrS32PtrInt(const char* label_id,const ImS32* values,const ImS32* szs,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotBubbles(label_id,values,szs,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBubbles_U32PtrU32PtrInt(const char* label_id,const ImU32* values,const ImU32* szs,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotBubbles(label_id,values,szs,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBubbles_S64PtrS64PtrInt(const char* label_id,const ImS64* values,const ImS64* szs,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotBubbles(label_id,values,szs,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBubbles_U64PtrU64PtrInt(const char* label_id,const ImU64* values,const ImU64* szs,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotBubbles(label_id,values,szs,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBubbles_FloatPtrFloatPtrFloatPtr(const char* label_id,const float* xs,const float* ys,const float* szs,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotBubbles(label_id,xs,ys,szs,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBubbles_doublePtrdoublePtrdoublePtr(const char* label_id,const double* xs,const double* ys,const double* szs,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotBubbles(label_id,xs,ys,szs,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBubbles_S16PtrS16PtrS16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,const ImS16* szs,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotBubbles(label_id,xs,ys,szs,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBubbles_U16PtrU16PtrU16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,const ImU16* szs,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotBubbles(label_id,xs,ys,szs,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBubbles_S32PtrS32PtrS32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,const ImS32* szs,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotBubbles(label_id,xs,ys,szs,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBubbles_U32PtrU32PtrU32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,const ImU32* szs,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotBubbles(label_id,xs,ys,szs,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBubbles_S64PtrS64PtrS64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,const ImS64* szs,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotBubbles(label_id,xs,ys,szs,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotBubbles_U64PtrU64PtrU64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,const ImU64* szs,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotBubbles(label_id,xs,ys,szs,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotDigital_FloatPtr(const char* label_id,const float* xs,const float* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotDigital(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotDigital_doublePtr(const char* label_id,const double* xs,const double* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotDigital(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotDigital_S16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotDigital(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotDigital_U16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotDigital(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotDigital_S32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotDigital(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotDigital_U32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotDigital(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotDigital_S64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotDigital(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotDigital_U64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotDigital(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotDigitalG(const char* label_id,ImPlotGetter getter,void* data,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotDigitalG(label_id,getter,data,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotDummy(const char* label_id,const ImPlotSpec* spec)
{
    ImPlot::PlotDummy(label_id,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotErrorBars_FloatPtrFloatPtrFloatPtrInt(const char* label_id,const float* xs,const float* ys,const float* err,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotErrorBars(label_id,xs,ys,err,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotErrorBars_doublePtrdoublePtrdoublePtrInt(const char* label_id,const double* xs,const double* ys,const double* err,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotErrorBars(label_id,xs,ys,err,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotErrorBars_S16PtrS16PtrS16PtrInt(const char* label_id,const ImS16* xs,const ImS16* ys,const ImS16* err,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotErrorBars(label_id,xs,ys,err,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotErrorBars_U16PtrU16PtrU16PtrInt(const char* label_id,const ImU16* xs,const ImU16* ys,const ImU16* err,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotErrorBars(label_id,xs,ys,err,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotErrorBars_S32PtrS32PtrS32PtrInt(const char* label_id,const ImS32* xs,const ImS32* ys,const ImS32* err,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotErrorBars(label_id,xs,ys,err,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotErrorBars_U32PtrU32PtrU32PtrInt(const char* label_id,const ImU32* xs,const ImU32* ys,const ImU32* err,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotErrorBars(label_id,xs,ys,err,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotErrorBars_S64PtrS64PtrS64PtrInt(const char* label_id,const ImS64* xs,const ImS64* ys,const ImS64* err,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotErrorBars(label_id,xs,ys,err,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotErrorBars_U64PtrU64PtrU64PtrInt(const char* label_id,const ImU64* xs,const ImU64* ys,const ImU64* err,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotErrorBars(label_id,xs,ys,err,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotErrorBars_FloatPtrFloatPtrFloatPtrFloatPtr(const char* label_id,const float* xs,const float* ys,const float* neg,const float* pos,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotErrorBars(label_id,xs,ys,neg,pos,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotErrorBars_doublePtrdoublePtrdoublePtrdoublePtr(const char* label_id,const double* xs,const double* ys,const double* neg,const double* pos,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotErrorBars(label_id,xs,ys,neg,pos,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotErrorBars_S16PtrS16PtrS16PtrS16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,const ImS16* neg,const ImS16* pos,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotErrorBars(label_id,xs,ys,neg,pos,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotErrorBars_U16PtrU16PtrU16PtrU16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,const ImU16* neg,const ImU16* pos,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotErrorBars(label_id,xs,ys,neg,pos,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotErrorBars_S32PtrS32PtrS32PtrS32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,const ImS32* neg,const ImS32* pos,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotErrorBars(label_id,xs,ys,neg,pos,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotErrorBars_U32PtrU32PtrU32PtrU32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,const ImU32* neg,const ImU32* pos,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotErrorBars(label_id,xs,ys,neg,pos,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotErrorBars_S64PtrS64PtrS64PtrS64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,const ImS64* neg,const ImS64* pos,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotErrorBars(label_id,xs,ys,neg,pos,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotErrorBars_U64PtrU64PtrU64PtrU64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,const ImU64* neg,const ImU64* pos,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotErrorBars(label_id,xs,ys,neg,pos,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotHeatmap_FloatPtr(const char* label_id,const float* values,int rows,int cols,double scale_min,double scale_max,const char* label_fmt,ImPlotPoint bounds_min,ImPlotPoint bounds_max,const ImPlotSpec* spec)
{
    ImPlot::PlotHeatmap(label_id,values,rows,cols,scale_min,scale_max,label_fmt,ConvertToCPP_ImPlotPoint(bounds_min),ConvertToCPP_ImPlotPoint(bounds_max),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotHeatmap_doublePtr(const char* label_id,const double* values,int rows,int cols,double scale_min,double scale_max,const char* label_fmt,ImPlotPoint bounds_min,ImPlotPoint bounds_max,const ImPlotSpec* spec)
{
    ImPlot::PlotHeatmap(label_id,values,rows,cols,scale_min,scale_max,label_fmt,ConvertToCPP_ImPlotPoint(bounds_min),ConvertToCPP_ImPlotPoint(bounds_max),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotHeatmap_S16Ptr(const char* label_id,const ImS16* values,int rows,int cols,double scale_min,double scale_max,const char* label_fmt,ImPlotPoint bounds_min,ImPlotPoint bounds_max,const ImPlotSpec* spec)
{
    ImPlot::PlotHeatmap(label_id,values,rows,cols,scale_min,scale_max,label_fmt,ConvertToCPP_ImPlotPoint(bounds_min),ConvertToCPP_ImPlotPoint(bounds_max),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotHeatmap_U16Ptr(const char* label_id,const ImU16* values,int rows,int cols,double scale_min,double scale_max,const char* label_fmt,ImPlotPoint bounds_min,ImPlotPoint bounds_max,const ImPlotSpec* spec)
{
    ImPlot::PlotHeatmap(label_id,values,rows,cols,scale_min,scale_max,label_fmt,ConvertToCPP_ImPlotPoint(bounds_min),ConvertToCPP_ImPlotPoint(bounds_max),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotHeatmap_S32Ptr(const char* label_id,const ImS32* values,int rows,int cols,double scale_min,double scale_max,const char* label_fmt,ImPlotPoint bounds_min,ImPlotPoint bounds_max,const ImPlotSpec* spec)
{
    ImPlot::PlotHeatmap(label_id,values,rows,cols,scale_min,scale_max,label_fmt,ConvertToCPP_ImPlotPoint(bounds_min),ConvertToCPP_ImPlotPoint(bounds_max),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotHeatmap_U32Ptr(const char* label_id,const ImU32* values,int rows,int cols,double scale_min,double scale_max,const char* label_fmt,ImPlotPoint bounds_min,ImPlotPoint bounds_max,const ImPlotSpec* spec)
{
    ImPlot::PlotHeatmap(label_id,values,rows,cols,scale_min,scale_max,label_fmt,ConvertToCPP_ImPlotPoint(bounds_min),ConvertToCPP_ImPlotPoint(bounds_max),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotHeatmap_S64Ptr(const char* label_id,const ImS64* values,int rows,int cols,double scale_min,double scale_max,const char* label_fmt,ImPlotPoint bounds_min,ImPlotPoint bounds_max,const ImPlotSpec* spec)
{
    ImPlot::PlotHeatmap(label_id,values,rows,cols,scale_min,scale_max,label_fmt,ConvertToCPP_ImPlotPoint(bounds_min),ConvertToCPP_ImPlotPoint(bounds_max),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotHeatmap_U64Ptr(const char* label_id,const ImU64* values,int rows,int cols,double scale_min,double scale_max,const char* label_fmt,ImPlotPoint bounds_min,ImPlotPoint bounds_max,const ImPlotSpec* spec)
{
    ImPlot::PlotHeatmap(label_id,values,rows,cols,scale_min,scale_max,label_fmt,ConvertToCPP_ImPlotPoint(bounds_min),ConvertToCPP_ImPlotPoint(bounds_max),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double ImPlot_PlotHistogram_FloatPtr(const char* label_id,const float* values,int count,int bins,double bar_scale,ImPlotRange range,const ImPlotSpec* spec)
{
    return ImPlot::PlotHistogram(label_id,values,count,bins,bar_scale,ConvertToCPP_ImPlotRange(range),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double ImPlot_PlotHistogram_doublePtr(const char* label_id,const double* values,int count,int bins,double bar_scale,ImPlotRange range,const ImPlotSpec* spec)
{
    return ImPlot::PlotHistogram(label_id,values,count,bins,bar_scale,ConvertToCPP_ImPlotRange(range),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double ImPlot_PlotHistogram_S16Ptr(const char* label_id,const ImS16* values,int count,int bins,double bar_scale,ImPlotRange range,const ImPlotSpec* spec)
{
    return ImPlot::PlotHistogram(label_id,values,count,bins,bar_scale,ConvertToCPP_ImPlotRange(range),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double ImPlot_PlotHistogram_U16Ptr(const char* label_id,const ImU16* values,int count,int bins,double bar_scale,ImPlotRange range,const ImPlotSpec* spec)
{
    return ImPlot::PlotHistogram(label_id,values,count,bins,bar_scale,ConvertToCPP_ImPlotRange(range),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double ImPlot_PlotHistogram_S32Ptr(const char* label_id,const ImS32* values,int count,int bins,double bar_scale,ImPlotRange range,const ImPlotSpec* spec)
{
    return ImPlot::PlotHistogram(label_id,values,count,bins,bar_scale,ConvertToCPP_ImPlotRange(range),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double ImPlot_PlotHistogram_U32Ptr(const char* label_id,const ImU32* values,int count,int bins,double bar_scale,ImPlotRange range,const ImPlotSpec* spec)
{
    return ImPlot::PlotHistogram(label_id,values,count,bins,bar_scale,ConvertToCPP_ImPlotRange(range),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double ImPlot_PlotHistogram_S64Ptr(const char* label_id,const ImS64* values,int count,int bins,double bar_scale,ImPlotRange range,const ImPlotSpec* spec)
{
    return ImPlot::PlotHistogram(label_id,values,count,bins,bar_scale,ConvertToCPP_ImPlotRange(range),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double ImPlot_PlotHistogram_U64Ptr(const char* label_id,const ImU64* values,int count,int bins,double bar_scale,ImPlotRange range,const ImPlotSpec* spec)
{
    return ImPlot::PlotHistogram(label_id,values,count,bins,bar_scale,ConvertToCPP_ImPlotRange(range),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double ImPlot_PlotHistogram2D_FloatPtr(const char* label_id,const float* xs,const float* ys,int count,int x_bins,int y_bins,ImPlotRect range,const ImPlotSpec* spec)
{
    return ImPlot::PlotHistogram2D(label_id,xs,ys,count,x_bins,y_bins,ConvertToCPP_ImPlotRect(range),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double ImPlot_PlotHistogram2D_doublePtr(const char* label_id,const double* xs,const double* ys,int count,int x_bins,int y_bins,ImPlotRect range,const ImPlotSpec* spec)
{
    return ImPlot::PlotHistogram2D(label_id,xs,ys,count,x_bins,y_bins,ConvertToCPP_ImPlotRect(range),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double ImPlot_PlotHistogram2D_S16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,int count,int x_bins,int y_bins,ImPlotRect range,const ImPlotSpec* spec)
{
    return ImPlot::PlotHistogram2D(label_id,xs,ys,count,x_bins,y_bins,ConvertToCPP_ImPlotRect(range),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double ImPlot_PlotHistogram2D_U16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,int count,int x_bins,int y_bins,ImPlotRect range,const ImPlotSpec* spec)
{
    return ImPlot::PlotHistogram2D(label_id,xs,ys,count,x_bins,y_bins,ConvertToCPP_ImPlotRect(range),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double ImPlot_PlotHistogram2D_S32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,int count,int x_bins,int y_bins,ImPlotRect range,const ImPlotSpec* spec)
{
    return ImPlot::PlotHistogram2D(label_id,xs,ys,count,x_bins,y_bins,ConvertToCPP_ImPlotRect(range),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double ImPlot_PlotHistogram2D_U32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,int count,int x_bins,int y_bins,ImPlotRect range,const ImPlotSpec* spec)
{
    return ImPlot::PlotHistogram2D(label_id,xs,ys,count,x_bins,y_bins,ConvertToCPP_ImPlotRect(range),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double ImPlot_PlotHistogram2D_S64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,int count,int x_bins,int y_bins,ImPlotRect range,const ImPlotSpec* spec)
{
    return ImPlot::PlotHistogram2D(label_id,xs,ys,count,x_bins,y_bins,ConvertToCPP_ImPlotRect(range),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API double ImPlot_PlotHistogram2D_U64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,int count,int x_bins,int y_bins,ImPlotRect range,const ImPlotSpec* spec)
{
    return ImPlot::PlotHistogram2D(label_id,xs,ys,count,x_bins,y_bins,ConvertToCPP_ImPlotRect(range),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotImage(const char* label_id,ImTextureRef tex_ref,ImPlotPoint bounds_min,ImPlotPoint bounds_max,ImVec2 uv0,ImVec2 uv1,ImVec4 tint_col,const ImPlotSpec* spec)
{
    ImPlot::PlotImage(label_id,ConvertToCPP_ImTextureRef(tex_ref),ConvertToCPP_ImPlotPoint(bounds_min),ConvertToCPP_ImPlotPoint(bounds_max),ConvertToCPP_ImVec2(uv0),ConvertToCPP_ImVec2(uv1),ConvertToCPP_ImVec4(tint_col),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotInfLines_FloatPtr(const char* label_id,const float* values,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotInfLines(label_id,values,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotInfLines_doublePtr(const char* label_id,const double* values,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotInfLines(label_id,values,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotInfLines_S16Ptr(const char* label_id,const ImS16* values,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotInfLines(label_id,values,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotInfLines_U16Ptr(const char* label_id,const ImU16* values,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotInfLines(label_id,values,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotInfLines_S32Ptr(const char* label_id,const ImS32* values,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotInfLines(label_id,values,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotInfLines_U32Ptr(const char* label_id,const ImU32* values,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotInfLines(label_id,values,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotInfLines_S64Ptr(const char* label_id,const ImS64* values,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotInfLines(label_id,values,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotInfLines_U64Ptr(const char* label_id,const ImU64* values,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotInfLines(label_id,values,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLine_FloatPtrInt(const char* label_id,const float* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotLine(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLine_doublePtrInt(const char* label_id,const double* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotLine(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLine_S16PtrInt(const char* label_id,const ImS16* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotLine(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLine_U16PtrInt(const char* label_id,const ImU16* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotLine(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLine_S32PtrInt(const char* label_id,const ImS32* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotLine(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLine_U32PtrInt(const char* label_id,const ImU32* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotLine(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLine_S64PtrInt(const char* label_id,const ImS64* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotLine(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLine_U64PtrInt(const char* label_id,const ImU64* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotLine(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLine_FloatPtrFloatPtr(const char* label_id,const float* xs,const float* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotLine(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLine_doublePtrdoublePtr(const char* label_id,const double* xs,const double* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotLine(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLine_S16PtrS16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotLine(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLine_U16PtrU16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotLine(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLine_S32PtrS32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotLine(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLine_U32PtrU32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotLine(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLine_S64PtrS64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotLine(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLine_U64PtrU64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotLine(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotLineG(const char* label_id,ImPlotGetter getter,void* data,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotLineG(label_id,getter,data,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotPieChart_FloatPtrPlotFormatter(const char* const label_ids[],const float* values,int count,double x,double y,double radius,ImPlotFormatter fmt,void* fmt_data,double angle0,const ImPlotSpec* spec)
{
    ImPlot::PlotPieChart(label_ids,values,count,x,y,radius,fmt,fmt_data,angle0,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotPieChart_doublePtrPlotFormatter(const char* const label_ids[],const double* values,int count,double x,double y,double radius,ImPlotFormatter fmt,void* fmt_data,double angle0,const ImPlotSpec* spec)
{
    ImPlot::PlotPieChart(label_ids,values,count,x,y,radius,fmt,fmt_data,angle0,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotPieChart_S16PtrPlotFormatter(const char* const label_ids[],const ImS16* values,int count,double x,double y,double radius,ImPlotFormatter fmt,void* fmt_data,double angle0,const ImPlotSpec* spec)
{
    ImPlot::PlotPieChart(label_ids,values,count,x,y,radius,fmt,fmt_data,angle0,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotPieChart_U16PtrPlotFormatter(const char* const label_ids[],const ImU16* values,int count,double x,double y,double radius,ImPlotFormatter fmt,void* fmt_data,double angle0,const ImPlotSpec* spec)
{
    ImPlot::PlotPieChart(label_ids,values,count,x,y,radius,fmt,fmt_data,angle0,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotPieChart_S32PtrPlotFormatter(const char* const label_ids[],const ImS32* values,int count,double x,double y,double radius,ImPlotFormatter fmt,void* fmt_data,double angle0,const ImPlotSpec* spec)
{
    ImPlot::PlotPieChart(label_ids,values,count,x,y,radius,fmt,fmt_data,angle0,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotPieChart_U32PtrPlotFormatter(const char* const label_ids[],const ImU32* values,int count,double x,double y,double radius,ImPlotFormatter fmt,void* fmt_data,double angle0,const ImPlotSpec* spec)
{
    ImPlot::PlotPieChart(label_ids,values,count,x,y,radius,fmt,fmt_data,angle0,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotPieChart_S64PtrPlotFormatter(const char* const label_ids[],const ImS64* values,int count,double x,double y,double radius,ImPlotFormatter fmt,void* fmt_data,double angle0,const ImPlotSpec* spec)
{
    ImPlot::PlotPieChart(label_ids,values,count,x,y,radius,fmt,fmt_data,angle0,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotPieChart_U64PtrPlotFormatter(const char* const label_ids[],const ImU64* values,int count,double x,double y,double radius,ImPlotFormatter fmt,void* fmt_data,double angle0,const ImPlotSpec* spec)
{
    ImPlot::PlotPieChart(label_ids,values,count,x,y,radius,fmt,fmt_data,angle0,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotPieChart_FloatPtrStr(const char* const label_ids[],const float* values,int count,double x,double y,double radius,const char* label_fmt,double angle0,const ImPlotSpec* spec)
{
    ImPlot::PlotPieChart(label_ids,values,count,x,y,radius,label_fmt,angle0,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotPieChart_doublePtrStr(const char* const label_ids[],const double* values,int count,double x,double y,double radius,const char* label_fmt,double angle0,const ImPlotSpec* spec)
{
    ImPlot::PlotPieChart(label_ids,values,count,x,y,radius,label_fmt,angle0,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotPieChart_S16PtrStr(const char* const label_ids[],const ImS16* values,int count,double x,double y,double radius,const char* label_fmt,double angle0,const ImPlotSpec* spec)
{
    ImPlot::PlotPieChart(label_ids,values,count,x,y,radius,label_fmt,angle0,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotPieChart_U16PtrStr(const char* const label_ids[],const ImU16* values,int count,double x,double y,double radius,const char* label_fmt,double angle0,const ImPlotSpec* spec)
{
    ImPlot::PlotPieChart(label_ids,values,count,x,y,radius,label_fmt,angle0,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotPieChart_S32PtrStr(const char* const label_ids[],const ImS32* values,int count,double x,double y,double radius,const char* label_fmt,double angle0,const ImPlotSpec* spec)
{
    ImPlot::PlotPieChart(label_ids,values,count,x,y,radius,label_fmt,angle0,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotPieChart_U32PtrStr(const char* const label_ids[],const ImU32* values,int count,double x,double y,double radius,const char* label_fmt,double angle0,const ImPlotSpec* spec)
{
    ImPlot::PlotPieChart(label_ids,values,count,x,y,radius,label_fmt,angle0,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotPieChart_S64PtrStr(const char* const label_ids[],const ImS64* values,int count,double x,double y,double radius,const char* label_fmt,double angle0,const ImPlotSpec* spec)
{
    ImPlot::PlotPieChart(label_ids,values,count,x,y,radius,label_fmt,angle0,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotPieChart_U64PtrStr(const char* const label_ids[],const ImU64* values,int count,double x,double y,double radius,const char* label_fmt,double angle0,const ImPlotSpec* spec)
{
    ImPlot::PlotPieChart(label_ids,values,count,x,y,radius,label_fmt,angle0,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatter_FloatPtrInt(const char* label_id,const float* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotScatter(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatter_doublePtrInt(const char* label_id,const double* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotScatter(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatter_S16PtrInt(const char* label_id,const ImS16* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotScatter(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatter_U16PtrInt(const char* label_id,const ImU16* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotScatter(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatter_S32PtrInt(const char* label_id,const ImS32* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotScatter(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatter_U32PtrInt(const char* label_id,const ImU32* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotScatter(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatter_S64PtrInt(const char* label_id,const ImS64* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotScatter(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatter_U64PtrInt(const char* label_id,const ImU64* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotScatter(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatter_FloatPtrFloatPtr(const char* label_id,const float* xs,const float* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotScatter(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatter_doublePtrdoublePtr(const char* label_id,const double* xs,const double* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotScatter(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatter_S16PtrS16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotScatter(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatter_U16PtrU16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotScatter(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatter_S32PtrS32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotScatter(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatter_U32PtrU32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotScatter(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatter_S64PtrS64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotScatter(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatter_U64PtrU64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotScatter(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotScatterG(const char* label_id,ImPlotGetter getter,void* data,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotScatterG(label_id,getter,data,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_FloatPtrInt(const char* label_id,const float* values,int count,double yref,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,values,count,yref,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_doublePtrInt(const char* label_id,const double* values,int count,double yref,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,values,count,yref,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_S16PtrInt(const char* label_id,const ImS16* values,int count,double yref,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,values,count,yref,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_U16PtrInt(const char* label_id,const ImU16* values,int count,double yref,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,values,count,yref,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_S32PtrInt(const char* label_id,const ImS32* values,int count,double yref,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,values,count,yref,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_U32PtrInt(const char* label_id,const ImU32* values,int count,double yref,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,values,count,yref,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_S64PtrInt(const char* label_id,const ImS64* values,int count,double yref,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,values,count,yref,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_U64PtrInt(const char* label_id,const ImU64* values,int count,double yref,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,values,count,yref,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_FloatPtrFloatPtrInt(const char* label_id,const float* xs,const float* ys,int count,double yref,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,xs,ys,count,yref,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_doublePtrdoublePtrInt(const char* label_id,const double* xs,const double* ys,int count,double yref,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,xs,ys,count,yref,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_S16PtrS16PtrInt(const char* label_id,const ImS16* xs,const ImS16* ys,int count,double yref,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,xs,ys,count,yref,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_U16PtrU16PtrInt(const char* label_id,const ImU16* xs,const ImU16* ys,int count,double yref,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,xs,ys,count,yref,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_S32PtrS32PtrInt(const char* label_id,const ImS32* xs,const ImS32* ys,int count,double yref,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,xs,ys,count,yref,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_U32PtrU32PtrInt(const char* label_id,const ImU32* xs,const ImU32* ys,int count,double yref,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,xs,ys,count,yref,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_S64PtrS64PtrInt(const char* label_id,const ImS64* xs,const ImS64* ys,int count,double yref,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,xs,ys,count,yref,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_U64PtrU64PtrInt(const char* label_id,const ImU64* xs,const ImU64* ys,int count,double yref,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,xs,ys,count,yref,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_FloatPtrFloatPtrFloatPtr(const char* label_id,const float* xs,const float* ys1,const float* ys2,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,xs,ys1,ys2,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_doublePtrdoublePtrdoublePtr(const char* label_id,const double* xs,const double* ys1,const double* ys2,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,xs,ys1,ys2,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_S16PtrS16PtrS16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys1,const ImS16* ys2,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,xs,ys1,ys2,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_U16PtrU16PtrU16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys1,const ImU16* ys2,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,xs,ys1,ys2,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_S32PtrS32PtrS32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys1,const ImS32* ys2,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,xs,ys1,ys2,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_U32PtrU32PtrU32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys1,const ImU32* ys2,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,xs,ys1,ys2,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_S64PtrS64PtrS64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys1,const ImS64* ys2,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,xs,ys1,ys2,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShaded_U64PtrU64PtrU64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys1,const ImU64* ys2,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotShaded(label_id,xs,ys1,ys2,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotShadedG(const char* label_id,ImPlotGetter getter1,void* data1,ImPlotGetter getter2,void* data2,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotShadedG(label_id,getter1,data1,getter2,data2,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairs_FloatPtrInt(const char* label_id,const float* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotStairs(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairs_doublePtrInt(const char* label_id,const double* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotStairs(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairs_S16PtrInt(const char* label_id,const ImS16* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotStairs(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairs_U16PtrInt(const char* label_id,const ImU16* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotStairs(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairs_S32PtrInt(const char* label_id,const ImS32* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotStairs(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairs_U32PtrInt(const char* label_id,const ImU32* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotStairs(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairs_S64PtrInt(const char* label_id,const ImS64* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotStairs(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairs_U64PtrInt(const char* label_id,const ImU64* values,int count,double xscale,double xstart,const ImPlotSpec* spec)
{
    ImPlot::PlotStairs(label_id,values,count,xscale,xstart,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairs_FloatPtrFloatPtr(const char* label_id,const float* xs,const float* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotStairs(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairs_doublePtrdoublePtr(const char* label_id,const double* xs,const double* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotStairs(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairs_S16PtrS16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotStairs(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairs_U16PtrU16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotStairs(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairs_S32PtrS32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotStairs(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairs_U32PtrU32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotStairs(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairs_S64PtrS64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotStairs(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairs_U64PtrU64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotStairs(label_id,xs,ys,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStairsG(const char* label_id,ImPlotGetter getter,void* data,int count,const ImPlotSpec* spec)
{
    ImPlot::PlotStairsG(label_id,getter,data,count,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStems_FloatPtrInt(const char* label_id,const float* values,int count,double ref,double scale,double start,const ImPlotSpec* spec)
{
    ImPlot::PlotStems(label_id,values,count,ref,scale,start,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStems_doublePtrInt(const char* label_id,const double* values,int count,double ref,double scale,double start,const ImPlotSpec* spec)
{
    ImPlot::PlotStems(label_id,values,count,ref,scale,start,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStems_S16PtrInt(const char* label_id,const ImS16* values,int count,double ref,double scale,double start,const ImPlotSpec* spec)
{
    ImPlot::PlotStems(label_id,values,count,ref,scale,start,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStems_U16PtrInt(const char* label_id,const ImU16* values,int count,double ref,double scale,double start,const ImPlotSpec* spec)
{
    ImPlot::PlotStems(label_id,values,count,ref,scale,start,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStems_S32PtrInt(const char* label_id,const ImS32* values,int count,double ref,double scale,double start,const ImPlotSpec* spec)
{
    ImPlot::PlotStems(label_id,values,count,ref,scale,start,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStems_U32PtrInt(const char* label_id,const ImU32* values,int count,double ref,double scale,double start,const ImPlotSpec* spec)
{
    ImPlot::PlotStems(label_id,values,count,ref,scale,start,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStems_S64PtrInt(const char* label_id,const ImS64* values,int count,double ref,double scale,double start,const ImPlotSpec* spec)
{
    ImPlot::PlotStems(label_id,values,count,ref,scale,start,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStems_U64PtrInt(const char* label_id,const ImU64* values,int count,double ref,double scale,double start,const ImPlotSpec* spec)
{
    ImPlot::PlotStems(label_id,values,count,ref,scale,start,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStems_FloatPtrFloatPtr(const char* label_id,const float* xs,const float* ys,int count,double ref,const ImPlotSpec* spec)
{
    ImPlot::PlotStems(label_id,xs,ys,count,ref,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStems_doublePtrdoublePtr(const char* label_id,const double* xs,const double* ys,int count,double ref,const ImPlotSpec* spec)
{
    ImPlot::PlotStems(label_id,xs,ys,count,ref,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStems_S16PtrS16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,int count,double ref,const ImPlotSpec* spec)
{
    ImPlot::PlotStems(label_id,xs,ys,count,ref,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStems_U16PtrU16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,int count,double ref,const ImPlotSpec* spec)
{
    ImPlot::PlotStems(label_id,xs,ys,count,ref,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStems_S32PtrS32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,int count,double ref,const ImPlotSpec* spec)
{
    ImPlot::PlotStems(label_id,xs,ys,count,ref,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStems_U32PtrU32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,int count,double ref,const ImPlotSpec* spec)
{
    ImPlot::PlotStems(label_id,xs,ys,count,ref,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStems_S64PtrS64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,int count,double ref,const ImPlotSpec* spec)
{
    ImPlot::PlotStems(label_id,xs,ys,count,ref,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotStems_U64PtrU64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,int count,double ref,const ImPlotSpec* spec)
{
    ImPlot::PlotStems(label_id,xs,ys,count,ref,ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API void ImPlot_PlotText(const char* text,double x,double y,ImVec2 pix_offset,const ImPlotSpec* spec)
{
    ImPlot::PlotText(text,x,y,ConvertToCPP_ImVec2(pix_offset),ConvertToCPP_ImPlotSpec(spec));
}
CIMGUI_API ImVec2 ImPlot_PlotToPixels_PlotPoint(ImPlotPoint plt,ImAxis x_axis,ImAxis y_axis)
{
    return ImPlot::PlotToPixels(ConvertToCPP_ImPlotPoint(plt),x_axis,y_axis);
}
CIMGUI_API ImVec2 ImPlot_PlotToPixels_double(double x,double y,ImAxis x_axis,ImAxis y_axis)
{
    return ImPlot::PlotToPixels(x,y,x_axis,y_axis);
}
CIMGUI_API void ImPlot_PopColormap(int count)
{
    ImPlot::PopColormap(count);
}
CIMGUI_API void ImPlot_PopPlotClipRect()
{
    ImPlot::PopPlotClipRect();
}
CIMGUI_API void ImPlot_PopStyleColor(int count)
{
    ImPlot::PopStyleColor(count);
}
CIMGUI_API void ImPlot_PopStyleVar(int count)
{
    ImPlot::PopStyleVar(count);
}
CIMGUI_API void ImPlot_PushColormap_PlotColormap(ImPlotColormap cmap)
{
    ImPlot::PushColormap(cmap);
}
CIMGUI_API void ImPlot_PushColormap_Str(const char* name)
{
    ImPlot::PushColormap(name);
}
CIMGUI_API void ImPlot_PushPlotClipRect(float expand)
{
    ImPlot::PushPlotClipRect(expand);
}
CIMGUI_API void ImPlot_PushStyleColor_U32(ImPlotCol idx,ImU32 col)
{
    ImPlot::PushStyleColor(idx,col);
}
CIMGUI_API void ImPlot_PushStyleColor_Vec4(ImPlotCol idx,ImVec4 col)
{
    ImPlot::PushStyleColor(idx,ConvertToCPP_ImVec4(col));
}
CIMGUI_API void ImPlot_PushStyleVar_Float(ImPlotStyleVar idx,float val)
{
    ImPlot::PushStyleVar(idx,val);
}
CIMGUI_API void ImPlot_PushStyleVar_Int(ImPlotStyleVar idx,int val)
{
    ImPlot::PushStyleVar(idx,val);
}
CIMGUI_API void ImPlot_PushStyleVar_Vec2(ImPlotStyleVar idx,ImVec2 val)
{
    ImPlot::PushStyleVar(idx,ConvertToCPP_ImVec2(val));
}
CIMGUI_API ImVec4 ImPlot_SampleColormap(float t,ImPlotColormap cmap)
{
    return ImPlot::SampleColormap(t,cmap);
}
CIMGUI_API void ImPlot_SetAxes(ImAxis x_axis,ImAxis y_axis)
{
    ImPlot::SetAxes(x_axis,y_axis);
}
CIMGUI_API void ImPlot_SetAxis(ImAxis axis)
{
    ImPlot::SetAxis(axis);
}
CIMGUI_API void ImPlot_SetCurrentContext(ImPlotContext* ctx)
{
    ImPlot::SetCurrentContext(ctx);
}
CIMGUI_API void ImPlot_SetImGuiContext(ImGuiContext* ctx)
{
    ImPlot::SetImGuiContext(ctx);
}
CIMGUI_API void ImPlot_SetNextAxesLimits(double x_min,double x_max,double y_min,double y_max,ImPlotCond cond)
{
    ImPlot::SetNextAxesLimits(x_min,x_max,y_min,y_max,cond);
}
CIMGUI_API void ImPlot_SetNextAxesToFit()
{
    ImPlot::SetNextAxesToFit();
}
CIMGUI_API void ImPlot_SetNextAxisLimits(ImAxis axis,double v_min,double v_max,ImPlotCond cond)
{
    ImPlot::SetNextAxisLimits(axis,v_min,v_max,cond);
}
CIMGUI_API void ImPlot_SetNextAxisLinks(ImAxis axis,double* link_min,double* link_max)
{
    ImPlot::SetNextAxisLinks(axis,link_min,link_max);
}
CIMGUI_API void ImPlot_SetNextAxisToFit(ImAxis axis)
{
    ImPlot::SetNextAxisToFit(axis);
}
CIMGUI_API void ImPlot_SetupAxes(const char* x_label,const char* y_label,ImPlotAxisFlags x_flags,ImPlotAxisFlags y_flags)
{
    ImPlot::SetupAxes(x_label,y_label,x_flags,y_flags);
}
CIMGUI_API void ImPlot_SetupAxesLimits(double x_min,double x_max,double y_min,double y_max,ImPlotCond cond)
{
    ImPlot::SetupAxesLimits(x_min,x_max,y_min,y_max,cond);
}
CIMGUI_API void ImPlot_SetupAxis(ImAxis axis,const char* label,ImPlotAxisFlags flags)
{
    ImPlot::SetupAxis(axis,label,flags);
}
CIMGUI_API void ImPlot_SetupAxisFormat_Str(ImAxis axis,const char* fmt)
{
    ImPlot::SetupAxisFormat(axis,fmt);
}
CIMGUI_API void ImPlot_SetupAxisFormat_PlotFormatter(ImAxis axis,ImPlotFormatter formatter,void* data)
{
    ImPlot::SetupAxisFormat(axis,formatter,data);
}
CIMGUI_API void ImPlot_SetupAxisLimits(ImAxis axis,double v_min,double v_max,ImPlotCond cond)
{
    ImPlot::SetupAxisLimits(axis,v_min,v_max,cond);
}
CIMGUI_API void ImPlot_SetupAxisLimitsConstraints(ImAxis axis,double v_min,double v_max)
{
    ImPlot::SetupAxisLimitsConstraints(axis,v_min,v_max);
}
CIMGUI_API void ImPlot_SetupAxisLinks(ImAxis axis,double* link_min,double* link_max)
{
    ImPlot::SetupAxisLinks(axis,link_min,link_max);
}
CIMGUI_API void ImPlot_SetupAxisScale_PlotScale(ImAxis axis,ImPlotScale scale)
{
    ImPlot::SetupAxisScale(axis,scale);
}
CIMGUI_API void ImPlot_SetupAxisScale_PlotTransform(ImAxis axis,ImPlotTransform forward,ImPlotTransform inverse,void* data)
{
    ImPlot::SetupAxisScale(axis,forward,inverse,data);
}
CIMGUI_API void ImPlot_SetupAxisTicks_doublePtr(ImAxis axis,const double* values,int n_ticks,const char* const labels[],bool keep_default)
{
    ImPlot::SetupAxisTicks(axis,values,n_ticks,labels,keep_default);
}
CIMGUI_API void ImPlot_SetupAxisTicks_double(ImAxis axis,double v_min,double v_max,int n_ticks,const char* const labels[],bool keep_default)
{
    ImPlot::SetupAxisTicks(axis,v_min,v_max,n_ticks,labels,keep_default);
}
CIMGUI_API void ImPlot_SetupAxisZoomConstraints(ImAxis axis,double z_min,double z_max)
{
    ImPlot::SetupAxisZoomConstraints(axis,z_min,z_max);
}
CIMGUI_API void ImPlot_SetupFinish()
{
    ImPlot::SetupFinish();
}
CIMGUI_API void ImPlot_SetupLegend(ImPlotLocation location,ImPlotLegendFlags flags)
{
    ImPlot::SetupLegend(location,flags);
}
CIMGUI_API void ImPlot_SetupMouseText(ImPlotLocation location,ImPlotMouseTextFlags flags)
{
    ImPlot::SetupMouseText(location,flags);
}
CIMGUI_API bool ImPlot_ShowColormapSelector(const char* label)
{
    return ImPlot::ShowColormapSelector(label);
}
CIMGUI_API void ImPlot_ShowDemoWindow(bool* p_open)
{
    ImPlot::ShowDemoWindow(p_open);
}
CIMGUI_API bool ImPlot_ShowInputMapSelector(const char* label)
{
    return ImPlot::ShowInputMapSelector(label);
}
CIMGUI_API void ImPlot_ShowMetricsWindow(bool* p_popen)
{
    ImPlot::ShowMetricsWindow(p_popen);
}
CIMGUI_API void ImPlot_ShowStyleEditor(ImPlotStyle* ref)
{
    ImPlot::ShowStyleEditor(ref);
}
CIMGUI_API bool ImPlot_ShowStyleSelector(const char* label)
{
    return ImPlot::ShowStyleSelector(label);
}
CIMGUI_API void ImPlot_ShowUserGuide()
{
    ImPlot::ShowUserGuide();
}
CIMGUI_API void ImPlot_StyleColorsAuto(ImPlotStyle* dst)
{
    ImPlot::StyleColorsAuto(dst);
}
CIMGUI_API void ImPlot_StyleColorsClassic(ImPlotStyle* dst)
{
    ImPlot::StyleColorsClassic(dst);
}
CIMGUI_API void ImPlot_StyleColorsDark(ImPlotStyle* dst)
{
    ImPlot::StyleColorsDark(dst);
}
CIMGUI_API void ImPlot_StyleColorsLight(ImPlotStyle* dst)
{
    ImPlot::StyleColorsLight(dst);
}
CIMGUI_API void ImPlot_TagX_Bool(double x,ImVec4 col,bool round)
{
    ImPlot::TagX(x,ConvertToCPP_ImVec4(col),round);
}
CIMGUI_API void ImPlot_TagY_Bool(double y,ImVec4 col,bool round)
{
    ImPlot::TagY(y,ConvertToCPP_ImVec4(col),round);
}
