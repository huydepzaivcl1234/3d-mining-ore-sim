"""Build polished low-poly day/night ores with Blender and export Unity FBX."""
from __future__ import annotations
import math
import os
import random
import bpy
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
MODEL_DIR = os.path.join(ROOT, "Assets", "Ores", "Models")
SOURCE_DIR = os.path.join(ROOT, "ArtSource", "Blender")
PREVIEW_DIR = os.path.join(ROOT, "previews")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def make_material(name, base, metallic, roughness, emission=None, strength=0.0):
    result = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    result.diffuse_color = base
    result.use_nodes = True
    bsdf = result.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = base
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission is not None:
        bsdf.inputs["Emission Color"].default_value = emission
        bsdf.inputs["Emission Strength"].default_value = strength
    return result


def flat_shade(target):
    for polygon in target.data.polygons:
        polygon.use_smooth = False


def add_rock(name, location, scale, seed, assigned_material, subdivisions=2):
    rng = random.Random(seed)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1.0, location=location)
    target = bpy.context.object
    target.name = name
    for vertex in target.data.vertices:
        direction = vertex.co.normalized()
        wobble = 1.0 + rng.uniform(-0.18, 0.18)
        vertical = 1.0 + 0.08 * math.sin(direction.x * 8.0 + direction.y * 5.0)
        vertex.co *= wobble * vertical
    target.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    target.data.materials.append(assigned_material)
    flat_shade(target)
    return target


def add_crystal(name, location, scale, rotation, assigned_material, seed):
    rng = random.Random(seed)
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.42, radius2=0.18,
                                   depth=1.0, location=location, rotation=rotation)
    target = bpy.context.object
    target.name = name
    target.scale = scale
    for vertex in target.data.vertices:
        vertex.co.x *= 1.0 + rng.uniform(-0.10, 0.10)
        vertex.co.y *= 1.0 + rng.uniform(-0.10, 0.10)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = target.modifiers.new("Chipped edges", "BEVEL")
    bevel.width = 0.035
    bevel.segments = 1
    bpy.context.view_layer.objects.active = target
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    target.data.materials.append(assigned_material)
    flat_shade(target)
    return target


def join_objects(objects, name):
    bpy.ops.object.select_all(action="DESELECT")
    for target in objects:
        target.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    result = bpy.context.object
    result.name = name
    return result


def ground_origin(target):
    minimum_z = min((target.matrix_world @ Vector(corner)).z for corner in target.bound_box)
    target.location.z -= minimum_z
    bpy.context.view_layer.update()
    bpy.context.scene.cursor.location = (target.location.x, target.location.y, 0.0)
    bpy.context.view_layer.objects.active = target
    target.select_set(True)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")


def build_ore(name, seed, body_colors, core_colors, rubble, crystals):
    body_mat = make_material(name.replace(" ", "") + "_Body", body_colors, 0.12, 0.78)
    core_mat = make_material(name.replace(" ", "") + "_Core", core_colors[0], 0.28, 0.22,
                             core_colors[1], core_colors[2])
    pieces = [add_rock("Body", (0, 0, 0.82), (1.17, 1.02, 0.82), seed, body_mat)]
    for index, (position, scale) in enumerate(rubble):
        pieces.append(add_rock(f"Rubble_{index + 1:02d}", position, scale,
                               seed + 20 + index, body_mat, 1))
    for index, (position, scale, rotation) in enumerate(crystals):
        pieces.append(add_crystal(f"Core_{index + 1:02d}", position, scale, rotation,
                                  core_mat, seed + 60 + index))
    result = join_objects(pieces, name)
    ground_origin(result)
    return result


def export_asset(target, stem):
    bpy.ops.object.select_all(action="DESELECT")
    target.select_set(True)
    bpy.context.view_layer.objects.active = target
    target.location = (0, 0, 0)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SOURCE_DIR, stem + ".blend"))
    bpy.ops.export_scene.fbx(filepath=os.path.join(MODEL_DIR, stem + ".fbx"), use_selection=True,
        apply_unit_scale=True, bake_space_transform=False, axis_forward="-Z", axis_up="Y",
        object_types={"MESH"}, add_leaf_bones=False, bake_anim=False, path_mode="AUTO")


def render_preview(light, dark):
    light.location.x = -1.65
    dark.location.x = 1.65
    bpy.ops.object.camera_add(location=(5.9, -8.7, 5.0))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0, 0, 0.9)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = camera
    bpy.ops.object.light_add(type="AREA", location=(-3.5, -4.5, 7.0))
    bpy.context.object.data.energy = 1100
    bpy.context.object.data.shape = "DISK"
    bpy.context.object.data.size = 5.0
    bpy.ops.object.light_add(type="AREA", location=(4.0, 1.5, 4.5))
    bpy.context.object.data.energy = 700
    bpy.context.object.data.color = (0.28, 0.42, 0.55)
    bpy.context.object.data.size = 3.0
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x, scene.render.resolution_y, scene.render.resolution_percentage = 1000, 600, 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = os.path.join(PREVIEW_DIR, "day_night_ores_blender.png")
    scene.world.color = (0.035, 0.045, 0.06)
    bpy.ops.render.render(write_still=True)


def main():
    bpy.context.preferences.filepaths.save_version = 0
    for folder in (MODEL_DIR, SOURCE_DIR, PREVIEW_DIR):
        os.makedirs(folder, exist_ok=True)
    clear_scene()
    light = build_ore("Light Stone", 710, (0.34, 0.31, 0.25, 1.0),
        ((1.0, 0.62, 0.08, 1.0), (1.0, 0.42, 0.025, 1.0), 5.0),
        [((-0.92, -0.10, 0.34), (0.46, 0.40, 0.32)), ((0.90, 0.10, 0.30), (0.43, 0.48, 0.30)),
         ((-0.34, 0.82, 0.28), (0.41, 0.36, 0.27)), ((0.48, -0.76, 0.30), (0.39, 0.44, 0.29))],
        [((-0.58, -0.34, 1.18), (0.52, 0.48, 1.20), (0.10, -0.35, -0.20)),
         ((0.22, -0.58, 1.38), (0.62, 0.58, 1.48), (-0.18, 0.18, 0.08)),
         ((0.66, 0.10, 1.05), (0.46, 0.43, 1.05), (0.24, 0.35, 0.24)),
         ((-0.12, 0.50, 1.14), (0.38, 0.40, 0.92), (-0.20, -0.12, -0.18))])
    dark = build_ore("Dark Stone", 920, (0.018, 0.022, 0.028, 1.0),
        ((0.025, 0.065, 0.075, 1.0), (0.008, 0.035, 0.045, 1.0), 2.2),
        [((-0.98, 0.12, 0.28), (0.50, 0.46, 0.33)), ((0.90, -0.18, 0.33), (0.48, 0.42, 0.37)),
         ((0.38, 0.82, 0.26), (0.42, 0.50, 0.30)), ((-0.46, -0.78, 0.26), (0.44, 0.39, 0.28)),
         ((0.02, 0.02, 1.52), (0.36, 0.38, 0.32))],
        [((-0.56, -0.30, 1.13), (0.48, 0.43, 1.34), (0.18, -0.34, -0.24)),
         ((0.18, -0.55, 1.32), (0.55, 0.52, 1.62), (-0.20, 0.10, 0.06)),
         ((0.66, 0.04, 1.10), (0.44, 0.40, 1.18), (0.20, 0.38, 0.22)),
         ((-0.16, 0.52, 1.06), (0.40, 0.38, 1.02), (-0.18, -0.12, -0.18))])
    export_asset(light, "light_stone")
    export_asset(dark, "dark_stone")
    render_preview(light, dark)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SOURCE_DIR, "day_night_ores.blend"))
    print("CREATED", os.path.join(MODEL_DIR, "light_stone.fbx"))
    print("CREATED", os.path.join(MODEL_DIR, "dark_stone.fbx"))


if __name__ == "__main__":
    main()
