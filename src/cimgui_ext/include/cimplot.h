//auto-generated
#ifndef _CIMGUI_PLOT_H_
#define _CIMGUI_PLOT_H_
#include "cimgui_ext.h"
#include "dcimgui_nodefaultargfunctions.h"
#ifdef __cplusplus
extern "C" {
#endif

typedef int (*ImPlotFormatter)(double value, char* buff, int size, void* user_data);
typedef double (*ImPlotTransform)(double value, void* user_data);

typedef int ImAxis;
typedef int ImPlotAxisFlags;
typedef int ImPlotBarGroupsFlags;
typedef int ImPlotBarsFlags;
typedef int ImPlotBin;
typedef int ImPlotBubblesFlags;
typedef int ImPlotCol;
typedef int ImPlotColormap;
typedef int ImPlotColormapScaleFlags;
typedef int ImPlotCond;
typedef int ImPlotDateFmt;
typedef int ImPlotDigitalFlags;
typedef int ImPlotDragToolFlags;
typedef int ImPlotDummyFlags;
typedef int ImPlotErrorBarsFlags;
typedef int ImPlotFlags;
typedef int ImPlotHeatmapFlags;
typedef int ImPlotHistogramFlags;
typedef int ImPlotImageFlags;
typedef int ImPlotInfLinesFlags;
typedef int ImPlotItemFlags;
typedef int ImPlotLegendFlags;
typedef int ImPlotLineFlags;
typedef int ImPlotLocation;
typedef int ImPlotMarker;
typedef int ImPlotMarkerInternal;
typedef int ImPlotMouseTextFlags;
typedef int ImPlotPieChartFlags;
typedef int ImPlotProp;
typedef int ImPlotScale;
typedef int ImPlotScatterFlags;
typedef int ImPlotShadedFlags;
typedef int ImPlotStairsFlags;
typedef int ImPlotStemsFlags;
typedef int ImPlotStyleVar;
typedef int ImPlotSubplotFlags;
typedef int ImPlotTextFlags;
typedef int ImPlotTimeFmt;
typedef int ImPlotTimeUnit;
enum ImAxis_
{
    ImAxis_X1 = 0,
    ImAxis_X2 = 1,
    ImAxis_X3 = 2,
    ImAxis_Y1 = 3,
    ImAxis_Y2 = 4,
    ImAxis_Y3 = 5,
    ImAxis_COUNT = 6,
};
enum ImPlotAxisFlags_
{
    ImPlotAxisFlags_None = 0,
    ImPlotAxisFlags_NoLabel = 1 << 0,
    ImPlotAxisFlags_NoGridLines = 1 << 1,
    ImPlotAxisFlags_NoTickMarks = 1 << 2,
    ImPlotAxisFlags_NoTickLabels = 1 << 3,
    ImPlotAxisFlags_NoInitialFit = 1 << 4,
    ImPlotAxisFlags_NoMenus = 1 << 5,
    ImPlotAxisFlags_NoSideSwitch = 1 << 6,
    ImPlotAxisFlags_NoHighlight = 1 << 7,
    ImPlotAxisFlags_Opposite = 1 << 8,
    ImPlotAxisFlags_Foreground = 1 << 9,
    ImPlotAxisFlags_Invert = 1 << 10,
    ImPlotAxisFlags_AutoFit = 1 << 11,
    ImPlotAxisFlags_RangeFit = 1 << 12,
    ImPlotAxisFlags_PanStretch = 1 << 13,
    ImPlotAxisFlags_LockMin = 1 << 14,
    ImPlotAxisFlags_LockMax = 1 << 15,
    ImPlotAxisFlags_Lock = ImPlotAxisFlags_LockMin | ImPlotAxisFlags_LockMax,
    ImPlotAxisFlags_NoDecorations = ImPlotAxisFlags_NoLabel | ImPlotAxisFlags_NoGridLines | ImPlotAxisFlags_NoTickMarks | ImPlotAxisFlags_NoTickLabels,
    ImPlotAxisFlags_AuxDefault = ImPlotAxisFlags_NoGridLines | ImPlotAxisFlags_Opposite,
};
enum ImPlotBarGroupsFlags_
{
    ImPlotBarGroupsFlags_None = 0,
    ImPlotBarGroupsFlags_Horizontal = 1 << 10,
    ImPlotBarGroupsFlags_Stacked = 1 << 11,
};
enum ImPlotBarsFlags_
{
    ImPlotBarsFlags_None = 0,
    ImPlotBarsFlags_Horizontal = 1 << 10,
};
enum ImPlotBin_
{
    ImPlotBin_Sqrt = -1,
    ImPlotBin_Sturges = -2,
    ImPlotBin_Rice = -3,
    ImPlotBin_Scott = -4,
};
enum ImPlotBubblesFlags_
{
    ImPlotBubblesFlags_None = 0,
};
enum ImPlotCol_
{
    ImPlotCol_FrameBg = 0,
    ImPlotCol_PlotBg = 1,
    ImPlotCol_PlotBorder = 2,
    ImPlotCol_LegendBg = 3,
    ImPlotCol_LegendBorder = 4,
    ImPlotCol_LegendText = 5,
    ImPlotCol_TitleText = 6,
    ImPlotCol_InlayText = 7,
    ImPlotCol_AxisText = 8,
    ImPlotCol_AxisGrid = 9,
    ImPlotCol_AxisTick = 10,
    ImPlotCol_AxisBg = 11,
    ImPlotCol_AxisBgHovered = 12,
    ImPlotCol_AxisBgActive = 13,
    ImPlotCol_Selection = 14,
    ImPlotCol_Crosshairs = 15,
    ImPlotCol_COUNT = 16,
};
enum ImPlotColormapScaleFlags_
{
    ImPlotColormapScaleFlags_None = 0,
    ImPlotColormapScaleFlags_NoLabel = 1 << 0,
    ImPlotColormapScaleFlags_Opposite = 1 << 1,
    ImPlotColormapScaleFlags_Invert = 1 << 2,
};
enum ImPlotColormap_
{
    ImPlotColormap_Deep = 0,
    ImPlotColormap_Dark = 1,
    ImPlotColormap_Pastel = 2,
    ImPlotColormap_Paired = 3,
    ImPlotColormap_Viridis = 4,
    ImPlotColormap_Plasma = 5,
    ImPlotColormap_Hot = 6,
    ImPlotColormap_Cool = 7,
    ImPlotColormap_Pink = 8,
    ImPlotColormap_Jet = 9,
    ImPlotColormap_Twilight = 10,
    ImPlotColormap_RdBu = 11,
    ImPlotColormap_BrBG = 12,
    ImPlotColormap_PiYG = 13,
    ImPlotColormap_Spectral = 14,
    ImPlotColormap_Greys = 15,
};
enum ImPlotCond_
{
    ImPlotCond_None = ImGuiCond_None,
    ImPlotCond_Always = ImGuiCond_Always,
    ImPlotCond_Once = ImGuiCond_Once,
};
enum ImPlotDateFmt_
{
    ImPlotDateFmt_None = 0,
    ImPlotDateFmt_DayMo = 1,
    ImPlotDateFmt_DayMoYr = 2,
    ImPlotDateFmt_MoYr = 3,
    ImPlotDateFmt_Mo = 4,
    ImPlotDateFmt_Yr = 5,
};
enum ImPlotDigitalFlags_
{
    ImPlotDigitalFlags_None = 0,
};
enum ImPlotDragToolFlags_
{
    ImPlotDragToolFlags_None = 0,
    ImPlotDragToolFlags_NoCursors = 1 << 0,
    ImPlotDragToolFlags_NoFit = 1 << 1,
    ImPlotDragToolFlags_NoInputs = 1 << 2,
    ImPlotDragToolFlags_Delayed = 1 << 3,
};
enum ImPlotDummyFlags_
{
    ImPlotDummyFlags_None = 0,
};
enum ImPlotErrorBarsFlags_
{
    ImPlotErrorBarsFlags_None = 0,
    ImPlotErrorBarsFlags_Horizontal = 1 << 10,
};
enum ImPlotFlags_
{
    ImPlotFlags_None = 0,
    ImPlotFlags_NoTitle = 1 << 0,
    ImPlotFlags_NoLegend = 1 << 1,
    ImPlotFlags_NoMouseText = 1 << 2,
    ImPlotFlags_NoInputs = 1 << 3,
    ImPlotFlags_NoMenus = 1 << 4,
    ImPlotFlags_NoBoxSelect = 1 << 5,
    ImPlotFlags_NoFrame = 1 << 6,
    ImPlotFlags_Equal = 1 << 7,
    ImPlotFlags_Crosshairs = 1 << 8,
    ImPlotFlags_CanvasOnly = ImPlotFlags_NoTitle | ImPlotFlags_NoLegend | ImPlotFlags_NoMenus | ImPlotFlags_NoBoxSelect | ImPlotFlags_NoMouseText,
};
enum ImPlotHeatmapFlags_
{
    ImPlotHeatmapFlags_None = 0,
    ImPlotHeatmapFlags_ColMajor = 1 << 10,
};
enum ImPlotHistogramFlags_
{
    ImPlotHistogramFlags_None = 0,
    ImPlotHistogramFlags_Horizontal = 1 << 10,
    ImPlotHistogramFlags_Cumulative = 1 << 11,
    ImPlotHistogramFlags_Density = 1 << 12,
    ImPlotHistogramFlags_NoOutliers = 1 << 13,
    ImPlotHistogramFlags_ColMajor = 1 << 14,
};
enum ImPlotImageFlags_
{
    ImPlotImageFlags_None = 0,
};
enum ImPlotInfLinesFlags_
{
    ImPlotInfLinesFlags_None = 0,
    ImPlotInfLinesFlags_Horizontal = 1 << 10,
};
enum ImPlotItemFlags_
{
    ImPlotItemFlags_None = 0,
    ImPlotItemFlags_NoLegend = 1 << 0,
    ImPlotItemFlags_NoFit = 1 << 1,
};
enum ImPlotLegendFlags_
{
    ImPlotLegendFlags_None = 0,
    ImPlotLegendFlags_NoButtons = 1 << 0,
    ImPlotLegendFlags_NoHighlightItem = 1 << 1,
    ImPlotLegendFlags_NoHighlightAxis = 1 << 2,
    ImPlotLegendFlags_NoMenus = 1 << 3,
    ImPlotLegendFlags_Outside = 1 << 4,
    ImPlotLegendFlags_Horizontal = 1 << 5,
    ImPlotLegendFlags_Sort = 1 << 6,
    ImPlotLegendFlags_Reverse = 1 << 7,
};
enum ImPlotLineFlags_
{
    ImPlotLineFlags_None = 0,
    ImPlotLineFlags_Segments = 1 << 10,
    ImPlotLineFlags_Loop = 1 << 11,
    ImPlotLineFlags_SkipNaN = 1 << 12,
    ImPlotLineFlags_NoClip = 1 << 13,
    ImPlotLineFlags_Shaded = 1 << 14,
};
enum ImPlotLocation_
{
    ImPlotLocation_Center = 0,
    ImPlotLocation_North = 1 << 0,
    ImPlotLocation_South = 1 << 1,
    ImPlotLocation_West = 1 << 2,
    ImPlotLocation_East = 1 << 3,
    ImPlotLocation_NorthWest = ImPlotLocation_North | ImPlotLocation_West,
    ImPlotLocation_NorthEast = ImPlotLocation_North | ImPlotLocation_East,
    ImPlotLocation_SouthWest = ImPlotLocation_South | ImPlotLocation_West,
    ImPlotLocation_SouthEast = ImPlotLocation_South | ImPlotLocation_East,
};
enum ImPlotMarkerInternal_
{
    ImPlotMarker_Invalid = -3,
};
enum ImPlotMarker_
{
    ImPlotMarker_None = -2,
    ImPlotMarker_Auto = -1,
    ImPlotMarker_Circle = 0,
    ImPlotMarker_Square = 1,
    ImPlotMarker_Diamond = 2,
    ImPlotMarker_Up = 3,
    ImPlotMarker_Down = 4,
    ImPlotMarker_Left = 5,
    ImPlotMarker_Right = 6,
    ImPlotMarker_Cross = 7,
    ImPlotMarker_Plus = 8,
    ImPlotMarker_Asterisk = 9,
    ImPlotMarker_COUNT = 10,
};
enum ImPlotMouseTextFlags_
{
    ImPlotMouseTextFlags_None = 0,
    ImPlotMouseTextFlags_NoAuxAxes = 1 << 0,
    ImPlotMouseTextFlags_NoFormat = 1 << 1,
    ImPlotMouseTextFlags_ShowAlways = 1 << 2,
};
enum ImPlotPieChartFlags_
{
    ImPlotPieChartFlags_None = 0,
    ImPlotPieChartFlags_Normalize = 1 << 10,
    ImPlotPieChartFlags_IgnoreHidden = 1 << 11,
    ImPlotPieChartFlags_Exploding = 1 << 12,
};
enum ImPlotProp_
{
    ImPlotProp_LineColor = 0,
    ImPlotProp_LineWeight = 1,
    ImPlotProp_FillColor = 2,
    ImPlotProp_FillAlpha = 3,
    ImPlotProp_Marker = 4,
    ImPlotProp_MarkerSize = 5,
    ImPlotProp_MarkerLineColor = 6,
    ImPlotProp_MarkerFillColor = 7,
    ImPlotProp_Size = 8,
    ImPlotProp_Offset = 9,
    ImPlotProp_Stride = 10,
    ImPlotProp_Flags = 11,
};
enum ImPlotScale_
{
    ImPlotScale_Linear = 0,
    ImPlotScale_Time = 1,
    ImPlotScale_Log10 = 2,
    ImPlotScale_SymLog = 3,
};
enum ImPlotScatterFlags_
{
    ImPlotScatterFlags_None = 0,
    ImPlotScatterFlags_NoClip = 1 << 10,
};
enum ImPlotShadedFlags_
{
    ImPlotShadedFlags_None = 0,
};
enum ImPlotStairsFlags_
{
    ImPlotStairsFlags_None = 0,
    ImPlotStairsFlags_PreStep = 1 << 10,
    ImPlotStairsFlags_Shaded = 1 << 11,
};
enum ImPlotStemsFlags_
{
    ImPlotStemsFlags_None = 0,
    ImPlotStemsFlags_Horizontal = 1 << 10,
};
enum ImPlotStyleVar_
{
    ImPlotStyleVar_PlotDefaultSize = 0,
    ImPlotStyleVar_PlotMinSize = 1,
    ImPlotStyleVar_PlotBorderSize = 2,
    ImPlotStyleVar_MinorAlpha = 3,
    ImPlotStyleVar_MajorTickLen = 4,
    ImPlotStyleVar_MinorTickLen = 5,
    ImPlotStyleVar_MajorTickSize = 6,
    ImPlotStyleVar_MinorTickSize = 7,
    ImPlotStyleVar_MajorGridSize = 8,
    ImPlotStyleVar_MinorGridSize = 9,
    ImPlotStyleVar_PlotPadding = 10,
    ImPlotStyleVar_LabelPadding = 11,
    ImPlotStyleVar_LegendPadding = 12,
    ImPlotStyleVar_LegendInnerPadding = 13,
    ImPlotStyleVar_LegendSpacing = 14,
    ImPlotStyleVar_MousePosPadding = 15,
    ImPlotStyleVar_AnnotationPadding = 16,
    ImPlotStyleVar_FitPadding = 17,
    ImPlotStyleVar_DigitalPadding = 18,
    ImPlotStyleVar_DigitalSpacing = 19,
    ImPlotStyleVar_COUNT = 20,
};
enum ImPlotSubplotFlags_
{
    ImPlotSubplotFlags_None = 0,
    ImPlotSubplotFlags_NoTitle = 1 << 0,
    ImPlotSubplotFlags_NoLegend = 1 << 1,
    ImPlotSubplotFlags_NoMenus = 1 << 2,
    ImPlotSubplotFlags_NoResize = 1 << 3,
    ImPlotSubplotFlags_NoAlign = 1 << 4,
    ImPlotSubplotFlags_ShareItems = 1 << 5,
    ImPlotSubplotFlags_LinkRows = 1 << 6,
    ImPlotSubplotFlags_LinkCols = 1 << 7,
    ImPlotSubplotFlags_LinkAllX = 1 << 8,
    ImPlotSubplotFlags_LinkAllY = 1 << 9,
    ImPlotSubplotFlags_ColMajor = 1 << 10,
};
enum ImPlotTextFlags_
{
    ImPlotTextFlags_None = 0,
    ImPlotTextFlags_Vertical = 1 << 10,
};
enum ImPlotTimeFmt_
{
    ImPlotTimeFmt_None = 0,
    ImPlotTimeFmt_Us = 1,
    ImPlotTimeFmt_SUs = 2,
    ImPlotTimeFmt_SMs = 3,
    ImPlotTimeFmt_S = 4,
    ImPlotTimeFmt_MinSMs = 5,
    ImPlotTimeFmt_HrMinSMs = 6,
    ImPlotTimeFmt_HrMinS = 7,
    ImPlotTimeFmt_HrMin = 8,
    ImPlotTimeFmt_Hr = 9,
};
enum ImPlotTimeUnit_
{
    ImPlotTimeUnit_Us = 0,
    ImPlotTimeUnit_Ms = 1,
    ImPlotTimeUnit_S = 2,
    ImPlotTimeUnit_Min = 3,
    ImPlotTimeUnit_Hr = 4,
    ImPlotTimeUnit_Day = 5,
    ImPlotTimeUnit_Mo = 6,
    ImPlotTimeUnit_Yr = 7,
    ImPlotTimeUnit_COUNT = 8,
};
struct ImPlotInputMap
{
    ImGuiMouseButton Pan;
    int PanMod;
    ImGuiMouseButton Fit;
    ImGuiMouseButton Select;
    ImGuiMouseButton SelectCancel;
    int SelectMod;
    int SelectHorzMod;
    int SelectVertMod;
    ImGuiMouseButton Menu;
    int OverrideMod;
    int ZoomMod;
    float ZoomRate;
};
struct ImPlotPoint
{
    double x;
    double y;
};
struct ImPlotRange
{
    double Min;
    double Max;
};
struct ImPlotRect
{
    ImPlotRange X;
    ImPlotRange Y;
};
struct ImPlotSpec
{
    ImVec4 LineColor;
    float LineWeight;
    ImVec4 FillColor;
    float FillAlpha;
    ImPlotMarker Marker;
    float MarkerSize;
    ImVec4 MarkerLineColor;
    ImVec4 MarkerFillColor;
    float Size;
    int Offset;
    int Stride;
    ImPlotItemFlags Flags;
};
struct ImPlotStyle
{
    ImVec2 PlotDefaultSize;
    ImVec2 PlotMinSize;
    float PlotBorderSize;
    float MinorAlpha;
    ImVec2 MajorTickLen;
    ImVec2 MinorTickLen;
    ImVec2 MajorTickSize;
    ImVec2 MinorTickSize;
    ImVec2 MajorGridSize;
    ImVec2 MinorGridSize;
    ImVec2 PlotPadding;
    ImVec2 LabelPadding;
    ImVec2 LegendPadding;
    ImVec2 LegendInnerPadding;
    ImVec2 LegendSpacing;
    ImVec2 MousePosPadding;
    ImVec2 AnnotationPadding;
    ImVec2 FitPadding;
    float DigitalPadding;
    float DigitalSpacing;
    ImVec4 Colors[ImPlotCol_COUNT];
    ImPlotColormap Colormap;
    bool UseLocalTime;
    bool UseISO8601;
    bool Use24HourClock;
};
typedef struct Formatter_Time_Data Formatter_Time_Data;
typedef struct ImPlotAlignmentData ImPlotAlignmentData;
typedef struct ImPlotAnnotation ImPlotAnnotation;
typedef struct ImPlotAnnotationCollection ImPlotAnnotationCollection;
typedef struct ImPlotAxis ImPlotAxis;
typedef struct ImPlotAxisColor ImPlotAxisColor;
typedef struct ImPlotColormapData ImPlotColormapData;
typedef struct ImPlotContext ImPlotContext;
typedef struct ImPlotDateTimeSpec ImPlotDateTimeSpec;
typedef struct ImPlotInputMap ImPlotInputMap;
typedef struct ImPlotItem ImPlotItem;
typedef struct ImPlotItemGroup ImPlotItemGroup;
typedef struct ImPlotLegend ImPlotLegend;
typedef struct ImPlotNextItemData ImPlotNextItemData;
typedef struct ImPlotNextPlotData ImPlotNextPlotData;
typedef struct ImPlotPlot ImPlotPlot;
typedef struct ImPlotPoint ImPlotPoint;
typedef struct ImPlotPointError ImPlotPointError;
typedef struct ImPlotRange ImPlotRange;
typedef struct ImPlotRect ImPlotRect;
typedef struct ImPlotSpec ImPlotSpec;
typedef struct ImPlotStyle ImPlotStyle;
typedef struct ImPlotSubplot ImPlotSubplot;
typedef struct ImPlotTag ImPlotTag;
typedef struct ImPlotTagCollection ImPlotTagCollection;
typedef struct ImPlotTick ImPlotTick;
typedef struct ImPlotTicker ImPlotTicker;
typedef struct ImPlotTime ImPlotTime;

typedef ImPlotPoint (*ImPlotGetter)(int idx, void* user_data);

CIMGUI_API ImPlotColormap ImPlot_AddColormap_Vec4Ptr(const char* name,const ImVec4* cols,int size,bool qual);
CIMGUI_API ImPlotColormap ImPlot_AddColormap_U32Ptr(const char* name,const ImU32* cols,int size,bool qual);
CIMGUI_API void ImPlot_Annotation_Bool(double x,double y,ImVec4 col,ImVec2 pix_offset,bool clamp,bool round);
CIMGUI_API bool ImPlot_BeginAlignedPlots(const char* group_id,bool vertical);
CIMGUI_API bool ImPlot_BeginDragDropSourceAxis(ImAxis axis,ImGuiDragDropFlags flags);
CIMGUI_API bool ImPlot_BeginDragDropSourceItem(const char* label_id,ImGuiDragDropFlags flags);
CIMGUI_API bool ImPlot_BeginDragDropSourcePlot(ImGuiDragDropFlags flags);
CIMGUI_API bool ImPlot_BeginDragDropTargetAxis(ImAxis axis);
CIMGUI_API bool ImPlot_BeginDragDropTargetLegend();
CIMGUI_API bool ImPlot_BeginDragDropTargetPlot();
CIMGUI_API bool ImPlot_BeginLegendPopup(const char* label_id,ImGuiMouseButton mouse_button);
CIMGUI_API bool ImPlot_BeginPlot(const char* title_id,ImVec2 size,ImPlotFlags flags);
CIMGUI_API bool ImPlot_BeginSubplots(const char* title_id,int rows,int cols,ImVec2 size,ImPlotSubplotFlags flags,float* row_ratios,float* col_ratios);
CIMGUI_API void ImPlot_BustColorCache(const char* plot_title_id);
CIMGUI_API void ImPlot_CancelPlotSelection();
CIMGUI_API bool ImPlot_ColormapButton(const char* label,ImVec2 size,ImPlotColormap cmap);
CIMGUI_API void ImPlot_ColormapIcon(ImPlotColormap cmap);
CIMGUI_API void ImPlot_ColormapScale(const char* label,double scale_min,double scale_max,ImVec2 size,const char* format,ImPlotColormapScaleFlags flags,ImPlotColormap cmap);
CIMGUI_API bool ImPlot_ColormapSlider(const char* label,float* t,ImVec4* out,const char* format,ImPlotColormap cmap);
CIMGUI_API ImPlotContext* ImPlot_CreateContext();
CIMGUI_API void ImPlot_DestroyContext(ImPlotContext* ctx);
CIMGUI_API bool ImPlot_DragLineX(int id,double* x,ImVec4 col,float thickness,ImPlotDragToolFlags flags,bool* out_clicked,bool* out_hovered,bool* out_held);
CIMGUI_API bool ImPlot_DragLineY(int id,double* y,ImVec4 col,float thickness,ImPlotDragToolFlags flags,bool* out_clicked,bool* out_hovered,bool* out_held);
CIMGUI_API bool ImPlot_DragPoint(int id,double* x,double* y,ImVec4 col,float size,ImPlotDragToolFlags flags,bool* out_clicked,bool* out_hovered,bool* out_held);
CIMGUI_API bool ImPlot_DragRect(int id,double* x1,double* y1,double* x2,double* y2,ImVec4 col,ImPlotDragToolFlags flags,bool* out_clicked,bool* out_hovered,bool* out_held);
CIMGUI_API void ImPlot_EndAlignedPlots();
CIMGUI_API void ImPlot_EndDragDropSource();
CIMGUI_API void ImPlot_EndDragDropTarget();
CIMGUI_API void ImPlot_EndLegendPopup();
CIMGUI_API void ImPlot_EndPlot();
CIMGUI_API void ImPlot_EndSubplots();
CIMGUI_API ImVec4 ImPlot_GetColormapColor(int idx,ImPlotColormap cmap);
CIMGUI_API int ImPlot_GetColormapCount();
CIMGUI_API ImPlotColormap ImPlot_GetColormapIndex(const char* name);
CIMGUI_API const char* ImPlot_GetColormapName(ImPlotColormap cmap);
CIMGUI_API int ImPlot_GetColormapSize(ImPlotColormap cmap);
CIMGUI_API ImPlotContext* ImPlot_GetCurrentContext();
CIMGUI_API ImPlotInputMap* ImPlot_GetInputMap();
CIMGUI_API ImVec4 ImPlot_GetLastItemColor();
CIMGUI_API const char* ImPlot_GetMarkerName(ImPlotMarker idx);
CIMGUI_API ImDrawList* ImPlot_GetPlotDrawList();
CIMGUI_API ImPlotRect ImPlot_GetPlotLimits(ImAxis x_axis,ImAxis y_axis);
CIMGUI_API ImPlotPoint ImPlot_GetPlotMousePos(ImAxis x_axis,ImAxis y_axis);
CIMGUI_API ImVec2 ImPlot_GetPlotPos();
CIMGUI_API ImPlotRect ImPlot_GetPlotSelection(ImAxis x_axis,ImAxis y_axis);
CIMGUI_API ImVec2 ImPlot_GetPlotSize();
CIMGUI_API ImPlotStyle* ImPlot_GetStyle();
CIMGUI_API const char* ImPlot_GetStyleColorName(ImPlotCol idx);
CIMGUI_API void ImPlot_HideNextItem(bool hidden,ImPlotCond cond);
CIMGUI_API bool ImPlot_IsAxisHovered(ImAxis axis);
CIMGUI_API bool ImPlot_IsLegendEntryHovered(const char* label_id);
CIMGUI_API bool ImPlot_IsPlotHovered();
CIMGUI_API bool ImPlot_IsPlotSelected();
CIMGUI_API bool ImPlot_IsSubplotsHovered();
CIMGUI_API void ImPlot_ItemIcon_Vec4(ImVec4 col);
CIMGUI_API void ImPlot_ItemIcon_U32(ImU32 col);
CIMGUI_API void ImPlot_MapInputDefault(ImPlotInputMap* dst);
CIMGUI_API void ImPlot_MapInputReverse(ImPlotInputMap* dst);
CIMGUI_API ImVec4 ImPlot_NextColormapColor();
CIMGUI_API ImPlotMarker ImPlot_NextMarker();
CIMGUI_API ImPlotPoint ImPlot_PixelsToPlot_Vec2(ImVec2 pix,ImAxis x_axis,ImAxis y_axis);
CIMGUI_API ImPlotPoint ImPlot_PixelsToPlot_Float(float x,float y,ImAxis x_axis,ImAxis y_axis);
CIMGUI_API void ImPlot_PlotBarGroups_FloatPtr(const char* const label_ids[],const float* values,int item_count,int group_count,double group_size,double shift,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBarGroups_doublePtr(const char* const label_ids[],const double* values,int item_count,int group_count,double group_size,double shift,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBarGroups_S16Ptr(const char* const label_ids[],const ImS16* values,int item_count,int group_count,double group_size,double shift,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBarGroups_U16Ptr(const char* const label_ids[],const ImU16* values,int item_count,int group_count,double group_size,double shift,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBarGroups_S32Ptr(const char* const label_ids[],const ImS32* values,int item_count,int group_count,double group_size,double shift,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBarGroups_U32Ptr(const char* const label_ids[],const ImU32* values,int item_count,int group_count,double group_size,double shift,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBarGroups_S64Ptr(const char* const label_ids[],const ImS64* values,int item_count,int group_count,double group_size,double shift,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBarGroups_U64Ptr(const char* const label_ids[],const ImU64* values,int item_count,int group_count,double group_size,double shift,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBars_FloatPtrInt(const char* label_id,const float* values,int count,double bar_size,double shift,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBars_doublePtrInt(const char* label_id,const double* values,int count,double bar_size,double shift,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBars_S16PtrInt(const char* label_id,const ImS16* values,int count,double bar_size,double shift,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBars_U16PtrInt(const char* label_id,const ImU16* values,int count,double bar_size,double shift,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBars_S32PtrInt(const char* label_id,const ImS32* values,int count,double bar_size,double shift,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBars_U32PtrInt(const char* label_id,const ImU32* values,int count,double bar_size,double shift,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBars_S64PtrInt(const char* label_id,const ImS64* values,int count,double bar_size,double shift,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBars_U64PtrInt(const char* label_id,const ImU64* values,int count,double bar_size,double shift,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBars_FloatPtrFloatPtr(const char* label_id,const float* xs,const float* ys,int count,double bar_size,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBars_doublePtrdoublePtr(const char* label_id,const double* xs,const double* ys,int count,double bar_size,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBars_S16PtrS16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,int count,double bar_size,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBars_U16PtrU16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,int count,double bar_size,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBars_S32PtrS32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,int count,double bar_size,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBars_U32PtrU32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,int count,double bar_size,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBars_S64PtrS64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,int count,double bar_size,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBars_U64PtrU64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,int count,double bar_size,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBarsG(const char* label_id,ImPlotGetter getter,void* data,int count,double bar_size,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBubbles_FloatPtrFloatPtrInt(const char* label_id,const float* values,const float* szs,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBubbles_doublePtrdoublePtrInt(const char* label_id,const double* values,const double* szs,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBubbles_S16PtrS16PtrInt(const char* label_id,const ImS16* values,const ImS16* szs,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBubbles_U16PtrU16PtrInt(const char* label_id,const ImU16* values,const ImU16* szs,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBubbles_S32PtrS32PtrInt(const char* label_id,const ImS32* values,const ImS32* szs,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBubbles_U32PtrU32PtrInt(const char* label_id,const ImU32* values,const ImU32* szs,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBubbles_S64PtrS64PtrInt(const char* label_id,const ImS64* values,const ImS64* szs,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBubbles_U64PtrU64PtrInt(const char* label_id,const ImU64* values,const ImU64* szs,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBubbles_FloatPtrFloatPtrFloatPtr(const char* label_id,const float* xs,const float* ys,const float* szs,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBubbles_doublePtrdoublePtrdoublePtr(const char* label_id,const double* xs,const double* ys,const double* szs,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBubbles_S16PtrS16PtrS16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,const ImS16* szs,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBubbles_U16PtrU16PtrU16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,const ImU16* szs,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBubbles_S32PtrS32PtrS32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,const ImS32* szs,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBubbles_U32PtrU32PtrU32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,const ImU32* szs,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBubbles_S64PtrS64PtrS64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,const ImS64* szs,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotBubbles_U64PtrU64PtrU64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,const ImU64* szs,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotDigital_FloatPtr(const char* label_id,const float* xs,const float* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotDigital_doublePtr(const char* label_id,const double* xs,const double* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotDigital_S16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotDigital_U16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotDigital_S32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotDigital_U32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotDigital_S64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotDigital_U64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotDigitalG(const char* label_id,ImPlotGetter getter,void* data,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotDummy(const char* label_id,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotErrorBars_FloatPtrFloatPtrFloatPtrInt(const char* label_id,const float* xs,const float* ys,const float* err,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotErrorBars_doublePtrdoublePtrdoublePtrInt(const char* label_id,const double* xs,const double* ys,const double* err,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotErrorBars_S16PtrS16PtrS16PtrInt(const char* label_id,const ImS16* xs,const ImS16* ys,const ImS16* err,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotErrorBars_U16PtrU16PtrU16PtrInt(const char* label_id,const ImU16* xs,const ImU16* ys,const ImU16* err,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotErrorBars_S32PtrS32PtrS32PtrInt(const char* label_id,const ImS32* xs,const ImS32* ys,const ImS32* err,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotErrorBars_U32PtrU32PtrU32PtrInt(const char* label_id,const ImU32* xs,const ImU32* ys,const ImU32* err,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotErrorBars_S64PtrS64PtrS64PtrInt(const char* label_id,const ImS64* xs,const ImS64* ys,const ImS64* err,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotErrorBars_U64PtrU64PtrU64PtrInt(const char* label_id,const ImU64* xs,const ImU64* ys,const ImU64* err,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotErrorBars_FloatPtrFloatPtrFloatPtrFloatPtr(const char* label_id,const float* xs,const float* ys,const float* neg,const float* pos,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotErrorBars_doublePtrdoublePtrdoublePtrdoublePtr(const char* label_id,const double* xs,const double* ys,const double* neg,const double* pos,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotErrorBars_S16PtrS16PtrS16PtrS16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,const ImS16* neg,const ImS16* pos,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotErrorBars_U16PtrU16PtrU16PtrU16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,const ImU16* neg,const ImU16* pos,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotErrorBars_S32PtrS32PtrS32PtrS32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,const ImS32* neg,const ImS32* pos,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotErrorBars_U32PtrU32PtrU32PtrU32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,const ImU32* neg,const ImU32* pos,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotErrorBars_S64PtrS64PtrS64PtrS64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,const ImS64* neg,const ImS64* pos,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotErrorBars_U64PtrU64PtrU64PtrU64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,const ImU64* neg,const ImU64* pos,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotHeatmap_FloatPtr(const char* label_id,const float* values,int rows,int cols,double scale_min,double scale_max,const char* label_fmt,ImPlotPoint bounds_min,ImPlotPoint bounds_max,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotHeatmap_doublePtr(const char* label_id,const double* values,int rows,int cols,double scale_min,double scale_max,const char* label_fmt,ImPlotPoint bounds_min,ImPlotPoint bounds_max,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotHeatmap_S16Ptr(const char* label_id,const ImS16* values,int rows,int cols,double scale_min,double scale_max,const char* label_fmt,ImPlotPoint bounds_min,ImPlotPoint bounds_max,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotHeatmap_U16Ptr(const char* label_id,const ImU16* values,int rows,int cols,double scale_min,double scale_max,const char* label_fmt,ImPlotPoint bounds_min,ImPlotPoint bounds_max,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotHeatmap_S32Ptr(const char* label_id,const ImS32* values,int rows,int cols,double scale_min,double scale_max,const char* label_fmt,ImPlotPoint bounds_min,ImPlotPoint bounds_max,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotHeatmap_U32Ptr(const char* label_id,const ImU32* values,int rows,int cols,double scale_min,double scale_max,const char* label_fmt,ImPlotPoint bounds_min,ImPlotPoint bounds_max,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotHeatmap_S64Ptr(const char* label_id,const ImS64* values,int rows,int cols,double scale_min,double scale_max,const char* label_fmt,ImPlotPoint bounds_min,ImPlotPoint bounds_max,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotHeatmap_U64Ptr(const char* label_id,const ImU64* values,int rows,int cols,double scale_min,double scale_max,const char* label_fmt,ImPlotPoint bounds_min,ImPlotPoint bounds_max,const ImPlotSpec spec);
CIMGUI_API double ImPlot_PlotHistogram_FloatPtr(const char* label_id,const float* values,int count,int bins,double bar_scale,ImPlotRange range,const ImPlotSpec spec);
CIMGUI_API double ImPlot_PlotHistogram_doublePtr(const char* label_id,const double* values,int count,int bins,double bar_scale,ImPlotRange range,const ImPlotSpec spec);
CIMGUI_API double ImPlot_PlotHistogram_S16Ptr(const char* label_id,const ImS16* values,int count,int bins,double bar_scale,ImPlotRange range,const ImPlotSpec spec);
CIMGUI_API double ImPlot_PlotHistogram_U16Ptr(const char* label_id,const ImU16* values,int count,int bins,double bar_scale,ImPlotRange range,const ImPlotSpec spec);
CIMGUI_API double ImPlot_PlotHistogram_S32Ptr(const char* label_id,const ImS32* values,int count,int bins,double bar_scale,ImPlotRange range,const ImPlotSpec spec);
CIMGUI_API double ImPlot_PlotHistogram_U32Ptr(const char* label_id,const ImU32* values,int count,int bins,double bar_scale,ImPlotRange range,const ImPlotSpec spec);
CIMGUI_API double ImPlot_PlotHistogram_S64Ptr(const char* label_id,const ImS64* values,int count,int bins,double bar_scale,ImPlotRange range,const ImPlotSpec spec);
CIMGUI_API double ImPlot_PlotHistogram_U64Ptr(const char* label_id,const ImU64* values,int count,int bins,double bar_scale,ImPlotRange range,const ImPlotSpec spec);
CIMGUI_API double ImPlot_PlotHistogram2D_FloatPtr(const char* label_id,const float* xs,const float* ys,int count,int x_bins,int y_bins,ImPlotRect range,const ImPlotSpec spec);
CIMGUI_API double ImPlot_PlotHistogram2D_doublePtr(const char* label_id,const double* xs,const double* ys,int count,int x_bins,int y_bins,ImPlotRect range,const ImPlotSpec spec);
CIMGUI_API double ImPlot_PlotHistogram2D_S16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,int count,int x_bins,int y_bins,ImPlotRect range,const ImPlotSpec spec);
CIMGUI_API double ImPlot_PlotHistogram2D_U16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,int count,int x_bins,int y_bins,ImPlotRect range,const ImPlotSpec spec);
CIMGUI_API double ImPlot_PlotHistogram2D_S32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,int count,int x_bins,int y_bins,ImPlotRect range,const ImPlotSpec spec);
CIMGUI_API double ImPlot_PlotHistogram2D_U32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,int count,int x_bins,int y_bins,ImPlotRect range,const ImPlotSpec spec);
CIMGUI_API double ImPlot_PlotHistogram2D_S64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,int count,int x_bins,int y_bins,ImPlotRect range,const ImPlotSpec spec);
CIMGUI_API double ImPlot_PlotHistogram2D_U64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,int count,int x_bins,int y_bins,ImPlotRect range,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotImage(const char* label_id,ImTextureRef tex_ref,ImPlotPoint bounds_min,ImPlotPoint bounds_max,ImVec2 uv0,ImVec2 uv1,ImVec4 tint_col,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotInfLines_FloatPtr(const char* label_id,const float* values,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotInfLines_doublePtr(const char* label_id,const double* values,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotInfLines_S16Ptr(const char* label_id,const ImS16* values,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotInfLines_U16Ptr(const char* label_id,const ImU16* values,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotInfLines_S32Ptr(const char* label_id,const ImS32* values,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotInfLines_U32Ptr(const char* label_id,const ImU32* values,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotInfLines_S64Ptr(const char* label_id,const ImS64* values,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotInfLines_U64Ptr(const char* label_id,const ImU64* values,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLine_FloatPtrInt(const char* label_id,const float* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLine_doublePtrInt(const char* label_id,const double* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLine_S16PtrInt(const char* label_id,const ImS16* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLine_U16PtrInt(const char* label_id,const ImU16* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLine_S32PtrInt(const char* label_id,const ImS32* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLine_U32PtrInt(const char* label_id,const ImU32* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLine_S64PtrInt(const char* label_id,const ImS64* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLine_U64PtrInt(const char* label_id,const ImU64* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLine_FloatPtrFloatPtr(const char* label_id,const float* xs,const float* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLine_doublePtrdoublePtr(const char* label_id,const double* xs,const double* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLine_S16PtrS16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLine_U16PtrU16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLine_S32PtrS32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLine_U32PtrU32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLine_S64PtrS64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLine_U64PtrU64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotLineG(const char* label_id,ImPlotGetter getter,void* data,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotPieChart_FloatPtrPlotFormatter(const char* const label_ids[],const float* values,int count,double x,double y,double radius,ImPlotFormatter fmt,void* fmt_data,double angle0,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotPieChart_doublePtrPlotFormatter(const char* const label_ids[],const double* values,int count,double x,double y,double radius,ImPlotFormatter fmt,void* fmt_data,double angle0,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotPieChart_S16PtrPlotFormatter(const char* const label_ids[],const ImS16* values,int count,double x,double y,double radius,ImPlotFormatter fmt,void* fmt_data,double angle0,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotPieChart_U16PtrPlotFormatter(const char* const label_ids[],const ImU16* values,int count,double x,double y,double radius,ImPlotFormatter fmt,void* fmt_data,double angle0,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotPieChart_S32PtrPlotFormatter(const char* const label_ids[],const ImS32* values,int count,double x,double y,double radius,ImPlotFormatter fmt,void* fmt_data,double angle0,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotPieChart_U32PtrPlotFormatter(const char* const label_ids[],const ImU32* values,int count,double x,double y,double radius,ImPlotFormatter fmt,void* fmt_data,double angle0,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotPieChart_S64PtrPlotFormatter(const char* const label_ids[],const ImS64* values,int count,double x,double y,double radius,ImPlotFormatter fmt,void* fmt_data,double angle0,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotPieChart_U64PtrPlotFormatter(const char* const label_ids[],const ImU64* values,int count,double x,double y,double radius,ImPlotFormatter fmt,void* fmt_data,double angle0,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotPieChart_FloatPtrStr(const char* const label_ids[],const float* values,int count,double x,double y,double radius,const char* label_fmt,double angle0,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotPieChart_doublePtrStr(const char* const label_ids[],const double* values,int count,double x,double y,double radius,const char* label_fmt,double angle0,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotPieChart_S16PtrStr(const char* const label_ids[],const ImS16* values,int count,double x,double y,double radius,const char* label_fmt,double angle0,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotPieChart_U16PtrStr(const char* const label_ids[],const ImU16* values,int count,double x,double y,double radius,const char* label_fmt,double angle0,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotPieChart_S32PtrStr(const char* const label_ids[],const ImS32* values,int count,double x,double y,double radius,const char* label_fmt,double angle0,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotPieChart_U32PtrStr(const char* const label_ids[],const ImU32* values,int count,double x,double y,double radius,const char* label_fmt,double angle0,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotPieChart_S64PtrStr(const char* const label_ids[],const ImS64* values,int count,double x,double y,double radius,const char* label_fmt,double angle0,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotPieChart_U64PtrStr(const char* const label_ids[],const ImU64* values,int count,double x,double y,double radius,const char* label_fmt,double angle0,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatter_FloatPtrInt(const char* label_id,const float* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatter_doublePtrInt(const char* label_id,const double* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatter_S16PtrInt(const char* label_id,const ImS16* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatter_U16PtrInt(const char* label_id,const ImU16* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatter_S32PtrInt(const char* label_id,const ImS32* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatter_U32PtrInt(const char* label_id,const ImU32* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatter_S64PtrInt(const char* label_id,const ImS64* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatter_U64PtrInt(const char* label_id,const ImU64* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatter_FloatPtrFloatPtr(const char* label_id,const float* xs,const float* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatter_doublePtrdoublePtr(const char* label_id,const double* xs,const double* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatter_S16PtrS16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatter_U16PtrU16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatter_S32PtrS32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatter_U32PtrU32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatter_S64PtrS64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatter_U64PtrU64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotScatterG(const char* label_id,ImPlotGetter getter,void* data,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_FloatPtrInt(const char* label_id,const float* values,int count,double yref,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_doublePtrInt(const char* label_id,const double* values,int count,double yref,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_S16PtrInt(const char* label_id,const ImS16* values,int count,double yref,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_U16PtrInt(const char* label_id,const ImU16* values,int count,double yref,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_S32PtrInt(const char* label_id,const ImS32* values,int count,double yref,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_U32PtrInt(const char* label_id,const ImU32* values,int count,double yref,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_S64PtrInt(const char* label_id,const ImS64* values,int count,double yref,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_U64PtrInt(const char* label_id,const ImU64* values,int count,double yref,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_FloatPtrFloatPtrInt(const char* label_id,const float* xs,const float* ys,int count,double yref,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_doublePtrdoublePtrInt(const char* label_id,const double* xs,const double* ys,int count,double yref,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_S16PtrS16PtrInt(const char* label_id,const ImS16* xs,const ImS16* ys,int count,double yref,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_U16PtrU16PtrInt(const char* label_id,const ImU16* xs,const ImU16* ys,int count,double yref,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_S32PtrS32PtrInt(const char* label_id,const ImS32* xs,const ImS32* ys,int count,double yref,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_U32PtrU32PtrInt(const char* label_id,const ImU32* xs,const ImU32* ys,int count,double yref,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_S64PtrS64PtrInt(const char* label_id,const ImS64* xs,const ImS64* ys,int count,double yref,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_U64PtrU64PtrInt(const char* label_id,const ImU64* xs,const ImU64* ys,int count,double yref,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_FloatPtrFloatPtrFloatPtr(const char* label_id,const float* xs,const float* ys1,const float* ys2,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_doublePtrdoublePtrdoublePtr(const char* label_id,const double* xs,const double* ys1,const double* ys2,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_S16PtrS16PtrS16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys1,const ImS16* ys2,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_U16PtrU16PtrU16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys1,const ImU16* ys2,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_S32PtrS32PtrS32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys1,const ImS32* ys2,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_U32PtrU32PtrU32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys1,const ImU32* ys2,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_S64PtrS64PtrS64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys1,const ImS64* ys2,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShaded_U64PtrU64PtrU64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys1,const ImU64* ys2,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotShadedG(const char* label_id,ImPlotGetter getter1,void* data1,ImPlotGetter getter2,void* data2,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairs_FloatPtrInt(const char* label_id,const float* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairs_doublePtrInt(const char* label_id,const double* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairs_S16PtrInt(const char* label_id,const ImS16* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairs_U16PtrInt(const char* label_id,const ImU16* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairs_S32PtrInt(const char* label_id,const ImS32* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairs_U32PtrInt(const char* label_id,const ImU32* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairs_S64PtrInt(const char* label_id,const ImS64* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairs_U64PtrInt(const char* label_id,const ImU64* values,int count,double xscale,double xstart,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairs_FloatPtrFloatPtr(const char* label_id,const float* xs,const float* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairs_doublePtrdoublePtr(const char* label_id,const double* xs,const double* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairs_S16PtrS16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairs_U16PtrU16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairs_S32PtrS32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairs_U32PtrU32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairs_S64PtrS64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairs_U64PtrU64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStairsG(const char* label_id,ImPlotGetter getter,void* data,int count,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStems_FloatPtrInt(const char* label_id,const float* values,int count,double ref,double scale,double start,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStems_doublePtrInt(const char* label_id,const double* values,int count,double ref,double scale,double start,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStems_S16PtrInt(const char* label_id,const ImS16* values,int count,double ref,double scale,double start,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStems_U16PtrInt(const char* label_id,const ImU16* values,int count,double ref,double scale,double start,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStems_S32PtrInt(const char* label_id,const ImS32* values,int count,double ref,double scale,double start,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStems_U32PtrInt(const char* label_id,const ImU32* values,int count,double ref,double scale,double start,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStems_S64PtrInt(const char* label_id,const ImS64* values,int count,double ref,double scale,double start,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStems_U64PtrInt(const char* label_id,const ImU64* values,int count,double ref,double scale,double start,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStems_FloatPtrFloatPtr(const char* label_id,const float* xs,const float* ys,int count,double ref,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStems_doublePtrdoublePtr(const char* label_id,const double* xs,const double* ys,int count,double ref,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStems_S16PtrS16Ptr(const char* label_id,const ImS16* xs,const ImS16* ys,int count,double ref,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStems_U16PtrU16Ptr(const char* label_id,const ImU16* xs,const ImU16* ys,int count,double ref,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStems_S32PtrS32Ptr(const char* label_id,const ImS32* xs,const ImS32* ys,int count,double ref,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStems_U32PtrU32Ptr(const char* label_id,const ImU32* xs,const ImU32* ys,int count,double ref,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStems_S64PtrS64Ptr(const char* label_id,const ImS64* xs,const ImS64* ys,int count,double ref,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotStems_U64PtrU64Ptr(const char* label_id,const ImU64* xs,const ImU64* ys,int count,double ref,const ImPlotSpec spec);
CIMGUI_API void ImPlot_PlotText(const char* text,double x,double y,ImVec2 pix_offset,const ImPlotSpec spec);
CIMGUI_API ImVec2 ImPlot_PlotToPixels_PlotPoint(ImPlotPoint plt,ImAxis x_axis,ImAxis y_axis);
CIMGUI_API ImVec2 ImPlot_PlotToPixels_double(double x,double y,ImAxis x_axis,ImAxis y_axis);
CIMGUI_API void ImPlot_PopColormap(int count);
CIMGUI_API void ImPlot_PopPlotClipRect();
CIMGUI_API void ImPlot_PopStyleColor(int count);
CIMGUI_API void ImPlot_PopStyleVar(int count);
CIMGUI_API void ImPlot_PushColormap_PlotColormap(ImPlotColormap cmap);
CIMGUI_API void ImPlot_PushColormap_Str(const char* name);
CIMGUI_API void ImPlot_PushPlotClipRect(float expand);
CIMGUI_API void ImPlot_PushStyleColor_U32(ImPlotCol idx,ImU32 col);
CIMGUI_API void ImPlot_PushStyleColor_Vec4(ImPlotCol idx,ImVec4 col);
CIMGUI_API void ImPlot_PushStyleVar_Float(ImPlotStyleVar idx,float val);
CIMGUI_API void ImPlot_PushStyleVar_Int(ImPlotStyleVar idx,int val);
CIMGUI_API void ImPlot_PushStyleVar_Vec2(ImPlotStyleVar idx,ImVec2 val);
CIMGUI_API ImVec4 ImPlot_SampleColormap(float t,ImPlotColormap cmap);
CIMGUI_API void ImPlot_SetAxes(ImAxis x_axis,ImAxis y_axis);
CIMGUI_API void ImPlot_SetAxis(ImAxis axis);
CIMGUI_API void ImPlot_SetCurrentContext(ImPlotContext* ctx);
CIMGUI_API void ImPlot_SetImGuiContext(ImGuiContext* ctx);
CIMGUI_API void ImPlot_SetNextAxesLimits(double x_min,double x_max,double y_min,double y_max,ImPlotCond cond);
CIMGUI_API void ImPlot_SetNextAxesToFit();
CIMGUI_API void ImPlot_SetNextAxisLimits(ImAxis axis,double v_min,double v_max,ImPlotCond cond);
CIMGUI_API void ImPlot_SetNextAxisLinks(ImAxis axis,double* link_min,double* link_max);
CIMGUI_API void ImPlot_SetNextAxisToFit(ImAxis axis);
CIMGUI_API void ImPlot_SetupAxes(const char* x_label,const char* y_label,ImPlotAxisFlags x_flags,ImPlotAxisFlags y_flags);
CIMGUI_API void ImPlot_SetupAxesLimits(double x_min,double x_max,double y_min,double y_max,ImPlotCond cond);
CIMGUI_API void ImPlot_SetupAxis(ImAxis axis,const char* label,ImPlotAxisFlags flags);
CIMGUI_API void ImPlot_SetupAxisFormat_Str(ImAxis axis,const char* fmt);
CIMGUI_API void ImPlot_SetupAxisFormat_PlotFormatter(ImAxis axis,ImPlotFormatter formatter,void* data);
CIMGUI_API void ImPlot_SetupAxisLimits(ImAxis axis,double v_min,double v_max,ImPlotCond cond);
CIMGUI_API void ImPlot_SetupAxisLimitsConstraints(ImAxis axis,double v_min,double v_max);
CIMGUI_API void ImPlot_SetupAxisLinks(ImAxis axis,double* link_min,double* link_max);
CIMGUI_API void ImPlot_SetupAxisScale_PlotScale(ImAxis axis,ImPlotScale scale);
CIMGUI_API void ImPlot_SetupAxisScale_PlotTransform(ImAxis axis,ImPlotTransform forward,ImPlotTransform inverse,void* data);
CIMGUI_API void ImPlot_SetupAxisTicks_doublePtr(ImAxis axis,const double* values,int n_ticks,const char* const labels[],bool keep_default);
CIMGUI_API void ImPlot_SetupAxisTicks_double(ImAxis axis,double v_min,double v_max,int n_ticks,const char* const labels[],bool keep_default);
CIMGUI_API void ImPlot_SetupAxisZoomConstraints(ImAxis axis,double z_min,double z_max);
CIMGUI_API void ImPlot_SetupFinish();
CIMGUI_API void ImPlot_SetupLegend(ImPlotLocation location,ImPlotLegendFlags flags);
CIMGUI_API void ImPlot_SetupMouseText(ImPlotLocation location,ImPlotMouseTextFlags flags);
CIMGUI_API bool ImPlot_ShowColormapSelector(const char* label);
CIMGUI_API void ImPlot_ShowDemoWindow(bool* p_open);
CIMGUI_API bool ImPlot_ShowInputMapSelector(const char* label);
CIMGUI_API void ImPlot_ShowMetricsWindow(bool* p_popen);
CIMGUI_API void ImPlot_ShowStyleEditor(ImPlotStyle* ref);
CIMGUI_API bool ImPlot_ShowStyleSelector(const char* label);
CIMGUI_API void ImPlot_ShowUserGuide();
CIMGUI_API void ImPlot_StyleColorsAuto(ImPlotStyle* dst);
CIMGUI_API void ImPlot_StyleColorsClassic(ImPlotStyle* dst);
CIMGUI_API void ImPlot_StyleColorsDark(ImPlotStyle* dst);
CIMGUI_API void ImPlot_StyleColorsLight(ImPlotStyle* dst);
CIMGUI_API void ImPlot_TagX_Bool(double x,ImVec4 col,bool round);
CIMGUI_API void ImPlot_TagY_Bool(double y,ImVec4 col,bool round);


#ifdef __cplusplus
}
#endif
#endif
