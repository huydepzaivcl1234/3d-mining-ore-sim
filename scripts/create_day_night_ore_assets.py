"""Create two low-poly timed ores.

The script runs with regular Python to emit Unity-ready OBJ files. When run from
Blender it also imports those files, saves an editable .blend source, and exports
FBX copies. Geometry is deterministic so the source can be regenerated safely.
"""

import math
import os
import random


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
MODEL_DIR = os.path.join(ROOT, "Assets", "Ores", "Models")
SOURCE_DIR = os.path.join(ROOT, "ArtSource", "Blender")


class ObjWriter:
    def __init__(self, mtl_name):
        self.lines = [f"mtllib {mtl_name}"]
        self.vertex_count = 0

    def add_mesh(self, name, vertices, faces, material):
        self.lines.extend((f"o {name}", f"usemtl {material}"))
        for x, y, z in vertices:
            self.lines.append(f"v {x:.6f} {y:.6f} {z:.6f}")
        for face in faces:
            indices = " ".join(str(self.vertex_count + index + 1) for index in face)
            self.lines.append(f"f {indices}")
        self.vertex_count += len(vertices)

    def save(self, path):
        with open(path, "w", encoding="utf-8", newline="\n") as stream:
            stream.write("\n".join(self.lines) + "\n")


def rock_mesh(seed, center=(0.0, 0.78, 0.0), scale=(1.0, 0.78, 0.9), segments=10):
    rng = random.Random(seed)
    vertices = [(center[0], center[1] - scale[1], center[2])]
    rings = ((-0.58, 0.72), (0.02, 1.0), (0.58, 0.68))
    for ring_y, radius in rings:
        for index in range(segments):
            angle = math.tau * index / segments
            wobble = 1.0 + rng.uniform(-0.15, 0.15)
            vertices.append((
                center[0] + math.cos(angle) * scale[0] * radius * wobble,
                center[1] + ring_y * scale[1] + rng.uniform(-0.07, 0.07),
                center[2] + math.sin(angle) * scale[2] * radius * wobble,
            ))
    vertices.append((center[0], center[1] + scale[1], center[2]))

    faces = []
    for index in range(segments):
        next_index = (index + 1) % segments
        faces.append((0, 1 + next_index, 1 + index))
    for ring in range(2):
        first = 1 + ring * segments
        second = first + segments
        for index in range(segments):
            next_index = (index + 1) % segments
            faces.append((first + index, first + next_index, second + next_index, second + index))
    top = len(vertices) - 1
    last_ring = 1 + 2 * segments
    for index in range(segments):
        next_index = (index + 1) % segments
        faces.append((last_ring + index, last_ring + next_index, top))
    return vertices, faces


def shard_mesh(center, scale, rotation_y, seed):
    rng = random.Random(seed)
    base = [
        (-0.62, -0.5, -0.48), (0.58, -0.5, -0.42),
        (0.48, -0.5, 0.55), (-0.55, -0.5, 0.48),
        (-0.38, 0.5, -0.30), (0.34, 0.5, -0.27),
        (0.29, 0.5, 0.36), (-0.33, 0.5, 0.32),
    ]
    cosine, sine = math.cos(rotation_y), math.sin(rotation_y)
    vertices = []
    for x, y, z in base:
        x *= scale[0] * (1.0 + rng.uniform(-0.08, 0.08))
        y *= scale[1] * (1.0 + rng.uniform(-0.08, 0.08))
        z *= scale[2] * (1.0 + rng.uniform(-0.08, 0.08))
        vertices.append((
            center[0] + x * cosine - z * sine,
            center[1] + y,
            center[2] + x * sine + z * cosine,
        ))
    faces = [
        (0, 3, 2, 1), (4, 5, 6, 7),
        (0, 1, 5, 4), (1, 2, 6, 5),
        (2, 3, 7, 6), (3, 0, 4, 7),
    ]
    return vertices, faces


def write_ore(stem, seed, body_color, glow_color):
    os.makedirs(MODEL_DIR, exist_ok=True)
    writer = ObjWriter(stem + ".mtl")
    body_vertices, body_faces = rock_mesh(seed)
    writer.add_mesh("Body", body_vertices, body_faces, "Body")

    placements = [
        ((-0.92, 0.62, 0.18), (0.58, 0.92, 0.55), -0.35),
        ((0.88, 0.55, 0.28), (0.52, 1.05, 0.48), 0.42),
        ((0.05, 1.35, -0.42), (0.62, 1.18, 0.52), -0.12),
        ((-0.48, 0.92, 0.72), (0.50, 0.86, 0.48), 0.58),
        ((0.52, 0.86, -0.70), (0.46, 0.78, 0.45), -0.66),
        ((-0.58, 0.30, -0.62), (0.42, 0.72, 0.40), 0.24),
        ((0.58, 0.27, 0.68), (0.44, 0.68, 0.42), -0.48),
    ]
    for index, (center, scale, rotation) in enumerate(placements, start=1):
        vertices, faces = shard_mesh(center, scale, rotation, seed + index)
        writer.add_mesh(f"GlowShard_{index:02d}", vertices, faces, "Glow")

    obj_path = os.path.join(MODEL_DIR, stem + ".obj")
    writer.save(obj_path)
    mtl_path = os.path.join(MODEL_DIR, stem + ".mtl")
    with open(mtl_path, "w", encoding="utf-8", newline="\n") as stream:
        stream.write(
            "newmtl Body\n"
            f"Kd {body_color[0]} {body_color[1]} {body_color[2]}\n"
            "Ns 12\n\n"
            "newmtl Glow\n"
            f"Kd {glow_color[0]} {glow_color[1]} {glow_color[2]}\n"
            f"Ke {glow_color[0] * 2.5} {glow_color[1] * 2.5} {glow_color[2] * 2.5}\n"
            "Ns 80\n"
        )
    return obj_path


def save_blender_source(obj_paths):
    try:
        import bpy
    except ImportError:
        return False

    bpy.ops.wm.read_factory_settings(use_empty=True)
    for index, path in enumerate(obj_paths):
        bpy.ops.wm.obj_import(filepath=path)
        imported = list(bpy.context.selected_objects)
        offset = -1.6 if index == 0 else 1.6
        for target in imported:
            target.location.x += offset
    os.makedirs(SOURCE_DIR, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SOURCE_DIR, "day_night_ores.blend"))
    return True


def main():
    light = write_ore("light_stone", 710, (0.38, 0.40, 0.43), (1.0, 0.78, 0.18))
    dark = write_ore("dark_stone", 920, (0.045, 0.035, 0.065), (0.42, 0.06, 0.92))
    blender_saved = save_blender_source((light, dark))
    print("CREATED", light)
    print("CREATED", dark)
    print("BLENDER_SOURCE_SAVED", blender_saved)


if __name__ == "__main__":
    main()
