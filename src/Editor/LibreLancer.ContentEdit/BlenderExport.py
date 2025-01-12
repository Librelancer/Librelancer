import bpy
import re

GLTF_FILEPATH={0}
BLEND_FILEPATH={1}
HARDPOINT_SCALE={2}

def add_gizmo_mesh(parent, scale):
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
    mesh_data = bpy.data.meshes.new("HardpointGizmo.mesh")
    mesh_data.from_pydata(positions, [], faces)
    mesh_data.update()
    gz = bpy.data.objects.new("HardpointGizmo", mesh_data)
    gz["export_ignore"] = True
    gz.display_type = "WIRE"
    gz.parent = parent
    gz.hide_render = True
    gz.hide_select = True
    bpy.context.scene.collection.objects.link(gz)

bpy.ops.wm.read_homefile(use_empty=True)
bpy.ops.import_scene.gltf(filepath=GLTF_FILEPATH)

for obj in bpy.context.scene.objects:
    if 'hardpoint' in obj:
        obj.empty_display_size = HARDPOINT_SCALE * 0.3
        obj.empty_display_type = 'CUBE'
        add_gizmo_mesh(obj, HARDPOINT_SCALE)
    elif 'construct' in obj:
        obj.empty_display_size = 0.5
        obj.empty_display_type = 'CUBE'
    elif 'hull' in obj or re.search(r'\$lod\d$', obj.name):
        obj.hide_set(True)
        obj.hide_render = True

bpy.ops.wm.save_as_mainfile(filepath=BLEND_FILEPATH)
