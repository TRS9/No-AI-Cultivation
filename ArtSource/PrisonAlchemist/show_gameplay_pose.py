import bpy
rig=bpy.data.objects['PrisonAlchemist_Rig'];rig.animation_data.action=bpy.data.actions['Meditate'];bpy.context.scene.frame_set(20)
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':area.spaces.active.shading.type='MATERIAL'
RESULT={'pose':'Meditate'}
