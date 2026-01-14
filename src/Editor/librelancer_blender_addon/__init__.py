bl_info = {
    "name": "Librelancer Utilities",
    "blender": (4, 5, 2),
    "category": "Object",
}

from . import add_hardpoint
from . import hardpoint_panel

def register():
    add_hardpoint.register()
    hardpoint_panel.register()

def unregister():
    add_hardpoint.unregister()
    hardpoint_panel.unregister()

if __name__ == "__main__":
    register()
