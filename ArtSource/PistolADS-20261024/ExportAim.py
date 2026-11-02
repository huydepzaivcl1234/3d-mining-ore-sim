"""Run in Blender's Text Editor after keying Pistol_Aim_Control at frame 12.
Exports only camera-local presentation, never changes hand bones or reload clips.
"""
import bpy, json
from pathlib import Path
from mathutils import Matrix, Vector

out=Path(bpy.data.filepath).parent
scene=bpy.data.scenes['FPS_Pistol_Aim_Preview']
camera=scene.camera;root=scene.objects['Player_Rig_aim']
win=bpy.context.window_manager.windows[0];win.scene=scene
old_frame=scene.frame_current
scene.frame_set(1);win.view_layer.update()
hip=camera.matrix_world.inverted()@root.matrix_world
scene.frame_set(12);win.view_layer.update()
aim=camera.matrix_world.inverted()@root.matrix_world
mirror=Matrix.Diagonal((1,1,-1,1))
delta=mirror@aim@hip.inverted()@mirror
data=json.loads((out/'PistolAim.json').read_text())
position=delta@Vector(data['hipPosition']);q=delta.to_quaternion()
data['aimPosition']=list(position);data['aimQuaternion']=[q.x,q.y,q.z,q.w]
data['aimFov']=__import__('math').degrees(camera.data.angle_y)
(out/'PistolAim.json').write_text(json.dumps(data,indent=2))
scene.frame_set(old_frame)
print('Exported camera-local pistol ADS to',out/'PistolAim.json')
print('BUILD_OK')
