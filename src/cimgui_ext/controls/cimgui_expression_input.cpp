#include "imgui.h"
#include "imgui_internal.h"
#include "cimgui_ext.h"
#include "expr.h"

using namespace ImGui;

#if defined(_MSC_VER) && !defined(snprintf)
#define ImSnprintf  _snprintf
#else
#define ImSnprintf  snprintf
#endif

// Those MIN/MAX values are not define because we need to point to them
static const signed char    IM_S8_MIN  = -128;
static const signed char    IM_S8_MAX  = 127;
static const unsigned char  IM_U8_MIN  = 0;
static const unsigned char  IM_U8_MAX  = 0xFF;
static const signed short   IM_S16_MIN = -32768;
static const signed short   IM_S16_MAX = 32767;
static const unsigned short IM_U16_MIN = 0;
static const unsigned short IM_U16_MAX = 0xFFFF;
static const ImS32          IM_S32_MIN = INT_MIN;    // (-2147483647 - 1), (0x80000000);
static const ImS32          IM_S32_MAX = INT_MAX;    // (2147483647), (0x7FFFFFFF)
static const ImU32          IM_U32_MIN = 0;
static const ImU32          IM_U32_MAX = UINT_MAX;   // (0xFFFFFFFF)
#ifdef LLONG_MIN
static const ImS64          IM_S64_MIN = LLONG_MIN;  // (-9223372036854775807ll - 1ll);
static const ImS64          IM_S64_MAX = LLONG_MAX;  // (9223372036854775807ll);
#else
static const ImS64          IM_S64_MIN = -9223372036854775807LL - 1;
static const ImS64          IM_S64_MAX = 9223372036854775807LL;
#endif
static const ImU64          IM_U64_MIN = 0;
#ifdef ULLONG_MAX
static const ImU64          IM_U64_MAX = ULLONG_MAX; // (0xFFFFFFFFFFFFFFFFull);
#else
static const ImU64          IM_U64_MAX = (2ULL * 9223372036854775807LL + 1);
#endif

static bool igExtEvaluateExpression(const char *buf, double* result)
{
    *result = 0;
    struct expr_var_list vars = {0};
    struct expr_func user_funcs[] = { {NULL, NULL, NULL, 0} };
    struct expr *e = expr_create(buf, strlen(buf), &vars, user_funcs);
    if(e == NULL)
    {
        return false;
    }
    *result = expr_eval(e);
    expr_destroy(e, &vars);
    return true;
}

static bool emptyOrWhiteSpace(const char *str)
{
    while(ImCharIsBlankA(*str))
        str++;
    return *str == 0;
}

// LIBRELANCER: Patched ApplyFromText that allows for expression evaluation
static bool igExtDataTypeApplyFromText(const char* buf, ImGuiDataType data_type, void* p_data, const char* format, void* p_data_when_empty)
{
    // Copy the value in an opaque buffer so we can compare at the end of the function if it changed at all.
    const ImGuiDataTypeInfo* type_info = DataTypeGetInfo(data_type);
    ImGuiDataTypeStorage data_backup;
    memcpy(&data_backup, p_data, type_info->Size);

    while (ImCharIsBlankA(*buf))
        buf++;
    if (!buf[0])
    {
        if (p_data_when_empty != NULL)
        {
            memcpy(p_data, p_data_when_empty, type_info->Size);
            return memcmp(&data_backup, p_data, type_info->Size) != 0;
        }
        return false;
    }

    // Sanitize format
    // - For float/double we have to ignore format with precision (e.g. "%.2f") because sscanf doesn't take them in, so force them into %f and %lf
    // - In theory could treat empty format as using default, but this would only cover rare/bizarre case of using InputScalar() + integer + format string without %.
    char format_sanitized[32];
    char format_with_n[64];
    if (data_type == ImGuiDataType_Float || data_type == ImGuiDataType_Double)
        format = type_info->ScanFmt;
    else
        format = ImParseFormatSanitizeForScanning(format, format_sanitized, IM_ARRAYSIZE(format_sanitized));    
    // Add %n to the format so we can get the number of characters sscanf consumed
    ImSnprintf(format_with_n, 64, "%s%%n", format);
    int sN = 0;

    // Small types need a 32-bit buffer to receive the result from scanf()
    // emptyOrWhiteSpace to assert that there's no extra characters after the successful number.
    int v32 = 0;
    if (sscanf(buf, format_with_n, type_info->Size >= 4 ? p_data : &v32, &sN) < 1 ||
        !emptyOrWhiteSpace(buf + sN))
    {
        // expression parser, sscanf did not work
        double exprResult;
        if(igExtEvaluateExpression(buf, &exprResult))
        {
            switch(data_type)
            {
                case ImGuiDataType_S32:
                {
                    int* v = (int*)p_data;
                    *v = (int)exprResult;
                    break;
                }
                case ImGuiDataType_Float:
                {
                    float* v = (float*)p_data;
                    *v = (float)exprResult;
                    break;
                }
                case ImGuiDataType_Double:
                {
                    double* v = (double*)p_data;
                    *v = exprResult;
                }
                default:
                    v32 = (int)exprResult;
                    break;
            }
        }
        else
        {
            return false;
        }
    }
    if (type_info->Size < 4)
    {
        if (data_type == ImGuiDataType_S8)
            *(ImS8*)p_data = (ImS8)ImClamp(v32, (int)IM_S8_MIN, (int)IM_S8_MAX);
        else if (data_type == ImGuiDataType_U8)
            *(ImU8*)p_data = (ImU8)ImClamp(v32, (int)IM_U8_MIN, (int)IM_U8_MAX);
        else if (data_type == ImGuiDataType_S16)
            *(ImS16*)p_data = (ImS16)ImClamp(v32, (int)IM_S16_MIN, (int)IM_S16_MAX);
        else if (data_type == ImGuiDataType_U16)
            *(ImU16*)p_data = (ImU16)ImClamp(v32, (int)IM_U16_MIN, (int)IM_U16_MAX);
        else
            IM_ASSERT(0);
    }

    return memcmp(&data_backup, p_data, type_info->Size) != 0;
}



// Note: p_data, p_step, p_step_fast are _pointers_ to a memory address holding the data. For an Input widget, p_step and p_step_fast are optional.
// Read code of e.g. InputFloat(), InputInt() etc. or examples in 'Demo->Widgets->Data Types' to understand how to use this function directly.
static bool igExtInputScalar(const char* label, ImGuiDataType data_type, void* p_data, const void* p_step, const void* p_step_fast, const char* format, ImGuiInputTextFlags flags)
{
    ImGuiWindow* window = GetCurrentWindow();
    if (window->SkipItems)
        return false;

    ImGuiContext& g = *GImGui;
    ImGuiStyle& style = g.Style;
    IM_ASSERT((flags & ImGuiInputTextFlags_EnterReturnsTrue) == 0); // Not supported by igExtInputScalar(). Please open an issue if you this would be useful to you. Otherwise use IsItemDeactivatedAfterEdit()!

    if (format == NULL)
        format = DataTypeGetInfo(data_type)->PrintFmt;

    void* p_data_default = (g.NextItemData.HasFlags & ImGuiNextItemDataFlags_HasRefVal) ? &g.NextItemData.RefVal : &g.DataTypeZeroValue;

    char buf[64];
    if ((flags & ImGuiInputTextFlags_DisplayEmptyRefVal) && DataTypeCompare(data_type, p_data, p_data_default) == 0)
        buf[0] = 0;
    else
        DataTypeFormatString(buf, IM_ARRAYSIZE(buf), data_type, p_data, format);

    // Disable the MarkItemEdited() call in InputText but keep ImGuiItemStatusFlags_Edited.
    // We call MarkItemEdited() ourselves by comparing the actual data rather than the string.
    g.NextItemData.ItemFlags |= ImGuiItemFlags_NoMarkEdited;
    flags |= ImGuiInputTextFlags_AutoSelectAll | (ImGuiInputTextFlags)ImGuiInputTextFlags_LocalizeDecimalPoint;

    bool value_changed = false;
    if (p_step == NULL)
    {
        if (InputText(label, buf, IM_ARRAYSIZE(buf), flags))
            value_changed = igExtDataTypeApplyFromText(buf, data_type, p_data, format, (flags & ImGuiInputTextFlags_ParseEmptyRefVal) ? p_data_default : NULL);
    }
    else
    {
        const float button_size = GetFrameHeight();

        BeginGroup(); // The only purpose of the group here is to allow the caller to query item data e.g. IsItemActive()
        PushID(label);
        SetNextItemWidth(ImMax(1.0f, CalcItemWidth() - (button_size + style.ItemInnerSpacing.x) * 2));
        if (InputText("", buf, IM_ARRAYSIZE(buf), flags)) // PushId(label) + "" gives us the expected ID from outside point of view
            value_changed = igExtDataTypeApplyFromText(buf, data_type, p_data, format, (flags & ImGuiInputTextFlags_ParseEmptyRefVal) ? p_data_default : NULL);
        IMGUI_TEST_ENGINE_ITEM_INFO(g.LastItemData.ID, label, g.LastItemData.StatusFlags | ImGuiItemStatusFlags_Inputable);

        // Step buttons
        const ImVec2 backup_frame_padding = style.FramePadding;
        style.FramePadding.x = style.FramePadding.y;
        if (flags & ImGuiInputTextFlags_ReadOnly)
            BeginDisabled();
        PushItemFlag(ImGuiItemFlags_ButtonRepeat, true);
        SameLine(0, style.ItemInnerSpacing.x);
        if (ButtonEx("-", ImVec2(button_size, button_size)))
        {
            DataTypeApplyOp(data_type, '-', p_data, p_data, g.IO.KeyCtrl && p_step_fast ? p_step_fast : p_step);
            value_changed = true;
        }
        SameLine(0, style.ItemInnerSpacing.x);
        if (ButtonEx("+", ImVec2(button_size, button_size)))
        {
            DataTypeApplyOp(data_type, '+', p_data, p_data, g.IO.KeyCtrl && p_step_fast ? p_step_fast : p_step);
            value_changed = true;
        }
        PopItemFlag();
        if (flags & ImGuiInputTextFlags_ReadOnly)
            EndDisabled();

        const char* label_end = FindRenderedTextEnd(label);
        if (label != label_end)
        {
            SameLine(0, style.ItemInnerSpacing.x);
            TextEx(label, label_end);
        }
        style.FramePadding = backup_frame_padding;

        PopID();
        EndGroup();
    }

    g.LastItemData.ItemFlags &= ~ImGuiItemFlags_NoMarkEdited;
    if (value_changed)
        MarkItemEdited(g.LastItemData.ID);

    return value_changed;
}

static bool igExtInputScalarN(const char* label, ImGuiDataType data_type, void* p_data, int components, const void* p_step, const void* p_step_fast, const char* format, ImGuiInputTextFlags flags)
{
    ImGuiWindow* window = GetCurrentWindow();
    if (window->SkipItems)
        return false;

    ImGuiContext& g = *GImGui;
    bool value_changed = false;
    BeginGroup();
    PushID(label);
    PushMultiItemsWidths(components, CalcItemWidth());
    size_t type_size = DataTypeGetInfo(data_type)->Size;
    for (int i = 0; i < components; i++)
    {
        PushID(i);
        if (i > 0)
            SameLine(0, g.Style.ItemInnerSpacing.x);
        value_changed |= igExtInputScalar("", data_type, p_data, p_step, p_step_fast, format, flags);
        PopID();
        PopItemWidth();
        p_data = (void*)((char*)p_data + type_size);
    }
    PopID();

    const char* label_end = FindRenderedTextEnd(label);
    if (label != label_end)
    {
        SameLine(0.0f, g.Style.ItemInnerSpacing.x);
        TextEx(label, label_end);
    }

    EndGroup();
    return value_changed;
}

CIMGUI_API int igExtInputFloat(const char* label, float* v, float step, float step_fast, const char* format, int flags)
{
    return igExtInputScalar(label, ImGuiDataType_Float, (void*)v, (void*)(step > 0.0f ? &step : NULL), (void*)(step_fast > 0.0f ? &step_fast : NULL), format, (ImGuiInputTextFlags)flags) ? 1 : 0;
}

CIMGUI_API int igExtInputFloat2(const char* label, float v[2], const char* format, int flags)
{
    return igExtInputScalarN(label, ImGuiDataType_Float, v, 2, NULL, NULL, format, (ImGuiInputTextFlags)flags) ? 1 : 0;
}

CIMGUI_API int igExtInputFloat3(const char* label, float v[3], const char* format, int flags)
{
    return igExtInputScalarN(label, ImGuiDataType_Float, v, 3, NULL, NULL, format, (ImGuiInputTextFlags)flags) ? 1 : 0;
}

CIMGUI_API int igExtInputFloat4(const char* label, float v[4], const char* format, int flags)
{
    return igExtInputScalarN(label, ImGuiDataType_Float, v, 4, NULL, NULL, format, (ImGuiInputTextFlags)flags) ? 1 : 0;
}

CIMGUI_API int igExtInputInt(const char* label, int* v, int step, int step_fast, int flags)
{
    // Hexadecimal input provided as a convenience but the flag name is awkward. Typically you'd use InputText() to parse your own data, if you want to handle prefixes.
    const char* format = (flags & (int)ImGuiInputTextFlags_CharsHexadecimal) ? "%08X" : "%d";
    return igExtInputScalar(label, ImGuiDataType_S32, (void*)v, (void*)(step > 0 ? &step : NULL), (void*)(step_fast > 0 ? &step_fast : NULL), format, (ImGuiInputTextFlags)flags) ? 1 : 0;
}

CIMGUI_API int igExtInputInt2(const char* label, int v[2], int flags)
{
    return igExtInputScalarN(label, ImGuiDataType_S32, v, 2, NULL, NULL, "%d", (ImGuiInputTextFlags)flags) ? 1 : 0;
}

CIMGUI_API int igExtInputInt3(const char* label, int v[3], int flags)
{
    return igExtInputScalarN(label, ImGuiDataType_S32, v, 3, NULL, NULL, "%d", (ImGuiInputTextFlags)flags) ? 1 : 0;
}

CIMGUI_API int igExtInputInt4(const char* label, int v[4], int flags)
{
    return igExtInputScalarN(label, ImGuiDataType_S32, v, 4, NULL, NULL, "%d", (ImGuiInputTextFlags)flags) ? 1 : 0;
}

CIMGUI_API int igExtInputDouble(const char* label, double* v, double step, double step_fast, const char* format, int flags)
{
    return igExtInputScalar(label, ImGuiDataType_Double, (void*)v, (void*)(step > 0.0 ? &step : NULL), (void*)(step_fast > 0.0 ? &step_fast : NULL), format, (ImGuiInputTextFlags)flags) ? 1 : 0;
}
