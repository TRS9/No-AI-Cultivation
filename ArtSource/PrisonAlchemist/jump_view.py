import bpy
from mathutils import Quaternion
rig=bpy.data.objects['PrisonAlchemist_Rig'];rig.animation_data.action=bpy.data.actions['Jump'];bpy.context.scene.frame_set(15)
for a in bpy.context.screen.areas:
    if a.type=='VIEW_3D':
        a.spaces.active.region_3d.view_rotation=bpy.data.objects['Character portrait'].rotation_euler.to_quaternion()
        a.spaces.active.region_3d.view_distance=2.8
RESULT={'pose':'Jump / tucked flight, frame 15'}
