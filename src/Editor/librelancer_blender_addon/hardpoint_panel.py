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
            row = layout.row()
            row.prop(context.scene, "hardpoint_prefix")
            row = layout.row()
            row.prop(context.scene, "hardpoint_middle")
            row = layout.row()
            row.prop(context.scene, "hardpoint_start_num")
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
    bpy.types.Scene.hardpoint_prefix = bpy.props.StringProperty(name="Hardpoint Prefix", default="Hp")
    bpy.types.Scene.hardpoint_middle = bpy.props.StringProperty(name="Middle Name", default="")
    bpy.types.Scene.hardpoint_hptype = bpy.props.StringProperty(default="fix")
    bpy.types.Scene.hardpoint_naming = bpy.props.StringProperty(default="number")
    bpy.types.Scene.hardpoint_start_num = bpy.props.IntProperty(name="Start Number", default=1, min=0)
    bpy.types.Scene.hardpoint_current_num = bpy.props.IntProperty(default=1)
    bpy.types.Scene.pending_location = bpy.props.FloatVectorProperty()
    bpy.types.Scene.pending_normal = bpy.props.FloatVectorProperty()
    bpy.types.Scene.pending_face_index = bpy.props.IntProperty()
    bpy.types.Scene.pending_object = bpy.props.PointerProperty(type=bpy.types.Object)
    bpy.types.Scene.view_direction = bpy.props.FloatVectorProperty()
    bpy.types.Scene.has_pending = bpy.props.BoolProperty(default=False)
    bpy.types.Scene.has_floor_pending = bpy.props.BoolProperty(default=False)
    bpy.types.Scene.place_done = bpy.props.BoolProperty(default=False)
    bpy.types.Scene.last_snap_type = bpy.props.StringProperty(default="none")
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
