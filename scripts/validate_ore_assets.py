import bpy
import os
import sys


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
ASSET_DIR = os.path.join(ROOT, "assets", "ores")
EXPECTED = {
    "stone_tier1.glb": ("stone", 1),
    "coal_tier2.glb": ("coal", 2),
    "copper_tier3.glb": ("copper", 3),
}


def fail(message):
    print("VALIDATION_ERROR:", message)
    raise SystemExit(1)


for filename, (ore_id, tier) in EXPECTED.items():
    path = os.path.join(ASSET_DIR, filename)
    if not os.path.isfile(path) or os.path.getsize(path) < 1000:
        fail(f"Missing or empty asset: {path}")

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=path)
    objects = list(bpy.context.scene.objects)
    meshes = [obj for obj in objects if obj.type == "MESH"]
    roots = [obj for obj in objects if obj.get("ore_id") == ore_id]
    if len(roots) != 1:
        fail(f"{filename}: expected one metadata root for {ore_id}, found {len(roots)}")
    if roots[0].get("tier") != tier:
        fail(f"{filename}: expected tier {tier}, found {roots[0].get('tier')}")
    if len(meshes) < 2:
        fail(f"{filename}: expected body and visible deposits")

    triangles = sum(sum(max(0, len(poly.vertices) - 2) for poly in mesh.data.polygons) for mesh in meshes)
    print(f"VALID {filename}: meshes={len(meshes)} triangles={triangles} tier={tier}")

blend_path = os.path.join(ASSET_DIR, "mining_ores.blend")
if not os.path.isfile(blend_path) or os.path.getsize(blend_path) < 1000:
    fail(f"Missing editable Blender source: {blend_path}")

print("ALL_ORE_ASSETS_VALID")
