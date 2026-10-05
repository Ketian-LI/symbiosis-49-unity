"""Rebuild the rigged hedgehog silhouette from the project's low-poly turnaround.

Load SourceAssets/Blender/SYMBIOSIS_49_Hedgehog_v04.blend before running.  The
source and the in-game FBX remain unchanged; this writes an editable v05 .blend
on the desktop and orthographic review images under Previews/HedgehogBlender_v05.
"""

import math
import random
import sys
from pathlib import Path

import bpy
from mathutils import Vector


TOOLS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS_DIR))
import create_hedgehog_animation as base  # noqa: E402


PROJECT_ROOT = TOOLS_DIR.parents[1]
SOURCE_BLEND = PROJECT_ROOT / "SourceAssets" / "Blender" / "SYMBIOSIS_49_Hedgehog_v04.blend"
OUTPUT_BLEND = (Path.home() / "Desktop" / "SYMBIOSIS_49_动物模型" /
                "SYMBIOSIS_49_刺猬_v05_三视图重做.blend")
PREVIEW_DIR = PROJECT_ROOT / "Previews" / "HedgehogBlender_v05"


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


def soft_low_poly_leg(name, start, end, radius, piece):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1.0,
                                          location=(start + end) * 0.5)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    obj.rotation_mode = "XYZ"
    obj.scale = (radius, radius, direction.length * 0.58)
    base.apply_scale(obj)
    base.assign_material(obj, piece)
    return obj


def layered_spine_mantle(materials):
    """One editable mesh of broad, staggered low-poly spines, not many needles."""
    rng = random.Random(49)
    vertices = []
    faces = []
    face_materials = []
    cx, cz = -0.18, 0.435
    rx, ry, rz = 0.635, 0.425, 0.375
    rows = 9
    columns = 15
    for row in range(rows):
        theta = 0.89 + row * (1.93 / (rows - 1))
        for column in range(columns):
            phi = -0.20 + column * (math.pi + 0.40) / (columns - 1)
            phi += (0.40 if row % 2 else 0.0) * (math.pi / columns)
            theta_j = theta + rng.uniform(-0.045, 0.045)
            ring = math.sin(theta_j)
            point = Vector((cx + rx * math.cos(theta_j),
                            ry * ring * math.cos(phi),
                            cz + rz * ring * math.sin(phi)))
            normal = Vector((math.cos(theta_j) / rx,
                             ring * math.cos(phi) / ry,
                             ring * math.sin(phi) / rz)).normalized()
            # The slant and flat four-sided base make the spikes read as a
            # layered fur mantle in profile, with a clear front-face opening.
            direction = (normal * 0.48 + Vector((-0.66, 0.0, 0.15))).normalized()
            length = rng.uniform(0.120, 0.175)
            if row in (0, rows - 1):
                length *= 0.86
            radius = rng.uniform(0.058, 0.072)
            tangent = direction.cross(Vector((0.0, 1.0, 0.0))).normalized()
            bitangent = direction.cross(tangent).normalized()
            start = len(vertices)
            # A broad diamond base and an off-axis point create triangular
            # highlights rather than thin quills or a smooth dark dome.
            for offset in (tangent * radius, bitangent * radius * 0.78,
                           -tangent * radius, -bitangent * radius * 0.78):
                vertices.append(tuple(point + offset - direction * 0.022))
            vertices.append(tuple(point + direction * length))
            tone = (row * 11 + column * 7 + rng.randrange(4)) % 13
            main_index = 2 if tone in (0, 5, 9) else (1 if tone in (3, 8) else 0)
            for facet in range(4):
                faces.append((start + facet, start + (facet + 1) % 4,
                              start + 4))
                face_materials.append(3 if main_index == 2 and facet == 0 else main_index)
            faces.append(tuple(start + facet for facet in (3, 2, 1, 0)))
            face_materials.append(main_index)
    mesh = bpy.data.meshes.new("Layered Hedgehog Spines Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("Layered Hedgehog Spines", mesh)
    bpy.context.collection.objects.link(obj)
    for piece in materials:
        mesh.materials.append(piece)
    for polygon, index in zip(mesh.polygons, face_materials):
        polygon.material_index = index
    return obj


def build_replacement(rig):
    shell_mat = material("Continuous Dark Spine Shell", "403025")
    dark = material("Charcoal Spines", "2F2520")
    brown = material("Ochre Spines", "523B2E")
    straw = material("Straw Spine Tips", "9A7753")
    face_mat = material("Warm Ochre Face", "D3B38C")
    cream = material("Cream Belly", "E8D2AF")
    paw_mat = material("Brown Paws", "6C4B3A")
    eye_mat = material("Dark Eyes", "211A18")
    inner_ear = material("Warm Ear Interiors", "A97867")

    for obj in tuple(bpy.data.objects):
        if obj.type == "MESH" and obj.name != "Plane":
            bpy.data.objects.remove(obj, do_unlink=True)

    objects = []

    def attach(obj, bone):
        base.bone_parent(obj, rig, bone)
        objects.append(obj)
        return obj

    underside = base.add_ico("Long Cream Underside", (0.04, 0.0, 0.245),
                             (0.57, 0.34, 0.17), cream, subdivisions=2)
    attach(underside, "body")
    shell = base.add_ico("Broad Dark Spine Shell", (-0.18, 0.0, 0.435),
                         (0.635, 0.425, 0.375), shell_mat, subdivisions=3)
    base.assign_faceted_palette(shell, (shell_mat, dark, brown, straw))
    attach(shell, "body")
    attach(layered_spine_mantle((brown, dark, straw, shell_mat)), "body")

    face = base.add_ico("Large Cream Face", (0.425, 0.0, 0.355),
                        (0.335, 0.276, 0.237), face_mat, subdivisions=2)
    attach(face, "head")
    attach(base.add_ico("Pale Face Mask", (0.575, 0.0, 0.305),
                        (0.205, 0.236, 0.137), cream, subdivisions=2), "head")
    attach(base.add_wedge("Gentle Pointed Muzzle", (0.64, 0.0, 0.353),
                          (0.90, 0.0, 0.315), 0.210, 0.145, cream), "head")
    attach(base.add_ico("Rounded Dark Nose", (0.880, 0.0, 0.318),
                        (0.054, 0.053, 0.049), eye_mat, subdivisions=1), "head")
    for label, side in (("Left", -1.0), ("Right", 1.0)):
        attach(base.add_ico(f"{label} Dark Eye",
                            (0.615, side * 0.177, 0.425),
                            (0.032, 0.018, 0.034), eye_mat, subdivisions=2), "head")
        ear = base.add_ico(f"{label} Rounded Ear",
                           (0.345, side * 0.256, 0.562),
                           (0.076, 0.075, 0.094), paw_mat, subdivisions=2)
        attach(ear, "head")
        attach(base.add_ico(f"{label} Ear Interior",
                            (0.385, side * 0.265, 0.565),
                            (0.035, 0.055, 0.063), inner_ear, subdivisions=2), "head")

    for bone, label, x, y, shoulder_x in (
        ("leg.FL", "Front Left", 0.40, -0.245, 0.23),
        ("leg.FR", "Front Right", 0.40, 0.245, 0.23),
        ("leg.BL", "Back Left", -0.50, -0.245, -0.35),
        ("leg.BR", "Back Right", -0.50, 0.245, -0.35),
    ):
        attach(soft_low_poly_leg(
            f"{label} Short Leg", (shoulder_x, y, 0.255),
            (x + 0.045, y, 0.065), 0.082, paw_mat), bone)
        attach(base.add_ico(f"{label} Broad Paw", (x + 0.065, y, 0.057),
                            (0.112, 0.091, 0.054), paw_mat, subdivisions=1), bone)
    return objects


def render_view(name, camera_location, target, scale, resolution):
    scene = bpy.context.scene
    camera = bpy.data.objects["Hedgehog Preview Camera"]
    camera.location = camera_location
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = scale
    base.look_at(camera, target)
    scene.camera = camera
    scene.render.resolution_x, scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(PREVIEW_DIR / f"Hedgehog_v05_{name}.png")
    bpy.ops.render.render(write_still=True)


def main():
    if Path(bpy.data.filepath).resolve() != SOURCE_BLEND.resolve():
        raise RuntimeError(f"Load the untouched v04 source first: {SOURCE_BLEND}")
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    OUTPUT_BLEND.parent.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.frame_set(1)
    rig = bpy.data.objects["Hedgehog Rig"]
    objects = build_replacement(rig)
    base.record_source_dimensions(rig, objects)
    render_view("front", (5.0, 0.0, 0.48), (0.13, 0.0, 0.47), 1.56, (800, 800))
    render_view("side", (0.0, -5.0, 0.50), (0.02, 0.0, 0.48), 1.95, (1100, 800))
    render_view("top", (0.02, 0.0, 5.0), (0.02, 0.0, 0.43), 1.95, (1100, 800))
    render_view("threequarter", (3.7, -5.2, 2.65), (0.05, 0.0, 0.42), 1.95, (1100, 800))
    scene.frame_set(39)
    render_view("waddle_frame_39", (3.7, -5.2, 2.65), (0.05, 0.0, 0.42), 1.95, (1100, 800))
    scene.frame_set(1)
    camera = bpy.data.objects["Hedgehog Preview Camera"]
    camera.location = (3.7, -5.2, 2.65)
    camera.data.ortho_scale = 1.95
    base.look_at(camera, (0.05, 0.0, 0.42))
    scene.render.resolution_x = 1100
    scene.render.resolution_y = 800
    scene.render.filepath = ""
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_BLEND))
    print(f"OUTPUT_BLEND={OUTPUT_BLEND}")
    print(f"PREVIEW_DIR={PREVIEW_DIR}")
    print(f"HEDGEHOG_PARTS={len(objects)}")


if __name__ == "__main__":
    main()
