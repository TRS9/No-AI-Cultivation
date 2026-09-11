import bpy, math, json
from mathutils import Vector
from pathlib import Path
ROOT=Path(r'C:/Users/trs13/OneDrive/Dokumente/Cultivation Games/Unity/No AI Cultivation')
SOURCE=ROOT/'ArtSource/PrisonAlchemist';DEST=ROOT/'Assets/_Project/Characters/PrisonAlchemist'
rig=bpy.data.objects['PrisonAlchemist_Rig'];body=bpy.data.objects['PrisonAlchemist_Body'];scene=bpy.context.scene
qa={}
definitions=json.loads((SOURCE/'clips.json').read_text())
for name,definition in definitions.items():
    end=definition['end'];frames=sorted(set([1,round(1+(end-1)*.25),round(1+(end-1)*.5),round(1+(end-1)*.75),end]))
    rig.animation_data.action=bpy.data.actions[name];samples=[]
    for frame in frames:
        scene.frame_set(frame);bpy.context.view_layer.update();ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get())
        points=[ev.matrix_world@v.co for v in ev.data.vertices]
        samples.append({'frame':frame,'min_z':min(v.z for v in points),'max_z':max(v.z for v in points),'finite':all(math.isfinite(c) for v in points for c in v)})
    qa[name]=samples
qa['vertices']=len(body.data.vertices);qa['triangles']=sum(len(p.vertices)-2 for p in body.data.polygons)
qa['bones']=len(rig.data.bones);qa['weight_sum_error']=max(abs(sum(g.weight for g in v.groups)-1) for v in body.data.vertices)
qa['unweighted_vertices']=sum(not v.groups for v in body.data.vertices)
qa['max_influences']=max(len(v.groups) for v in body.data.vertices)
qa['root_horizontal_translation']=max(abs(c) for action in ['Idle','Walk','Run','Jump'] for c in (rig.location.x,rig.location.y))
(SOURCE/'blender_qa.json').write_text(json.dumps(qa,indent=2))
rig.hide_set(False)
for ob in bpy.context.selected_objects:ob.select_set(False)
rig.select_set(True);body.select_set(True);bpy.context.view_layer.objects.active=rig
lods=[bpy.data.objects.get('PrisonAlchemist_Medium'),bpy.data.objects.get('PrisonAlchemist_Far')]
for ob in lods:
    if ob:ob.hide_set(False);ob.hide_render=False;ob.select_set(True)
# Export the rest skeleton, not the currently posed limbs.
rig.animation_data.action=None
for pb in rig.pose.bones:
    pb.rotation_quaternion=(1,0,0,0);pb.location=(0,0,0)
bpy.context.view_layer.update()
bpy.ops.export_scene.fbx(filepath=str(DEST/'PrisonAlchemist.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},
    global_scale=1.0,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
    use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=False,add_leaf_bones=False,
    primary_bone_axis='Y',secondary_bone_axis='X',use_armature_deform_only=False,
    bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True,bake_anim_step=1.0,bake_anim_simplify_factor=0.0,
    path_mode='AUTO',embed_textures=False)
rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_set(1);rig.hide_set(True)
for ob in lods:
    if ob:ob.hide_set(True);ob.hide_render=True
for ob in bpy.context.selected_objects:ob.select_set(False)
body.select_set(True);bpy.context.view_layer.objects.active=body
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'PrisonAlchemist.blend'))
RESULT={'fbx':str(DEST/'PrisonAlchemist.fbx'),'bytes':(DEST/'PrisonAlchemist.fbx').stat().st_size,'qa':qa}
