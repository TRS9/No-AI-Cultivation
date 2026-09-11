"""Match split coattails to their thigh to prevent trousers breaking through in Humanoid tuck poses."""
import bpy
body=bpy.data.objects['PrisonAlchemist_Body']
indices=set()
for p in body.data.polygons:
    material=body.data.materials[p.material_index].name
    for index in p.vertices:
        v=body.data.vertices[index]
        if material=='DeepJade' and v.co.z<1.03:indices.add(index)
        if material=='Copper' and .66<v.co.z<1.025 and abs(v.co.x)>.19 and v.co.y<-.14:indices.add(index)
for index in indices:
    v=body.data.vertices[index];label='Left' if v.co.x>0 else 'Right'
    body.vertex_groups['Hips'].remove([index])
    body.vertex_groups[label+'UpperLeg'].add([index],1,'REPLACE')
body.data.update()
RESULT={'coattail_vertices_reweighted':len(indices)}
