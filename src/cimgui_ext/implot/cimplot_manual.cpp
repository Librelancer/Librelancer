#define IMPLOT_DISABLE_OBSOLETE_FUNCTIONS
#include "imgui.h"
#include "cimplot_manual.h"
#include "implot.h"

CIMGUI_API void ImPlotSpec_Construct(ImPlotSpec *spec)
{
    *spec = ImPlotSpec();
}