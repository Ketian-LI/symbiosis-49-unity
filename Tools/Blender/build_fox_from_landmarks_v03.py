"""Build an editable low-poly fox from explicit 3D landmarks.

Coordinate convention:
  +X points from the tail toward the nose.
  +Y points toward the fox's left side.
  +Z points upward.
  (0, 0, 0) is the ground projection between the four paws.

The script intentionally writes a new v03 modelling file and does not replace
the current Unity FBX.  It also exports the source landmarks as UTF-8 CSV and
renders clean and landmark-only orthographic QA views.
"""

import csv
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


TOOLS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS_DIR))
import create_fox_animation as base  # noqa: E402


PROJECT_ROOT = TOOLS_DIR.parents[1]
OUTPUT_DIR = (
    Path.home()
    / "Desktop"
    / "SYMBIOSIS_49_房间_Blender模型"
    / "人物与动物_独立文件"
)
BLEND_PATH = OUTPUT_DIR / "Fox_狐狸_v03_点位建立.blend"
CSV_PATH = OUTPUT_DIR / "Fox_狐狸_v03_点位.csv"
PREVIEW_DIR = PROJECT_ROOT / "Previews" / "FoxBlender_v03_Landmarks"


# These are control landmarks rather than every surface vertex.  The actual
# low-poly surfaces are derived from them using faceted cross-sections.
LANDMARKS = {
    "P00_ORIGIN": ((0.00, 0.00, 0.00), "原点/地面中心", "Ground origin"),
    "P01_PELVIS": ((-0.34, 0.00, 1.00), "骨盆中心", "Pelvis centre"),
    "P02_BODY": ((-0.05, 0.00, 1.04), "躯干中心", "Torso centre"),
    "P03_CHEST": ((0.40, 0.00, 1.08), "胸腔中心", "Chest centre"),
    "P04_NECK": ((0.62, 0.00, 1.27), "颈根", "Neck base"),
    "P05_HEAD": ((0.82, 0.00, 1.44), "头部中心", "Head centre"),
    "P06_MUZZLE": ((1.01, 0.00, 1.35), "吻部根", "Muzzle base"),
    "P07_NOSE": ((1.31, 0.00, 1.25), "鼻尖", "Nose tip"),
    "P08_TAIL_BASE": ((-0.55, 0.00, 1.00), "尾根", "Tail base"),
    "P09_TAIL_MID": ((-0.98, 0.00, 0.73), "尾部最宽点", "Tail maximum"),
    "P10_TAIL_TIP": ((-1.47, 0.00, 0.61), "尾尖", "Tail tip"),
    "P11_EAR_L_ROOT": ((0.73, 0.15, 1.55), "左耳根", "Left ear root"),
    "P12_EAR_L_TIP": ((0.68, 0.18, 1.82), "左耳尖", "Left ear tip"),
    "P13_EAR_R_ROOT": ((0.73, -0.15, 1.55), "右耳根", "Right ear root"),
    "P14_EAR_R_TIP": ((0.68, -0.18, 1.82), "右耳尖", "Right ear tip"),
    "P15_HIP_BL": ((-0.36, 0.22, 0.98), "左后髋", "Back-left hip"),
    "P16_KNEE_BL": ((-0.45, 0.22, 0.52), "左后膝", "Back-left knee"),
    "P17_ANKLE_BL": ((-0.39, 0.22, 0.16), "左后踝", "Back-left ankle"),
    "P18_PAW_BL": ((-0.22, 0.22, 0.06), "左后掌", "Back-left paw"),
    "P19_HIP_BR": ((-0.36, -0.22, 0.98), "右后髋", "Back-right hip"),
    "P20_KNEE_BR": ((-0.45, -0.22, 0.52), "右后膝", "Back-right knee"),
    "P21_ANKLE_BR": ((-0.39, -0.22, 0.16), "右后踝", "Back-right ankle"),
    "P22_PAW_BR": ((-0.22, -0.22, 0.06), "右后掌", "Back-right paw"),
    "P23_HIP_FL": ((0.45, 0.21, 1.02), "左前肩", "Front-left shoulder"),
    "P24_KNEE_FL": ((0.47, 0.21, 0.53), "左前肘", "Front-left elbow"),
    "P25_ANKLE_FL": ((0.53, 0.21, 0.16), "左前腕", "Front-left wrist"),
    "P26_PAW_FL": ((0.68, 0.21, 0.06), "左前掌", "Front-left paw"),
    "P27_HIP_FR": ((0.45, -0.21, 1.02), "右前肩", "Front-right shoulder"),
    "P28_KNEE_FR": ((0.47, -0.21, 0.53), "右前肘", "Front-right elbow"),
    "P29_ANKLE_FR": ((0.53, -0.21, 0.16), "右前腕", "Front-right wrist"),
    "P30_PAW_FR": ((0.68, -0.21, 0.06), "右前掌", "Front-right paw"),
}


GUIDE_CHAINS = (
    ("P00_ORIGIN", "P02_BODY", "P03_CHEST", "P04_NECK", "P05_HEAD", "P06_MUZZLE", "P07_NOSE"),
    ("P01_PELVIS", "P08_TAIL_BASE", "P09_TAIL_MID", "P10_TAIL_TIP"),
    ("P05_HEAD", "P11_EAR_L_ROOT", "P12_EAR_L_TIP"),
    ("P05_HEAD", "P13_EAR_R_ROOT", "P14_EAR_R_TIP"),
    ("P15_HIP_BL", "P16_KNEE_BL", "P17_ANKLE_BL", "P18_PAW_BL"),
    ("P19_HIP_BR", "P20_KNEE_BR", "P21_ANKLE_BR", "P22_PAW_BR"),
    ("P23_HIP_FL", "P24_KNEE_FL", "P25_ANKLE_FL", "P26_PAW_FL"),
    ("P27_HIP_FR", "P28_KNEE_FR", "P29_ANKLE_FR", "P30_PAW_FR"),
)


def material(name, hex_value, roughness=0.9, bump_strength=0.04):
    return base.create_paper_material(
        name,
        base.hex_color(hex_value),
        roughness=roughness,
        bump_strength=bump_strength,
    )


def emission_material(name, hex_value, strength=3.0):
    result = bpy.data.materials.new(name)
    result.diffuse_color = base.hex_color(hex_value)
    result.use_nodes = True
    nodes = result.node_tree.nodes
    links = result.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeEmission")
    shader.inputs["Color"].default_value = base.hex_color(hex_value)
    shader.inputs["Strength"].default_value = strength
    links.new(shader.outputs["Emission"], output.inputs["Surface"])
    return result


def triangular_ear(name, root, tip, half_width, depth, mat):
    root = Vector(root)
    tip = Vector(tip)
    vertices = [
        (root.x + depth, root.y - half_width, root.z),
        (root.x + depth, root.y + half_width, root.z),
        (tip.x + depth * 0.25, tip.y, tip.z),
        (root.x - depth, root.y - half_width, root.z),
        (root.x - depth, root.y + half_width, root.z),
        (tip.x - depth * 0.25, tip.y, tip.z),
    ]
    faces = (
        (0, 1, 2),
        (5, 4, 3),
        (0, 3, 4, 1),
        (1, 4, 5, 2),
        (2, 5, 3, 0),
    )
    mesh = bpy.data.meshes.new(f"{name} Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    base.assign_material(obj, mat)
    return obj


def tail_shell(name, stations, mat, palette=None):
    facets = 8
    vertices = []
    for x, z, radius_y, radius_z in stations:
        for index in range(facets):
            angle = index / facets * math.tau
            vertices.append(
                (x, math.cos(angle) * radius_y, z + math.sin(angle) * radius_z)
            )
    faces = [tuple(range(facets - 1, -1, -1))]
    for ring in range(len(stations) - 1):
        for index in range(facets):
            next_index = (index + 1) % facets
            a = ring * facets + index
            b = ring * facets + next_index
            c = (ring + 1) * facets + next_index
            d = (ring + 1) * facets + index
            faces.extend(((a, b, c), (a, c, d)))
    last_ring = (len(stations) - 1) * facets
    faces.append(tuple(last_ring + index for index in range(facets)))
    mesh = bpy.data.meshes.new(f"{name} Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    base.assign_material(obj, mat)
    if palette:
        base.assign_faceted_palette(obj, palette)
    return obj


def limb_tube(name, start, end, start_radius, end_radius, mat):
    start = Vector(start)
    end = Vector(end)
    forward = (end - start).normalized()
    lateral = Vector((0.0, 1.0, 0.0))
    profile = forward.cross(lateral).normalized()
    facets = 6
    vertices = []
    for center, radius in ((start, start_radius), (end, end_radius)):
        for index in range(facets):
            angle = index / facets * math.tau
            vertices.append(
                tuple(
                    center
                    + lateral * math.cos(angle) * radius * 0.78
                    + profile * math.sin(angle) * radius
                )
            )
    faces = [tuple(range(facets - 1, -1, -1)), tuple(range(facets, facets * 2))]
    for index in range(facets):
        following = (index + 1) % facets
        faces.append((index, following, facets + following, facets + index))
    mesh = bpy.data.meshes.new(f"{name} Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    base.assign_material(obj, mat)
    return obj


def point(key):
    return LANDMARKS[key][0]


def build_fox():
    fur = material("V03 Fox Orange", "AD430D")
    fur_light = material("V03 Fox Orange Highlight", "D45B16")
    fur_dark = material("V03 Fox Auburn Shadow", "76240B")
    cream = material("V03 Warm Cream", "EEDFCB", bump_strength=0.025)
    cream_light = material("V03 Cream Highlight", "FFF5E6", bump_strength=0.02)
    cream_dark = material("V03 Cream Shadow", "CAB8A2", bump_strength=0.02)
    black = material("V03 Charcoal", "2B2827", bump_strength=0.02)
    paw = material("V03 Dark Paw", "3A3432", bump_strength=0.02)
    eye = material("V03 Eye", "141211", roughness=0.42, bump_strength=0.0)

    groups = {
        "body": [],
        "neck": [],
        "head": [],
        "tail.base": [],
        "tail.mid": [],
        "tail.tip": [],
        "leg.FL.upper": [],
        "leg.FL.lower": [],
        "leg.FR.upper": [],
        "leg.FR.lower": [],
        "leg.BL.upper": [],
        "leg.BL.lower": [],
        "leg.BR.upper": [],
        "leg.BR.lower": [],
    }

    def keep(obj, bone_name):
        groups[bone_name].append(obj)
        return obj

    torso = base.add_ico(
        "V03 Long Faceted Torso", (-0.08, 0.0, 1.03), (0.58, 0.285, 0.30), fur, subdivisions=2
    )
    base.assign_faceted_palette(torso, (fur, fur_light, fur_dark))
    keep(torso, "body")
    rump = base.add_ico(
        "V03 Rounded Rump", (-0.39, 0.0, 1.00), (0.35, 0.30, 0.33), fur, subdivisions=2
    )
    base.assign_faceted_palette(rump, (fur, fur_light, fur_dark))
    keep(rump, "body")
    chest = base.add_ico(
        "V03 Angular Chest", (0.40, 0.0, 1.07), (0.34, 0.275, 0.37), fur_light, subdivisions=2
    )
    base.assign_faceted_palette(chest, (fur_light, fur, fur_dark))
    keep(chest, "body")
    belly = base.add_ico(
        "V03 Cream Belly", (0.02, 0.0, 0.79), (0.44, 0.225, 0.09), cream, subdivisions=2
    )
    base.assign_faceted_palette(belly, (cream, cream_light, cream_dark))
    keep(belly, "body")
    bib = base.add_ico(
        "V03 Cream Chest Bib", (0.57, 0.0, 1.05), (0.15, 0.22, 0.29), cream, subdivisions=2
    )
    base.assign_faceted_palette(bib, (cream, cream_light, cream_dark))
    keep(bib, "body")

    neck = base.add_ellipsoid_between(
        "V03 Sloped Neck", (0.48, 0.0, 1.16), point("P04_NECK"), 0.235, fur, subdivisions=2
    )
    base.assign_faceted_palette(neck, (fur, fur_light, fur_dark))
    keep(neck, "neck")

    head = base.add_ico("V03 Angular Head", point("P05_HEAD"), (0.28, 0.23, 0.235), fur, subdivisions=2)
    base.assign_faceted_palette(head, (fur, fur_light, fur_dark))
    keep(head, "head")
    keep(
        base.add_wedge(
            "V03 Orange Upper Muzzle", point("P06_MUZZLE"), point("P07_NOSE"), 0.205, 0.16, fur_light
        ),
        "head",
    )
    keep(
        base.add_wedge(
            "V03 Cream Lower Muzzle", (0.99, 0.0, 1.27), (1.285, 0.0, 1.22), 0.19, 0.10, cream
        ),
        "head",
    )
    keep(base.add_ico("V03 Black Nose", point("P07_NOSE"), (0.052, 0.050, 0.042), black, subdivisions=1), "head")

    for label, suffix, side in (("Left", "L", 1.0), ("Right", "R", -1.0)):
        ear_root = point(f"P11_EAR_L_ROOT" if suffix == "L" else "P13_EAR_R_ROOT")
        ear_tip = point(f"P12_EAR_L_TIP" if suffix == "L" else "P14_EAR_R_TIP")
        keep(triangular_ear(f"V03 {label} Ear Dark Border", ear_root, ear_tip, 0.108, 0.045, black), "head")
        orange_root = (ear_root[0] + 0.050, ear_root[1], ear_root[2] + 0.014)
        orange_tip = (ear_tip[0] + 0.035, ear_tip[1], ear_tip[2] - 0.020)
        keep(triangular_ear(f"V03 {label} Ear Orange", orange_root, orange_tip, 0.088, 0.035, fur_dark), "head")
        inner_root = (ear_root[0] + 0.090, ear_root[1] - side * 0.006, ear_root[2] + 0.035)
        inner_tip = (ear_tip[0] + 0.060, ear_tip[1] - side * 0.006, ear_tip[2] - 0.055)
        keep(triangular_ear(f"V03 {label} Ear Cream", inner_root, inner_tip, 0.047, 0.015, cream_light), "head")
        cheek = base.add_ico(
            f"V03 {label} Cream Cheek", (1.00, side * 0.194, 1.30), (0.15, 0.032, 0.085), cream, subdivisions=1
        )
        keep(cheek, "head")
        eye_obj = base.add_ico(
            f"V03 {label} Eye", (0.995, side * 0.205, 1.43), (0.040, 0.014, 0.024), eye, subdivisions=1
        )
        keep(eye_obj, "head")

    tail_base = tail_shell(
        "V03 Tail Base",
        [(-0.48, 0.99, 0.15, 0.15), (-0.70, 0.89, 0.235, 0.22), (-0.90, 0.77, 0.30, 0.27)],
        fur_dark,
        (fur_dark, fur, fur_light),
    )
    keep(tail_base, "tail.base")
    tail_mid = tail_shell(
        "V03 Full Orange Tail",
        [(-0.90, 0.77, 0.30, 0.27), (-1.12, 0.66, 0.305, 0.25), (-1.28, 0.61, 0.25, 0.20)],
        fur,
        (fur, fur_light, fur_dark),
    )
    keep(tail_mid, "tail.mid")
    tail_tip = tail_shell(
        "V03 Cream Tail Tip",
        [(-1.28, 0.61, 0.25, 0.20), (-1.40, 0.60, 0.15, 0.14), (-1.49, 0.61, 0.025, 0.035)],
        cream,
        (cream, cream_light, cream_dark),
    )
    keep(tail_tip, "tail.tip")

    leg_specs = (
        ("FL", "Front Left", "P23_HIP_FL", "P24_KNEE_FL", "P25_ANKLE_FL", "P26_PAW_FL"),
        ("FR", "Front Right", "P27_HIP_FR", "P28_KNEE_FR", "P29_ANKLE_FR", "P30_PAW_FR"),
        ("BL", "Back Left", "P15_HIP_BL", "P16_KNEE_BL", "P17_ANKLE_BL", "P18_PAW_BL"),
        ("BR", "Back Right", "P19_HIP_BR", "P20_KNEE_BR", "P21_ANKLE_BR", "P22_PAW_BR"),
    )
    for key, label, hip_key, knee_key, ankle_key, paw_key in leg_specs:
        hip, knee, ankle, paw_point = map(point, (hip_key, knee_key, ankle_key, paw_key))
        upper = limb_tube(
            f"V03 {label} Orange Upper Leg",
            hip,
            (knee[0], knee[1], knee[2] - 0.035),
            0.155 if key.startswith("B") else 0.135,
            0.105,
            fur_dark,
        )
        base.assign_faceted_palette(upper, (fur_dark, fur, black))
        keep(upper, f"leg.{key}.upper")
        lower = limb_tube(
            f"V03 {label} Dark Sock",
            (knee[0], knee[1], knee[2] + 0.035),
            (ankle[0], ankle[1], ankle[2] - 0.04),
            0.105,
            0.075,
            black,
        )
        keep(lower, f"leg.{key}.lower")
        paw_obj = base.add_ico(
            f"V03 {label} Paw",
            paw_point,
            (0.165, 0.090, 0.060),
            paw,
            subdivisions=1,
        )
        keep(paw_obj, f"leg.{key}.lower")

    armature_data = bpy.data.armatures.new("Fox V03 Landmark Rig")
    armature = bpy.data.objects.new("Fox V03 Landmark Rig", armature_data)
    bpy.context.collection.objects.link(armature)
    armature.show_in_front = True
    armature["coordinate_system"] = "+X nose, +Y fox-left, +Z up"
    armature["origin_definition"] = "Ground projection between four paws"
    armature["reference"] = "User supplied front/side/top low-poly fox study, 2026-09-29"
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bone_specs = {
        "root": (point("P00_ORIGIN"), (0.0, 0.0, 0.30), None),
        "body": ((-0.05, 0.0, 0.62), (-0.05, 0.0, 1.18), "root"),
        "neck": ((0.45, 0.0, 1.08), point("P04_NECK"), "body"),
        "head": ((0.66, 0.0, 1.28), (0.82, 0.0, 1.61), "neck"),
        "tail.base": (point("P08_TAIL_BASE"), (-0.82, 0.0, 0.82), "body"),
        "tail.mid": ((-0.82, 0.0, 0.82), point("P09_TAIL_MID"), "tail.base"),
        "tail.tip": (point("P09_TAIL_MID"), point("P10_TAIL_TIP"), "tail.mid"),
        "leg.FL.upper": (point("P23_HIP_FL"), point("P24_KNEE_FL"), "body"),
        "leg.FL.lower": (point("P24_KNEE_FL"), point("P25_ANKLE_FL"), "leg.FL.upper"),
        "leg.FR.upper": (point("P27_HIP_FR"), point("P28_KNEE_FR"), "body"),
        "leg.FR.lower": (point("P28_KNEE_FR"), point("P29_ANKLE_FR"), "leg.FR.upper"),
        "leg.BL.upper": (point("P15_HIP_BL"), point("P16_KNEE_BL"), "body"),
        "leg.BL.lower": (point("P16_KNEE_BL"), point("P17_ANKLE_BL"), "leg.BL.upper"),
        "leg.BR.upper": (point("P19_HIP_BR"), point("P20_KNEE_BR"), "body"),
        "leg.BR.lower": (point("P20_KNEE_BR"), point("P21_ANKLE_BR"), "leg.BR.upper"),
    }
    edit_bones = {}
    for bone_name, (head_position, tail_position, parent_name) in bone_specs.items():
        bone = armature_data.edit_bones.new(bone_name)
        bone.head = head_position
        bone.tail = tail_position
        if parent_name:
            bone.parent = edit_bones[parent_name]
        edit_bones[bone_name] = bone
    bpy.ops.object.mode_set(mode="POSE")
    for pose_bone in armature.pose.bones:
        pose_bone.rotation_mode = "XYZ"
    bpy.ops.object.mode_set(mode="OBJECT")
    armature.select_set(False)

    fox_objects = []
    for bone_name, objects in groups.items():
        fox_objects.extend(objects)
        for obj in objects:
            base.bone_parent(obj, armature, bone_name)
    return armature, fox_objects


def create_landmark_guides():
    guide_collection = bpy.data.collections.new("FOX_LANDMARKS_v03")
    bpy.context.scene.collection.children.link(guide_collection)
    marker_material = emission_material("Landmark Marker", "20E5FF", strength=4.0)
    origin_material = emission_material("Origin Marker", "FF3B6B", strength=4.0)
    line_material = emission_material("Landmark Lines", "47BBD0", strength=2.5)

    guide_objects = []
    for key, (location, label_zh, label_en) in LANDMARKS.items():
        bpy.ops.mesh.primitive_ico_sphere_add(
            subdivisions=1,
            radius=0.038 if key == "P00_ORIGIN" else 0.024,
            location=location,
        )
        obj = bpy.context.object
        obj.name = f"{key}_{label_zh}"
        obj["label_zh"] = label_zh
        obj["label_en"] = label_en
        obj["coordinate"] = tuple(location)
        obj.show_name = True
        base.assign_material(obj, origin_material if key == "P00_ORIGIN" else marker_material)
        for collection in tuple(obj.users_collection):
            collection.objects.unlink(obj)
        guide_collection.objects.link(obj)
        guide_objects.append(obj)

    for chain_index, chain in enumerate(GUIDE_CHAINS):
        curve = bpy.data.curves.new(f"Guide Chain {chain_index:02d}", type="CURVE")
        curve.dimensions = "3D"
        curve.resolution_u = 1
        curve.bevel_depth = 0.006
        curve.bevel_resolution = 0
        spline = curve.splines.new("POLY")
        spline.points.add(len(chain) - 1)
        for index, key in enumerate(chain):
            x, y, z = point(key)
            spline.points[index].co = (x, y, z, 1.0)
        obj = bpy.data.objects.new(f"GUIDE_CHAIN_{chain_index:02d}", curve)
        guide_collection.objects.link(obj)
        base.assign_material(obj, line_material)
        guide_objects.append(obj)

    return guide_collection, guide_objects


def export_landmarks():
    with CSV_PATH.open("w", encoding="utf-8-sig", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(("id", "中文名称", "English name", "x", "y", "z"))
        for key, (location, label_zh, label_en) in LANDMARKS.items():
            writer.writerow((key, label_zh, label_en, *[f"{value:.3f}" for value in location]))


def set_objects_renderable(objects, renderable):
    for obj in objects:
        obj.hide_render = not renderable


def render_view(name, camera_location, target, ortho_scale, resolution):
    camera = bpy.data.objects["Fox Preview Camera"]
    camera.location = camera_location
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = ortho_scale
    base.look_at(camera, target)
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.resolution_x, scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(PREVIEW_DIR / f"Fox_v03_{name}.png")
    bpy.ops.render.render(write_still=True)


def configure_scene():
    base.configure_render()
    scene = bpy.context.scene
    scene.render.image_settings.color_mode = "RGBA"
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        scene.render.engine = "BLENDER_EEVEE"
    if hasattr(scene, "eevee"):
        scene.eevee.taa_render_samples = 32
    scene["fox_v03_axis_x"] = "+X toward nose"
    scene["fox_v03_axis_y"] = "+Y toward fox left"
    scene["fox_v03_axis_z"] = "+Z up"
    scene["fox_v03_origin"] = "Ground projection between paws"


def main():
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    base.clear_scene()
    configure_scene()
    armature, fox_objects = build_fox()
    base.animate_fox(armature)
    base.record_source_dimensions(armature, fox_objects)
    base.build_stage()
    guide_collection, guide_objects = create_landmark_guides()
    export_landmarks()

    scene = bpy.context.scene
    scene.frame_set(1)
    set_objects_renderable(guide_objects, False)
    set_objects_renderable(fox_objects, True)
    render_view("front", (5.0, 0.0, 1.16), (0.40, 0.0, 0.93), 2.20, (800, 800))
    render_view("side", (0.0, -5.2, 1.16), (-0.08, 0.0, 0.93), 3.35, (1200, 800))
    render_view("top", (-0.08, 0.0, 5.0), (-0.08, 0.0, 0.78), 3.35, (1200, 800))
    render_view("threequarter", (4.1, -5.3, 3.0), (-0.08, 0.0, 0.92), 3.20, (1200, 800))

    # Landmark-only QA views avoid hidden/occluded points while keeping the
    # exact source coordinates available in the saved Blender scene.
    set_objects_renderable(fox_objects, False)
    set_objects_renderable(guide_objects, True)
    render_view("landmarks_side", (0.0, -5.2, 1.16), (-0.08, 0.0, 0.93), 3.35, (1200, 800))
    render_view("landmarks_top", (-0.08, 0.0, 5.0), (-0.08, 0.0, 0.78), 3.35, (1200, 800))

    set_objects_renderable(fox_objects, True)
    set_objects_renderable(guide_objects, False)
    guide_collection.hide_viewport = False
    scene.frame_set(1)
    camera = bpy.data.objects["Fox Preview Camera"]
    camera.location = (4.1, -5.3, 3.0)
    camera.data.ortho_scale = 3.20
    base.look_at(camera, (-0.08, 0.0, 0.92))
    scene.render.filepath = ""
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    print(f"OUTPUT_BLEND={BLEND_PATH}")
    print(f"COORDINATES={CSV_PATH}")
    print(f"PREVIEW_DIR={PREVIEW_DIR}")
    print(f"LANDMARK_COUNT={len(LANDMARKS)}")
    print(f"FOX_PARTS={len(fox_objects)}")


if __name__ == "__main__":
    main()
