"""Build the SYMBIOSIS: 49 low-poly urban fox v01 with rig and animation."""

import math
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[2]
BLEND_PATH = PROJECT_ROOT / "SourceAssets" / "Blender" / "SYMBIOSIS_49_Fox_v01.blend"
FBX_PATH = PROJECT_ROOT / "Assets" / "Resources" / "Animals" / "Fox" / "Fox_Animated_v01.fbx"
PREVIEW_DIR = PROJECT_ROOT / "Previews" / "FoxBlender_v01"
FRAMES_DIR = PREVIEW_DIR / "frames"
HERO_PATH = PREVIEW_DIR / "FoxBlenderAnimation_v01.png"

for directory in (BLEND_PATH.parent, FBX_PATH.parent, PREVIEW_DIR, FRAMES_DIR):
    directory.mkdir(parents=True, exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.armatures,
        bpy.data.materials,
        bpy.data.cameras,
        bpy.data.lights,
    ):
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


def create_paper_material(name, color, roughness=0.92, bump_strength=0.065):
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
    coordinate = nodes.new("ShaderNodeTexCoord")
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 31.0
    noise.inputs["Detail"].default_value = 2.0
    noise.inputs["Roughness"].default_value = 0.64
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = bump_strength
    bump.inputs["Distance"].default_value = 0.026
    links.new(coordinate.outputs["Generated"], noise.inputs["Vector"])
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], principled.inputs["Normal"])
    links.new(principled.outputs["BSDF"], output.inputs["Surface"])
    return material


def assign_material(obj, material):
    if obj.data and hasattr(obj.data, "materials"):
        obj.data.materials.append(material)


def assign_faceted_palette(obj, materials):
    for material in materials:
        if obj.data.materials.get(material.name) is None:
            obj.data.materials.append(material)
    for polygon in obj.data.polygons:
        pattern = (polygon.index * 7 + 5) % 31
        if pattern in (0, 9, 22):
            polygon.material_index = 2
        elif pattern in (3, 12, 18, 27):
            polygon.material_index = 1
        else:
            polygon.material_index = 0


def apply_scale(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.select_set(False)


def add_ico(name, location, scale, material, subdivisions=2, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_ico_sphere_add(
        subdivisions=subdivisions,
        radius=1.0,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    apply_scale(obj)
    assign_material(obj, material)
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
    return obj


def add_ellipsoid_between(name, start, end, width, material, subdivisions=1):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    midpoint = (start + end) * 0.5
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1.0, location=midpoint)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    obj.rotation_mode = "XYZ"
    obj.scale = (width, width * 0.82, direction.length * 0.54)
    apply_scale(obj)
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
        (tip.x, tip.y - half_width * 0.08, tip.z),
        (tip.x, tip.y + half_width * 0.08, tip.z),
    ]
    faces = [(0, 1, 2, 3), (0, 4, 5, 1), (1, 5, 2), (2, 5, 4, 3), (3, 4, 0)]
    mesh = bpy.data.meshes.new(f"{name} Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    assign_material(obj, material)
    return obj


def add_ear(name, location, scale, material, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cone_add(
        vertices=5,
        radius1=1.0,
        radius2=0.05,
        depth=2.0,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    apply_scale(obj)
    assign_material(obj, material)
    return obj


def add_tapered_segment(name, start, end, start_radius, end_radius, material, facets=8):
    start = Vector(start)
    end = Vector(end)
    direction = (end - start).normalized()
    lateral = Vector((0.0, 1.0, 0.0))
    profile = direction.cross(lateral).normalized()
    vertices = []
    for center, radius in ((start, start_radius), (end, end_radius)):
        for index in range(facets):
            angle = index / facets * math.tau
            vertices.append(tuple(
                center
                + lateral * math.cos(angle) * radius * 0.78
                + profile * math.sin(angle) * radius
            ))
    faces = [tuple(range(facets - 1, -1, -1)), tuple(range(facets, facets * 2))]
    for index in range(facets):
        next_index = (index + 1) % facets
        faces.append((index, next_index, facets + next_index, facets + index))
    mesh = bpy.data.meshes.new(f"{name} Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    assign_material(obj, material)
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


def build_fox():
    fur = create_paper_material("Terracotta Red Fox Fur", hex_color("A94E2A"))
    fur_light = create_paper_material("Copper Fox Highlight", hex_color("C76A38"))
    fur_dark = create_paper_material("Auburn Back", hex_color("743620"))
    cream = create_paper_material("Warm Cream Markings", hex_color("DDC9A9"), bump_strength=0.05)
    black = create_paper_material("Charcoal Legs and Ears", hex_color("242321"), bump_strength=0.04)
    paw = create_paper_material("Dark Paws", hex_color("332B27"), bump_strength=0.035)
    eye = create_paper_material("Fox Eyes", hex_color("171513"), roughness=0.45, bump_strength=0.0)

    body_objects = []
    neck_objects = []
    head_objects = []
    tail_base_objects = []
    tail_mid_objects = []
    tail_tip_objects = []
    leg_groups = {
        "leg.FL.upper": [], "leg.FL.lower": [],
        "leg.FR.upper": [], "leg.FR.lower": [],
        "leg.BL.upper": [], "leg.BL.lower": [],
        "leg.BR.upper": [], "leg.BR.lower": [],
    }

    body = add_ico("Long Faceted Torso", (-0.16, 0.0, 0.82), (0.70, 0.31, 0.34), fur, subdivisions=2)
    assign_faceted_palette(body, (fur, fur_light, fur_dark))
    body_objects.append(body)
    body_objects.append(add_ico("Cream Belly", (0.02, 0.0, 0.57), (0.49, 0.235, 0.09), cream, subdivisions=2))
    chest = add_ico("Angular Chest", (0.43, 0.0, 0.90), (0.37, 0.28, 0.39), fur_light, subdivisions=2)
    assign_faceted_palette(chest, (fur_light, fur, fur_dark))
    body_objects.append(chest)
    body_objects.append(add_ico("Cream Throat and Chest", (0.63, -0.01, 0.89), (0.15, 0.22, 0.31), cream, subdivisions=2))

    neck = add_ellipsoid_between("Short Fox Neck", (0.57, 0.0, 0.97), (0.78, 0.0, 1.18), 0.24, fur, subdivisions=2)
    assign_faceted_palette(neck, (fur, fur_light, fur_dark))
    neck_objects.append(neck)

    head = add_ico("Angular Fox Head", (0.88, 0.0, 1.27), (0.31, 0.235, 0.25), fur, subdivisions=2)
    assign_faceted_palette(head, (fur, fur_light, fur_dark))
    head_objects.append(head)
    head_objects.append(add_wedge("Long Tapered Muzzle", (1.06, 0.0, 1.23), (1.46, 0.0, 1.14), 0.20, 0.16, cream))
    head_objects.append(add_ico("Black Nose", (1.47, 0.0, 1.14), (0.060, 0.052, 0.050), black, subdivisions=1))
    for side_name, side in (("Left", -1.0), ("Right", 1.0)):
        head_objects.append(add_ear(
            f"{side_name} Upright Ear",
            (0.80, side * 0.16, 1.56),
            (0.115, 0.075, 0.22),
            black,
            rotation=(0.0, math.radians(-5.0), 0.0),
        ))
        head_objects.append(add_ico(
            f"{side_name} Cream Cheek",
            (1.02, side * 0.215, 1.20),
            (0.20, 0.034, 0.105),
            cream,
            subdivisions=1,
        ))
        head_objects.append(add_ico(
            f"{side_name} Eye",
            (0.995, side * 0.226, 1.31),
            (0.031, 0.013, 0.028),
            eye,
            subdivisions=2,
        ))

    tail_base = add_tapered_segment(
        "Tail Base", (-0.74, 0.0, 0.86), (-1.16, 0.0, 0.73), 0.20, 0.28, fur_dark
    )
    assign_faceted_palette(tail_base, (fur_dark, fur, fur_light))
    tail_base_objects.append(tail_base)
    tail_mid = add_tapered_segment(
        "Tail Mid", (-1.13, 0.0, 0.74), (-1.55, 0.0, 0.58), 0.28, 0.22, fur
    )
    assign_faceted_palette(tail_mid, (fur, fur_light, fur_dark))
    tail_mid_objects.append(tail_mid)
    tail_tip = add_tapered_segment(
        "Cream Tail Tip", (-1.52, 0.0, 0.59), (-1.88, 0.0, 0.51), 0.22, 0.035, cream
    )
    tail_tip_objects.append(tail_tip)

    leg_specs = (
        ("FL", "Front Left", (0.48, -0.23, 0.91), (0.48, -0.23, 0.48), (0.60, -0.23, 0.12)),
        ("FR", "Front Right", (0.48, 0.23, 0.91), (0.48, 0.23, 0.48), (0.60, 0.23, 0.12)),
        ("BL", "Back Left", (-0.54, -0.24, 0.82), (-0.38, -0.24, 0.47), (-0.25, -0.24, 0.11)),
        ("BR", "Back Right", (-0.54, 0.24, 0.82), (-0.38, 0.24, 0.47), (-0.25, 0.24, 0.11)),
    )
    for key, label, hip, knee, ankle in leg_specs:
        upper_bone = f"leg.{key}.upper"
        lower_bone = f"leg.{key}.lower"
        upper = add_ellipsoid_between(f"{label} Upper Leg", hip, knee, 0.105, fur_dark, subdivisions=1)
        assign_faceted_palette(upper, (fur_dark, fur, black))
        leg_groups[upper_bone].append(upper)
        lower = add_ellipsoid_between(f"{label} Black Lower Leg", knee, ankle, 0.070, black, subdivisions=1)
        leg_groups[lower_bone].append(lower)
        leg_groups[lower_bone].append(add_ico(
            f"{label} Compact Paw",
            (ankle[0] + 0.075, ankle[1], 0.075),
            (0.14, 0.075, 0.055),
            paw,
            subdivisions=1,
        ))

    armature_data = bpy.data.armatures.new("Fox Rig")
    armature = bpy.data.objects.new("Fox Rig", armature_data)
    bpy.context.collection.objects.link(armature)
    armature.show_in_front = True
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bone_specs = {
        "root": ((0.0, 0.0, 0.05), (0.0, 0.0, 0.36), None),
        "body": ((-0.12, 0.0, 0.54), (-0.12, 0.0, 1.05), "root"),
        "neck": ((0.50, 0.0, 0.90), (0.76, 0.0, 1.20), "body"),
        "head": ((0.78, 0.0, 1.11), (0.93, 0.0, 1.46), "neck"),
        "tail.base": ((-0.67, 0.0, 0.82), (-1.10, 0.0, 0.72), "body"),
        "tail.mid": ((-1.10, 0.0, 0.72), (-1.51, 0.0, 0.58), "tail.base"),
        "tail.tip": ((-1.51, 0.0, 0.58), (-1.87, 0.0, 0.51), "tail.mid"),
        "leg.FL.upper": ((0.48, -0.23, 0.91), (0.48, -0.23, 0.48), "body"),
        "leg.FL.lower": ((0.48, -0.23, 0.48), (0.60, -0.23, 0.12), "leg.FL.upper"),
        "leg.FR.upper": ((0.48, 0.23, 0.91), (0.48, 0.23, 0.48), "body"),
        "leg.FR.lower": ((0.48, 0.23, 0.48), (0.60, 0.23, 0.12), "leg.FR.upper"),
        "leg.BL.upper": ((-0.54, -0.24, 0.82), (-0.38, -0.24, 0.47), "body"),
        "leg.BL.lower": ((-0.38, -0.24, 0.47), (-0.25, -0.24, 0.11), "leg.BL.upper"),
        "leg.BR.upper": ((-0.54, 0.24, 0.82), (-0.38, 0.24, 0.47), "body"),
        "leg.BR.lower": ((-0.38, 0.24, 0.47), (-0.25, 0.24, 0.11), "leg.BR.upper"),
    }
    edit_bones = {}
    for bone_name, (head_position, tail_position, parent_name) in bone_specs.items():
        bone = armature_data.edit_bones.new(bone_name)
        bone.head = head_position
        bone.tail = tail_position
        if parent_name is not None:
            bone.parent = edit_bones[parent_name]
        edit_bones[bone_name] = bone
    bpy.ops.object.mode_set(mode="POSE")
    for pose_bone in armature.pose.bones:
        pose_bone.rotation_mode = "XYZ"
    bpy.ops.object.mode_set(mode="OBJECT")
    armature.select_set(False)

    groups = (
        (body_objects, "body"),
        (neck_objects, "neck"),
        (head_objects, "head"),
        (tail_base_objects, "tail.base"),
        (tail_mid_objects, "tail.mid"),
        (tail_tip_objects, "tail.tip"),
    )
    all_objects = []
    for objects, bone_name in groups:
        all_objects.extend(objects)
        for obj in objects:
            bone_parent(obj, armature, bone_name)
    for bone_name, objects in leg_groups.items():
        all_objects.extend(objects)
        for obj in objects:
            bone_parent(obj, armature, bone_name)
    return armature, all_objects


def keyframe_pose(
    armature,
    frame,
    body_bob=0.0,
    body_pitch=0.0,
    body_roll=0.0,
    neck_pitch=0.0,
    head_pitch=0.0,
    head_yaw=0.0,
    tail_sway=0.0,
    tail_lift=0.0,
    fl=0.0,
    fr=0.0,
    bl=0.0,
    br=0.0,
    lower_fold=0.0,
):
    armature.location = (0.0, 0.0, body_bob)
    armature.keyframe_insert(data_path="location", frame=frame)
    bones = armature.pose.bones
    bones["body"].rotation_euler = (math.radians(body_roll), 0.0, math.radians(body_pitch))
    bones["neck"].rotation_euler = (0.0, 0.0, math.radians(neck_pitch))
    bones["head"].rotation_euler = (0.0, math.radians(head_yaw), math.radians(head_pitch))
    bones["tail.base"].rotation_euler = (math.radians(tail_sway), 0.0, math.radians(tail_lift))
    bones["tail.mid"].rotation_euler = (math.radians(tail_sway * 0.70), 0.0, math.radians(tail_lift * 0.45))
    bones["tail.tip"].rotation_euler = (math.radians(tail_sway * 0.42), 0.0, math.radians(-tail_lift * 0.28))
    swings = {"FL": fl, "FR": fr, "BL": bl, "BR": br}
    for key, angle in swings.items():
        bones[f"leg.{key}.upper"].rotation_euler = (0.0, 0.0, math.radians(angle))
        bones[f"leg.{key}.lower"].rotation_euler = (0.0, 0.0, math.radians(-angle * 0.38 + lower_fold))
    for bone in bones:
        bone.keyframe_insert(data_path="rotation_euler", frame=frame)
        bone.keyframe_insert(data_path="location", frame=frame)


def animate_fox(armature):
    scene = bpy.context.scene
    bpy.context.preferences.edit.keyframe_new_interpolation_type = "CONSTANT"
    scene.frame_start = 1
    scene.frame_end = 144
    scene.render.fps = 24
    scene.timeline_markers.new("Idle", frame=1)
    scene.timeline_markers.new("Trot", frame=25)
    scene.timeline_markers.new("Sniff", frame=73)
    scene.timeline_markers.new("Alert", frame=105)
    scene.timeline_markers.new("Settle", frame=133)
    for frame in range(1, 145, 2):
        if frame <= 24:
            phase = (frame - 1) / 24.0 * math.tau
            keyframe_pose(
                armature,
                frame,
                body_bob=math.sin(phase) * 0.006,
                head_yaw=math.sin(phase * 0.55) * 7.0,
                tail_sway=math.sin(phase * 0.75) * 5.0,
            )
        elif frame <= 72:
            phase = (frame - 25) / 12.0 * math.tau
            stride = math.sin(phase)
            keyframe_pose(
                armature,
                frame,
                body_bob=abs(stride) * 0.035,
                body_pitch=stride * 2.2,
                body_roll=stride * 1.7,
                head_pitch=-stride * 2.0,
                tail_sway=-stride * 6.0,
                tail_lift=abs(stride) * 4.0,
                fl=stride * 30.0,
                fr=-stride * 30.0,
                bl=-stride * 28.0,
                br=stride * 28.0,
                lower_fold=abs(stride) * 10.0,
            )
        elif frame <= 104:
            phase = ((frame - 73) % 16) / 16.0
            sniff = math.sin(phase * math.pi)
            keyframe_pose(
                armature,
                frame,
                body_bob=-sniff * 0.018,
                body_pitch=-sniff * 4.0,
                neck_pitch=-sniff * 28.0,
                head_pitch=-sniff * 38.0,
                head_yaw=math.sin(phase * math.tau) * 6.0,
                tail_lift=-sniff * 5.0,
                fl=-sniff * 8.0,
                fr=-sniff * 8.0,
            )
        elif frame <= 132:
            phase = (frame - 105) / 27.0
            alert = math.sin(phase * math.pi)
            keyframe_pose(
                armature,
                frame,
                body_bob=alert * 0.012,
                neck_pitch=alert * 8.0,
                head_pitch=alert * 7.0,
                head_yaw=alert * 22.0,
                tail_sway=math.sin(phase * math.pi * 2.0) * 4.0,
                tail_lift=alert * 8.0,
            )
        else:
            settle = (frame - 133) / 11.0
            keyframe_pose(
                armature,
                frame,
                body_bob=math.sin(settle * math.pi) * 0.005,
                head_yaw=(1.0 - settle) * 6.0,
                tail_sway=(1.0 - settle) * 4.0,
            )
    if armature.animation_data and armature.animation_data.action:
        armature.animation_data.action.name = "Fox_Demo_12fps"


def build_stage():
    floor_material = create_paper_material("Warm Paper Floor", hex_color("E9E3D7"), bump_strength=0.04)
    bpy.ops.mesh.primitive_plane_add(size=20.0, location=(0.0, 0.0, 0.0))
    assign_material(bpy.context.object, floor_material)
    world = bpy.context.scene.world
    world.use_nodes = True
    world.node_tree.nodes.get("Background").inputs["Color"].default_value = hex_color("F4F0E8")
    world.node_tree.nodes.get("Background").inputs["Strength"].default_value = 0.55
    for name, location, energy, size in (
        ("Soft Key", (4.0, -5.0, 6.0), 900.0, 4.5),
        ("Soft Fill", (-3.0, -1.5, 4.0), 420.0, 5.0),
        ("Soft Rim", (-2.0, 4.0, 4.5), 500.0, 3.5),
    ):
        bpy.ops.object.light_add(type="AREA", location=location)
        light = bpy.context.object
        light.name = name
        light.data.energy = energy
        light.data.size = size
        look_at(light, (0.0, 0.0, 0.85))
    camera_data = bpy.data.cameras.new("Fox Preview Camera")
    camera = bpy.data.objects.new("Fox Preview Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = (5.2, -7.6, 4.2)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 4.35
    look_at(camera, (-0.12, 0.0, 0.84))
    bpy.context.scene.camera = camera


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


def record_source_dimensions(armature, objects):
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    minimum = Vector((min(point.x for point in points), min(point.y for point in points), min(point.z for point in points)))
    maximum = Vector((max(point.x for point in points), max(point.y for point in points), max(point.z for point in points)))
    size = maximum - minimum
    horizontal_span = math.sqrt(size.x * size.x + size.y * size.y)
    armature["source_horizontal_span"] = horizontal_span
    armature["source_height"] = size.z
    print(f"SOURCE_HORIZONTAL_SPAN={horizontal_span:.6f}")
    print(f"SOURCE_HEIGHT={size.z:.6f}")


def export_fbx(armature, fox_objects):
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    for obj in fox_objects:
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
    scene.render.filepath = str(FRAMES_DIR / "frame_")
    scene.frame_step = 2
    bpy.ops.render.render(animation=True)
    scene.frame_step = 1
    scene.frame_set(93)
    scene.render.filepath = str(HERO_PATH)
    bpy.ops.render.render(write_still=True)


def main():
    clear_scene()
    configure_render()
    armature, fox_objects = build_fox()
    animate_fox(armature)
    record_source_dimensions(armature, fox_objects)
    build_stage()
    bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    export_fbx(armature, fox_objects)
    render_outputs()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    print(f"BLEND={BLEND_PATH}")
    print(f"FBX={FBX_PATH}")
    print(f"FRAMES={FRAMES_DIR}")
    print(f"HERO={HERO_PATH}")


if __name__ == "__main__":
    main()
