import bpy


class HardpointPanel(bpy.types.Panel):
    """Creates a Panel in the Object properties window"""
    bl_label = "Hardpoint Properties"
    bl_idname = "OBJECT_PT_hardpoints"
    bl_space_type = 'PROPERTIES'
    bl_region_type = 'WINDOW'
    bl_context = "object"

    def draw(self, context):
        layout = self.layout

        obj = context.object
        
        if not 'hardpoint' in obj:
            row = layout.row()
            row.label(text="Not a hardpoint")
            return

        row = layout.row()
        row.prop(obj, "hptype")

        if not 'hptype' in obj:
            return

        if obj['hptype'] == 'rev':
            row = layout.row()
            row.prop(obj, "min")
            row.prop(obj, "max")
            row = layout.row()
            row.prop(obj, "axis")

def get_hptype(self):
    if 'hptype' in self:
        return 1 if self['hptype'] == 'rev' else 0
    else:
        return 0

def set_hptype(self, value):
    self['hptype'] = 'rev' if value == 1 else 'fix'

def register():
    bpy.types.Object.min = bpy.props.FloatProperty(name="Minimum")
    bpy.types.Object.max = bpy.props.FloatProperty(name="Maximum")
    bpy.types.Object.hardpoint = bpy.props.BoolProperty(name="Hardpoint")
    bpy.types.Object.axis = bpy.props.FloatVectorProperty(name="Axis", default=(0.0, 1.0, 0.0))
    bpy.types.Object.hptype = bpy.props.EnumProperty(
        items = [('fix', 'Fixed', '', '', 0),
                 ('rev', 'Revolute', '', '', 1)],
        name = "Kind",
        default = 'fix',
        set = set_hptype,
        get = get_hptype)
    bpy.utils.register_class(HardpointPanel)

def unregister():
    bpy.utils.unregister_class(HardpointPanel)

if __name__ == "__main__":
    register()
