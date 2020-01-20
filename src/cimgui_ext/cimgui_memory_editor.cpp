#include "cimgui_ext.h"
#include "imgui.h"
#include "imgui_memory_editor.h"


IGEXPORT memoryedit_t igExtMemoryEditInit()
{
	return (memoryedit_t)(new MemoryEditor);
}

IGEXPORT void igExtMemoryEditDrawContents(memoryedit_t memedit, void *mem_data_void_ptr, size_t mem_size, size_t base_display_addr)
{
	MemoryEditor* object = (MemoryEditor*)memedit;
	object->DrawContents(mem_data_void_ptr, mem_size, base_display_addr);
}

IGEXPORT void igExtMemoryEditFree(memoryedit_t memedit)
{
	delete (MemoryEditor*)memedit;
}
