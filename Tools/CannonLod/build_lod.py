import bpy, bmesh, sys, json
sys.path.insert(0, r'C:/Users/huyancut/.codex/skills/blender-modeling/scripts')
import helpers as H
H.clear_scene()
root = r'D:/3d mining sim'
bpy.ops.import_scene.fbx(filepath=root+'/Assets/GameData/Tower/Cannon/Source/ReferenceCannon.fbx')
stats = []
for obj in list(bpy.context.scene.objects):
    if obj.type != 'MESH': continue
    bpy.context.view_layer.objects.active=obj
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.calc_area()<1e-9],context='FACES')
    bm.to_mesh(obj.data); bm.free(); obj.data.update()
    before=sum(len(p.vertices)-2 for p in obj.data.polygons)
    mod=obj.modifiers.new('Cannon_LOD','DECIMATE'); mod.ratio=.35
    bpy.ops.object.modifier_apply(modifier=mod.name)
    after=sum(len(p.vertices)-2 for p in obj.data.polygons)
    stats.append(dict(name=obj.name,before=before,after=after,dimensions=list(obj.dimensions)))
bpy.ops.export_scene.fbx(filepath=root+'/Assets/GameData/Tower/Cannon/Source/CannonLOD.fbx',use_selection=False,add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
bpy.ops.wm.save_as_mainfile(filepath=root+'/Tools/CannonLod/CannonLOD.blend')
with open(root+'/Tools/CannonLod/report.json','w') as f: json.dump(stats,f,indent=2)
print(stats)
print('BUILD_OK')
