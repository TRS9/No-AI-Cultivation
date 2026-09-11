"""Remove overlapping shoulder inserts; use the sleeve's original support rings."""
import bpy,bmesh
body=bpy.data.objects['PrisonAlchemist_Body']
if not body.get('shoulder_seam_clean'):
    # refine_silhouette appended two 16 x 9-vertex shoulder ellipsoids.
    bm=bmesh.new();bm.from_mesh(body.data);bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=list(bm.verts)[-288:],context='VERTS')
    bm.to_mesh(body.data);bm.free()
    names={g.index:g.name for g in body.vertex_groups}
    for v in body.data.vertices:
        if any(names[g.group].endswith('UpperArm') for g in v.groups) and abs(v.co.x)<.38:
            if v.co.z>1.51:v.co.z=1.51+(v.co.z-1.51)/.55
            # Keep the sleeve cap on the anatomical shoulder pivot; Chest weighting
            # here raises the rim when the arm drops, opening the armhole.
            label='Left' if v.co.x>0 else 'Right'
            body.vertex_groups['Chest'].remove([v.index])
            body.vertex_groups[label+'UpperArm'].add([v.index],1,'REPLACE')
    body.data.update();body['shoulder_seam_clean']=True
RESULT={'vertices':len(body.data.vertices),'shoulder_seam_clean':True}
