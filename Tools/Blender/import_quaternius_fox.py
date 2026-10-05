"""Import the CC0 Quaternius fox and export an editable Blender/Unity candidate.

Usage: blender -b --python import_quaternius_fox.py -- input.glb output.blend output.fbx
The original project fox is deliberately left untouched.
"""

import bpy
import sys


args = sys.argv[sys.argv.index("--") + 1:]
source_path, blend_path, fbx_path = args

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=source_path)

print("IMPORTED_OBJECTS", [(obj.name, obj.type) for obj in bpy.data.objects])
print("ACTIONS", [(action.name, int(action.frame_range[0]), int(action.frame_range[1])) for action in bpy.data.actions])
for obj in bpy.data.objects:
    if obj.type == "ARMATURE":
        print("ARMATURE", obj.name, len(obj.data.bones), tuple(round(v, 4) for v in obj.dimensions))
        if obj.animation_data:
            print("NLA", [(track.name, [(strip.name, int(strip.frame_start), int(strip.frame_end)) for strip in track.strips]) for track in obj.animation_data.nla_tracks])

for image in bpy.data.images:
    if image.source == "FILE":
        try:
            image.pack()
        except RuntimeError:
            pass

bpy.ops.wm.save_as_mainfile(filepath=blend_path)
bpy.ops.export_scene.fbx(
    filepath=fbx_path,
    use_selection=False,
    object_types={"ARMATURE", "MESH", "EMPTY"},
    apply_unit_scale=True,
    add_leaf_bones=False,
    bake_anim=True,
    bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False,
    bake_anim_use_all_actions=True,
    path_mode="COPY",
    embed_textures=True,
)
