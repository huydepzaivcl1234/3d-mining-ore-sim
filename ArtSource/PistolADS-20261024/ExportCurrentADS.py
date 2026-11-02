"""Export the currently displayed ADS hand pose, without modifying the authoring rig."""
import bpy,json,math
from pathlib import Path
from mathutils import Matrix,Vector

OUT=Path(r'D:/3d mining sim/ArtSource/PistolADS-20261024')
scene=bpy.context.scene
rig=scene.objects['Player_Rig_aim'];camera=scene.camera
frame=scene.frame_current
bpy.context.view_layer.update()
aim=camera.matrix_world.inverted()@rig.matrix_world
aim_fov=math.degrees(camera.data.angle_y)
poses={b.name:b.matrix_basis.copy() for b in rig.pose.bones}
oldmode=rig.mode
selection=list(bpy.context.selected_objects);active=bpy.context.view_layer.objects.active
if bpy.context.object and bpy.context.object.mode!='OBJECT':bpy.ops.object.mode_set(mode='OBJECT')
# Calibrate camera-local frame against the original hip frame, not against an arbitrary hold frame.
scene.frame_set(1);bpy.context.view_layer.update()
hip=camera.matrix_world.inverted()@rig.matrix_world
hip_world=rig.matrix_world.copy()
scene.frame_set(frame);bpy.context.view_layer.update()
mirror=Matrix.Diagonal((1,1,-1,1))
delta=mirror@aim@hip.inverted()@mirror
data=json.loads((OUT/'PistolAim.json').read_text())
position=delta@Vector(data['hipPosition']);q=delta.to_quaternion()
data.update(aimPosition=list(position),aimQuaternion=[q.x,q.y,q.z,q.w],aimFov=aim_fov,source=bpy.data.filepath,sourceFrame=frame)
(OUT/'PistolAim_Current.json').write_text(json.dumps(data,indent=2))
temp_scene=bpy.data.scenes.new('Temporary_ADS_Export')
temp_scene.render.fps=60;temp_scene.frame_start=1;temp_scene.frame_end=2
name_owner=bpy.data.objects.get('Armature')
if name_owner:name_owner.name='Temporary_Original_Armature_Name'
clone=rig.copy();clone.data=rig.data.copy();clone.name='Armature'
clone.animation_data_clear();clone.parent=None
temp_scene.collection.objects.link(clone)
clone.matrix_world=hip_world
skin=scene.objects['Ch15_FPS_Arms'].copy();skin.data=skin.data.copy()
temp_scene.collection.objects.link(skin);skin.parent=clone;skin.matrix_parent_inverse=Matrix.Identity(4);skin.matrix_basis=Matrix.Identity(4)
for modifier in skin.modifiers:
    if modifier.type=='ARMATURE':modifier.object=clone
try:
    bpy.context.window.scene=temp_scene
    for o in bpy.context.selected_objects:o.select_set(False)
    clone.select_set(True);skin.select_set(True);bpy.context.view_layer.objects.active=clone
    for f in (1,2):
        temp_scene.frame_set(f)
        for name,matrix in poses.items():
            b=clone.pose.bones[name];b.matrix_basis=matrix
            b.keyframe_insert('location',frame=f);b.keyframe_insert('rotation_quaternion',frame=f);b.keyframe_insert('scale',frame=f)
    clone.animation_data.action.name='Pistol_ADS_Custom'
    bpy.ops.export_scene.fbx(filepath=str(OUT/'Pistol_ADS_Custom.fbx'),use_selection=True,
        object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,
        bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,
        axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
finally:
    bpy.context.window.scene=scene;scene.frame_set(frame)
    bpy.data.objects.remove(skin,do_unlink=True);bpy.data.objects.remove(clone,do_unlink=True);bpy.data.scenes.remove(temp_scene)
    if name_owner:name_owner.name='Armature'
    for o in bpy.context.selected_objects:o.select_set(False)
    for o in selection:o.select_set(True)
    bpy.context.view_layer.objects.active=active
    if oldmode=='POSE':bpy.ops.object.mode_set(mode='POSE')
print('Exported current ADS at frame',frame,'FOV',aim_fov)
print('BUILD_OK')
