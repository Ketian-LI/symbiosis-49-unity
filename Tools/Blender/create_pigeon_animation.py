"""Build and render the SYMBIOSIS: 49 pigeon animation prototype.

Run with Blender 5.x:
    blender --background --python Tools/Blender/create_pigeon_animation.py

Outputs:
    SourceAssets/Blender/SYMBIOSIS_49_Pigeon_v02.blend
    Assets/Resources/Animals/Pigeon/Pigeon_Animated_v02.fbx
    Previews/PigeonBlender/frames/frame_####.png
    Previews/PigeonBlender/PigeonBlenderAnimation_v02.png
"""

import math
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[2]
BLEND_PATH = PROJECT_ROOT / "SourceAssets" / "Blender" / "SYMBIOSIS_49_Pigeon_v02.blend"
FBX_PATH = PROJECT_ROOT / "Assets" / "Resources" / "Animals" / "Pigeon" / "Pigeon_Animated_v02.fbx"
PREVIEW_DIR = PROJECT_ROOT / "Previews" / "PigeonBlender"
FRAMES_DIR = PREVIEW_DIR / "frames"
HERO_PATH = PREVIEW_DIR / "PigeonBlenderAnimation_v02.png"

for directory in (BLEND_PATH.parent, FBX_PATH.parent, PREVIEW_DIR, FRAMES_DIR):
    directory.mkdir(parents=True, exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.armatures, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def hex_color(value):
    value = value.lstrip("#")
    srgb = tuple(int(value[index:index + 2], 16) / 255.0 for index in (0, 2, 4))
    linear = tuple(
        channel / 12.92 if channel <= 0.04045 else ((channel + 0.055) / 1.055) ** 2.4
        for channel in srgb
    )
    return linear + (1.0,)


def create_paper_material(name, color, roughness=0.92, bump_strength=0.08):
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()

    output = nodes.new("ShaderNodeOutputMaterial")
    principled = nodes.new("ShaderNodeBsdfPrincipled")
    principled.inputs["Base Color"].default_value = color
    principled.inputs["Roughness"].default_value = roughness
    principled.inputs["Metallic"].default_value = 0.0

    texture_coordinate = nodes.new("ShaderNodeTexCoord")
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 24.0
    noise.inputs["Detail"].default_value = 2.2
    noise.inputs["Roughness"].default_value = 0.68
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = bump_strength
    bump.inputs["Distance"].default_value = 0.035

    links.new(texture_coordinate.outputs["Generated"], noise.inputs["Vector"])
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], principled.inputs["Normal"])
    links.new(principled.outputs["BSDF"], output.inputs["Surface"])
    return material


def assign_material(obj, material):
    if obj.data and hasattr(obj.data, "materials"):
        obj.data.materials.append(material)


def apply_scale(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.select_set(False)


def add_ico(name, location, scale, material, subdivisions=2, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1.0, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    apply_scale(obj)
    assign_material(obj, material)
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
    return obj


def add_cube(name, location, scale, material, rotation=(0.0, 0.0, 0.0), bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    apply_scale(obj)
    if bevel > 0.0:
        modifier = obj.modifiers.new("Soft paper edge", "BEVEL")
        modifier.width = bevel
        modifier.segments = 1
    assign_material(obj, material)
    return obj


def add_cylinder_between(name, start, end, radius, material, vertices=7):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    midpoint = (start + end) * 0.5
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=direction.length, location=midpoint)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    obj.rotation_mode = "XYZ"
    assign_material(obj, material)
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
    return obj


def add_wedge(name, base_center, tip, base_width, base_height, material):
    base = Vector(base_center)
    tip = Vector(tip)
    half_width = base_width * 0.5
    half_height = base_height * 0.5
    vertices = [
        (base.x, base.y - half_width, base.z - half_height),
        (base.x, base.y + half_width, base.z - half_height),
        (base.x, base.y + half_width, base.z + half_height),
        (base.x, base.y - half_width, base.z + half_height),
        (tip.x, tip.y - half_width * 0.16, tip.z),
        (tip.x, tip.y + half_width * 0.16, tip.z),
    ]
    faces = [
        (0, 1, 2, 3),
        (0, 4, 5, 1),
        (1, 5, 2),
        (2, 5, 4, 3),
        (3, 4, 0),
    ]
    mesh = bpy.data.meshes.new(f"{name} Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    assign_material(obj, material)
    return obj


def add_tapered_feather(name, start, end, width, thickness, material):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    flat_direction = Vector((direction.x, direction.y, 0.0)).normalized()
    side = Vector((-flat_direction.y, flat_direction.x, 0.0))
    start_side = side * width * 0.5
    end_side = side * width * 0.16
    up = Vector((0.0, 0.0, thickness * 0.5))
    vertices = [
        start - start_side - up,
        start + start_side - up,
        start + start_side + up,
        start - start_side + up,
        end - end_side - up,
        end + end_side - up,
        end + end_side + up,
        end - end_side + up,
    ]
    faces = [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ]
    mesh = bpy.data.meshes.new(f"{name} Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    assign_material(obj, material)
    return obj


def add_wing_band(name, center, width, height, depth, material, slope=0.0):
    """Add a thin, slanted paper strip to the outer face of a folded wing."""
    center = Vector(center)
    half_width = width * 0.5
    half_height = height * 0.5
    half_depth = depth * 0.5
    corners = [
        Vector((center.x + half_width, center.y, center.z + half_height + slope)),
        Vector((center.x - half_width, center.y, center.z + half_height - slope)),
        Vector((center.x - half_width, center.y, center.z - half_height - slope)),
        Vector((center.x + half_width, center.y, center.z - half_height + slope)),
    ]
    normal = Vector((0.0, half_depth, 0.0))
    vertices = [tuple(corner - normal) for corner in corners] + [tuple(corner + normal) for corner in corners]
    faces = [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ]
    mesh = bpy.data.meshes.new(f"{name} Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    assign_material(obj, material)
    return obj


def add_neck_feather(name, location, scale, material, angle=0.0):
    obj = add_ico(name, location, scale, material, subdivisions=1, rotation=(0.0, math.radians(angle), 0.0))
    return obj


def bone_parent(obj, armature, bone_name):
    world_matrix = obj.matrix_world.copy()
    obj.parent = armature
    obj.parent_type = "BONE"
    obj.parent_bone = bone_name
    obj.matrix_world = world_matrix


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def build_pigeon():
    body_grey = create_paper_material("Body Grey", hex_color("747C82"))
    chest_grey = create_paper_material("Chest Grey", hex_color("A9ABAA"))
    wing_grey = create_paper_material("Wing Silver", hex_color("AEB5BB"))
    wing_mid = create_paper_material("Wing Mid Grey", hex_color("858D94"))
    wing_dark = create_paper_material("Wing Charcoal", hex_color("353B42"))
    tail_grey = create_paper_material("Tail Slate", hex_color("555E65"))
    neck_green = create_paper_material("Neck Emerald", hex_color("38756C"), bump_strength=0.06)
    neck_purple = create_paper_material("Neck Violet", hex_color("705779"), bump_strength=0.06)
    beak_dark = create_paper_material("Beak Charcoal", hex_color("2A2C30"), bump_strength=0.02)
    cere_pale = create_paper_material("Pale Cere", hex_color("D9D1C4"), bump_strength=0.02)
    leg_rose = create_paper_material("Leg Rose", hex_color("C76568"), bump_strength=0.03)
    eye_orange = create_paper_material("Eye Orange", hex_color("D85A25"), roughness=0.55, bump_strength=0.0)
    pupil_dark = create_paper_material("Eye Pupil", hex_color("111316"), roughness=0.4, bump_strength=0.0)

    body_objects = []
    head_objects = []
    left_wing_objects = []
    right_wing_objects = []
    left_primary_objects = []
    right_primary_objects = []
    left_leg_objects = []
    right_leg_objects = []

    body_objects.append(add_ico("Body", (-0.12, 0.0, 0.90), (0.93, 0.51, 0.56), body_grey, subdivisions=2, rotation=(0.0, math.radians(-4.0), 0.0)))
    body_objects.append(add_ico("Chest", (0.38, 0.0, 1.00), (0.58, 0.47, 0.59), chest_grey, subdivisions=2, rotation=(0.0, math.radians(-8.0), 0.0)))
    body_objects.append(add_ico("Rump", (-0.67, 0.0, 0.88), (0.42, 0.42, 0.35), wing_mid, subdivisions=1, rotation=(0.0, math.radians(5.0), 0.0)))

    tail_specs = (
        (-0.18, -1.48, -0.23, 0.73),
        (-0.09, -1.52, -0.11, 0.70),
        (0.00, -1.55, 0.00, 0.69),
        (0.09, -1.52, 0.11, 0.70),
        (0.18, -1.48, 0.23, 0.73),
    )
    for index, (start_y, end_x, end_y, end_z) in enumerate(tail_specs):
        start = (-0.66, start_y, 0.84 + abs(start_y) * 0.10)
        end = (end_x, end_y, end_z)
        material = tail_grey if index in (1, 2, 3) else wing_dark
        body_objects.append(add_tapered_feather(f"Tail Feather {index + 1}", start, end, 0.22, 0.048, material))

    head_objects.append(add_ico("Neck Core", (0.44, 0.0, 1.32), (0.40, 0.37, 0.51), neck_green, subdivisions=2, rotation=(0.0, math.radians(-10.0), 0.0)))
    head_objects.append(add_ico("Head", (0.75, 0.0, 1.61), (0.31, 0.29, 0.30), body_grey, subdivisions=2, rotation=(0.0, math.radians(-5.0), 0.0)))
    head_objects.append(add_ico("Brow Plane", (0.87, -0.01, 1.67), (0.17, 0.29, 0.15), wing_mid, subdivisions=1, rotation=(0.0, math.radians(-10.0), 0.0)))
    head_objects.append(add_ico("Violet Gorget", (0.40, -0.31, 1.22), (0.30, 0.065, 0.25), neck_purple, subdivisions=1, rotation=(0.0, math.radians(8.0), 0.0)))

    feather_positions = [
        ((0.48, -0.33, 1.44), (0.15, 0.045, 0.16), neck_green, -12.0),
        ((0.34, -0.34, 1.39), (0.15, 0.045, 0.16), neck_green, 4.0),
        ((0.53, -0.34, 1.29), (0.16, 0.045, 0.16), neck_green, -5.0),
        ((0.27, -0.33, 1.27), (0.15, 0.045, 0.16), neck_purple, 8.0),
        ((0.43, -0.34, 1.18), (0.16, 0.045, 0.16), neck_purple, -7.0),
    ]
    for index, (location, scale, material, angle) in enumerate(feather_positions):
        head_objects.append(add_neck_feather(f"Neck Feather {index + 1}", location, scale, material, angle))

    head_objects.append(add_wedge("Upper Beak", (0.98, 0.0, 1.61), (1.31, 0.0, 1.57), 0.15, 0.13, beak_dark))
    head_objects.append(add_wedge("Lower Beak", (0.98, 0.0, 1.57), (1.25, 0.0, 1.55), 0.13, 0.08, wing_dark))
    head_objects.append(add_ico("Cere", (0.985, -0.012, 1.64), (0.115, 0.115, 0.072), cere_pale, subdivisions=1))
    head_objects.append(add_ico("Visible Eye Rim", (0.82, -0.282, 1.67), (0.057, 0.022, 0.057), pupil_dark, subdivisions=2))
    head_objects.append(add_ico("Visible Iris", (0.82, -0.299, 1.67), (0.045, 0.020, 0.045), eye_orange, subdivisions=2))
    head_objects.append(add_ico("Visible Pupil", (0.823, -0.313, 1.67), (0.019, 0.012, 0.019), pupil_dark, subdivisions=2))
    head_objects.append(add_ico("Far Eye Rim", (0.82, 0.282, 1.67), (0.057, 0.022, 0.057), pupil_dark, subdivisions=2))
    head_objects.append(add_ico("Far Iris", (0.82, 0.299, 1.67), (0.045, 0.020, 0.045), eye_orange, subdivisions=2))
    head_objects.append(add_ico("Far Pupil", (0.823, 0.313, 1.67), (0.019, 0.012, 0.019), pupil_dark, subdivisions=2))

    left_wing_objects.append(add_ico("Left Wing Coverts", (0.00, -0.46, 1.10), (0.67, 0.13, 0.36), wing_grey, subdivisions=2, rotation=(0.0, math.radians(11.0), math.radians(-2.0))))
    right_wing_objects.append(add_ico("Right Wing Coverts", (0.00, 0.46, 1.10), (0.67, 0.13, 0.36), wing_grey, subdivisions=2, rotation=(0.0, math.radians(11.0), math.radians(2.0))))

    wing_sets = (
        ("Left", -1.0, left_wing_objects, left_primary_objects),
        ("Right", 1.0, right_wing_objects, right_primary_objects),
    )
    for side_name, side, covert_collection, primary_collection in wing_sets:
        surface_y = side * 0.615
        covert_collection.append(add_wing_band(f"{side_name} Wing Bar Front", (0.08, surface_y, 1.10), 0.105, 0.52, 0.035, wing_dark, slope=-0.035))
        covert_collection.append(add_wing_band(f"{side_name} Wing Bar Rear", (-0.20, surface_y, 1.02), 0.095, 0.43, 0.035, wing_dark, slope=-0.030))
        for feather_index, z_offset in enumerate((0.12, 0.055, -0.015, -0.085)):
            start = (-0.24, side * (0.46 + feather_index * 0.012), 1.05 + z_offset)
            end = (-1.10 - feather_index * 0.07, side * (0.48 + feather_index * 0.035), 0.82 + z_offset * 0.65)
            feather_material = wing_mid if feather_index < 2 else wing_dark
            primary_collection.append(add_tapered_feather(f"{side_name} Primary Feather {feather_index + 1}", start, end, 0.205, 0.040, feather_material))

    leg_specs = (("Left", -0.22, left_leg_objects), ("Right", 0.22, right_leg_objects))
    for side_name, y_position, collection in leg_specs:
        collection.append(add_cylinder_between(f"{side_name} Shin", (0.22, y_position, 0.49), (0.21, y_position, 0.20), 0.034, leg_rose, vertices=7))
        ankle = Vector((0.21, y_position, 0.18))
        knuckle = Vector((0.30, y_position, 0.115))
        collection.append(add_cylinder_between(f"{side_name} Foot Palm", ankle, knuckle, 0.027, leg_rose, vertices=6))
        collection.append(add_cylinder_between(f"{side_name} Middle Toe", knuckle, (0.53, y_position, 0.075), 0.018, leg_rose, vertices=6))
        collection.append(add_cylinder_between(f"{side_name} Inner Toe", knuckle, (0.47, y_position + 0.13, 0.075), 0.017, leg_rose, vertices=6))
        collection.append(add_cylinder_between(f"{side_name} Outer Toe", knuckle, (0.46, y_position - 0.13, 0.075), 0.017, leg_rose, vertices=6))
        collection.append(add_cylinder_between(f"{side_name} Back Toe", ankle, (0.05, y_position, 0.09), 0.016, leg_rose, vertices=6))

    armature_data = bpy.data.armatures.new("Pigeon Rig")
    armature = bpy.data.objects.new("Pigeon Rig", armature_data)
    bpy.context.collection.objects.link(armature)
    armature.show_in_front = True
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")

    bone_specs = {
        "root": ((0.0, 0.0, 0.08), (0.0, 0.0, 0.48), None),
        "body": ((0.0, 0.0, 0.58), (0.0, 0.0, 1.08), "root"),
        "head": ((0.48, 0.0, 1.12), (0.48, 0.0, 1.57), "body"),
        "wing.L": ((-0.05, -0.35, 0.88), (-0.05, -0.35, 1.28), "body"),
        "wing.R": ((-0.05, 0.35, 0.88), (-0.05, 0.35, 1.28), "body"),
        "wingtip.L": ((-0.55, -0.48, 0.78), (-0.55, -0.48, 1.18), "wing.L"),
        "wingtip.R": ((-0.55, 0.48, 0.78), (-0.55, 0.48, 1.18), "wing.R"),
        "leg.L": ((0.23, -0.22, 0.12), (0.23, -0.22, 0.54), "body"),
        "leg.R": ((0.23, 0.22, 0.12), (0.23, 0.22, 0.54), "body"),
    }
    edit_bones = {}
    for bone_name, (head, tail, parent_name) in bone_specs.items():
        bone = armature_data.edit_bones.new(bone_name)
        bone.head = head
        bone.tail = tail
        if parent_name is not None:
            bone.parent = edit_bones[parent_name]
        edit_bones[bone_name] = bone

    bpy.ops.object.mode_set(mode="POSE")
    for pose_bone in armature.pose.bones:
        pose_bone.rotation_mode = "XYZ"
    bpy.ops.object.mode_set(mode="OBJECT")
    armature.select_set(False)

    for obj in body_objects:
        bone_parent(obj, armature, "body")
    for obj in head_objects:
        bone_parent(obj, armature, "head")
    for obj in left_wing_objects:
        bone_parent(obj, armature, "wing.L")
    for obj in right_wing_objects:
        bone_parent(obj, armature, "wing.R")
    for obj in left_primary_objects:
        bone_parent(obj, armature, "wingtip.L")
    for obj in right_primary_objects:
        bone_parent(obj, armature, "wingtip.R")
    for obj in left_leg_objects:
        bone_parent(obj, armature, "leg.L")
    for obj in right_leg_objects:
        bone_parent(obj, armature, "leg.R")

    all_bird_objects = (
        body_objects + head_objects + left_wing_objects + right_wing_objects
        + left_primary_objects + right_primary_objects + left_leg_objects + right_leg_objects
    )
    return armature, all_bird_objects


def keyframe_pose(armature, frame, body_bob=0.0, body_pitch=0.0, head_pitch=0.0, head_forward=0.0,
                  head_up=0.0, left_leg=0.0, right_leg=0.0, left_wing=0.0, right_wing=0.0,
                  wing_lift=0.0, wing_spread=0.0, wingtip_left=0.0, wingtip_right=0.0):
    armature.location = (0.0, 0.0, body_bob)
    armature.keyframe_insert(data_path="location", frame=frame)

    bones = armature.pose.bones
    body = bones["body"]
    head = bones["head"]
    wing_left = bones["wing.L"]
    wing_right = bones["wing.R"]
    wingtip_left_bone = bones["wingtip.L"]
    wingtip_right_bone = bones["wingtip.R"]
    leg_left = bones["leg.L"]
    leg_right = bones["leg.R"]

    body.rotation_euler = (0.0, 0.0, math.radians(body_pitch))
    head.rotation_euler = (0.0, 0.0, math.radians(head_pitch))
    head.location = (head_forward, head_up, 0.0)
    wing_left.rotation_euler = (math.radians(left_wing), 0.0, 0.0)
    wing_right.rotation_euler = (math.radians(right_wing), 0.0, 0.0)
    wing_left.location = (0.0, wing_lift, wing_spread)
    wing_right.location = (0.0, wing_lift, -wing_spread)
    wingtip_left_bone.rotation_euler = (math.radians(wingtip_left), 0.0, math.radians(-abs(wingtip_left) * 0.10))
    wingtip_right_bone.rotation_euler = (math.radians(wingtip_right), 0.0, math.radians(abs(wingtip_right) * 0.10))
    leg_left.rotation_euler = (0.0, 0.0, math.radians(left_leg))
    leg_right.rotation_euler = (0.0, 0.0, math.radians(right_leg))

    for bone in (body, head, wing_left, wing_right, wingtip_left_bone, wingtip_right_bone, leg_left, leg_right):
        bone.keyframe_insert(data_path="rotation_euler", frame=frame)
        bone.keyframe_insert(data_path="location", frame=frame)


def animate_pigeon(armature):
    scene = bpy.context.scene
    bpy.context.preferences.edit.keyframe_new_interpolation_type = "CONSTANT"
    scene.frame_start = 1
    scene.frame_end = 144
    scene.render.fps = 24
    scene.timeline_markers.new("Idle", frame=1)
    scene.timeline_markers.new("Walk", frame=25)
    scene.timeline_markers.new("Peck", frame=73)
    scene.timeline_markers.new("Flutter", frame=105)
    scene.timeline_markers.new("Settle", frame=133)

    for frame in range(1, 145, 2):
        if frame <= 24:
            time = (frame - 1) / 24.0
            keyframe_pose(
                armature,
                frame,
                body_bob=math.sin(time * math.tau) * 0.012,
                head_pitch=math.sin(time * math.tau * 0.7) * 3.0,
                head_forward=math.sin(time * math.tau * 0.7) * 0.018,
            )
        elif frame <= 72:
            phase = (frame - 25) / 12.0 * math.tau
            stride = math.sin(phase)
            bob = abs(math.sin(phase)) * 0.045
            keyframe_pose(
                armature,
                frame,
                body_bob=bob,
                body_pitch=-2.0 + stride * 1.5,
                head_pitch=-stride * 4.0,
                head_forward=stride * 0.075,
                head_up=-bob * 0.35,
                left_leg=stride * 28.0,
                right_leg=-stride * 28.0,
                left_wing=4.0 + stride * 1.5,
                right_wing=-4.0 - stride * 1.5,
            )
        elif frame <= 104:
            phase = ((frame - 73) % 16) / 16.0
            peck = math.sin(phase * math.pi)
            keyframe_pose(
                armature,
                frame,
                body_bob=-peck * 0.025,
                body_pitch=-peck * 6.0,
                head_pitch=-peck * 68.0,
                head_forward=peck * 0.20,
                head_up=-peck * 0.12,
            )
        elif frame <= 132:
            phase = (frame - 105) / 6.0 * math.tau
            flap = math.sin(phase)
            keyframe_pose(
                armature,
                frame,
                body_bob=abs(flap) * 0.055,
                body_pitch=3.0,
                head_pitch=-3.0,
                left_wing=flap * 62.0,
                right_wing=-flap * 62.0,
                wing_lift=abs(flap) * 0.38,
                wing_spread=abs(flap) * 0.20,
                wingtip_left=flap * 32.0,
                wingtip_right=-flap * 32.0,
            )
        else:
            settle = (frame - 133) / 11.0
            keyframe_pose(
                armature,
                frame,
                body_bob=math.sin(settle * math.pi) * 0.018,
                head_pitch=(1.0 - settle) * -4.0,
                left_wing=(1.0 - settle) * 8.0,
                right_wing=(1.0 - settle) * -8.0,
                wingtip_left=(1.0 - settle) * 5.0,
                wingtip_right=(1.0 - settle) * -5.0,
            )

    if armature.animation_data and armature.animation_data.action:
        armature.animation_data.action.name = "Pigeon_Demo_12fps"


def build_stage():
    floor_material = create_paper_material("Warm Paper Floor", hex_color("E9E3D7"), bump_strength=0.045)
    bpy.ops.mesh.primitive_plane_add(size=20.0, location=(0.0, 0.0, 0.0))
    floor = bpy.context.object
    floor.name = "Preview Floor"
    assign_material(floor, floor_material)

    world = bpy.context.scene.world
    world.use_nodes = True
    background = world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = hex_color("F4F0E8")
    background.inputs["Strength"].default_value = 0.55

    bpy.ops.object.light_add(type="AREA", location=(3.5, -4.2, 6.0))
    key_light = bpy.context.object
    key_light.name = "Soft Key"
    key_light.data.energy = 850.0
    key_light.data.shape = "DISK"
    key_light.data.size = 4.0
    look_at(key_light, (0.0, 0.0, 0.9))

    bpy.ops.object.light_add(type="AREA", location=(-3.0, -1.0, 3.2))
    fill_light = bpy.context.object
    fill_light.name = "Soft Fill"
    fill_light.data.energy = 420.0
    fill_light.data.size = 5.0
    look_at(fill_light, (0.0, 0.0, 0.9))

    bpy.ops.object.light_add(type="AREA", location=(-1.5, 3.5, 4.0))
    rim_light = bpy.context.object
    rim_light.name = "Soft Rim"
    rim_light.data.energy = 500.0
    rim_light.data.size = 3.0
    look_at(rim_light, (0.0, 0.0, 1.0))

    camera_data = bpy.data.cameras.new("Pigeon Preview Camera")
    camera = bpy.data.objects.new("Pigeon Preview Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = (4.2, -6.3, 3.5)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 3.45
    camera.data.lens = 52.0
    look_at(camera, (-0.05, 0.0, 0.90))
    bpy.context.scene.camera = camera
    return floor, camera


def configure_render():
    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 960
    scene.render.resolution_y = 540
    scene.render.resolution_percentage = 100
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = False
    scene.render.use_file_extension = True
    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        pass


def export_fbx(armature, bird_objects):
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    for obj in bird_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = armature

    if hasattr(bpy.ops.export_scene, "fbx"):
        bpy.ops.export_scene.fbx(
            filepath=str(FBX_PATH),
            use_selection=True,
            object_types={"ARMATURE", "MESH"},
            apply_unit_scale=True,
            apply_scale_options="FBX_SCALE_UNITS",
            add_leaf_bones=False,
            bake_anim=True,
            bake_anim_use_all_actions=False,
            bake_anim_use_nla_strips=False,
            bake_anim_simplify_factor=0.0,
            path_mode="AUTO",
        )
    else:
        bpy.ops.wm.fbx_export(filepath=str(FBX_PATH), use_selection=True)


def render_outputs():
    scene = bpy.context.scene
    for old_frame in FRAMES_DIR.glob("frame_*.png"):
        old_frame.unlink()
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.filepath = str(FRAMES_DIR / "frame_")
    scene.frame_step = 2
    bpy.ops.render.render(animation=True)
    scene.frame_step = 1

    scene.frame_set(62)
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.filepath = str(HERO_PATH)
    bpy.ops.render.render(write_still=True)


def main():
    clear_scene()
    configure_render()
    armature, bird_objects = build_pigeon()
    animate_pigeon(armature)
    build_stage()
    bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    export_fbx(armature, bird_objects)
    render_outputs()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    print(f"BLEND={BLEND_PATH}")
    print(f"FBX={FBX_PATH}")
    print(f"FRAMES={FRAMES_DIR}")
    print(f"HERO={HERO_PATH}")


if __name__ == "__main__":
    main()
