"""Build the SYMBIOSIS: 49 low-poly paper squirrel v02 from concept v01."""

import math
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[2]
BLEND_PATH = PROJECT_ROOT / "SourceAssets" / "Blender" / "SYMBIOSIS_49_Squirrel_v02.blend"
FBX_PATH = PROJECT_ROOT / "Assets" / "Resources" / "Animals" / "Squirrel" / "Squirrel_Animated_v02.fbx"
PREVIEW_DIR = PROJECT_ROOT / "Previews" / "SquirrelBlender_v02"
FRAMES_DIR = PREVIEW_DIR / "frames"
HERO_PATH = PREVIEW_DIR / "SquirrelBlenderAnimation_v02.png"

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


def create_paper_material(name, color, roughness=0.92, bump_strength=0.075):
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
    noise.inputs["Scale"].default_value = 30.0
    noise.inputs["Detail"].default_value = 2.1
    noise.inputs["Roughness"].default_value = 0.66
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = bump_strength
    bump.inputs["Distance"].default_value = 0.03
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
        pattern = (polygon.index * 11 + 5) % 29
        if pattern in (0, 13):
            polygon.material_index = 2
        elif pattern in (3, 9, 21):
            polygon.material_index = 1
        else:
            polygon.material_index = 0


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
    obj.scale = (width, width * 0.82, direction.length * 0.56)
    apply_scale(obj)
    assign_material(obj, material)
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
    return obj


def add_tapered_tail_segment(name, start, end, start_radius, end_radius, material, facets=8):
    start = Vector(start)
    end = Vector(end)
    direction = (end - start).normalized()
    lateral = Vector((0.0, 1.0, 0.0))
    profile = direction.cross(lateral).normalized()
    vertices = []
    for center, radius in ((start, start_radius), (end, end_radius)):
        for index in range(facets):
            angle = index / facets * math.tau
            point = center + lateral * math.cos(angle) * radius * 0.72 + profile * math.sin(angle) * radius
            vertices.append(tuple(point))
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
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
    return obj


def add_curved_tail(name, centers, radii, material, facets=8):
    centers = [Vector(center) for center in centers]
    vertices = []
    for ring_index, (center, radius) in enumerate(zip(centers, radii)):
        if ring_index == 0:
            tangent = centers[1] - center
        elif ring_index == len(centers) - 1:
            tangent = center - centers[ring_index - 1]
        else:
            tangent = centers[ring_index + 1] - centers[ring_index - 1]
        tangent.normalize()
        lateral = Vector((0.0, 1.0, 0.0))
        profile = tangent.cross(lateral).normalized()
        for index in range(facets):
            angle = index / facets * math.tau
            point = center + lateral * math.cos(angle) * radius * 0.76 + profile * math.sin(angle) * radius
            vertices.append(tuple(point))
    faces = [tuple(range(facets - 1, -1, -1))]
    for ring_index in range(len(centers) - 1):
        current = ring_index * facets
        following = (ring_index + 1) * facets
        for index in range(facets):
            next_index = (index + 1) % facets
            faces.append((current + index, current + next_index, following + next_index, following + index))
    last_ring = (len(centers) - 1) * facets
    faces.append(tuple(last_ring + index for index in range(facets)))
    mesh = bpy.data.meshes.new(f"{name} Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    assign_material(obj, material)
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
    return obj


def add_chest_bib(name, material):
    front_x = 0.605
    back_x = 0.565
    outline = [
        (-0.13, 0.86),
        (0.13, 0.86),
        (0.115, 0.66),
        (0.0, 0.49),
        (-0.115, 0.66),
    ]
    vertices = [(front_x, y, z) for y, z in outline] + [(back_x, y, z) for y, z in outline]
    faces = [(0, 1, 2, 3, 4), (9, 8, 7, 6, 5)]
    for index in range(5):
        next_index = (index + 1) % 5
        faces.append((index, next_index, 5 + next_index, 5 + index))
    mesh = bpy.data.meshes.new(f"{name} Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
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
        (tip.x, tip.y - half_width * 0.10, tip.z),
        (tip.x, tip.y + half_width * 0.10, tip.z),
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
    bpy.ops.mesh.primitive_cone_add(vertices=5, radius1=1.0, radius2=0.08, depth=2.0, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    apply_scale(obj)
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


def build_squirrel():
    fur = create_paper_material("Red Squirrel Fur", hex_color("A84E27"))
    fur_light = create_paper_material("Copper Fur Highlight", hex_color("C76A35"))
    fur_dark = create_paper_material("Deep Auburn Fur", hex_color("65301F"))
    belly = create_paper_material("Cream Belly", hex_color("DCC8A9"), bump_strength=0.055)
    paw = create_paper_material("Dark Paw", hex_color("5B3427"), bump_strength=0.04)
    eye = create_paper_material("Glossy Eye", hex_color("111313"), roughness=0.48, bump_strength=0.0)
    nose = create_paper_material("Nose", hex_color("231B19"), roughness=0.45, bump_strength=0.0)

    body_objects = []
    head_objects = []
    tail_base_objects = []
    tail_mid_objects = []
    tail_tip_objects = []
    fore_left = []
    fore_right = []
    hind_left = []
    hind_right = []

    body = add_ico("Long Red Squirrel Body", (-0.08, 0.0, 0.61), (0.52, 0.28, 0.34), fur, subdivisions=2, rotation=(0.0, math.radians(-6.0), 0.0))
    assign_faceted_palette(body, (fur, fur_light, fur_dark))
    body_objects.append(body)
    chest = add_ico("Raised Chest", (0.28, 0.0, 0.75), (0.31, 0.245, 0.36), fur_light, subdivisions=2, rotation=(0.0, math.radians(-8.0), 0.0))
    assign_faceted_palette(chest, (fur_light, fur, fur_dark))
    body_objects.append(chest)
    body_objects.append(add_ico("Cream Underside", (0.02, 0.0, 0.38), (0.35, 0.21, 0.09), belly, subdivisions=2))
    body_objects.append(add_chest_bib("Cream Chest Bib", belly))

    head = add_ico("Tapered Head", (0.54, 0.0, 1.04), (0.245, 0.205, 0.25), fur, subdivisions=2)
    assign_faceted_palette(head, (fur, fur_light, fur_dark))
    head_objects.append(head)
    head_objects.append(add_wedge("Short Muzzle", (0.70, 0.0, 1.01), (0.91, 0.0, 0.97), 0.155, 0.14, fur_light))
    head_objects.append(add_ico("Cream Chin", (0.75, -0.005, 0.955), (0.13, 0.13, 0.060), belly, subdivisions=1))
    head_objects.append(add_ico("Nose", (0.918, 0.0, 0.97), (0.045, 0.041, 0.041), nose, subdivisions=1))
    head_objects.append(add_ear("Left Tufted Ear", (0.48, -0.135, 1.30), (0.082, 0.060, 0.16), fur_dark, rotation=(0.0, math.radians(-7.0), 0.0)))
    head_objects.append(add_ear("Right Tufted Ear", (0.48, 0.135, 1.30), (0.082, 0.060, 0.16), fur_dark, rotation=(0.0, math.radians(-7.0), 0.0)))
    head_objects.append(add_ico("Visible Eye Rim", (0.635, -0.190, 1.105), (0.048, 0.016, 0.048), belly, subdivisions=2))
    head_objects.append(add_ico("Visible Eye", (0.642, -0.203, 1.108), (0.030, 0.012, 0.030), eye, subdivisions=2))
    head_objects.append(add_ico("Far Eye Rim", (0.635, 0.190, 1.105), (0.048, 0.016, 0.048), belly, subdivisions=2))
    head_objects.append(add_ico("Far Eye", (0.642, 0.203, 1.108), (0.030, 0.012, 0.030), eye, subdivisions=2))

    tail = add_curved_tail(
        "Continuous Bushy Tail",
        (
            (-0.45, 0.0, 0.62),
            (-0.60, 0.0, 0.76),
            (-0.75, 0.0, 0.96),
            (-0.86, 0.0, 1.19),
            (-0.90, 0.0, 1.41),
            (-0.83, 0.0, 1.59),
            (-0.70, 0.0, 1.67),
            (-0.57, 0.0, 1.60),
            (-0.48, 0.0, 1.48),
        ),
        (0.09, 0.14, 0.20, 0.27, 0.32, 0.29, 0.22, 0.12, 0.025),
        fur,
        facets=10,
    )
    assign_faceted_palette(tail, (fur, fur_light, fur_dark))
    tail_base_objects.append(tail)

    for side_name, side, fore_collection, hind_collection in (
        ("Left", -1.0, fore_left, hind_left),
        ("Right", 1.0, fore_right, hind_right),
    ):
        shoulder = (0.34, side * 0.205, 0.75)
        wrist = (0.48, side * 0.215, 0.34)
        fore_collection.append(add_ellipsoid_between(f"{side_name} Tapered Foreleg", shoulder, wrist, 0.060, fur_dark, subdivisions=1))
        fore_collection.append(add_ico(f"{side_name} Small Forepaw", (0.54, side * 0.215, 0.285), (0.115, 0.065, 0.055), paw, subdivisions=1))
        hind_collection.append(add_ico(f"{side_name} Rounded Hind Thigh", (-0.23, side * 0.245, 0.43), (0.25, 0.17, 0.225), fur_dark, subdivisions=2))
        hind_collection.append(add_ellipsoid_between(f"{side_name} Hind Shin", (-0.18, side * 0.245, 0.38), (0.03, side * 0.25, 0.16), 0.078, fur, subdivisions=1))
        hind_collection.append(add_ico(f"{side_name} Long Hind Paw", (0.16, side * 0.25, 0.105), (0.21, 0.090, 0.060), paw, subdivisions=1))

    armature_data = bpy.data.armatures.new("Squirrel Rig")
    armature = bpy.data.objects.new("Squirrel Rig", armature_data)
    bpy.context.collection.objects.link(armature)
    armature.show_in_front = True
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bone_specs = {
        "root": ((0.0, 0.0, 0.05), (0.0, 0.0, 0.35), None),
        "body": ((-0.02, 0.0, 0.34), (-0.02, 0.0, 0.88), "root"),
        "head": ((0.45, 0.0, 0.88), (0.45, 0.0, 1.30), "body"),
        "tail.base": ((-0.45, 0.0, 0.58), (-0.58, 0.0, 0.91), "body"),
        "tail.mid": ((-0.75, 0.0, 0.86), (-0.88, 0.0, 1.30), "tail.base"),
        "tail.tip": ((-0.88, 0.0, 1.22), (-0.78, 0.0, 1.61), "tail.mid"),
        "fore.L": ((0.34, -0.205, 0.75), (0.42, -0.215, 0.33), "body"),
        "fore.R": ((0.34, 0.205, 0.75), (0.42, 0.215, 0.33), "body"),
        "hind.L": ((-0.23, -0.245, 0.50), (-0.18, -0.25, 0.12), "body"),
        "hind.R": ((-0.23, 0.245, 0.50), (-0.18, 0.25, 0.12), "body"),
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

    groups = (
        (body_objects, "body"),
        (head_objects, "head"),
        (tail_base_objects, "tail.base"),
        (tail_mid_objects, "tail.mid"),
        (tail_tip_objects, "tail.tip"),
        (fore_left, "fore.L"),
        (fore_right, "fore.R"),
        (hind_left, "hind.L"),
        (hind_right, "hind.R"),
    )
    all_objects = []
    for objects, bone_name in groups:
        all_objects.extend(objects)
        for obj in objects:
            bone_parent(obj, armature, bone_name)
    return armature, all_objects


def keyframe_pose(
    armature,
    frame,
    body_bob=0.0,
    body_pitch=0.0,
    head_pitch=0.0,
    head_yaw=0.0,
    fore_left=0.0,
    fore_right=0.0,
    hind_left=0.0,
    hind_right=0.0,
    tail_sway=0.0,
    tail_curl=0.0,
):
    armature.location = (0.0, 0.0, body_bob)
    armature.keyframe_insert(data_path="location", frame=frame)
    bones = armature.pose.bones
    bones["body"].rotation_euler = (0.0, 0.0, math.radians(body_pitch))
    bones["head"].rotation_euler = (0.0, math.radians(head_yaw), math.radians(head_pitch))
    bones["fore.L"].rotation_euler = (0.0, 0.0, math.radians(fore_left))
    bones["fore.R"].rotation_euler = (0.0, 0.0, math.radians(fore_right))
    bones["hind.L"].rotation_euler = (0.0, 0.0, math.radians(hind_left))
    bones["hind.R"].rotation_euler = (0.0, 0.0, math.radians(hind_right))
    bones["tail.base"].rotation_euler = (math.radians(tail_sway), 0.0, math.radians(tail_curl))
    bones["tail.mid"].rotation_euler = (math.radians(tail_sway * 0.72), 0.0, math.radians(tail_curl * 0.62))
    bones["tail.tip"].rotation_euler = (math.radians(tail_sway * 0.48), 0.0, math.radians(-tail_curl * 0.34))
    for bone in bones:
        bone.keyframe_insert(data_path="rotation_euler", frame=frame)
        bone.keyframe_insert(data_path="location", frame=frame)


def animate_squirrel(armature):
    scene = bpy.context.scene
    bpy.context.preferences.edit.keyframe_new_interpolation_type = "CONSTANT"
    scene.frame_start = 1
    scene.frame_end = 144
    scene.render.fps = 24
    scene.timeline_markers.new("Idle", frame=1)
    scene.timeline_markers.new("Hop", frame=25)
    scene.timeline_markers.new("Forage", frame=73)
    scene.timeline_markers.new("Alert", frame=105)
    scene.timeline_markers.new("Settle", frame=133)
    for frame in range(1, 145, 2):
        if frame <= 24:
            time = (frame - 1) / 24.0
            keyframe_pose(
                armature,
                frame,
                body_bob=math.sin(time * math.tau) * 0.008,
                head_yaw=math.sin(time * math.tau * 0.55) * 7.0,
                tail_sway=math.sin(time * math.tau) * 7.0,
                tail_curl=math.sin(time * math.tau * 0.7) * 3.0,
            )
        elif frame <= 72:
            phase = (frame - 25) / 12.0 * math.tau
            stride = math.sin(phase)
            jump = max(0.0, math.sin(phase))
            keyframe_pose(
                armature,
                frame,
                body_bob=jump * 0.15,
                body_pitch=-6.0 + jump * 10.0,
                head_pitch=-stride * 5.0,
                fore_left=-stride * 34.0,
                fore_right=stride * 34.0,
                hind_left=stride * 42.0,
                hind_right=-stride * 42.0,
                tail_sway=-stride * 8.0,
                tail_curl=-jump * 10.0,
            )
        elif frame <= 104:
            phase = ((frame - 73) % 16) / 16.0
            forage = math.sin(phase * math.pi)
            keyframe_pose(
                armature,
                frame,
                body_bob=-forage * 0.02,
                body_pitch=-forage * 8.0,
                head_pitch=-forage * 58.0,
                fore_left=-forage * 46.0,
                fore_right=-forage * 46.0,
                tail_sway=math.sin(phase * math.tau) * 4.0,
            )
        elif frame <= 132:
            phase = (frame - 105) / 27.0
            alert = math.sin(phase * math.pi)
            keyframe_pose(
                armature,
                frame,
                body_bob=alert * 0.04,
                body_pitch=alert * 25.0,
                head_pitch=-alert * 11.0,
                head_yaw=alert * 16.0,
                fore_left=-alert * 74.0,
                fore_right=-alert * 74.0,
                hind_left=alert * 8.0,
                hind_right=alert * 8.0,
                tail_sway=math.sin(phase * math.pi * 2.0) * 8.0,
                tail_curl=alert * 6.0,
            )
        else:
            settle = (frame - 133) / 11.0
            keyframe_pose(
                armature,
                frame,
                body_bob=math.sin(settle * math.pi) * 0.01,
                head_yaw=(1.0 - settle) * 6.0,
                tail_sway=(1.0 - settle) * 5.0,
            )
    if armature.animation_data and armature.animation_data.action:
        armature.animation_data.action.name = "Squirrel_Demo_12fps"


def build_stage():
    floor_material = create_paper_material("Warm Paper Floor", hex_color("E9E3D7"), bump_strength=0.045)
    bpy.ops.mesh.primitive_plane_add(size=20.0, location=(0.0, 0.0, 0.0))
    assign_material(bpy.context.object, floor_material)
    world = bpy.context.scene.world
    world.use_nodes = True
    world.node_tree.nodes.get("Background").inputs["Color"].default_value = hex_color("F4F0E8")
    world.node_tree.nodes.get("Background").inputs["Strength"].default_value = 0.55
    for name, location, energy, size in (
        ("Soft Key", (3.5, -4.2, 5.5), 850.0, 4.0),
        ("Soft Fill", (-3.0, -1.0, 3.5), 400.0, 5.0),
        ("Soft Rim", (-1.5, 3.5, 4.2), 470.0, 3.0),
    ):
        bpy.ops.object.light_add(type="AREA", location=location)
        light = bpy.context.object
        light.name = name
        light.data.energy = energy
        light.data.size = size
        look_at(light, (0.0, 0.0, 0.9))
    camera_data = bpy.data.cameras.new("Squirrel Preview Camera")
    camera = bpy.data.objects.new("Squirrel Preview Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = (4.4, -6.4, 3.6)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 3.75
    look_at(camera, (-0.05, 0.0, 0.85))
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


def export_fbx(armature, squirrel_objects):
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    for obj in squirrel_objects:
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
    scene.frame_set(61)
    scene.render.filepath = str(HERO_PATH)
    bpy.ops.render.render(write_still=True)


def main():
    clear_scene()
    configure_render()
    armature, squirrel_objects = build_squirrel()
    animate_squirrel(armature)
    record_source_dimensions(armature, squirrel_objects)
    build_stage()
    bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    export_fbx(armature, squirrel_objects)
    render_outputs()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    print(f"BLEND={BLEND_PATH}")
    print(f"FBX={FBX_PATH}")
    print(f"FRAMES={FRAMES_DIR}")
    print(f"HERO={HERO_PATH}")


if __name__ == "__main__":
    main()
