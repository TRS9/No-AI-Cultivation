"""Corrections identified in the lit and posed viewport inspections."""
import bpy
body=bpy.data.objects['PrisonAlchemist_Body'];rig=bpy.data.objects['PrisonAlchemist_Rig']
if not body.get('silhouette_refined'):
    names={g.index:g.name for g in body.vertex_groups}
    coat_vertices=set();boot_vertices=set();toe_vertices=set()
    for poly in body.data.polygons:
        material=body.data.materials[poly.material_index].name
        for index in poly.vertices:
            v=body.data.vertices[index];bones=[names[g.group] for g in v.groups]
            if material=='DeepJade' and v.co.z<1.03:coat_vertices.add(index)
            if material=='Leather' and any('LowerLeg' in n for n in bones):boot_vertices.add(index)
            if material=='Copper' and any(n.endswith('Foot') for n in bones):toe_vertices.add(index)
    for v in body.data.vertices:
        bones=[names[g.group] for g in v.groups]
        if any(n.endswith('UpperArm') for n in bones) and abs(v.co.x)<.38 and v.co.z>1.51:
            v.co.z=1.51+(v.co.z-1.51)*.55
    for index in coat_vertices:
        v=body.data.vertices[index]
        v.co.y=(-1 if v.co.y<0 else 1)*(.153+.022*(1.02-v.co.z)/.35)
    for index in boot_vertices:
        v=body.data.vertices[index];center=.115 if v.co.x>0 else -.115
        v.co.x=center+(v.co.x-center)*1.12;v.co.y*=1.12
    for index in toe_vertices:
        v=body.data.vertices[index];v.co.y-=.019;v.co.z+=.022
    # Coattail piping follows the new front surface.
    for poly in body.data.polygons:
        if body.data.materials[poly.material_index].name=='Copper':
            for index in poly.vertices:
                v=body.data.vertices[index]
                if .66<v.co.z<1.025 and abs(v.co.x)>.19 and v.co.y<-.09:
                    v.co.y=-.157-.022*(1.02-v.co.z)/.35
    shoulders=[]
    for side,label in [(1,'Left'),(-1,'Right')]:
        ob=ellipsoid(label+' fitted shoulder',(side*.225,0,1.51),(.105,.104,.067),'Jade','Chest',16,8)
        upper=ob.vertex_groups.new(name=label+'UpperArm');chest=ob.vertex_groups['Chest']
        for v in ob.data.vertices:
            t=max(0,min(1,(abs(v.co.x)-.16)/.17));t=t*t*(3-2*t)
            chest.add([v.index],1-t,'REPLACE');upper.add([v.index],t,'REPLACE')
        shoulders.append(ob)
    for ob in bpy.context.selected_objects:ob.select_set(False)
    body.select_set(True)
    for ob in shoulders:ob.select_set(True)
    bpy.context.view_layer.objects.active=body;bpy.ops.object.join()
    body['silhouette_refined']=True;body.data.update()
RESULT={'vertices':len(body.data.vertices),'corrections':['shoulder continuity','coattail clearance','boot shaft clearance','toe cap fit']}
