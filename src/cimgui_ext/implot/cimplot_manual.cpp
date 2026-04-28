#define IMPLOT_DISABLE_OBSOLETE_FUNCTIONS
#include "imgui.h"
namespace cimgui
{
    #include "cimplot_manual.h"
}
#include "implot.h"

CIMGUI_API void cimgui::ImPlotSpec_Construct(cimgui::ImPlotSpec *spec)
{
    *(reinterpret_cast<::ImPlotSpec*>(spec)) = ::ImPlotSpec();
}
