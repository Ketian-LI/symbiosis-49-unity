"""Build the SYMBIOSIS: 49 low-poly paper hedgehog v04 from concept v02."""

import math
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[2]
BLEND_PATH = PROJECT_ROOT / "SourceAssets" / "Blender" / "SYMBIOSIS_49_Hedgehog_v04.blend"
FBX_PATH = PROJECT_ROOT / "Assets" / "Resources" / "Animals" / "Hedgehog" / "Hedgehog_Animated_v04.fbx"
PREVIEW_DIR = PROJECT_ROOT / "Previews" / "HedgehogBlender_v04"
FRAMES_DIR = PREVIEW_DIR / "frames"
HERO_PATH = PREVIEW_DIR / "HedgehogBlenderAnimation_v04.png"

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
    noise.inputs["Scale"].default_value = 34.0
    noise.inputs["Detail"].default_value = 2.0
    noise.inputs["Roughness"].default_value = 0.64
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = bump_strength
    bump.inputs["Distance"].default_value = 0.025
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
        pattern = (polygon.index * 7 + 3) % 23
        if pattern in (0, 11):
            polygon.material_index = 3
        elif pattern in (4, 15, 19):
            polygon.material_index = 2
        elif pattern in (2, 7, 13, 20):
            polygon.material_index = 1
        else:
            polygon.material_index = 0


def apply_scale(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.select_set(False)


def add_ico(name, location, scale, material, subdivisions=1, rotation=(0.0, 0.0, 0.0)):
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


def add_cylinder_between(name, start, end, radius, material, vertices=7):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    midpoint = (start + end) * 0.5
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=direction.length,
        location=midpoint,
    )
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


def add_spine(name, base, direction, length, radius, material):
    direction = Vector(direction).normalized()
    base = Vector(base)
    tip = base + direction * length
    midpoint = (base + tip) * 0.5
    bpy.ops.mesh.primitive_cone_add(
        vertices=5,
        radius1=radius,
        radius2=0.0,
        depth=length,
        location=midpoint,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    obj.rotation_mode = "XYZ"
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


def build_hedgehog():
    spine_shell = create_paper_material("Continuous Dark Spine Shell", hex_color("3F342B"))
    spine_dark = create_paper_material("Charcoal Spines", hex_color("24211E"))
    spine_mid = create_paper_material("Ochre Spines", hex_color("5B4638"))
    spine_light = create_paper_material("Straw Spine Tips", hex_color("9A774F"), bump_strength=0.05)
    face = create_paper_material("Warm Ochre Face", hex_color("C49B6C"))
    belly = create_paper_material("Cream Belly", hex_color("E0C79F"), bump_strength=0.055)
    paw = create_paper_material("Brown Paws", hex_color("5A4035"), bump_strength=0.04)
    eye = create_paper_material("Dark Eyes", hex_color("171513"), roughness=0.46, bump_strength=0.0)

    body_objects = []
    head_objects = []
    legs = {"leg.FL": [], "leg.FR": [], "leg.BL": [], "leg.BR": []}

    # A high, round shell is the primary silhouette.  The face is deliberately
    # nested beneath its front rim instead of being attached as a separate head.
    shell = add_ico("Continuous Faceted Spine Shell", (-0.07, 0.0, 0.42), (0.55, 0.40, 0.39), spine_shell, subdivisions=3)
    assign_faceted_palette(shell, (spine_shell, spine_dark, spine_mid, spine_light))
    body_objects.append(shell)
    body_objects.append(add_ico("Cream Underside", (0.01, -0.01, 0.19), (0.43, 0.31, 0.14), belly, subdivisions=2))

    silhouette_spines = [
        (-0.50, 0.00, 0.65), (-0.31, 0.00, 0.76), (-0.08, 0.00, 0.80),
        (0.14, 0.00, 0.75), (0.33, 0.00, 0.64),
        (-0.36, -0.36, 0.54), (-0.08, -0.39, 0.59), (0.22, -0.34, 0.53),
        (-0.36, 0.36, 0.54), (-0.08, 0.39, 0.59), (0.22, 0.34, 0.53),
        (-0.57, 0.00, 0.50),
    ]
    for spine_index, position in enumerate(silhouette_spines):
        x, y, _ = position
        direction = Vector((-0.10 + (x + 0.12) * 0.12, y * 0.42, 0.98)).normalized()
        body_objects.append(add_spine(
            f"Silhouette Spine {spine_index + 1:02d}",
            position,
            direction,
            0.047 + (spine_index % 3) * 0.006,
            0.030,
            (spine_light, spine_mid, spine_dark)[spine_index % 3],
        ))

    head_objects.append(add_ico("Tucked Short Face", (0.325, 0.0, 0.33), (0.175, 0.16, 0.135), face, subdivisions=2))
    head_objects.append(add_wedge("Very Short Muzzle", (0.40, 0.0, 0.33), (0.555, 0.0, 0.307), 0.115, 0.086, face))
    head_objects.append(add_ico("Nose", (0.562, 0.0, 0.307), (0.031, 0.029, 0.029), eye, subdivisions=1))
    for side_name, side in (("Visible", -1.0), ("Far", 1.0)):
        head_objects.append(add_ico(f"{side_name} Ear", (0.275, side * 0.145, 0.415), (0.027, 0.013, 0.029), spine_dark, subdivisions=1))
        head_objects.append(add_ico(f"{side_name} Eye", (0.410, side * 0.148, 0.362), (0.020, 0.009, 0.020), eye, subdivisions=2))

    leg_specs = (
        ("leg.FL", "Front Left", (0.23, -0.225, 0.16), (0.27, -0.225, 0.075)),
        ("leg.FR", "Front Right", (0.23, 0.225, 0.16), (0.27, 0.225, 0.075)),
        ("leg.BL", "Back Left", (-0.35, -0.225, 0.16), (-0.31, -0.225, 0.075)),
        ("leg.BR", "Back Right", (-0.35, 0.225, 0.16), (-0.31, 0.225, 0.075)),
    )
    for bone_name, label, start, end in leg_specs:
        # The limb itself stays inside the belly volume; only a tiny paw reads
        # from the gameplay camera, matching the approved concept silhouette.
        legs[bone_name].append(add_ico(f"{label} Tiny Paw", (end[0] + 0.022, end[1], 0.046), (0.047, 0.037, 0.020), paw, subdivisions=1))

    armature_data = bpy.data.armatures.new("Hedgehog Rig")
    armature = bpy.data.objects.new("Hedgehog Rig", armature_data)
    bpy.context.collection.objects.link(armature)
    armature.show_in_front = True
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bone_specs = {
        "root": ((0.0, 0.0, 0.04), (0.0, 0.0, 0.24), None),
        "body": ((-0.07, 0.0, 0.18), (-0.07, 0.0, 0.66), "root"),
        "head": ((0.33, 0.0, 0.23), (0.33, 0.0, 0.47), "body"),
        "leg.FL": ((0.23, -0.225, 0.16), (0.23, -0.225, 0.06), "body"),
        "leg.FR": ((0.23, 0.225, 0.16), (0.23, 0.225, 0.06), "body"),
        "leg.BL": ((-0.35, -0.225, 0.16), (-0.35, -0.225, 0.06), "body"),
        "leg.BR": ((-0.35, 0.225, 0.16), (-0.35, 0.225, 0.06), "body"),
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

    for obj in body_objects:
        bone_parent(obj, armature, "body")
    for obj in head_objects:
        bone_parent(obj, armature, "head")
    for bone_name, objects in legs.items():
        for obj in objects:
            bone_parent(obj, armature, bone_name)

    all_objects = body_objects + head_objects
    for objects in legs.values():
        all_objects.extend(objects)
    return armature, all_objects


def keyframe_pose(
    armature,
    frame,
    body_bob=0.0,
    body_pitch=0.0,
    body_roll=0.0,
    body_squash=0.0,
    head_pitch=0.0,
    head_yaw=0.0,
    head_retract=0.0,
    tuck=0.0,
    front_left=0.0,
    front_right=0.0,
    back_left=0.0,
    back_right=0.0,
):
    armature.location = (0.0, 0.0, body_bob)
    armature.keyframe_insert(data_path="location", frame=frame)
    bones = armature.pose.bones
    bones["body"].rotation_euler = (math.radians(body_roll), 0.0, math.radians(body_pitch))
    bones["body"].scale = (1.0 - body_squash * 0.10, 1.0, 1.0 - body_squash * 0.16)
    bones["head"].rotation_euler = (0.0, math.radians(head_yaw), math.radians(head_pitch))
    bones["head"].location = (-head_retract * 0.24, -head_retract * 0.05, 0.0)
    head_scale = 1.0 - tuck * 0.78
    bones["head"].scale = (head_scale, head_scale, head_scale)
    bones["leg.FL"].rotation_euler = (0.0, 0.0, math.radians(front_left))
    bones["leg.FR"].rotation_euler = (0.0, 0.0, math.radians(front_right))
    bones["leg.BL"].rotation_euler = (0.0, 0.0, math.radians(back_left))
    bones["leg.BR"].rotation_euler = (0.0, 0.0, math.radians(back_right))
    leg_scale = 1.0 - tuck * 0.88
    for leg_name in ("leg.FL", "leg.FR", "leg.BL", "leg.BR"):
        bones[leg_name].scale = (leg_scale, leg_scale, leg_scale)
    for bone in bones:
        bone.keyframe_insert(data_path="rotation_euler", frame=frame)
        bone.keyframe_insert(data_path="location", frame=frame)
        bone.keyframe_insert(data_path="scale", frame=frame)


def animate_hedgehog(armature):
    scene = bpy.context.scene
    bpy.context.preferences.edit.keyframe_new_interpolation_type = "CONSTANT"
    scene.frame_start = 1
    scene.frame_end = 144
    scene.render.fps = 24
    scene.timeline_markers.new("Idle", frame=1)
    scene.timeline_markers.new("Waddle", frame=25)
    scene.timeline_markers.new("Sniff", frame=73)
    scene.timeline_markers.new("Curl", frame=105)
    scene.timeline_markers.new("Settle", frame=133)
    for frame in range(1, 145, 2):
        if frame <= 24:
            time = (frame - 1) / 24.0
            keyframe_pose(
                armature,
                frame,
                body_bob=math.sin(time * math.tau) * 0.005,
                head_yaw=math.sin(time * math.tau * 0.60) * 6.0,
            )
        elif frame <= 72:
            phase = (frame - 25) / 12.0 * math.tau
            stride = math.sin(phase)
            keyframe_pose(
                armature,
                frame,
                body_bob=abs(stride) * 0.025,
                body_pitch=stride * 2.0,
                body_roll=stride * 3.5,
                head_pitch=-stride * 3.0,
                front_left=stride * 24.0,
                front_right=-stride * 24.0,
                back_left=-stride * 22.0,
                back_right=stride * 22.0,
            )
        elif frame <= 104:
            phase = ((frame - 73) % 16) / 16.0
            sniff = math.sin(phase * math.pi)
            keyframe_pose(
                armature,
                frame,
                body_bob=-sniff * 0.010,
                body_pitch=-sniff * 4.0,
                head_pitch=-sniff * 40.0,
                head_yaw=math.sin(phase * math.tau) * 9.0,
                front_left=-sniff * 12.0,
                front_right=-sniff * 12.0,
            )
        elif frame <= 132:
            phase = (frame - 105) / 27.0
            curl = math.sin(phase * math.pi)
            keyframe_pose(
                armature,
                frame,
                body_bob=-curl * 0.045,
                body_pitch=curl * 8.0,
                body_squash=curl,
                head_pitch=-curl * 80.0,
                head_retract=curl * 1.18,
                tuck=curl,
                front_left=-curl * 70.0,
                front_right=-curl * 70.0,
                back_left=curl * 58.0,
                back_right=curl * 58.0,
            )
        else:
            settle = (frame - 133) / 11.0
            keyframe_pose(
                armature,
                frame,
                body_bob=math.sin(settle * math.pi) * 0.006,
                head_yaw=(1.0 - settle) * 5.0,
            )
    if armature.animation_data and armature.animation_data.action:
        armature.animation_data.action.name = "Hedgehog_Demo_12fps"


def build_stage():
    floor_material = create_paper_material("Warm Paper Floor", hex_color("E9E3D7"), bump_strength=0.045)
    bpy.ops.mesh.primitive_plane_add(size=20.0, location=(0.0, 0.0, 0.0))
    floor = bpy.context.object
    floor.name = "Plane"
    assign_material(floor, floor_material)
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
        look_at(light, (0.0, 0.0, 0.45))
    camera_data = bpy.data.cameras.new("Hedgehog Preview Camera")
    camera = bpy.data.objects.new("Hedgehog Preview Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = (3.6, -5.4, 2.8)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 2.45
    look_at(camera, (0.05, 0.0, 0.42))
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


def export_fbx(armature, hedgehog_objects):
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    for obj in hedgehog_objects:
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
    armature, hedgehog_objects = build_hedgehog()
    animate_hedgehog(armature)
    record_source_dimensions(armature, hedgehog_objects)
    build_stage()
    bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    export_fbx(armature, hedgehog_objects)
    render_outputs()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    print(f"BLEND={BLEND_PATH}")
    print(f"FBX={FBX_PATH}")
    print(f"FRAMES={FRAMES_DIR}")
    print(f"HERO={HERO_PATH}")


if __name__ == "__main__":
    main()
