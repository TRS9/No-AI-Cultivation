import bpy, json, math
from mathutils import Quaternion, Vector
rig=bpy.data.objects['PrisonAlchemist_Rig'];body=bpy.data.objects['PrisonAlchemist_Body'];scene=bpy.context.scene
rig.animation_data.action=bpy.data.actions['Run'];scene.frame_set(6)
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_rotation=bpy.data.objects['Character portrait'].rotation_euler.to_quaternion()
        area.spaces.active.region_3d.view_distance=2.9
        area.spaces.active.region_3d.view_location=(0,0,1)
bpy.context.view_layer.update()
evaluated=body.evaluated_get(bpy.context.evaluated_depsgraph_get())
coords=[evaluated.matrix_world@v.co for v in evaluated.data.vertices]
RESULT={'pose':'Run frame 6','bounds':[[min(v[i] for v in coords),max(v[i] for v in coords)] for i in range(3)],'weight_sum_error':max(abs(sum(g.weight for g in v.groups)-1) for v in body.data.vertices)}
