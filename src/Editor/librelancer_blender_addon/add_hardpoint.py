import bpy
import bmesh
from bpy_extras.object_utils import AddObjectHelper

from bpy.props import (
    FloatProperty,
)

def gizmo_mesh(scale):
    baseSize = 0.33 * scale
    arrowSize = 0.62 * scale
    arrowLength = arrowSize * 3
    arrowOffset = baseSize + arrowSize if baseSize > 0 else 0
    faces = [
        [0, 1, 2], [0, 2, 3], [0, 3, 4],
        [0, 4, 1], [1, 5, 2], [2, 5, 3],
        [3, 5, 4], [4, 5, 1], [6, 7, 8],
        [6, 8, 9], [6, 9, 10], [6, 10, 7],
        [7, 11, 8], [8, 11, 9], [9, 11, 10],
        [10, 11, 7]
    ]
    positions = [
        [0, 0, baseSize], [-baseSize, -baseSize, 0],
        [baseSize, -baseSize, 0], [baseSize, baseSize, 0],
        [-baseSize, baseSize, 0], [0, 0, 0],
        [0, arrowLength, arrowOffset], [-arrowSize, 0, arrowOffset],
        [0, 0, arrowOffset + arrowSize], [arrowSize, 0, arrowOffset],
        [0, 0, arrowOffset - arrowSize], [0, -arrowSize, arrowOffset]
    ]
    return positions, faces


class AddHardpoint(bpy.types.Operator, AddObjectHelper):
    """Add a hardpoint"""
    bl_idname = "mesh.primitive_hardpoint_add"
    bl_label = "Add Hardpoint"
    bl_options = {'REGISTER', 'UNDO'}

    scale: FloatProperty(
        name="Scale",
        description="Hardpoint Scale",
        min=0.01, max=100.0,
        default=1.0,
    )

    def execute(self, context):

        verts_loc, faces = gizmo_mesh(
            self.scale,
        )

        mesh = bpy.data.meshes.new("HardpointGizmo")

        bm = bmesh.new()

        for v_co in verts_loc:
            bm.verts.new(v_co)

        bm.verts.ensure_lookup_table()
        for f_idx in faces:
            bm.faces.new([bm.verts[i] for i in f_idx])

        bm.to_mesh(mesh)
        mesh.update()

        # add the mesh as an object into the scene with this utility module
        from bpy_extras import object_utils

        parent = object_utils.object_data_add(context, None, operator=self, name='Hardpoint')
        parent["hardpoint"] = True
        parent["hptype"] = "fix"
        parent.empty_display_size = 0.3 * self.scale
        parent.empty_display_type = 'CUBE'
        child = object_utils.object_data_add(context, mesh, None)
        child.parent = parent
        child["export_ignore"] = True
        child.hide_render = True
        child.hide_select = True
        child.display_type = "WIRE"
        return {'FINISHED'}


def menu_func(self, context):
    self.layout.operator(AddHardpoint.bl_idname, icon='MESH_CUBE')


# Register and add to the "add mesh" menu (required to use F3 search "Add Hardpoint" for quick access).
def register():
    bpy.utils.register_class(AddHardpoint)
    bpy.types.VIEW3D_MT_add.append(menu_func)


def unregister():
    bpy.utils.unregister_class(AddHardpoint)
    bpy.types.VIEW3D_MT_add.remove(menu_func)


if __name__ == "__main__":
    register()

    # test call
    bpy.ops.mesh.primitive_hardpoint_add()
