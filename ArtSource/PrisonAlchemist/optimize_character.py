"""One palette atlas/material per visible LOD; retain original UVs in SourceUV."""
import bpy,json
from pathlib import Path
ROOT=Path(r'C:/Users/trs13/OneDrive/Dokumente/Cultivation Games/Unity/No AI Cultivation')
DEST=ROOT/'Assets/_Project/Characters/PrisonAlchemist';body=bpy.data.objects['PrisonAlchemist_Body']
rig=bpy.data.objects['PrisonAlchemist_Rig']
if not body.get('palette_atlas'):
    colors=[tuple(m.diffuse_color) for m in body.data.materials]
    names=[m.name for m in body.data.materials]
    atlas=bpy.data.images.new('AlchemistPalette',width=64,height=64,alpha=True)
    atlas.colorspace_settings.name='Non-Color'
    pixels=[]
    for y in range(64):
        for x in range(64):
            index=(y//16)*4+x//16
            pixels.extend(colors[index] if index<len(colors) else (0,0,0,1))
    atlas.pixels.foreach_set(pixels);atlas.filepath_raw=str(DEST/'AlchemistPalette.png');atlas.file_format='PNG';atlas.save()
    if body.data.uv_layers.active:body.data.uv_layers.active.name='SourceUV'
    uv=body.data.uv_layers.new(name='PaletteUV');uv.active_render=True
    body.data.uv_layers.active=uv
    for poly in body.data.polygons:
        index=poly.material_index;point=((index%4+.5)/4,(index//4+.5)/4)
        for loop in poly.loop_indices:uv.data[loop].uv=point
        poly.material_index=0
    # FBX uses the first UV channel for Unity's material sampling.
    original=body.data.uv_layers.get('SourceUV')
    original_data=[tuple(v.uv) for v in original.data]
    palette_data=[tuple(v.uv) for v in uv.data]
    original.name='PaletteUV0';uv.name='SourceUV'
    for i,point in enumerate(palette_data):original.data[i].uv=point
    for i,point in enumerate(original_data):uv.data[i].uv=point
    body.data.uv_layers.active=original;original.active_render=True
    material=bpy.data.materials.new('AlchemistPalette');material.use_nodes=True
    shader=material.node_tree.nodes.get('Principled BSDF')
    texture=material.node_tree.nodes.new('ShaderNodeTexImage');texture.image=atlas;texture.interpolation='Closest'
    material.node_tree.links.new(texture.outputs['Color'],shader.inputs['Base Color'])
    shader.inputs['Roughness'].default_value=.65
    body.data.materials.clear();body.data.materials.append(material);body['palette_atlas']=True
    (DEST/'palette_swatches.json').write_text(json.dumps({'swatches':[dict(name=n,r=c[0],g=c[1],b=c[2]) for n,c in zip(names,colors)]},indent=2))

rig.animation_data.action=None
for pb in rig.pose.bones:pb.rotation_quaternion=(1,0,0,0);pb.location=(0,0,0)
lods=[]
for name,ratio in [('PrisonAlchemist_Medium',.50),('PrisonAlchemist_Far',.22)]:
    old=bpy.data.objects.get(name)
    if old:bpy.data.objects.remove(old,do_unlink=True)
    ob=body.copy();ob.data=body.data.copy();ob.name=name;body.users_collection[0].objects.link(ob)
    bpy.context.view_layer.objects.active=ob;ob.hide_set(False)
    # Simplify the rest mesh before the Armature modifier deforms it.
    mod=ob.modifiers.new('LOD simplification','DECIMATE');mod.ratio=ratio;mod.use_collapse_triangulate=True
    bpy.ops.object.modifier_move_up(modifier=mod.name)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    lods.append({'name':name,'triangles':sum(len(p.vertices)-2 for p in ob.data.polygons)})
    ob.hide_set(True);ob.hide_render=True
rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
RESULT={'lods':lods,'materials_per_lod':1,'palette':str(DEST/'AlchemistPalette.png')}
