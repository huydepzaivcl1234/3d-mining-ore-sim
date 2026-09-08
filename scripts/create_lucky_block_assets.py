"""Create three stylized Lucky Blocks with Blender 5.2 and export Unity FBX assets."""
from __future__ import annotations

import math
import os

import bpy
from mathutils import Vector


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
MODEL_DIR = os.path.join(ROOT, "Assets", "Ores", "Models")
SOURCE_DIR = os.path.join(ROOT, "ArtSource", "Blender", "LuckyBlocks")
PREVIEW_DIR = os.path.join(ROOT, "previews")


VARIANTS = {
    "gold_lucky_block": {
        "name": "Gold Lucky Block",
        "body": (1.00, 0.48, 0.025, 1.0),
        "panel": [(1.00, 0.72, 0.08, 1.0)],
        "edge": (0.78, 0.19, 0.018, 1.0),
        "mark": (1.00, 0.97, 0.72, 1.0),
        "metallic": 0.42,
    },
    "diamond_lucky_block": {
        "name": "Diamond Lucky Block",
        "body": (0.015, 0.43, 0.68, 1.0),
        "panel": [(0.04, 0.78, 1.00, 1.0)],
        "edge": (0.015, 0.20, 0.38, 1.0),
        "mark": (0.78, 1.00, 1.00, 1.0),
        "metallic": 0.56,
    },
    "rainbow_lucky_block": {
        "name": "Rainbow Lucky Block",
        "body": (0.22, 0.035, 0.35, 1.0),
        "panel": [
            (1.00, 0.10, 0.12, 1.0),
            (1.00, 0.42, 0.04, 1.0),
            (1.00, 0.90, 0.06, 1.0),
            (0.08, 0.88, 0.30, 1.0),
            (0.05, 0.55, 1.00, 1.0),
            (0.66, 0.12, 1.00, 1.0),
        ],
        "edge": (0.98, 0.76, 0.12, 1.0),
        "mark": (1.00, 1.00, 1.00, 1.0),
        "metallic": 0.34,
    },
}


FACES = [
    ((0, -1, 0), (0, -0.955, 0)),
    ((1, 0, 0), (0.955, 0, 0)),
    ((0, 1, 0), (0, 0.955, 0)),
    ((-1, 0, 0), (-0.955, 0, 0)),
    ((0, 0, 1), (0, 0, 0.955)),
]


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        if collection.users == 0:
            bpy.data.collections.remove(collection)


def make_material(name, color, metallic=0.0, roughness=0.4, emission_strength=0.0):
    material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission_strength > 0.0:
        bsdf.inputs["Emission Color"].default_value = color
        bsdf.inputs["Emission Strength"].default_value = emission_strength
    return material


def bevel_object(target, width, segments=1):
    modifier = target.modifiers.new("Rounded bevel", "BEVEL")
    modifier.width = width
    modifier.segments = segments
    modifier.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = target
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def add_cube(name, location, scale, material, bevel=0.04):
    bpy.ops.mesh.primitive_cube_add(size=2.0, location=location)
    target = bpy.context.object
    target.name = name
    target.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0.0:
        bevel_object(target, bevel)
    target.data.materials.append(material)
    return target


def add_face_plate(name, normal, location, material):
    normal_vector = Vector(normal)
    plate = add_cube(name, location, (0.72, 0.72, 0.055), material, 0.055)
    plate.rotation_mode = "QUATERNION"
    plate.rotation_quaternion = normal_vector.to_track_quat("Z", "Y")
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    return plate


def add_question_mark(name, normal, location, material):
    normal_vector = Vector(normal)
    mark_location = Vector(location) + normal_vector * 0.075
    bpy.ops.object.text_add(location=mark_location)
    target = bpy.context.object
    target.name = name
    target.data.body = "?"
    target.data.align_x = "CENTER"
    target.data.align_y = "CENTER"
    target.data.size = 1.0
    target.data.extrude = 0.045
    target.data.bevel_depth = 0.018
    target.data.bevel_resolution = 1
    target.rotation_mode = "QUATERNION"
    target.rotation_quaternion = normal_vector.to_track_quat("Z", "Y")
    target.scale = (0.72, 0.72, 0.72)
    target.data.materials.append(material)
    bpy.context.view_layer.objects.active = target
    bpy.ops.object.convert(target="MESH")
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return target


def add_edge_guards(material):
    pieces = []
    edge_offset = 0.88
    length = 1.56
    thickness = 0.105
    for x in (-edge_offset, edge_offset):
        for y in (-edge_offset, edge_offset):
            pieces.append(add_cube("Vertical Guard", (x, y, 0),
                                   (thickness, thickness, length * 0.5), material, 0.035))
    for z in (-edge_offset, edge_offset):
        for y in (-edge_offset, edge_offset):
            pieces.append(add_cube("Horizontal X Guard", (0, y, z),
                                   (length * 0.5, thickness, thickness), material, 0.035))
        for x in (-edge_offset, edge_offset):
            pieces.append(add_cube("Horizontal Y Guard", (x, 0, z),
                                   (thickness, length * 0.5, thickness), material, 0.035))
    return pieces


def add_corner_caps(material):
    pieces = []
    for x in (-0.88, 0.88):
        for y in (-0.88, 0.88):
            for z in (-0.88, 0.88):
                pieces.append(add_cube("Corner Cap", (x, y, z),
                                       (0.16, 0.16, 0.16), material, 0.055))
    return pieces


def join_meshes(objects, name):
    bpy.ops.object.select_all(action="DESELECT")
    for target in objects:
        target.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    result = bpy.context.object
    result.name = name
    for polygon in result.data.polygons:
        polygon.use_smooth = False
    return result


def set_grounded_origin(target):
    bpy.context.view_layer.update()
    minimum_z = min((target.matrix_world @ Vector(corner)).z for corner in target.bound_box)
    target.location.z -= minimum_z
    bpy.context.view_layer.update()
    bpy.context.scene.cursor.location = (target.location.x, target.location.y, 0.0)
    bpy.context.view_layer.objects.active = target
    target.select_set(True)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")


def build_variant(stem, spec):
    prefix = stem.title().replace("_", "")
    body = make_material(prefix + "_Body", spec["body"], spec["metallic"], 0.30)
    edge = make_material(prefix + "_Edge", spec["edge"], 0.58, 0.23)
    mark = make_material(prefix + "_Question", spec["mark"], 0.18, 0.18, 0.35)
    panel_materials = [
        make_material(f"{prefix}_Panel_{index + 1:02d}", color,
                      spec["metallic"] * 0.7, 0.27, 0.08)
        for index, color in enumerate(spec["panel"])
    ]

    pieces = [add_cube("Core", (0, 0, 0), (0.86, 0.86, 0.86), body, 0.13)]
    pieces.extend(add_edge_guards(edge))
    pieces.extend(add_corner_caps(edge))
    for index, (normal, location) in enumerate(FACES):
        panel_material = panel_materials[index % len(panel_materials)]
        pieces.append(add_face_plate(f"Face Panel {index + 1}", normal, location, panel_material))
        pieces.append(add_question_mark(f"Question Mark {index + 1}", normal, location, mark))

    result = join_meshes(pieces, spec["name"])
    set_grounded_origin(result)
    return result


def export_variant(stem, spec):
    clear_scene()
    target = build_variant(stem, spec)
    source_path = os.path.join(SOURCE_DIR, stem + ".blend")
    export_path = os.path.join(MODEL_DIR, stem + ".fbx")
    bpy.ops.wm.save_as_mainfile(filepath=source_path)
    bpy.ops.object.select_all(action="DESELECT")
    target.select_set(True)
    bpy.context.view_layer.objects.active = target
    bpy.ops.export_scene.fbx(
        filepath=export_path,
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=False,
        axis_forward="-Z",
        axis_up="Y",
        object_types={"MESH"},
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
    )
    return source_path, export_path


def render_collection_preview():
    clear_scene()
    targets = []
    positions = (-2.45, 0.0, 2.45)
    for position, (stem, spec) in zip(positions, VARIANTS.items()):
        target = build_variant(stem, spec)
        target.location.x = position
        targets.append(target)

    floor = add_cube("Preview Floor", (0, 0, -0.12), (4.8, 2.8, 0.10),
                     make_material("PreviewFloor", (0.035, 0.045, 0.07, 1), 0.05, 0.48), 0.08)
    floor.select_set(False)
    bpy.ops.object.camera_add(location=(7.5, -11.5, 7.0))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0, 0, 1.0)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 57
    bpy.context.scene.camera = camera

    bpy.ops.object.light_add(type="AREA", location=(-4.0, -5.0, 8.5))
    key = bpy.context.object
    key.data.energy = 1250
    key.data.shape = "DISK"
    key.data.size = 5.0
    bpy.ops.object.light_add(type="AREA", location=(5.0, -1.0, 5.0))
    fill = bpy.context.object
    fill.data.energy = 900
    fill.data.color = (0.22, 0.48, 1.0)
    fill.data.size = 4.0

    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 24
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 650
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = os.path.join(PREVIEW_DIR, "lucky_blocks_blender.png")
    scene.world.color = (0.012, 0.018, 0.035)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SOURCE_DIR, "lucky_blocks.blend"))
    bpy.ops.render.render(write_still=True)


def main():
    bpy.context.preferences.filepaths.save_version = 0
    for folder in (MODEL_DIR, SOURCE_DIR, PREVIEW_DIR):
        os.makedirs(folder, exist_ok=True)
    for stem, spec in VARIANTS.items():
        source_path, export_path = export_variant(stem, spec)
        print("CREATED", source_path)
        print("CREATED", export_path)
    render_collection_preview()
    print("CREATED", os.path.join(SOURCE_DIR, "lucky_blocks.blend"))
    print("CREATED", os.path.join(PREVIEW_DIR, "lucky_blocks_blender.png"))


if __name__ == "__main__":
    main()
