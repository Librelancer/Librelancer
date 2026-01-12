import bpy
import bmesh
from bpy_extras.object_utils import AddObjectHelper
from bpy_extras.view3d_utils import region_2d_to_origin_3d, region_2d_to_vector_3d
from mathutils import Matrix, Vector

from bpy.props import (
    FloatProperty,
    StringProperty,
    EnumProperty,
)

hardpoint_items = [
    ("HpBayDoor01", "HpBayDoor01", ""),
    ("HpBayDoor02", "HpBayDoor02", ""),
    ("HpCM", "HpCM", ""),
    ("HpCockpit", "HpCockpit", ""),
    ("HpContrail", "HpContrail", ""),
    ("HpDockCam", "HpDockCam", ""),
    ("HpDockLight", "HpDockLight", ""),
    ("HpDockMount", "HpDockMount", ""),
    ("HpDockPoint", "HpDockPoint", ""),
    ("HpEngine", "HpEngine", ""),
    ("HpFX", "HpFX", ""),
    ("HpHeadLight", "HpHeadLight", ""),
    ("HpLaunchCam", "HpLaunchCam", ""),
    ("HpMine", "HpMine", ""),
    ("HpMount", "HpMount", ""),
    ("HpPilot", "HpPilot", ""),
    ("HpRunningLight", "HpRunningLight", ""),
    ("HpShield", "HpShield", ""),
    ("HpSpecialEquipment", "HpSpecialEquipment", ""),
    ("HpThruster", "HpThruster", ""),
    ("HpTractor_Source", "HpTractor_Source", ""),
    ("HpFire", "HpFire", ""),
    ("HpConnect", "HpConnect", ""),
    ("HpTorpedo", "HpTorpedo", ""),
    ("HpTurret", "HpTurret", ""),
    ("HpWeapon", "HpWeapon", ""),
]

hardpoint_types = {
    "HpBayDoor01": ("fix", "none"),
    "HpBayDoor02": ("fix", "none"),
    "HpCM": ("fix", "number"),
    "HpCockpit": ("fix", "none"),
    "HpContrail": ("fix", "number"),
    "HpDockCam": ("fix", "letter"),
    "HpDockLight": ("fix", "number"),
    "HpDockMount": ("fix", "letter"),
    "HpDockPoint": ("fix", "letter"),
    "HpEngine": ("fix", "number"),
    "HpFX": ("fix", "number"),
    "HpHeadLight": ("fix", "number"),
    "HpLaunchCam": ("fix", "letter"),
    "HpMine": ("fix", "number"),
    "HpMount": ("fix", "none"),
    "HpPilot": ("fix", "none"),
    "HpRunningLight": ("fix", "number"),
    "HpShield": ("fix", "number"),
    "HpSpecialEquipment": ("fix", "number"),
    "HpThruster": ("fix", "number"),
    "HpTractor_Source": ("fix", "none"),
    "HpFire": ("fix", "number"),
    "HpConnect": ("fix", "none"),
    "HpTorpedo": ("rev", "number"),
    "HpTurret": ("rev", "number"),
    "HpWeapon": ("rev", "number"),
}

def get_next_suffix(context, prefix, middle, naming):
    if naming == 'none':
        return ''
    base = prefix + middle
    existing = [obj.name for obj in context.scene.objects if obj.name.startswith(base)]
    if naming == 'number':
        numbers = []
        for name in existing:
            suffix = name[len(base):]
            if suffix.isdigit():
                numbers.append(int(suffix))
        next_num = max(numbers) + 1 if numbers else 1
        return str(next_num).zfill(2)
    elif naming == 'letter':
        letters = []
        for name in existing:
            suffix = name[len(base):]
            if len(suffix) == 1 and suffix.isalpha():
                letters.append(ord(suffix.upper()) - ord('A') + 1)
        next_num = max(letters) + 1 if letters else 1
        return chr(ord('A') + next_num - 1)

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


class SetMiddleName(bpy.types.Operator):
    """Set the hardpoint type or custom name"""
    bl_idname = "hardpoint.set_middle_name"
    bl_label = "Set Hardpoint"
    bl_options = {'REGISTER', 'UNDO'}

    mode: EnumProperty(items=[('type', 'Select Hardpoint Type', ''), ('custom', 'Enter Custom Middle Name', ''), ('preset', 'Place Preset', '')], default='type', name="Mode")
    selected_name: EnumProperty(items=hardpoint_items, name="Hardpoint Type")
    middle_name: StringProperty(name="Middle Name", default="")
    preset: EnumProperty(items=[('docking_berth', 'Docking Berth', '')], name="Preset")
    snap_type: EnumProperty(items=[('none', 'None', ''), ('face_center', 'Face Center', ''), ('nearest_vertex', 'Nearest Vertex', '')], default='none', name="Snap Type")

    def draw(self, context):
        layout = self.layout
        layout.prop(self, "mode")
        if self.mode == 'type':
            layout.prop(self, "selected_name")
        elif self.mode == 'custom':
            layout.prop(self, "middle_name")
        elif self.mode == 'preset':
            layout.prop(self, "preset")
        if context.scene.has_pending:
            layout.prop(self, "snap_type")

    def execute(self, context):
        
        # Store the last used snap type
        context.scene.last_snap_type = self.snap_type
        
        if self.mode == 'preset':
            if self.preset == 'docking_berth':
                self.place_docking_berth_preset(context)
            context.scene.has_pending = False
            context.scene.has_floor_pending = False
            context.scene.place_done = True
            return {'FINISHED'}
        elif self.mode == 'custom':
            context.scene.hardpoint_middle = self.middle_name
            context.scene.hardpoint_hptype = 'fix'
            context.scene.hardpoint_naming = 'number'
        else:
            selected = self.selected_name
            hptype, naming = hardpoint_types[selected]
            context.scene.hardpoint_hptype = hptype
            context.scene.hardpoint_naming = naming
            context.scene.hardpoint_middle = selected.replace('Hp', '')
         
        if context.scene.has_pending:
            location = context.scene.pending_location
            normal = context.scene.pending_normal
            if self.snap_type != 'none' and context.scene.pending_object:
                obj = context.scene.pending_object
                face = obj.data.polygons[context.scene.pending_face_index]
                if self.snap_type == 'face_center':
                    center_local = face.center
                    location = obj.matrix_world @ center_local
                elif self.snap_type == 'nearest_vertex':
                    hit_location = Vector(context.scene.pending_location)
                    vertices = [obj.data.vertices[i] for i in face.vertices]
                    distances = [(v.co, (obj.matrix_world @ v.co - hit_location).length) for v in vertices]
                    closest_co = min(distances, key=lambda x: x[1])[0]
                    location = obj.matrix_world @ closest_co
                normal_local = face.normal
                normal = (obj.matrix_world.to_3x3() @ normal_local).normalized()
        elif context.scene.has_floor_pending:
            location = context.scene.pending_location
            normal = context.scene.pending_normal
        else:
            return {'CANCELLED'} 
         
        # Calculate rotation
        z_axis = Vector((0, 0, 1))
        rot_matrix = z_axis.rotation_difference(normal).to_matrix().to_4x4()
        rotation_euler = rot_matrix.to_euler()
        # Generate name
        suffix = get_next_suffix(context, context.scene.hardpoint_prefix, context.scene.hardpoint_middle, context.scene.hardpoint_naming)
        name = f"{context.scene.hardpoint_prefix}{context.scene.hardpoint_middle}{suffix}"
        # Add parent empty
        parent = bpy.data.objects.new(name, None)
        parent.location = location
        parent.rotation_euler = rotation_euler
        parent["hardpoint"] = True
        parent["hptype"] = context.scene.hardpoint_hptype
        parent.empty_display_size = 0.3 * 1.0
        parent.empty_display_type = 'CUBE'
        context.scene.collection.objects.link(parent)
        # Add child mesh
        verts_loc, faces = gizmo_mesh(1.0)
        mesh = bpy.data.meshes.new("HardpointGizmo")
        bm = bmesh.new()
        for v_co in verts_loc:
            bm.verts.new(v_co)
        bm.verts.ensure_lookup_table()
        for f_idx in faces:
            bm.faces.new([bm.verts[i] for i in f_idx])
        bm.to_mesh(mesh)
        mesh.update()
        child = bpy.data.objects.new("HardpointGizmo", mesh)
        child.location = location
        child.rotation_euler = rotation_euler
        child.parent = parent
        child["export_ignore"] = True
        child.hide_render = True
        child.hide_select = True
        child.display_type = "WIRE"
        child.location = (0.0, 0.0, 0.0)
        child.rotation_euler = (0.0, 0.0, 0.0)
        child.matrix_parent_inverse = Matrix.Translation((0.0, 0.0, 0.0))
        context.scene.collection.objects.link(child)
        context.scene.has_pending = False
        context.scene.has_floor_pending = False
        context.scene.place_done = True
         
        return {'FINISHED'}

    def place_docking_berth_preset(self, context): # Test, will be here for now, later move to another "presets" file
        # Get base location and normal
        if context.scene.has_pending:
            location = context.scene.pending_location
            normal = context.scene.pending_normal
        elif context.scene.has_floor_pending:
            location = context.scene.pending_location
            normal = context.scene.pending_normal
        else:
            return

        # Find next letter for DockMount
        base = "HpDockMount"
        existing = [obj.name for obj in context.scene.objects if obj.name.startswith(base)]
        letters = []
        for name in existing:
            suffix = name[len(base):]
            if len(suffix) == 1 and suffix.isalpha():
                letters.append(ord(suffix.upper()) - ord('A') + 1)
        next_num = max(letters) + 1 if letters else 1
        letter = chr(ord('A') + next_num - 1)

        # Place HpDockMount at location
        dock_mount = self.place_single_hardpoint(context, location, normal, f"HpDockMount{letter}", "fix")

        # Get the rotation matrix of the first hardpoint
        rot_matrix = dock_mount.rotation_euler.to_matrix()
        
        # Local docking direction is (0, 0, 1) in the hardpoint's local space
        local_docking_dir = Vector((0, -1, 0))
        
        # Transform the local docking direction to world space using the rotation matrix
        docking_dir = rot_matrix @ local_docking_dir
        docking_dir.normalize()

        # Place 1st HpDockPoint
        offset1 = Vector(location) + docking_dir * 15.0
        self.place_single_hardpoint(context, offset1, normal, f"HpDockPoint{letter}01", "fix")

        # Place another HpDockPoint at location + 4.5 * docking_dir
        offset2 = Vector(location) + docking_dir * 38.5
        self.place_single_hardpoint(context, offset2, normal, f"HpDockPoint{letter}02", "fix")

    def place_single_hardpoint(self, context, location, normal, name, hptype):
        # Calculate rotation
        z_axis = Vector((0, 0, 1))
        rot_matrix = z_axis.rotation_difference(normal).to_matrix().to_4x4()
        rotation_euler = rot_matrix.to_euler()
        # Add parent empty
        parent = bpy.data.objects.new(name, None)
        parent.location = location
        parent.rotation_euler = rotation_euler
        parent["hardpoint"] = True
        parent["hptype"] = hptype
        parent.empty_display_size = 0.3 * 1.0
        parent.empty_display_type = 'CUBE'
        context.scene.collection.objects.link(parent)
        # Add child mesh
        verts_loc, faces = gizmo_mesh(1.0)
        mesh = bpy.data.meshes.new("HardpointGizmo")
        bm = bmesh.new()
        for v_co in verts_loc:
            bm.verts.new(v_co)
        bm.verts.ensure_lookup_table()
        for f_idx in faces:
            bm.faces.new([bm.verts[i] for i in f_idx])
        bm.to_mesh(mesh)
        mesh.update()
        child = bpy.data.objects.new("HardpointGizmo", mesh)
        child.location = location
        child.rotation_euler = rotation_euler
        child.parent = parent
        child["export_ignore"] = True
        child.hide_render = True
        child.hide_select = True
        child.display_type = "WIRE"
        child.location = (0.0, 0.0, 0.0)
        child.rotation_euler = (0.0, 0.0, 0.0)
        child.matrix_parent_inverse = Matrix.Translation((0.0, 0.0, 0.0))
        context.scene.collection.objects.link(child)
        return parent

    def invoke(self, context, event):
        return context.window_manager.invoke_props_dialog(self)


class AddHardpoint(bpy.types.Operator):
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

    def invoke(self, context, event):
        context.scene.place_done = False
        context.workspace.status_text_set("Adding hardpoint: Click to snap to face, Shift+click to set name, Ctrl+click to place at center")
        context.window_manager.modal_handler_add(self)
        return {'RUNNING_MODAL'}

    def modal(self, context, event):
        if context.scene.place_done:
            context.scene.place_done = False
            context.workspace.status_text_set(None)
            return {'FINISHED'}
        if event.type == 'LEFTMOUSE' and event.value == 'PRESS':
            if event.ctrl:
                # Place at world center
                self.place_hardpoint(context, (0.0, 0.0, 0.0), (0.0, 0.0, 1.0))
                context.workspace.status_text_set(None)
                return {'FINISHED'}
            elif event.shift:
                # Raycast first
                region = context.region
                rv3d = context.region_data
                coord = event.mouse_region_x, event.mouse_region_y
                view_vector = region_2d_to_vector_3d(region, rv3d, coord)
                ray_origin = region_2d_to_origin_3d(region, rv3d, coord)
                depsgraph = context.evaluated_depsgraph_get()
                result = depsgraph.scene.ray_cast(depsgraph, ray_origin, view_vector)
                if result[0]:  # Hit
                    hit_location = result[1]
                    hit_normal = result[2]
                    hit_face_index = result[3]
                    hit_object = result[4]
                    context.scene.pending_location = hit_location
                    context.scene.pending_normal = hit_normal
                    context.scene.pending_face_index = hit_face_index
                    context.scene.pending_object = hit_object
                    context.scene.view_direction = view_vector
                    context.scene.has_pending = True
                    context.scene.has_floor_pending = False
                else:
                    # Snap to floor
                    origin = region_2d_to_origin_3d(region, rv3d, coord)
                    direction = region_2d_to_vector_3d(region, rv3d, coord)
                    if direction.z != 0:
                        t = -origin.z / direction.z
                        if t > 0:
                            floor_location = origin + t * direction
                            context.scene.pending_location = floor_location
                            context.scene.pending_normal = (0.0, 0.0, 1.0)
                            context.scene.view_direction = view_vector
                            context.scene.has_pending = False
                            context.scene.has_floor_pending = True
                        else:
                            context.workspace.status_text_set(None)
                            return {'CANCELLED'}
                    else:
                        context.workspace.status_text_set(None)
                        return {'CANCELLED'}
                # Set middle name
                bpy.ops.hardpoint.set_middle_name('INVOKE_DEFAULT')
                return {'RUNNING_MODAL'}
            else:
                # Raycast from mouse position
                region = context.region
                rv3d = context.region_data
                coord = event.mouse_region_x, event.mouse_region_y
                view_vector = region_2d_to_vector_3d(region, rv3d, coord)
                ray_origin = region_2d_to_origin_3d(region, rv3d, coord)
                depsgraph = context.evaluated_depsgraph_get()
                result = depsgraph.scene.ray_cast(depsgraph, ray_origin, view_vector)
                if result[0]:  # Hit
                    hit_location = result[1]
                    hit_normal = result[2]
                    hit_object = result[4]
                    hit_face_index = result[3]
                    
                    # Check if there is a pending snap type from the last operation
                    if hasattr(context.scene, 'last_snap_type') and context.scene.last_snap_type != 'none':
                        face = hit_object.data.polygons[hit_face_index]
                        if context.scene.last_snap_type == 'face_center':
                            center_local = face.center
                            hit_location = hit_object.matrix_world @ center_local
                        elif context.scene.last_snap_type == 'nearest_vertex':
                            hit_location_vec = Vector(hit_location)
                            vertices = [hit_object.data.vertices[i] for i in face.vertices]
                            distances = [(v.co, (hit_object.matrix_world @ v.co - hit_location_vec).length) for v in vertices]
                            closest_co = min(distances, key=lambda x: x[1])[0]
                            hit_location = hit_object.matrix_world @ closest_co
                    
                    self.place_hardpoint(context, hit_location, hit_normal)
                    context.workspace.status_text_set(None)
                    return {'FINISHED'}
                else:
                    context.workspace.status_text_set(None)
                    return {'CANCELLED'}
        elif event.type in {'RIGHTMOUSE', 'ESC'}:
            context.workspace.status_text_set(None)
            return {'CANCELLED'}
        return {'RUNNING_MODAL'}

    def place_hardpoint(self, context, location, normal):
        verts_loc, faces = gizmo_mesh(self.scale)
        mesh = bpy.data.meshes.new("HardpointGizmo")
        bm = bmesh.new()
        for v_co in verts_loc:
            bm.verts.new(v_co)
        bm.verts.ensure_lookup_table()
        for f_idx in faces:
            bm.faces.new([bm.verts[i] for i in f_idx])
        bm.to_mesh(mesh)
        mesh.update()

        # Calculate rotation to align Z with normal
        z_axis = Vector((0, 0, 1))
        rot_matrix = z_axis.rotation_difference(normal).to_matrix().to_4x4()
        rotation_euler = rot_matrix.to_euler()

        # Generate name
        suffix = get_next_suffix(context, context.scene.hardpoint_prefix, context.scene.hardpoint_middle, context.scene.hardpoint_naming)
        name = f"{context.scene.hardpoint_prefix}{context.scene.hardpoint_middle}{suffix}"

        # Add parent empty
        parent = bpy.data.objects.new(name, None)
        parent.location = location
        parent.rotation_euler = rotation_euler
        parent["hardpoint"] = True
        parent["hptype"] = "fix"
        parent.empty_display_size = 0.3 * self.scale
        parent.empty_display_type = 'CUBE'
        context.scene.collection.objects.link(parent)

        # Add child mesh
        child = bpy.data.objects.new("HardpointGizmo", mesh)
        child.location = location
        child.rotation_euler = rotation_euler
        child.parent = parent
        child["export_ignore"] = True
        child.hide_render = True
        child.hide_select = True
        child.display_type = "WIRE"
        child.location = (0.0, 0.0, 0.0)
        child.rotation_euler = (0.0, 0.0, 0.0)
        child.matrix_parent_inverse = Matrix.Translation((0.0, 0.0, 0.0))
        context.scene.collection.objects.link(child)
        return {'FINISHED'}


def menu_func(self, context):
    self.layout.operator(AddHardpoint.bl_idname, icon='MESH_CUBE')


# Register and add to the "add mesh" menu (required to use F3 search "Add Hardpoint" for quick access).
def register():
    bpy.utils.register_class(SetMiddleName)
    bpy.utils.register_class(AddHardpoint)
    bpy.types.VIEW3D_MT_add.append(menu_func)
    # Add keymap for the AddHardpoint operator
    wm = bpy.context.window_manager
    kc = wm.keyconfigs.addon
    if kc:
        km = kc.keymaps.new(name='3D View', space_type='VIEW_3D')
        kmi = km.keymap_items.new(AddHardpoint.bl_idname, 'H', 'PRESS', ctrl=True, shift=True)


def unregister():
    bpy.utils.unregister_class(SetMiddleName)
    bpy.utils.unregister_class(AddHardpoint)
    bpy.types.VIEW3D_MT_add.remove(menu_func)
    # Remove keymap for the AddHardpoint operator
    wm = bpy.context.window_manager
    kc = wm.keyconfigs.addon
    if kc:
        km = kc.keymaps.get('3D View')
        if km:
            for kmi in km.keymap_items:
                if kmi.idname == AddHardpoint.bl_idname:
                    km.keymap_items.remove(kmi)
                    break


if __name__ == "__main__":
    register()

    # test call
    bpy.ops.mesh.primitive_hardpoint_add()
