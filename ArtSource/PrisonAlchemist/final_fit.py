"""Resolve boot trim intersections seen in the second lit inspection."""
import bpy
body=bpy.data.objects['PrisonAlchemist_Body']
if not body.get('final_boot_fit'):
    names={g.index:g.name for g in body.vertex_groups}
    wrap=set();toe=set()
    for p in body.data.polygons:
        material=body.data.materials[p.material_index].name
        for index in p.vertices:
            v=body.data.vertices[index];bones=[names[g.group] for g in v.groups]
            if material=='Linen' and .19<v.co.z<.38 and any(n.endswith('LowerLeg') for n in bones):wrap.add(index)
            if material=='Copper' and any(n.endswith('Foot') for n in bones):toe.add(index)
    for index in wrap:
        v=body.data.vertices[index];center=.115 if v.co.x>0 else -.115
        v.co.x=center+(v.co.x-center)*1.24;v.co.y*=1.24
    for index in toe:body.data.vertices[index].co.y-=.024
    body.data.update();body['final_boot_fit']=True
RESULT={'boot_wrap_vertices':len(wrap) if 'wrap' in globals() else 0,'final_boot_fit':True}
