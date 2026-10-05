"""Refine the existing rigged fox to match the supplied front/side/top study.

Run with Blender in background mode and Fox_v01.blend loaded. This script leaves
v01 and the in-game FBX untouched; it writes a new editable v02 .blend and
orthographic QA renders.
"""

import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


TOOLS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS_DIR))
import create_fox_animation as base  # noqa: E402


PROJECT_ROOT = TOOLS_DIR.parents[1]
SOURCE_BLEND = PROJECT_ROOT / "SourceAssets" / "Blender" / "SYMBIOSIS_49_Fox_v01.blend"
OUTPUT_BLEND = (Path.home() / "Desktop" / "SYMBIOSIS_49_动物模型" /
                "SYMBIOSIS_49_狐狸_v02_三视图改型.blend")
PREVIEW_DIR = PROJECT_ROOT / "Previews" / "FoxBlender_v02"


def material(name, color):
    result = bpy.data.materials.get(name)
    if result is None:
        result = base.create_paper_material(name, base.hex_color(color))
    rgba = base.hex_color(color)
    result.diffuse_color = rgba
    if result.use_nodes:
        shader = result.node_tree.nodes.get("Principled BSDF")
        if shader is not None:
            shader.inputs["Base Color"].default_value = rgba
    return result


def triangular_ear(name, side, center, scale, mat):
    bpy.ops.mesh.primitive_cone_add(
        vertices=4, radius1=1.0, radius2=0.015, depth=2.0,
        location=(center[0], side * center[1], center[2]),
        rotation=(0.0, math.radians(-8.0), math.radians(side * 8.0)),
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    base.apply_scale(obj)
    base.assign_material(obj, mat)
    return obj


def tail_shell(name, stations, mat, palette=None):
    facets = 8
    vertices = []
    for x, z, radius_y, radius_z in stations:
        for index in range(facets):
            angle = index / facets * math.tau
            vertices.append((x, math.cos(angle) * radius_y,
                             z + math.sin(angle) * radius_z))
    faces = [tuple(range(facets - 1, -1, -1))]
    for ring in range(len(stations) - 1):
        for index in range(facets):
            a = ring * facets + index
            b = ring * facets + (index + 1) % facets
            c = (ring + 1) * facets + (index + 1) % facets
            d = (ring + 1) * facets + index
            faces.extend(((a, b, c), (a, c, d)))
    faces.append(tuple((len(stations) - 1) * facets + index
                       for index in range(facets)))
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
    """Nonzero joint radii keep upper and lower leg pieces visually joined."""
    start = Vector(start)
    end = Vector(end)
    forward = (end - start).normalized()
    lateral = Vector((0.0, 1.0, 0.0))
    depth = forward.cross(lateral).normalized()
    facets = 8
    vertices = []
    for center, radius in ((start, start_radius), (end, end_radius)):
        for index in range(facets):
            angle = index / facets * math.tau
            point = center + lateral * math.cos(angle) * radius * 0.78 + \
                depth * math.sin(angle) * radius
            vertices.append(tuple(point))
    faces = [tuple(range(facets - 1, -1, -1)),
             tuple(range(facets, facets * 2))]
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


def build_replacement(armature):
    fur = material("Terracotta Red Fox Fur", "A9430B")
    fur_light = material("Copper Fox Highlight", "C26012")
    fur_dark = material("Auburn Back", "85300B")
    cream = material("Warm Cream Markings", "E7D4B5")
    cream_light = material("Cream Light Facets", "F4E4C9")
    cream_dark = material("Cream Shadow Facets", "CBB69A")
    black = material("Charcoal Legs and Ears", "2C2926")
    paw = material("Dark Paws", "38312C")
    eye = material("Fox Eyes", "1E1916")

    for obj in tuple(bpy.data.objects):
        if obj.type == "MESH" and obj.name != "Plane":
            bpy.data.objects.remove(obj, do_unlink=True)

    fox_objects = []

    def attach(obj, bone):
        base.bone_parent(obj, armature, bone)
        fox_objects.append(obj)
        return obj

    body = base.add_ico("Long Faceted Torso", (-0.18, 0.0, 0.84),
                        (0.78, 0.315, 0.315), fur, subdivisions=2)
    base.assign_faceted_palette(body, (fur, fur_light, fur_dark))
    attach(body, "body")
    attach(base.add_ico("Cream Belly", (0.06, 0.0, 0.635),
                        (0.54, 0.23, 0.105), cream, subdivisions=2), "body")
    chest = base.add_ico("Sloped Fox Chest", (0.44, 0.0, 0.89),
                         (0.35, 0.28, 0.35), fur_light, subdivisions=2)
    base.assign_faceted_palette(chest, (fur_light, fur, fur_dark))
    attach(chest, "body")
    chest_cream = base.add_ico("Rounded Cream Chest", (0.83, 0.0, 0.965),
                               (0.175, 0.205, 0.255), cream, subdivisions=2)
    base.assign_faceted_palette(chest_cream, (cream, cream_light, cream_dark))
    attach(chest_cream, "body")

    neck = base.add_ellipsoid_between(
        "Fox Neck", (0.54, 0.0, 0.99), (0.76, 0.0, 1.19),
        0.22, fur, subdivisions=2)
    base.assign_faceted_palette(neck, (fur, fur_light, fur_dark))
    attach(neck, "neck")

    head = base.add_ico("Angular Fox Head", (0.89, 0.0, 1.29),
                        (0.31, 0.245, 0.255), fur, subdivisions=2)
    base.assign_faceted_palette(head, (fur, fur_light, fur_dark))
    attach(head, "head")
    attach(base.add_wedge("Orange Tapered Upper Muzzle", (1.05, 0.0, 1.24),
                          (1.45, 0.0, 1.17), 0.225, 0.17, fur_light), "head")
    attach(base.add_wedge("Cream Lower Muzzle", (1.04, 0.0, 1.14),
                          (1.43, 0.0, 1.105), 0.20, 0.11, cream), "head")
    attach(base.add_ico("Black Nose", (1.465, 0.0, 1.14),
                        (0.055, 0.055, 0.045), black, subdivisions=1), "head")
    for label, side in (("Left", -1.0), ("Right", 1.0)):
        attach(base.add_ico(f"{label} Cream Cheek",
                            (1.055, side * 0.197, 1.205),
                            (0.135, 0.030, 0.068), cream, subdivisions=1), "head")
        attach(base.add_ico(f"{label} Almond Eye",
                            (1.045, side * 0.190, 1.355),
                            (0.047, 0.013, 0.026), eye, subdivisions=2), "head")
        attach(triangular_ear(f"{label} Dark Ear Border", side,
                              (0.805, 0.170, 1.56), (0.125, 0.13, 0.19), black), "head")
        attach(triangular_ear(f"{label} Orange Outer Ear", side,
                              (0.835, 0.170, 1.555), (0.112, 0.112, 0.176), fur), "head")
        attach(triangular_ear(f"{label} Cream Inner Ear", side,
                              (0.920, 0.170, 1.54), (0.043, 0.065, 0.117), cream_light), "head")

    attach(tail_shell("Full Tail Base", [
        (-0.72, 0.81, 0.16, 0.16),
        (-0.92, 0.73, 0.22, 0.21),
        (-1.12, 0.65, 0.26, 0.25),
    ], fur, (fur, fur_light, fur_dark)), "tail.base")
    attach(tail_shell("Full Orange Tail", [
        (-1.12, 0.65, 0.26, 0.25),
        (-1.36, 0.56, 0.315, 0.29),
        (-1.60, 0.48, 0.285, 0.26),
    ], fur, (fur, fur_light, fur_dark)), "tail.mid")
    attach(tail_shell("Cream Tail Tip", [
        (-1.60, 0.48, 0.285, 0.26),
        (-1.79, 0.44, 0.22, 0.20),
        (-2.03, 0.41, 0.025, 0.035),
    ], cream, (cream, cream_light, cream_dark)), "tail.tip")

    legs = (
        ("FL", "Front Left", (0.48, -0.23, 0.91), (0.50, -0.23, 0.47), (0.59, -0.23, 0.12)),
        ("FR", "Front Right", (0.48, 0.23, 0.91), (0.50, 0.23, 0.47), (0.59, 0.23, 0.12)),
        ("BL", "Back Left", (-0.56, -0.24, 0.81), (-0.41, -0.24, 0.46), (-0.32, -0.24, 0.12)),
        ("BR", "Back Right", (-0.56, 0.24, 0.81), (-0.41, 0.24, 0.46), (-0.32, 0.24, 0.12)),
    )
    for key, label, hip, knee, ankle in legs:
        upper = limb_tube(
            f"{label} Orange Upper Leg", hip,
            (knee[0], knee[1], knee[2] - 0.045),
            0.17 if key.startswith("B") else 0.145, 0.12, fur_dark)
        base.assign_faceted_palette(upper, (fur_dark, fur, black))
        attach(upper, f"leg.{key}.upper")
        attach(limb_tube(
            f"{label} Dark Sock", (knee[0], knee[1], knee[2] + 0.045),
            (ankle[0], ankle[1], ankle[2] - 0.02), 0.125, 0.095, black),
            f"leg.{key}.lower")
        attach(base.add_ico(
            f"{label} Broad Paw", (ankle[0] + 0.08, ankle[1], 0.072),
            (0.16, 0.085, 0.067), paw, subdivisions=1), f"leg.{key}.lower")
    return fox_objects


def render_view(name, camera_location, target, ortho_scale, resolution):
    scene = bpy.context.scene
    camera = bpy.data.objects.get("Fox Preview Camera")
    camera.location = camera_location
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = ortho_scale
    base.look_at(camera, target)
    scene.camera = camera
    scene.render.resolution_x, scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(PREVIEW_DIR / f"Fox_v02_{name}.png")
    bpy.ops.render.render(write_still=True)


def main():
    if Path(bpy.data.filepath).resolve() != SOURCE_BLEND.resolve():
        raise RuntimeError(f"Load the untouched v01 source first: {SOURCE_BLEND}")
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    OUTPUT_BLEND.parent.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.frame_set(1)
    armature = bpy.data.objects["Fox Rig"]
    fox_objects = build_replacement(armature)
    base.record_source_dimensions(armature, fox_objects)
    render_view("front", (5.5, 0.0, 1.28), (0.30, 0.0, 0.95), 2.35, (800, 800))
    render_view("side", (0.0, -6.0, 1.35), (-0.25, 0.0, 0.93), 4.10, (1200, 800))
    render_view("top", (-0.25, 0.0, 6.0), (-0.25, 0.0, 0.65), 4.10, (1200, 800))
    render_view("threequarter", (4.7, -6.4, 3.1), (-0.23, 0.0, 0.86), 4.0, (1200, 800))
    scene.frame_set(39)
    render_view("trot_frame_39", (4.7, -6.4, 3.1), (-0.23, 0.0, 0.86), 4.0, (1200, 800))
    scene.frame_set(1)
    camera = bpy.data.objects["Fox Preview Camera"]
    camera.location = (4.7, -6.4, 3.1)
    camera.data.ortho_scale = 4.0
    base.look_at(camera, (-0.23, 0.0, 0.86))
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 800
    scene.render.filepath = ""
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_BLEND))
    print(f"OUTPUT_BLEND={OUTPUT_BLEND}")
    print(f"PREVIEW_DIR={PREVIEW_DIR}")
    print(f"FOX_PARTS={len(fox_objects)}")


if __name__ == "__main__":
    main()
