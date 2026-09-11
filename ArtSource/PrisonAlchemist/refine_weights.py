import bpy
body=bpy.data.objects['PrisonAlchemist_Body']
chest=body.vertex_groups['Chest'];modified=0
for vertex in body.data.vertices:
    for label in ['Left','Right']:
        group=body.vertex_groups[label+'UpperArm']
        weight=next((g.weight for g in vertex.groups if g.group==group.index),0)
        x=abs(vertex.co.x)
        if weight>.99 and x<.345:
            t=max(0,min(1,(x-.20)/.145));t=t*t*(3-2*t)
            group.add([vertex.index],t,'REPLACE');chest.add([vertex.index],1-t,'REPLACE');modified+=1
body.data.update()
RESULT={'shoulder_vertices_refined':modified,'reason':'Blend sleeve armhole into chest over three support rings; retain elbow transition loops.'}
