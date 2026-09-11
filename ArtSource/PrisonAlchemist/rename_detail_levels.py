import bpy
for old,new in [('PrisonAlchemist_LOD1','PrisonAlchemist_Medium'),('PrisonAlchemist_LOD2','PrisonAlchemist_Far')]:
 ob=bpy.data.objects.get(old)
 if ob:ob.name=new
RESULT={'renamed':True}
