import bpy
import math
import os
import random
from mathutils import Vector


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
GLB_DIR = os.path.join(ROOT, "ArtSource", "GLB")
BLEND_DIR = os.path.join(ROOT, "ArtSource", "Blender")
UNITY_MODEL_DIR = os.path.join(ROOT, "Assets", "Ores", "Models")
PREVIEW_DIR = os.path.join(ROOT, "previews")


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.materials, bpy.data.curves, bpy.data.meshes, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def make_material(name, color, roughness=0.8, metallic=0.0):
    material = bpy.data.materials.new(name)
    material.diffuse_color = (*color, 1.0)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    return material


def move_to_collection(obj, collection):
    for current in list(obj.users_collection):
        current.objects.unlink(obj)
    collection.objects.link(obj)


def create_rock(name, collection, material, seed, scale=(1.0, 0.85, 0.75), subdivisions=2):
    rng = random.Random(seed)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1.0)
    rock = bpy.context.object
    rock.name = name
    move_to_collection(rock, collection)
    rock.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    for vertex in rock.data.vertices:
        direction = vertex.co.normalized()
        directional = 0.07 * math.sin(direction.x * 8.1 + direction.z * 3.7)
        vertex.co *= 1.0 + rng.uniform(-0.16, 0.14) + directional

    rock.data.materials.append(material)
    for polygon in rock.data.polygons:
        polygon.use_smooth = False
    bevel = rock.modifiers.new("EdgeSoftening", "BEVEL")
    bevel.width = 0.035
    bevel.segments = 1
    return rock


def add_chunk(name, collection, parent, material, location, scale, rotation, seed):
    chunk = create_rock(name, collection, material, seed, scale=scale, subdivisions=1)
    chunk.location = location
    chunk.rotation_euler = rotation
    chunk.parent = parent
    return chunk


def add_crystal(name, collection, parent, material, location, scale, rotation):
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.34, radius2=0.19, depth=0.65)
    crystal = bpy.context.object
    crystal.name = name
    move_to_collection(crystal, collection)
    crystal.data.materials.append(material)
    crystal.location = location
    crystal.scale = scale
    crystal.rotation_euler = rotation
    crystal.parent = parent
    bevel = crystal.modifiers.new("CrystalBevel", "BEVEL")
    bevel.width = 0.025
    bevel.segments = 1
    return crystal


def create_label(text, location, color):
    bpy.ops.object.text_add(location=location, rotation=(math.radians(72), 0, 0))
    label = bpy.context.object
    label.name = "Label_" + text.replace(" ", "_")
    label.data.body = text
    label.data.align_x = "CENTER"
    label.data.size = 0.42
    label.data.extrude = 0.012
    label.data.bevel_depth = 0.004
    label.data.materials.append(color)
    return label


def create_ore_collection(ore_id, display_name, tier, x, base_mat, accent_mat, style):
    collection = bpy.data.collections.new("ORE_" + ore_id.upper())
    bpy.context.scene.collection.children.link(collection)

    root = bpy.data.objects.new(ore_id + "_root", None)
    collection.objects.link(root)
    root.location.x = x
    root["ore_id"] = ore_id
    root["display_name"] = display_name
    root["tier"] = tier
    root["mining_power_required"] = {1: 1, 2: 3, 3: 6}[tier]
    root["base_sell_value"] = {1: 1, 2: 4, 3: 9}[tier]

    rock = create_rock(ore_id + "_body", collection, base_mat, 100 + tier, subdivisions=2)
    rock.parent = root

    if style == "stone":
        detail_mat = accent_mat
        placements = [
            ((-0.46, -0.57, 0.16), (0.19, 0.09, 0.26), (0.2, 0.0, -0.35)),
            ((0.38, -0.67, 0.04), (0.13, 0.06, 0.19), (-0.1, 0.25, 0.2)),
            ((0.12, -0.63, 0.49), (0.11, 0.05, 0.16), (0.35, 0.1, -0.1)),
        ]
    elif style == "coal":
        detail_mat = accent_mat
        placements = [
            ((-0.52, -0.46, 0.34), (0.24, 0.16, 0.38), (0.2, 0.45, -0.4)),
            ((0.0, -0.69, 0.13), (0.28, 0.14, 0.33), (-0.2, 0.05, 0.15)),
            ((0.48, -0.5, 0.29), (0.2, 0.13, 0.3), (0.15, -0.4, 0.35)),
            ((0.23, -0.49, 0.58), (0.13, 0.10, 0.22), (-0.1, 0.3, -0.2)),
            ((-0.25, -0.52, -0.26), (0.16, 0.10, 0.23), (0.35, 0.1, 0.25)),
        ]
    else:
        detail_mat = accent_mat
        placements = [
            ((-0.56, -0.45, 0.28), (0.25, 0.12, 0.34), (0.1, 0.4, -0.4)),
            ((-0.16, -0.7, -0.02), (0.32, 0.10, 0.28), (-0.25, 0.1, 0.25)),
            ((0.34, -0.58, 0.28), (0.27, 0.12, 0.4), (0.2, -0.35, 0.2)),
            ((0.54, -0.35, -0.18), (0.18, 0.10, 0.26), (-0.15, 0.2, 0.4)),
            ((0.04, -0.48, 0.58), (0.16, 0.10, 0.23), (0.25, 0.05, -0.15)),
            ((-0.38, -0.52, -0.29), (0.2, 0.09, 0.24), (0.05, -0.3, -0.15)),
        ]

    for index, (location, scale, rotation) in enumerate(placements, start=1):
        add_chunk(
            f"{ore_id}_deposit_{index:02d}", collection, root, detail_mat,
            location, scale, rotation, 500 + tier * 20 + index
        )

    return collection, root


def export_collection(collection, root, glb_output_path, fbx_output_path):
    previous_location = root.location.copy()
    root.location = (0, 0, 0)
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action="DESELECT")
    for obj in collection.all_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.gltf(
        filepath=glb_output_path,
        export_format="GLB",
        use_selection=True,
        export_apply=True,
        export_yup=True,
        export_materials="EXPORT",
        export_extras=True,
    )
    bpy.ops.export_scene.fbx(
        filepath=fbx_output_path,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        use_mesh_modifiers=True,
        bake_anim=False,
        path_mode="AUTO",
    )
    root.location = previous_location
    bpy.context.view_layer.update()


def setup_presentation(text_mat):
    bpy.ops.mesh.primitive_plane_add(size=14, location=(0, 0, -0.93))
    floor = bpy.context.object
    floor.name = "Preview_Ground"
    floor_mat = make_material("MAT_PreviewGround", (0.035, 0.045, 0.055), 0.92)
    floor.data.materials.append(floor_mat)

    create_label("TIER 1  STONE", (-3.0, -1.25, -0.82), text_mat)
    create_label("TIER 2  COAL", (0.0, -1.25, -0.82), text_mat)
    create_label("TIER 3  COPPER", (3.0, -1.25, -0.82), text_mat)

    bpy.ops.object.camera_add(location=(7.9, -11.8, 7.0))
    camera = bpy.context.object
    camera.name = "Preview_Camera"
    bpy.context.scene.camera = camera
    direction = Vector((0, 0, 0.0)) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 52

    for name, location, energy, size, color in [
        ("Key", (-4.5, -5.0, 7.5), 1200, 5.0, (1.0, 0.82, 0.65)),
        ("Fill", (5.5, -2.5, 4.0), 850, 4.0, (0.55, 0.72, 1.0)),
        ("Rim", (0.0, 4.0, 6.5), 1050, 3.0, (0.75, 0.88, 1.0)),
    ]:
        bpy.ops.object.light_add(type="AREA", location=location)
        light = bpy.context.object
        light.name = "Preview_" + name
        light.data.energy = energy
        light.data.shape = "DISK"
        light.data.size = size
        light.data.color = color
        light.rotation_euler = (0, 0, 0)
        light.rotation_euler = (Vector((0, 0, 0)) - light.location).to_track_quat("-Z", "Y").to_euler()

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = os.path.join(PREVIEW_DIR, "ore_set_preview.png")
    scene.render.film_transparent = False
    scene.world.color = (0.012, 0.018, 0.028)
    scene.view_settings.look = "AgX - Medium High Contrast"


def main():
    os.makedirs(GLB_DIR, exist_ok=True)
    os.makedirs(BLEND_DIR, exist_ok=True)
    os.makedirs(UNITY_MODEL_DIR, exist_ok=True)
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    reset_scene()

    stone_base = make_material("MAT_Stone_Base", (0.34, 0.37, 0.40), 0.92)
    stone_detail = make_material("MAT_Stone_Detail", (0.53, 0.57, 0.60), 0.86)
    coal_base = make_material("MAT_Coal_Base", (0.055, 0.065, 0.075), 0.76)
    coal_detail = make_material("MAT_Coal_Deposit", (0.095, 0.115, 0.135), 0.24, 0.12)
    copper_base = make_material("MAT_Copper_Rock", (0.27, 0.18, 0.13), 0.88)
    copper_detail = make_material("MAT_Copper_Deposit", (0.82, 0.29, 0.075), 0.28, 0.72)
    text_mat = make_material("MAT_Label", (0.86, 0.91, 0.98), 0.55)

    ores = [
        create_ore_collection("stone", "Stone", 1, -3.0, stone_base, stone_detail, "stone"),
        create_ore_collection("coal", "Coal", 2, 0.0, coal_base, coal_detail, "coal"),
        create_ore_collection("copper", "Copper", 3, 3.0, copper_base, copper_detail, "copper"),
    ]

    for (collection, root), stem in zip(ores, ("stone_tier1", "coal_tier2", "copper_tier3")):
        export_collection(
            collection,
            root,
            os.path.join(GLB_DIR, stem + ".glb"),
            os.path.join(UNITY_MODEL_DIR, stem + ".fbx"),
        )

    setup_presentation(text_mat)
    scene = bpy.context.scene
    scene["asset_set"] = "Mining Ore Starter Set"
    scene["units"] = "meters"
    scene["tier_order"] = "Stone=1, Coal=2, Copper=3"
    blend_path = os.path.join(BLEND_DIR, "mining_ores.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    bpy.ops.render.render(write_still=True)
    print("CREATED", blend_path)
    print("RENDERED", scene.render.filepath)


if __name__ == "__main__":
    main()
